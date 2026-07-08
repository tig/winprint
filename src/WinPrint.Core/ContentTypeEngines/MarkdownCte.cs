// Copyright Kindel, LLC - http://www.kindel.com
// Published under the MIT License at https://github.com/tig/winprint

using System.Globalization;
using System.Net;
using System.Net.Http;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;
using Markdig;
using Markdig.Extensions.Tables;
using Markdig.Syntax;
using Markdig.Syntax.Inlines;
using Serilog;
using WinPrint.Core.Abstractions;
using WinPrint.Core.Models;
using WinPrint.Core.Services;

namespace WinPrint.Core.ContentTypeEngines;

/// <summary>
///     Implements text/x-markdown by rendering the Markdig AST richly through the
///     <see cref="IGraphicsContext" /> pipeline: headings (scaled + bold), bold/italic, inline code,
///     bulleted/numbered/nested lists, blockquotes (indented with a bar), fenced/indented code blocks
///     (monospace on a shaded background), horizontal rules, and links (colored). Reflow word-wraps
///     styled runs into variable-height <see cref="MarkdownLine" />s and paginates by cumulative height.
///     Images that sit alone in a paragraph (<c>![alt](src)</c> or a standalone HTML <c>&lt;img&gt;</c>)
///     are decoded and drawn through <see cref="IGraphicsContext.DrawImage" />, scaled to fit the page;
///     GIFs render their first frame. Local files (resolved relative to
///     <see cref="ContentTypeEngineBase.SourceFileName" />), <c>data:</c> URIs, and <c>http(s)</c>
///     URLs are supported. Anything that fails to load (and inline images mixed with text) falls back
///     to alt text. When <see cref="RenderMermaidDiagrams" /> is enabled (the default),
///     <c>```mermaid</c> fences are rendered to images via <see cref="MermaidRenderer" /> (by default
///     the in-process <see cref="MermaiderRenderer" />); set <see cref="MermaidBackend" /> to
///     <c>service</c> for the remote mermaid.ink-compatible service at
///     <see cref="MermaidServiceUrl" />. A diagram that fails to render falls back to a plain code
///     block.
/// </summary>
public class MarkdownCte : ContentTypeEngineBase
{
    private static readonly string[] s_supportedContentTypes = ["text/x-markdown"];

    private static readonly MarkdownPipeline s_pipeline =
        new MarkdownPipelineBuilder().UseAdvancedExtensions().Build();

    private static readonly HttpClient s_http = new() { Timeout = TimeSpan.FromSeconds(10) };

    private static readonly Regex s_htmlImgTagRegex = new(
        @"<img\b(?<attrs>[^>]*)/?>",
        RegexOptions.IgnoreCase | RegexOptions.Compiled | RegexOptions.CultureInvariant);

    private static readonly Regex s_htmlImgAttrRegex = new(
        @"(?<name>src|alt)\s*=\s*(?:""(?<dq>[^""]*)""|'(?<sq>[^']*)'|(?<uq>[^\s""'/>]+))",
        RegexOptions.IgnoreCase | RegexOptions.Compiled | RegexOptions.CultureInvariant);

    private static readonly Regex s_htmlCommentRegex = new(
        @"<!--.*?-->",
        RegexOptions.Singleline | RegexOptions.Compiled | RegexOptions.CultureInvariant);

    /// <summary>
    ///     Decoded image bytes keyed by source URL/path; null marks a load that failed (don't retry).
    ///     This is the render-side BUILD cache: <see cref="RenderAsync" /> replaces it with a fresh
    ///     instance at the start of every render and publishes it to <see cref="_paintCache" /> only on
    ///     completion, so a paint racing a re-render (the TUI repaints while the mermaid/image preloads
    ///     await the network) never observes a half-built cache.
    /// </summary>
    private Dictionary<string, byte[]?> _imageCache = new(StringComparer.Ordinal);

    /// <summary>The last completed render's image cache — the only cache <see cref="PaintImage" /> reads.</summary>
    private Dictionary<string, byte[]?> _paintCache = new(StringComparer.Ordinal);

    /// <summary>Prefix distinguishing rendered mermaid diagrams from image URLs in <see cref="_imageCache" />.</summary>
    private const string MermaidCachePrefix = "mermaid:";

    private static readonly GraphicsColor TextColor = GraphicsColor.FromRgb(0x1d, 0x1d, 0x1f);
    private static readonly GraphicsColor LinkColor = GraphicsColor.FromRgb(0x0b, 0x57, 0xd0);
    private static readonly GraphicsColor CodeColor = GraphicsColor.FromRgb(0x37, 0x37, 0x37);
    private static readonly GraphicsColor InlineCodeColor = GraphicsColor.FromRgb(0x9a, 0x34, 0x4f);
    private static readonly GraphicsColor QuoteColor = GraphicsColor.FromRgb(0x5a, 0x5a, 0x5a);
    private static readonly GraphicsColor CodeBgColor = GraphicsColor.FromRgb(0xf3, 0xf3, 0xf5);
    private static readonly GraphicsColor QuoteBarColor = GraphicsColor.FromRgb(0xcf, 0xd3, 0xda);
    private static readonly GraphicsColor RuleColor = GraphicsColor.FromRgb(0xc7, 0xcc, 0xd6);
    private static readonly GraphicsColor TableHeaderColor = GraphicsColor.FromRgb(0xee, 0xf1, 0xf6);

    /// <summary>
    ///     Render-side BUILD list (see <see cref="_imageCache" /> for the build/paint split); published
    ///     to <see cref="_paintLines" /> only when a render completes.
    /// </summary>
    private List<MarkdownLine> _lines = [];

    /// <summary>The last completed render's lines — the only list <see cref="PaintPage" /> enumerates.</summary>
    private List<MarkdownLine> _paintLines = [];

    private float _baseLineHeight;
    private float _baseSizePx;
    private int _dpiY = 96;
    private float _indentStep;
    private int _pageCount;

    public override string[] SupportedContentTypes => s_supportedContentTypes;

    /// <summary>
    ///     When true (the default), each <c>```mermaid</c> fence is rendered to an image via
    ///     <see cref="MermaidRenderer" />; when false, fences render as plain code blocks. On by
    ///     default because the default backend (<see cref="MermaiderRenderer" />) renders entirely
    ///     in-process; nothing is sent anywhere unless <see cref="MermaidBackend" /> is switched to
    ///     the remote service.
    /// </summary>
    public bool RenderMermaidDiagrams { get; set; } = true;

    /// <summary>
    ///     Selects the diagram backend when no <see cref="MermaidRenderer" /> is injected:
    ///     <c>builtin</c> (the default) renders in-process via <see cref="MermaiderRenderer" />;
    ///     <c>service</c> renders via the remote mermaid.ink-compatible service at
    ///     <see cref="MermaidServiceUrl" />, which sends the diagram source to that service (the same
    ///     consideration as <see cref="HtmlCte.AllowRemoteResources" />, so it is opt-in). The remote
    ///     service supports more diagram types than the builtin renderer; builtin failures fall back
    ///     to a code block, never silently to the network.
    /// </summary>
    public string MermaidBackend { get; set; } = "builtin";

    /// <summary>Base URL of the mermaid.ink-compatible service used when <see cref="MermaidBackend" /> is <c>service</c>.</summary>
    public string MermaidServiceUrl { get; set; } = "https://mermaid.ink";

    /// <summary>Diagram renderer override (tests / alternate backends); null uses <see cref="MermaidInkRenderer" />.</summary>
    [JsonIgnore]
    public IMermaidRenderer? MermaidRenderer { get; set; }

    public override void CopyPropertiesFrom(ModelBase? source)
    {
        base.CopyPropertiesFrom(source);
        if (source is MarkdownCte src)
        {
            RenderMermaidDiagrams = src.RenderMermaidDiagrams;
            MermaidBackend = src.MermaidBackend;
            MermaidServiceUrl = src.MermaidServiceUrl;
            MermaidRenderer = src.MermaidRenderer;
        }
    }

    public override async Task<bool> SetDocumentAsync(string doc)
    {
        Document = doc ?? string.Empty;
        return await Task.FromResult(true);
    }

    public override async Task<int> RenderAsync(PrintResolution? printerResolution,
        EventHandler<string>? reflowProgress)
    {
        LogService.TraceMessage();
        if (Document is null)
        {
            throw new InvalidOperationException("Document can't be null for RenderAsync");
        }

        ArgumentNullException.ThrowIfNull(printerResolution);
        int dpiX = printerResolution.X;
        int dpiY = printerResolution.Y;
        if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows) || dpiX < 0 || dpiY < 0)
        {
            dpiX = dpiY = 96;
        }

        _dpiY = dpiY;
        // Fresh build instances: the previous render's lines/cache stay published (and safe to
        // paint) until this render completes — never mutate what PaintPage may be enumerating.
        _lines = [];
        _imageCache = new Dictionary<string, byte[]?>(StringComparer.Ordinal);

        IGraphicsContext g = ResolveMeasurementContext(dpiX, dpiY, out IDisposable? owner);
        var fontCache = new Dictionary<string, IGraphicsFont>();
        try
        {
            g.SetTextRenderingMode(GraphicsTextRenderingMode);
            _baseSizePx = ContentSettings!.Font.Size / 72F * 96F;
            _baseLineHeight = GetFont(g, fontCache, 1f, GraphicsFontStyle.Regular).GetHeight(dpiY);
            _indentStep = _baseSizePx * 1.6f;

            if (PageSize.Height < _baseLineHeight)
            {
                throw new InvalidOperationException(
                    $"Line height ({_baseLineHeight:F2}) exceeds page height ({PageSize.Height:F2}).");
            }

            MarkdownDocument ast = Markdown.Parse(Document, s_pipeline);
            await PreloadImagesAsync(ast);
            await PreloadMermaidDiagramsAsync(ast);
            WalkBlocks(ast, g, fontCache, 0, 0);

            _pageCount = Paginate();
            // Publish for painting only now that the build is complete.
            _paintLines = _lines;
            _paintCache = _imageCache;
            Log.Debug("Rendered {pages} Markdown pages from {lines} lines.", _pageCount, _lines.Count);
            return await Task.FromResult(_pageCount);
        }
        finally
        {
            foreach (IGraphicsFont f in fontCache.Values)
            {
                f.Dispose();
            }

            owner?.Dispose();
        }
    }

    public override void PaintPage(IGraphicsContext g, int pageNum)
    {
        LogService.TraceMessage($"{pageNum}");
        // Snapshot the published render: a concurrent re-render swaps _paintLines/_paintCache as
        // complete units, so painting from locals can never see a half-built collection.
        List<MarkdownLine> lines = _paintLines;
        if (lines.Count == 0)
        {
            return;
        }

        g.SetTextRenderingMode(GraphicsTextRenderingMode);
        GraphicsFontUnit unit = g.IsDisplayUnit ? GraphicsFontUnit.Point : GraphicsFontUnit.Pixel;
        float basePt = g.IsDisplayUnit ? ContentSettings!.Font.Size : ContentSettings!.Font.Size / 72F * 96F;

        using IGraphicsBrush codeBg = g.CreateSolidBrush(CodeBgColor);
        using IGraphicsBrush quoteBar = g.CreateSolidBrush(QuoteBarColor);
        using IGraphicsBrush headerBg = g.CreateSolidBrush(TableHeaderColor);
        using IGraphicsPen rulePen = g.CreatePen(RuleColor, 2f);
        using IGraphicsPen gridPen = g.CreatePen(RuleColor);

        foreach (MarkdownLine line in lines)
        {
            if (line.Page != pageNum)
            {
                continue;
            }

            if (line.CodeBackground)
            {
                g.FillRectangle(codeBg, line.Indent - _indentStep * 0.3f, line.Y,
                    PageSize.Width - (line.Indent - _indentStep * 0.3f), line.Height);
            }

            if (line.QuoteBar)
            {
                g.FillRectangle(quoteBar, line.Indent - _indentStep * 0.6f, line.Y, _baseSizePx * 0.22f, line.Height);
            }

            // After block decoration (so an image inside a blockquote still gets its gutter bar).
            if (line.Image is { } image)
            {
                PaintImage(g, line, image);
                continue;
            }

            if (line.Rule)
            {
                float midY = line.Y + line.Height / 2f;
                g.DrawLine(rulePen, line.Indent, midY, PageSize.Width, midY);
                continue;
            }

            if (line.ColumnEdges is { Count: > 1 } edges)
            {
                if (line.HeaderShade)
                {
                    g.FillRectangle(headerBg, edges[0], line.Y, edges[^1] - edges[0], line.Height);
                }

                foreach (float ex in edges)
                {
                    g.DrawLine(gridPen, ex, line.Y, ex, line.Y + line.Height);
                }

                if (line.TableRowTop)
                {
                    g.DrawLine(gridPen, edges[0], line.Y, edges[^1], line.Y);
                }

                if (line.TableRowBottom)
                {
                    g.DrawLine(gridPen, edges[0], line.Y + line.Height, edges[^1], line.Y + line.Height);
                }
            }

            float x = line.Indent;
            foreach (MarkdownRun run in line.Runs)
            {
                if (run.Text.Length == 0)
                {
                    continue;
                }

                using IGraphicsFont font = g.CreateFont(ContentSettings.Font.Family, basePt * run.Scale, run.Style,
                    unit);
                using IGraphicsBrush brush = g.CreateSolidBrush(run.Color);
                float rx = run.X ?? x;
                g.DrawString(run.Text, font, brush, rx, line.Y, GraphicsStringFormat);
                x = rx + Measure(g, run.Text, font).Width;
            }
        }
    }

    private void PaintImage(IGraphicsContext g, MarkdownLine line, MarkdownImage image)
    {
        if (_paintCache.TryGetValue(image.CacheKey, out byte[]? bytes) && bytes is { Length: > 0 })
        {
            using var ms = new MemoryStream(bytes);
            using IGraphicsImage? decoded = g.LoadImage(ms);
            if (decoded is not null)
            {
                g.DrawImage(decoded, line.Indent, line.Y, image.Width, image.Height);
                return;
            }
        }

        // The image decoded during reflow but not on this paint context (e.g. a backend format
        // mismatch). Fall back to alt text so the page never shows a blank hole.
        GraphicsFontUnit unit = g.IsDisplayUnit ? GraphicsFontUnit.Point : GraphicsFontUnit.Pixel;
        float basePt = g.IsDisplayUnit ? ContentSettings!.Font.Size : ContentSettings!.Font.Size / 72F * 96F;
        using IGraphicsFont font = g.CreateFont(ContentSettings.Font.Family, basePt, GraphicsFontStyle.Italic, unit);
        using IGraphicsBrush brush = g.CreateSolidBrush(QuoteColor);
        g.DrawString($"🖼 {image.AltText}", font, brush, line.Indent, line.Y, GraphicsStringFormat);
    }

    // ---- AST walk ---------------------------------------------------------------------------------

    private void WalkBlocks(IEnumerable<Block> blocks, IGraphicsContext g, Dictionary<string, IGraphicsFont> fonts,
        int indentLevel, int quoteDepth)
    {
        foreach (Block block in blocks)
        {
            switch (block)
            {
                case HeadingBlock h:
                    EmitInline(h.Inline, g, fonts, HeadingScale(h.Level), GraphicsFontStyle.Bold,
                        quoteDepth > 0 ? QuoteColor : TextColor, indentLevel, quoteDepth,
                        _baseLineHeight * (h.Level <= 2 ? 1.0f : 0.7f));
                    break;
                case ParagraphBlock p when GetStandaloneHtmlImages(p) is { } htmlImages:
                    foreach ((string src, string alt) in htmlImages)
                    {
                        EmitImage(src, alt, g, fonts, indentLevel, quoteDepth);
                    }

                    break;
                case ParagraphBlock p when GetStandaloneImages(p) is { } images:
                    foreach (LinkInline image in images)
                    {
                        EmitImage(image.Url ?? string.Empty, GetInlineText(image), g, fonts, indentLevel, quoteDepth);
                    }

                    break;
                case HtmlBlock html when !IsHtmlComment(html):
                    foreach ((string src, string alt) in ParseHtmlImages(html))
                    {
                        EmitImage(src, alt, g, fonts, indentLevel, quoteDepth);
                    }

                    break;
                case ParagraphBlock p:
                    EmitInline(p.Inline, g, fonts, 1f, GraphicsFontStyle.Regular,
                        quoteDepth > 0 ? QuoteColor : TextColor, indentLevel, quoteDepth, _baseLineHeight * 0.5f);
                    break;
                case ListBlock list:
                    EmitList(list, g, fonts, indentLevel, quoteDepth);
                    break;
                case QuoteBlock quote:
                    WalkBlocks(quote, g, fonts, indentLevel, quoteDepth + 1);
                    break;
                case FencedCodeBlock fence when TryGetRenderedMermaid(fence, out string mermaidKey):
                    EmitImage(mermaidKey, "mermaid diagram", g, fonts, indentLevel, quoteDepth);
                    break;
                case CodeBlock code: // covers FencedCodeBlock too (incl. unrendered mermaid fences)
                    EmitCodeBlock(code, g, fonts, indentLevel, quoteDepth);
                    break;
                case ThematicBreakBlock:
                    EmitRule(indentLevel, quoteDepth);
                    break;
                case Table table:
                    EmitTable(table, g, fonts, indentLevel, quoteDepth);
                    break;
                case ContainerBlock container:
                    WalkBlocks(container, g, fonts, indentLevel, quoteDepth);
                    break;
                case LeafBlock leaf when leaf.Inline is not null:
                    EmitInline(leaf.Inline, g, fonts, 1f, GraphicsFontStyle.Regular, TextColor, indentLevel,
                        quoteDepth, _baseLineHeight * 0.5f);
                    break;
            }
        }
    }

    private void EmitList(ListBlock list, IGraphicsContext g, Dictionary<string, IGraphicsFont> fonts,
        int indentLevel, int quoteDepth)
    {
        int ordinal = TryParseStart(list.OrderedStart, 1);
        foreach (Block child in list)
        {
            if (child is not ListItemBlock item)
            {
                continue;
            }

            string marker = list.IsOrdered
                ? $"{ordinal.ToString(CultureInfo.InvariantCulture)}."
                : "•";
            ordinal++;
            EmitListItem(item, marker, g, fonts, indentLevel, quoteDepth);
        }
    }

    private void EmitListItem(ListItemBlock item, string marker, IGraphicsContext g,
        Dictionary<string, IGraphicsFont> fonts, int indentLevel, int quoteDepth)
    {
        List<Block> children = [.. item];
        ContainerInline? firstInline = children.Count > 0 && children[0] is LeafBlock { Inline: { } li } ? li : null;

        IGraphicsFont markerFont = GetFont(g, fonts, 1f, GraphicsFontStyle.Regular);
        var tokens = new List<MarkdownRun>();
        AppendText($"{marker} ", 1f, GraphicsFontStyle.Regular, quoteDepth > 0 ? QuoteColor : TextColor, tokens);
        float markerWidth = Measure(g, $"{marker} ", markerFont).Width;
        if (firstInline is not null)
        {
            FlattenInlines(firstInline, 1f, GraphicsFontStyle.Regular, quoteDepth > 0 ? QuoteColor : TextColor, tokens);
        }

        float quoteGutter = quoteDepth * _indentStep;
        float markerIndent = quoteGutter + indentLevel * _indentStep + _indentStep * 0.4f;
        List<MarkdownLine> lines = WrapTokens(tokens, g, fonts, markerIndent, markerIndent + markerWidth,
            PageSize.Width);
        Decorate(lines, _baseLineHeight * 0.18f, quoteDepth > 0);
        _lines.AddRange(lines);

        // Nested blocks (sub-lists, extra paragraphs) render one level deeper.
        for (int i = firstInline is null ? 0 : 1; i < children.Count; i++)
        {
            WalkBlocks([children[i]], g, fonts, indentLevel + 1, quoteDepth);
        }
    }

    private void EmitCodeBlock(CodeBlock code, IGraphicsContext g,
        Dictionary<string, IGraphicsFont> fonts, int indentLevel, int quoteDepth)
    {
        float indent = quoteDepth * _indentStep + indentLevel * _indentStep + _indentStep * 0.4f;
        IGraphicsFont font = GetFont(g, fonts, 0.92f, GraphicsFontStyle.Regular);
        float height = font.GetHeight(_dpiY);
        float charWidth = Math.Max(1f, Measure(g, "M", font).Width);
        int maxChars = Math.Max(8, (int)Math.Floor((PageSize.Width - indent) / charWidth));

        string tab = new(' ', Math.Max(0, ContentSettings!.TabSpaces));
        bool first = true;
        for (int n = 0; n < code.Lines.Count; n++)
        {
            string raw = code.Lines.Lines[n].Slice.ToString().Replace("\t", tab);
            // char-wrap long code lines so they don't clip
            for (int start = 0; start == 0 || start < raw.Length; start += maxChars)
            {
                string seg = raw.Length == 0
                    ? string.Empty
                    : raw.Substring(start, Math.Min(maxChars, raw.Length - start));
                var line = new MarkdownLine
                {
                    Indent = indent,
                    Height = height,
                    CodeBackground = true,
                    QuoteBar = quoteDepth > 0,
                    SpaceBefore = first ? _baseLineHeight * 0.5f : 0f
                };
                if (seg.Length > 0)
                {
                    line.Runs.Add(new MarkdownRun { Text = seg, Scale = 0.92f, Color = CodeColor });
                }

                _lines.Add(line);
                first = false;
                if (raw.Length == 0)
                {
                    break;
                }
            }
        }
    }

    private void EmitRule(int indentLevel, int quoteDepth)
    {
        float indent = quoteDepth * _indentStep + indentLevel * _indentStep;
        _lines.Add(new MarkdownLine
        {
            Indent = indent,
            Height = _baseLineHeight,
            Rule = true,
            QuoteBar = quoteDepth > 0,
            SpaceBefore = _baseLineHeight * 0.5f
        });
    }

    private void EmitImage(string url, string alt, IGraphicsContext g, Dictionary<string, IGraphicsFont> fonts,
        int indentLevel, int quoteDepth)
    {
        float indent = quoteDepth * _indentStep + indentLevel * _indentStep;
        byte[]? bytes = url.Length > 0 && _imageCache.TryGetValue(url, out byte[]? cached) ? cached : null;

        IGraphicsImage? probe = null;
        if (bytes is { Length: > 0 })
        {
            using var ms = new MemoryStream(bytes);
            probe = g.LoadImage(ms);
        }

        if (probe is null)
        {
            EmitImageAltFallback(alt, g, fonts, indent, quoteDepth);
            return;
        }

        using (probe)
        {
            float maxWidth = Math.Max(1f, PageSize.Width - indent);
            float maxHeight = Math.Max(1f, PageSize.Height - _baseLineHeight * 0.5f);
            float drawWidth = probe.Width;
            float drawHeight = probe.Height;
            if (drawWidth <= 0 || drawHeight <= 0)
            {
                EmitImageAltFallback(alt, g, fonts, indent, quoteDepth);
                return;
            }

            if (drawWidth > maxWidth)
            {
                float s = maxWidth / drawWidth;
                drawWidth *= s;
                drawHeight *= s;
            }

            if (drawHeight > maxHeight)
            {
                float s = maxHeight / drawHeight;
                drawWidth *= s;
                drawHeight *= s;
            }

            _lines.Add(new MarkdownLine
            {
                Indent = indent,
                Height = drawHeight,
                SpaceBefore = _baseLineHeight * 0.5f,
                QuoteBar = quoteDepth > 0,
                Image = new MarkdownImage { CacheKey = url, AltText = alt, Width = drawWidth, Height = drawHeight }
            });
        }
    }

    private void EmitImageAltFallback(string alt, IGraphicsContext g, Dictionary<string, IGraphicsFont> fonts,
        float indent, int quoteDepth)
    {
        var tokens = new List<MarkdownRun>();
        AppendText($"🖼 {alt}", 1f, GraphicsFontStyle.Italic, QuoteColor, tokens);
        List<MarkdownLine> lines = WrapTokens(tokens, g, fonts, indent, indent, PageSize.Width);
        Decorate(lines, _baseLineHeight * 0.5f, quoteDepth > 0);
        _lines.AddRange(lines);
    }

    private void EmitTable(Table table, IGraphicsContext g, Dictionary<string, IGraphicsFont> fonts,
        int indentLevel, int quoteDepth)
    {
        // Flatten every cell's inline content into tokens; track header rows and the column count.
        var rows = new List<(bool Header, List<List<MarkdownRun>> Cells)>();
        int cols = 0;
        foreach (Block rowBlock in table)
        {
            if (rowBlock is not TableRow row)
            {
                continue;
            }

            var cells = new List<List<MarkdownRun>>();
            GraphicsFontStyle style = row.IsHeader ? GraphicsFontStyle.Bold : GraphicsFontStyle.Regular;
            GraphicsColor color = quoteDepth > 0 ? QuoteColor : TextColor;
            foreach (Block cellBlock in row)
            {
                if (cellBlock is not TableCell cell)
                {
                    continue;
                }

                var toks = new List<MarkdownRun>();
                foreach (Block b in cell)
                {
                    if (b is LeafBlock { Inline: { } cin })
                    {
                        FlattenInlines(cin, 1f, style, color, toks);
                    }
                }

                cells.Add(toks);
            }

            cols = Math.Max(cols, cells.Count);
            rows.Add((row.IsHeader, cells));
        }

        if (cols == 0)
        {
            return;
        }

        float indent = quoteDepth * _indentStep + indentLevel * _indentStep;
        float avail = PageSize.Width - indent;
        float pad = _baseSizePx * 0.4f;

        // Column widths: natural content width per column, scaled to fit the available width.
        float[] widths = new float[cols];
        foreach ((bool _, List<List<MarkdownRun>> cells) in rows)
        {
            for (int c = 0; c < cells.Count; c++)
            {
                widths[c] = Math.Max(widths[c], MeasureTokens(g, fonts, cells[c]) + 2 * pad);
            }
        }

        float total = widths.Sum();
        if (total > avail && total > 0)
        {
            for (int c = 0; c < cols; c++)
            {
                widths[c] *= avail / total;
            }
        }

        float[] edges = new float[cols + 1];
        edges[0] = indent;
        for (int c = 0; c < cols; c++)
        {
            edges[c + 1] = edges[c] + widths[c];
        }

        IReadOnlyList<float> edgeList = edges;

        for (int r = 0; r < rows.Count; r++)
        {
            (bool header, List<List<MarkdownRun>> cells) = rows[r];
            var cellLines = new List<List<MarkdownLine>>();
            int visual = 1;
            for (int c = 0; c < cols; c++)
            {
                List<MarkdownRun> toks = c < cells.Count ? cells[c] : [];
                List<MarkdownLine> wrapped = WrapTokens(toks, g, fonts, 0f, 0f, Math.Max(1f, widths[c] - 2 * pad));
                cellLines.Add(wrapped);
                visual = Math.Max(visual, wrapped.Count);
            }

            for (int k = 0; k < visual; k++)
            {
                var rowLine = new MarkdownLine
                {
                    Indent = indent,
                    Height = _baseLineHeight,
                    ColumnEdges = edgeList,
                    TableRowTop = k == 0,
                    TableRowBottom = r == rows.Count - 1 && k == visual - 1,
                    HeaderShade = header,
                    QuoteBar = quoteDepth > 0,
                    SpaceBefore = r == 0 && k == 0 ? _baseLineHeight * 0.5f : 0f
                };

                for (int c = 0; c < cols; c++)
                {
                    if (k >= cellLines[c].Count)
                    {
                        continue;
                    }

                    bool firstInCell = true;
                    foreach (MarkdownRun run in cellLines[c][k].Runs)
                    {
                        if (firstInCell)
                        {
                            run.X = edges[c] + pad;
                            firstInCell = false;
                        }

                        rowLine.Runs.Add(run);
                    }
                }

                _lines.Add(rowLine);
            }
        }
    }

    private float MeasureTokens(IGraphicsContext g, Dictionary<string, IGraphicsFont> fonts, List<MarkdownRun> tokens)
    {
        float w = 0f;
        foreach (MarkdownRun t in tokens)
        {
            if (t.Text == "\n")
            {
                continue;
            }

            w += Measure(g, t.Text, GetFont(g, fonts, t.Scale, t.Style)).Width;
        }

        return w;
    }

    // ---- images -----------------------------------------------------------------------------------

    /// <summary>
    ///     Returns the image inlines of a paragraph that contains only images and whitespace (so it can
    ///     be rendered as block images), or null when the paragraph mixes images with other content.
    /// </summary>
    private static List<LinkInline>? GetStandaloneImages(ParagraphBlock paragraph)
    {
        if (paragraph.Inline is null)
        {
            return null;
        }

        var images = new List<LinkInline>();
        foreach (Inline node in paragraph.Inline)
        {
            switch (node)
            {
                case LinkInline { IsImage: true } image:
                    images.Add(image);
                    break;
                case LiteralInline lit when string.IsNullOrWhiteSpace(lit.Content.ToString()):
                case LineBreakInline:
                    break;
                default:
                    return null;
            }
        }

        return images.Count > 0 ? images : null;
    }

    /// <summary>
    ///     Returns <c>&lt;img&gt;</c> tags from a paragraph that contains only HTML images and whitespace.
    /// </summary>
    private static List<(string Src, string Alt)>? GetStandaloneHtmlImages(ParagraphBlock paragraph)
    {
        if (paragraph.Inline is null)
        {
            return null;
        }

        var images = new List<(string Src, string Alt)>();
        foreach (Inline node in paragraph.Inline)
        {
            switch (node)
            {
                case HtmlInline html:
                    foreach ((string src, string alt) in ParseHtmlImgTags(html.Tag))
                    {
                        images.Add((src, alt));
                    }

                    break;
                case LiteralInline lit when string.IsNullOrWhiteSpace(lit.Content.ToString()):
                case LineBreakInline:
                    break;
                default:
                    return null;
            }
        }

        return images.Count > 0 ? images : null;
    }

    private static IEnumerable<(string Src, string Alt)> ParseHtmlImages(HtmlBlock block)
    {
        if (IsHtmlComment(block))
        {
            yield break;
        }

        foreach ((string src, string alt) in ParseHtmlImgTags(block.Lines.ToString()))
        {
            yield return (src, alt);
        }
    }

    private static bool IsHtmlComment(HtmlBlock block)
    {
        string text = block.Lines.ToString().Trim();
        return text.StartsWith("<!--", StringComparison.Ordinal)
               && text.EndsWith("-->", StringComparison.Ordinal);
    }

    private static string StripHtmlComments(string html)
    {
        return s_htmlCommentRegex.Replace(html, string.Empty);
    }

    private static IEnumerable<(string Src, string Alt)> ParseHtmlImgTags(string html)
    {
        if (string.IsNullOrWhiteSpace(html))
        {
            yield break;
        }

        foreach (Match tag in s_htmlImgTagRegex.Matches(StripHtmlComments(html)))
        {
            string attrs = tag.Groups["attrs"].Value;
            string? src = null;
            string alt = string.Empty;
            foreach (Match attr in s_htmlImgAttrRegex.Matches(attrs))
            {
                string name = attr.Groups["name"].Value;
                string value = WebUtility.HtmlDecode(attr.Groups["dq"].Success ? attr.Groups["dq"].Value
                    : attr.Groups["sq"].Success ? attr.Groups["sq"].Value
                    : attr.Groups["uq"].Value);
                if (name.Equals("src", StringComparison.OrdinalIgnoreCase))
                {
                    src = value;
                }
                else if (name.Equals("alt", StringComparison.OrdinalIgnoreCase))
                {
                    alt = value;
                }
            }

            if (!string.IsNullOrWhiteSpace(src))
            {
                yield return (src, alt);
            }
        }
    }

    private async Task PreloadImagesAsync(MarkdownDocument ast)
    {
        foreach (ParagraphBlock paragraph in ast.Descendants<ParagraphBlock>())
        {
            if (GetStandaloneImages(paragraph) is { } images)
            {
                foreach (LinkInline image in images)
                {
                    await PreloadImageUrlAsync(image.Url ?? string.Empty);
                }
            }

            if (GetStandaloneHtmlImages(paragraph) is { } htmlImages)
            {
                foreach ((string src, _) in htmlImages)
                {
                    await PreloadImageUrlAsync(src);
                }
            }
        }

        foreach (HtmlBlock html in ast.Descendants<HtmlBlock>())
        {
            if (IsHtmlComment(html))
            {
                continue;
            }

            foreach ((string src, _) in ParseHtmlImages(html))
            {
                await PreloadImageUrlAsync(src);
            }
        }
    }

    // ---- mermaid ----------------------------------------------------------------------------------

    /// <summary>
    ///     Renders every <c>```mermaid</c> fence to image bytes (via <see cref="MermaidRenderer" /> or
    ///     the <see cref="MermaidBackend" />-selected default) into <see cref="_imageCache" />, keyed
    ///     by <see cref="MermaidCachePrefix" /> + source so identical diagrams render once. No-op
    ///     unless <see cref="RenderMermaidDiagrams" /> is enabled; a null result marks a failed render
    ///     so the fence falls back to a plain code block.
    /// </summary>
    private async Task PreloadMermaidDiagramsAsync(MarkdownDocument ast)
    {
        if (!RenderMermaidDiagrams)
        {
            return;
        }

        IMermaidRenderer? renderer = null;
        foreach (FencedCodeBlock fence in ast.Descendants<FencedCodeBlock>())
        {
            if (!IsMermaidFence(fence))
            {
                continue;
            }

            string diagram = GetFenceText(fence);
            string key = MermaidCachePrefix + diagram;
            if (diagram.Length == 0 || _imageCache.ContainsKey(key))
            {
                continue;
            }

            renderer ??= ResolveMermaidRenderer();
            _imageCache[key] = await renderer.RenderAsync(diagram);
        }
    }

    /// <summary>
    ///     The renderer the preload uses: an injected <see cref="MermaidRenderer" /> wins; otherwise
    ///     <see cref="MermaidBackend" /> picks the in-process <see cref="MermaiderRenderer" />
    ///     (<c>builtin</c>, the default) or the remote <see cref="MermaidInkRenderer" /> (<c>service</c>).
    ///     Unrecognized backend values fall back to builtin so a config typo never causes network I/O.
    /// </summary>
    internal IMermaidRenderer ResolveMermaidRenderer()
    {
        if (MermaidRenderer is not null)
        {
            return MermaidRenderer;
        }

        return string.Equals(MermaidBackend, "service", StringComparison.OrdinalIgnoreCase)
            ? new MermaidInkRenderer(MermaidServiceUrl)
            : new MermaiderRenderer();
    }

    /// <summary>
    ///     True when <paramref name="fence" /> is a mermaid fence whose diagram rendered successfully
    ///     during reflow; <paramref name="cacheKey" /> then locates the image bytes in the cache.
    /// </summary>
    private bool TryGetRenderedMermaid(FencedCodeBlock fence, out string cacheKey)
    {
        cacheKey = string.Empty;
        if (!IsMermaidFence(fence))
        {
            return false;
        }

        string key = MermaidCachePrefix + GetFenceText(fence);
        if (_imageCache.TryGetValue(key, out byte[]? bytes) && bytes is { Length: > 0 })
        {
            cacheKey = key;
            return true;
        }

        return false;
    }

    /// <summary>Matches the fence's language (the first word of its info string) against "mermaid".</summary>
    private static bool IsMermaidFence(FencedCodeBlock fence)
    {
        ReadOnlySpan<char> info = fence.Info is null ? [] : fence.Info.AsSpan().Trim();
        int end = info.IndexOfAny(' ', '\t');
        ReadOnlySpan<char> language = end < 0 ? info : info[..end];
        return language.Equals("mermaid", StringComparison.OrdinalIgnoreCase);
    }

    private static string GetFenceText(CodeBlock code)
    {
        var sb = new StringBuilder();
        for (int n = 0; n < code.Lines.Count; n++)
        {
            if (n > 0)
            {
                sb.Append('\n');
            }

            sb.Append(code.Lines.Lines[n].Slice.ToString());
        }

        return sb.ToString();
    }

    private async Task PreloadImageUrlAsync(string url)
    {
        if (url.Length == 0 || _imageCache.ContainsKey(url))
        {
            return;
        }

        _imageCache[url] = await LoadImageBytesAsync(url);
    }

    private async Task<byte[]?> LoadImageBytesAsync(string url)
    {
        if (string.IsNullOrWhiteSpace(url))
        {
            return null;
        }

        byte[]? dataUri = TryDecodeDataUri(url);
        if (dataUri is not null)
        {
            return dataUri;
        }

        if (url.StartsWith("http://", StringComparison.OrdinalIgnoreCase) ||
            url.StartsWith("https://", StringComparison.OrdinalIgnoreCase))
        {
            try
            {
                return await s_http.GetByteArrayAsync(url);
            }
            catch (Exception ex)
            {
                Log.Debug(ex, "Markdown: failed to fetch image {url}", url);
                return null;
            }
        }

        try
        {
            string path = ResolveLocalPath(url);
            return File.Exists(path) ? await File.ReadAllBytesAsync(path) : null;
        }
        catch (Exception ex)
        {
            Log.Debug(ex, "Markdown: failed to read image {url}", url);
            return null;
        }
    }

    private string ResolveLocalPath(string url)
    {
        string path = url;
        if (url.StartsWith("file://", StringComparison.OrdinalIgnoreCase) &&
            Uri.TryCreate(url, UriKind.Absolute, out Uri? fileUri))
        {
            return fileUri.LocalPath;
        }

        path = Uri.UnescapeDataString(path);
        if (Path.IsPathRooted(path))
        {
            return path;
        }

        string? dir = string.IsNullOrEmpty(SourceFileName) ? null : Path.GetDirectoryName(SourceFileName);
        return string.IsNullOrEmpty(dir) ? path : Path.Combine(dir, path);
    }

    private static byte[]? TryDecodeDataUri(string url)
    {
        if (!url.StartsWith("data:", StringComparison.OrdinalIgnoreCase))
        {
            return null;
        }

        int comma = url.IndexOf(',');
        if (comma < 0)
        {
            return null;
        }

        string meta = url[5..comma];
        string data = url[(comma + 1)..];
        try
        {
            return meta.Contains("base64", StringComparison.OrdinalIgnoreCase)
                ? Convert.FromBase64String(data)
                : Encoding.UTF8.GetBytes(Uri.UnescapeDataString(data));
        }
        catch (Exception)
        {
            return null;
        }
    }

    private void EmitInline(ContainerInline? inline, IGraphicsContext g, Dictionary<string, IGraphicsFont> fonts,
        float scale, GraphicsFontStyle style, GraphicsColor color, int indentLevel, int quoteDepth, float spaceBefore)
    {
        var tokens = new List<MarkdownRun>();
        FlattenInlines(inline, scale, style, color, tokens);
        if (tokens.Count == 0)
        {
            return;
        }

        float indent = quoteDepth * _indentStep + indentLevel * _indentStep;
        List<MarkdownLine> lines = WrapTokens(tokens, g, fonts, indent, indent, PageSize.Width);
        Decorate(lines, spaceBefore, quoteDepth > 0);
        _lines.AddRange(lines);
    }

    private static void Decorate(List<MarkdownLine> lines, float spaceBefore, bool quote)
    {
        for (int i = 0; i < lines.Count; i++)
        {
            if (i == 0)
            {
                lines[i].SpaceBefore = spaceBefore;
            }

            if (quote)
            {
                lines[i].QuoteBar = true;
            }
        }
    }

    // ---- inline flattening ------------------------------------------------------------------------

    private void FlattenInlines(ContainerInline? container, float scale, GraphicsFontStyle style,
        GraphicsColor color, List<MarkdownRun> tokens)
    {
        if (container is null)
        {
            return;
        }

        foreach (Inline node in container)
        {
            switch (node)
            {
                case LiteralInline lit:
                    AppendText(lit.Content.ToString(), scale, style, color, tokens);
                    break;
                case EmphasisInline em:
                    GraphicsFontStyle emStyle = style |
                                                (em.DelimiterCount >= 2
                                                    ? GraphicsFontStyle.Bold
                                                    : GraphicsFontStyle.Italic);
                    FlattenInlines(em, scale, emStyle, color, tokens);
                    break;
                case CodeInline ci:
                    tokens.Add(new MarkdownRun
                    { Text = ci.Content, Scale = scale, Style = style, Color = InlineCodeColor });
                    break;
                case LinkInline link when link.IsImage:
                    AppendText($"🖼 {GetInlineText(link)}", scale, GraphicsFontStyle.Italic, QuoteColor, tokens);
                    break;
                case LinkInline link:
                    FlattenInlines(link, scale, style, LinkColor, tokens);
                    break;
                case AutolinkInline auto:
                    AppendText(auto.Url, scale, style, LinkColor, tokens);
                    break;
                case LineBreakInline lb when lb.IsHard:
                    tokens.Add(new MarkdownRun { Text = "\n" });
                    break;
                case LineBreakInline:
                    AppendText(" ", scale, style, color, tokens);
                    break;
                case ContainerInline nested:
                    FlattenInlines(nested, scale, style, color, tokens);
                    break;
            }
        }
    }

    private static string GetInlineText(ContainerInline container)
    {
        var sb = new StringBuilder();
        foreach (Inline node in container)
        {
            if (node is LiteralInline lit)
            {
                sb.Append(lit.Content.ToString());
            }
            else if (node is ContainerInline nested)
            {
                sb.Append(GetInlineText(nested));
            }
        }

        return sb.ToString();
    }

    private static void AppendText(string text, float scale, GraphicsFontStyle style, GraphicsColor color,
        List<MarkdownRun> tokens)
    {
        int i = 0;
        while (i < text.Length)
        {
            bool space = char.IsWhiteSpace(text[i]);
            int start = i;
            while (i < text.Length && char.IsWhiteSpace(text[i]) == space)
            {
                i++;
            }

            tokens.Add(new MarkdownRun
            {
                Text = space ? " " : text[start..i],
                Scale = scale,
                Style = style,
                Color = color,
                IsSpace = space
            });
        }
    }

    // ---- wrapping & pagination --------------------------------------------------------------------

    private List<MarkdownLine> WrapTokens(List<MarkdownRun> tokens, IGraphicsContext g,
        Dictionary<string, IGraphicsFont> fonts, float firstIndent, float restIndent, float maxWidth)
    {
        var lines = new List<MarkdownLine>();
        var line = new MarkdownLine { Indent = firstIndent };
        float x = 0f;
        float maxHeight = 0f;
        bool any = false;

        void Flush()
        {
            line.Height = maxHeight > 0 ? maxHeight : _baseLineHeight;
            lines.Add(line);
            line = new MarkdownLine { Indent = restIndent };
            x = 0f;
            maxHeight = 0f;
            any = false;
        }

        foreach (MarkdownRun tok in tokens)
        {
            if (tok.Text == "\n")
            {
                Flush();
                continue;
            }

            IGraphicsFont font = GetFont(g, fonts, tok.Scale, tok.Style);
            float w = Measure(g, tok.Text, font).Width;
            float h = font.GetHeight(_dpiY);
            float avail = maxWidth - line.Indent;

            if (tok.IsSpace)
            {
                if (!any)
                {
                    continue; // drop leading space
                }

                line.Runs.Add(tok);
                x += w;
                maxHeight = Math.Max(maxHeight, h);
                continue;
            }

            if (any && x + w > avail)
            {
                Flush();
            }

            // A single token wider than a full empty line (e.g. a long URL) can't be placed whole
            // without clipping; hard-split it character-by-character to fit, mirroring TextCte. The 1px
            // tolerance avoids splitting at exact-fit column boundaries (sub-pixel measurement drift).
            if (w > maxWidth - line.Indent + 1f && tok.Text.Length > 1)
            {
                string remaining = tok.Text;
                while (remaining.Length > 0)
                {
                    float curAvail = maxWidth - line.Indent - x;
                    int fit = FitChars(g, remaining, font, curAvail);
                    if (fit <= 0)
                    {
                        if (any)
                        {
                            Flush();
                            continue; // retry on a fresh line
                        }

                        fit = 1; // guarantee progress on an empty line
                    }

                    string piece = remaining[..fit];
                    line.Runs.Add(new MarkdownRun
                    { Text = piece, Scale = tok.Scale, Style = tok.Style, Color = tok.Color });
                    x += Measure(g, piece, font).Width;
                    maxHeight = Math.Max(maxHeight, h);
                    any = true;
                    remaining = remaining[fit..];
                    if (remaining.Length > 0)
                    {
                        Flush();
                    }
                }

                continue;
            }

            line.Runs.Add(tok);
            x += w;
            maxHeight = Math.Max(maxHeight, h);
            any = true;
        }

        if (any || lines.Count == 0)
        {
            line.Height = maxHeight > 0 ? maxHeight : _baseLineHeight;
            lines.Add(line);
        }

        return lines;
    }

    private int Paginate()
    {
        if (_lines.Count == 0)
        {
            return 0;
        }

        int page = 1;
        float y = 0f;
        bool firstOnPage = true;
        foreach (MarkdownLine line in _lines)
        {
            float gap = firstOnPage ? 0f : line.SpaceBefore;
            if (!firstOnPage && y + gap + line.Height > PageSize.Height)
            {
                page++;
                y = 0f;
                gap = 0f;
                firstOnPage = true;
            }

            y += gap;
            line.Page = page;
            line.Y = y;
            y += line.Height;
            firstOnPage = false;
        }

        return page;
    }

    // ---- fonts / measuring ------------------------------------------------------------------------

    private IGraphicsFont GetFont(IGraphicsContext g, Dictionary<string, IGraphicsFont> cache, float scale,
        GraphicsFontStyle style)
    {
        string key = $"{scale:F3}|{(int)style}";
        if (!cache.TryGetValue(key, out IGraphicsFont? font))
        {
            font = g.CreateFont(ContentSettings!.Font.Family, _baseSizePx * scale, style, GraphicsFontUnit.Pixel);
            cache[key] = font;
        }

        return font;
    }

    private GraphicsSizeF Measure(IGraphicsContext g, string text, IGraphicsFont font)
    {
        var proposed = new GraphicsSizeF(PageSize.Width, _baseLineHeight * 4f);
        return g.MeasureString(text, font, proposed, GraphicsStringFormat, out _, out _);
    }

    /// <summary>Longest prefix of <paramref name="text" /> (in characters) that fits <paramref name="width" />.</summary>
    private int FitChars(IGraphicsContext g, string text, IGraphicsFont font, float width)
    {
        if (width <= 0)
        {
            return 0;
        }

        var proposed = new GraphicsSizeF(width, _baseLineHeight * 4f);
        g.MeasureString(text, font, proposed, GraphicsStringFormat, out int charsFitted, out _);
        return Math.Min(charsFitted, text.Length);
    }

    private static float HeadingScale(int level)
    {
        return level switch
        {
            1 => 1.9f,
            2 => 1.55f,
            3 => 1.3f,
            4 => 1.15f,
            5 => 1.05f,
            _ => 0.95f
        };
    }

    private static int TryParseStart(string? start, int fallback)
    {
        return int.TryParse(start, NumberStyles.Integer, CultureInfo.InvariantCulture, out int n) ? n : fallback;
    }
}

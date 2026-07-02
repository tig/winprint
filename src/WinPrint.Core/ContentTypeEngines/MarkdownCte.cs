// Copyright Kindel, LLC - http://www.kindel.com
// Published under the MIT License at https://github.com/tig/winprint

using System.Globalization;
using System.Net.Http;
using System.Runtime.InteropServices;
using System.Text;
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
///     Images that sit alone in a paragraph (<c>![alt](src)</c>) are decoded and drawn through
///     <see cref="IGraphicsContext.DrawImage" />, scaled to fit the page; local files (resolved relative
///     to <see cref="ContentTypeEngineBase.SourceFileName" />), <c>data:</c> URIs, and <c>http(s)</c>
///     URLs are supported. Anything that fails to load (and inline images mixed with text) falls back
///     to alt text.
/// </summary>
public class MarkdownCte : ContentTypeEngineBase
{
    private static readonly string[] s_supportedContentTypes = ["text/x-markdown"];

    private static readonly MarkdownPipeline s_pipeline =
        new MarkdownPipelineBuilder().UseAdvancedExtensions().Build();

    private static readonly HttpClient s_http = new() { Timeout = TimeSpan.FromSeconds(10) };

    /// <summary>Decoded image bytes keyed by source URL/path; null marks a load that failed (don't retry).</summary>
    private readonly Dictionary<string, byte[]?> _imageCache = new(StringComparer.Ordinal);

    private static readonly GraphicsColor TextColor = GraphicsColor.FromRgb(0x1d, 0x1d, 0x1f);
    private static readonly GraphicsColor LinkColor = GraphicsColor.FromRgb(0x0b, 0x57, 0xd0);
    private static readonly GraphicsColor CodeColor = GraphicsColor.FromRgb(0x37, 0x37, 0x37);
    private static readonly GraphicsColor InlineCodeColor = GraphicsColor.FromRgb(0x9a, 0x34, 0x4f);
    private static readonly GraphicsColor QuoteColor = GraphicsColor.FromRgb(0x5a, 0x5a, 0x5a);
    private static readonly GraphicsColor CodeBgColor = GraphicsColor.FromRgb(0xf3, 0xf3, 0xf5);
    private static readonly GraphicsColor QuoteBarColor = GraphicsColor.FromRgb(0xcf, 0xd3, 0xda);
    private static readonly GraphicsColor RuleColor = GraphicsColor.FromRgb(0xc7, 0xcc, 0xd6);
    private static readonly GraphicsColor TableHeaderColor = GraphicsColor.FromRgb(0xee, 0xf1, 0xf6);

    private readonly List<MarkdownLine> _lines = [];
    private float _baseLineHeight;
    private float _baseSizePx;
    private int _dpiY = 96;
    private float _indentStep;
    private int _pageCount;

    public override string[] SupportedContentTypes => s_supportedContentTypes;

    public static MarkdownCte Create()
    {
        var engine = new MarkdownCte();
        engine.CopyPropertiesFrom(ModelLocator.Current.Settings.MarkdownContentTypeEngineSettings);
        return engine;
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
        _lines.Clear();
        _imageCache.Clear();

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
            WalkBlocks(ast, g, fontCache, 0, 0);

            _pageCount = Paginate();
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
        if (_lines.Count == 0)
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

        foreach (MarkdownLine line in _lines)
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
        if (_imageCache.TryGetValue(image.CacheKey, out byte[]? bytes) && bytes is { Length: > 0 })
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
                case ParagraphBlock p when GetStandaloneImages(p) is { } images:
                    foreach (LinkInline image in images)
                    {
                        EmitImage(image.Url ?? string.Empty, GetInlineText(image), g, fonts, indentLevel, quoteDepth);
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
                case CodeBlock code: // covers FencedCodeBlock too
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

    private async Task PreloadImagesAsync(MarkdownDocument ast)
    {
        foreach (ParagraphBlock paragraph in ast.Descendants<ParagraphBlock>())
        {
            if (GetStandaloneImages(paragraph) is not { } images)
            {
                continue;
            }

            foreach (LinkInline image in images)
            {
                string url = image.Url ?? string.Empty;
                if (url.Length == 0 || _imageCache.ContainsKey(url))
                {
                    continue;
                }

                _imageCache[url] = await LoadImageBytesAsync(url);
            }
        }
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

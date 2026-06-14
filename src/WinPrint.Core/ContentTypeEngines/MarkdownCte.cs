// Copyright Kindel, LLC - http://www.kindel.com
// Published under the MIT License at https://github.com/tig/winprint

using System.Globalization;
using System.Runtime.InteropServices;
using Markdig;
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
///     Images render as alt text — <see cref="IGraphicsContext" /> has no image primitive.
/// </summary>
public class MarkdownCte : ContentTypeEngineBase
{
    private static readonly string[] s_supportedContentTypes = ["text/x-markdown"];

    private static readonly MarkdownPipeline s_pipeline =
        new MarkdownPipelineBuilder().UseAdvancedExtensions().Build();

    private static readonly GraphicsColor TextColor = GraphicsColor.FromRgb(0x1d, 0x1d, 0x1f);
    private static readonly GraphicsColor LinkColor = GraphicsColor.FromRgb(0x0b, 0x57, 0xd0);
    private static readonly GraphicsColor CodeColor = GraphicsColor.FromRgb(0x37, 0x37, 0x37);
    private static readonly GraphicsColor InlineCodeColor = GraphicsColor.FromRgb(0x9a, 0x34, 0x4f);
    private static readonly GraphicsColor QuoteColor = GraphicsColor.FromRgb(0x5a, 0x5a, 0x5a);
    private static readonly GraphicsColor CodeBgColor = GraphicsColor.FromRgb(0xf3, 0xf3, 0xf5);
    private static readonly GraphicsColor QuoteBarColor = GraphicsColor.FromRgb(0xcf, 0xd3, 0xda);
    private static readonly GraphicsColor RuleColor = GraphicsColor.FromRgb(0xc7, 0xcc, 0xd6);

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

    public override async Task<int> RenderAsync(PrintResolution? printerResolution, EventHandler<string>? reflowProgress)
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
            WalkBlocks(ast, g, fontCache, indentLevel: 0, quoteDepth: 0);

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
        using IGraphicsPen rulePen = g.CreatePen(RuleColor, 2f);

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

            if (line.Rule)
            {
                float midY = line.Y + (line.Height / 2f);
                g.DrawLine(rulePen, line.Indent, midY, PageSize.Width, midY);
                continue;
            }

            float x = line.Indent;
            foreach (MarkdownRun run in line.Runs)
            {
                if (run.Text.Length == 0)
                {
                    continue;
                }

                using IGraphicsFont font = g.CreateFont(ContentSettings.Font.Family, basePt * run.Scale, run.Style, unit);
                using IGraphicsBrush brush = g.CreateSolidBrush(run.Color);
                g.DrawString(run.Text, font, brush, x, line.Y, GraphicsStringFormat);
                x += Measure(g, run.Text, font).Width;
            }
        }
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
                case Markdig.Syntax.CodeBlock code: // covers FencedCodeBlock too
                    EmitCodeBlock(code, g, fonts, indentLevel, quoteDepth);
                    break;
                case ThematicBreakBlock:
                    EmitRule(indentLevel, quoteDepth);
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
        float markerIndent = quoteGutter + (indentLevel * _indentStep) + (_indentStep * 0.4f);
        List<MarkdownLine> lines = WrapTokens(tokens, g, fonts, markerIndent, markerIndent + markerWidth);
        Decorate(lines, _baseLineHeight * 0.18f, quoteDepth > 0);
        _lines.AddRange(lines);

        // Nested blocks (sub-lists, extra paragraphs) render one level deeper.
        for (int i = firstInline is null ? 0 : 1; i < children.Count; i++)
        {
            WalkBlocks([children[i]], g, fonts, indentLevel + 1, quoteDepth);
        }
    }

    private void EmitCodeBlock(Markdig.Syntax.CodeBlock code, IGraphicsContext g,
        Dictionary<string, IGraphicsFont> fonts, int indentLevel, int quoteDepth)
    {
        float indent = (quoteDepth * _indentStep) + (indentLevel * _indentStep) + (_indentStep * 0.4f);
        IGraphicsFont font = GetFont(g, fonts, 0.92f, GraphicsFontStyle.Regular);
        float height = font.GetHeight(_dpiY);
        float charWidth = Math.Max(1f, Measure(g, "M", font).Width);
        int maxChars = Math.Max(8, (int)Math.Floor((PageSize.Width - indent) / charWidth));

        bool first = true;
        for (int n = 0; n < code.Lines.Count; n++)
        {
            string raw = code.Lines.Lines[n].Slice.ToString().Replace("\t", "    ");
            // char-wrap long code lines so they don't clip
            for (int start = 0; start == 0 || start < raw.Length; start += maxChars)
            {
                string seg = raw.Length == 0 ? string.Empty : raw.Substring(start, Math.Min(maxChars, raw.Length - start));
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
        float indent = (quoteDepth * _indentStep) + (indentLevel * _indentStep);
        _lines.Add(new MarkdownLine
        {
            Indent = indent,
            Height = _baseLineHeight,
            Rule = true,
            QuoteBar = quoteDepth > 0,
            SpaceBefore = _baseLineHeight * 0.5f
        });
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

        float indent = (quoteDepth * _indentStep) + (indentLevel * _indentStep);
        List<MarkdownLine> lines = WrapTokens(tokens, g, fonts, indent, indent);
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
                                                (em.DelimiterCount >= 2 ? GraphicsFontStyle.Bold : GraphicsFontStyle.Italic);
                    FlattenInlines(em, scale, emStyle, color, tokens);
                    break;
                case CodeInline ci:
                    tokens.Add(new MarkdownRun { Text = ci.Content, Scale = scale, Style = style, Color = InlineCodeColor });
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
        var sb = new System.Text.StringBuilder();
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
        Dictionary<string, IGraphicsFont> fonts, float firstIndent, float restIndent)
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
            float avail = PageSize.Width - line.Indent;

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

using System.Drawing;
using System.Globalization;
using System.Runtime.InteropServices;
using Serilog;
using TextMateSharp.Grammars;
using TextMateSharp.Internal.Grammars;
using TextMateSharp.Registry;
using WinPrint.Core.Abstractions;
using WinPrint.Core.Models;
using WinPrint.Core.Services;
using TextMateFontStyle = TextMateSharp.Themes.FontStyle;

namespace WinPrint.Core.ContentTypeEngines;

/// <summary>
///     Syntax-highlighting content engine backed by TextMateSharp bundled grammars.
/// </summary>
public class TextMateCte : ContentTypeEngineBase, IDisposable
{
    private static readonly string[] s_supportedContentTypes = ["text/plain"];

    private IGraphicsFont? _cachedFont;
    private bool _disposed;
    private string? _filePath;
    private IGrammar? _grammar;
    private float _lineHeight;
    private float _lineNumberWidth;
    private int _linesPerPage;
    private Registry? _registry;
    private string? _resolvedScopeName;
    private List<TextMateWrappedLine>? _wrappedLines;

    public string? ContentType { get; private set; }
    public string? Language { get; private set; }

    public override string[] SupportedContentTypes => s_supportedContentTypes;

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    public static TextMateCte Create()
    {
        var engine = new TextMateCte();
        engine.CopyPropertiesFrom(WinPrintServices.Current.Settings.TextMateContentTypeEngineSettings);
        return engine;
    }

    public void Configure(string? contentType, string? language, string? filePath)
    {
        ContentType = contentType;
        Language = language;
        _filePath = filePath;
    }

    private void Dispose(bool disposing)
    {
        LogService.TraceMessage($"disposing: {disposing}");

        if (_disposed)
        {
            return;
        }

        if (disposing)
        {
            _cachedFont?.Dispose();
            _wrappedLines = null;
        }

        _disposed = true;
    }

    public override async Task<bool> SetDocumentAsync(string doc)
    {
        Document = doc;
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

        if (printerResolution is null)
        {
            throw new ArgumentNullException(nameof(printerResolution));
        }

        int dpiX = printerResolution.X;
        int dpiY = printerResolution.Y;
        if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows) || dpiX < 0 || dpiY < 0)
        {
            dpiX = dpiY = 96;
        }

        // Obtain a graphics context used for determining glyph metrics. Production uses System.Drawing
        // (Windows); tests/non-Windows hosts inject a platform-neutral context via MeasurementContext.
        IDisposable? ownedContext = null;
        IGraphicsContext g = ResolveMeasurementContext(dpiX, dpiY, out ownedContext);

        try
        {
            g.SetTextRenderingMode(GraphicsTextRenderingMode);

            _cachedFont?.Dispose();
            _cachedFont = g.CreateFont(ContentSettings!.Font.Family, ContentSettings.Font.Size / 72F * 96F,
                (GraphicsFontStyle)ContentSettings.Font.Style, GraphicsFontUnit.Pixel);

            _lineHeight = _cachedFont.GetHeight(dpiY);
            if (PageSize.Height < _lineHeight)
            {
                throw new InvalidOperationException(
                    $"The line height ({_lineHeight:F2}) is greater than page height ({PageSize.Height:F2}). " +
                    $"PageSize={PageSize.Width:F2}x{PageSize.Height:F2}, Font={ContentSettings.Font.Family} {ContentSettings.Font.Size}pt, DPI={dpiY}");
            }

            _linesPerPage = (int)Math.Floor(PageSize.Height / _lineHeight);
            int logicalLineCount = CountLogicalLines(Document, ContentSettings.NewPageOnFormFeed);
            int lineNumberDigits = Math.Max(3, logicalLineCount.ToString(CultureInfo.InvariantCulture).Length);
            _lineNumberWidth = ContentSettings.LineNumbers
                ? MeasureRun(g, new string('0', lineNumberDigits + 1), _cachedFont).Width
                : 0;

            float charWidth = Math.Max(1, MeasureRun(g, "W", _cachedFont).Width);
            int maxLineChars = Math.Max(1, (int)Math.Floor((PageSize.Width - _lineNumberWidth) / charWidth));

            InitializeGrammar();
            _wrappedLines = TokenizeAndWrap(Document, maxLineChars);

            int pages = (int)Math.Ceiling(_wrappedLines.Count / (double)_linesPerPage);
            Log.Debug(
                "Rendered {pages} TextMate pages of {linesperpage} lines per page, for a total of {lines} lines.",
                pages, _linesPerPage, _wrappedLines.Count);
            return await Task.FromResult(pages);
        }
        finally
        {
            ownedContext?.Dispose();
        }
    }

    public override void PaintPage(IGraphicsContext graphicsContext, int pageNum)
    {
        LogService.TraceMessage($"{pageNum}");
        if (_wrappedLines is null || _cachedFont is null)
        {
            Log.Debug("TextMateCte must be rendered before painting.");
            return;
        }

        graphicsContext.SetTextRenderingMode(GraphicsTextRenderingMode);

        // Create IGraphicsFont equivalents for cross-platform rendering
        GraphicsFontUnit unit = graphicsContext.IsDisplayUnit ? GraphicsFontUnit.Point : GraphicsFontUnit.Pixel;
        float size = graphicsContext.IsDisplayUnit
            ? ContentSettings!.Font.Size
            : ContentSettings!.Font.Size / 72F * 96F;
        using IGraphicsFont baseFont = graphicsContext.CreateFont(ContentSettings.Font.Family, size,
            (GraphicsFontStyle)ContentSettings.Font.Style, unit);

        int firstLineOnPage = _linesPerPage * (pageNum - 1);
        int i;
        for (i = firstLineOnPage; i < firstLineOnPage + _linesPerPage && i < _wrappedLines.Count; i++)
        {
            TextMateWrappedLine line = _wrappedLines[i];
            float yPos = (i - firstLineOnPage) * _lineHeight;

            if (ContentSettings.LineNumbers && _lineNumberWidth != 0)
            {
                if (line.NonWrappedLineNumber > 0)
                {
                    string lineNumber = line.NonWrappedLineNumber.ToString(CultureInfo.InvariantCulture);
                    float measuredWidth = MeasureRun(graphicsContext, lineNumber, baseFont).Width;
                    float x = ContentSettings.LineNumberSeparator
                        ? _lineNumberWidth - 6 - measuredWidth
                        : 0;
                    graphicsContext.DrawString(lineNumber, baseFont, graphicsContext.GrayBrush, x, yPos,
                        GraphicsStringFormat);
                }

                if (ContentSettings.LineNumberSeparator)
                {
                    graphicsContext.DrawLine(graphicsContext.GrayPen, _lineNumberWidth - 4, yPos,
                        _lineNumberWidth - 4, yPos + _lineHeight);
                }
            }

            if (ContentSettings.Diagnostics)
            {
                graphicsContext.DrawRectangle(graphicsContext.RedPen, _lineNumberWidth, yPos,
                    PageSize.Width - _lineNumberWidth, _lineHeight);
            }

            float xPos = _lineNumberWidth;
            foreach (TextMateWrappedRun run in line.Runs)
            {
                if (run.Start >= line.Text.Length || run.Length <= 0)
                {
                    continue;
                }

                string text = line.Text.Substring(run.Start, Math.Min(run.Length, line.Text.Length - run.Start));
                GraphicsFontStyle fontStyle = GetGraphicsFontStyle(run.FontStyle);
                using IGraphicsFont runFont = graphicsContext.CreateFont(ContentSettings.Font.Family, size,
                    fontStyle, unit);
                using IGraphicsBrush brush = graphicsContext.CreateSolidBrush(
                    GraphicsColor.FromArgb(run.Foreground.A, run.Foreground.R, run.Foreground.G, run.Foreground.B));
                graphicsContext.DrawString(text, runFont, brush, xPos, yPos, GraphicsStringFormat);
                GraphicsSizeF measuredSize = MeasureRun(graphicsContext, text, runFont);

                if (ContentSettings.Diagnostics)
                {
                    using IGraphicsPen orangePen = graphicsContext.CreatePen(
                        GraphicsColor.FromRgb(255, 165, 0));
                    graphicsContext.DrawRectangle(orangePen, xPos, yPos, measuredSize.Width, measuredSize.Height);
                }

                xPos += measuredSize.Width;
            }
        }

        Log.Debug("Painted {lineOnPage} TextMate lines.", i - 1);
    }

    /// <summary>
    ///     Measures <paramref name="text" /> using the same typographic <see cref="GraphicsStringFormat" />
    ///     that <see cref="PaintPage" /> draws with. Measuring with the no-format overload uses GDI+'s
    ///     default format, which pads ~1/6 em on each side; when used to advance the pen between per-token
    ///     runs that padding accumulates into a visible gap before every token (and inflates the gutter
    ///     width and wrap column). Keeping measure and draw on the same format avoids that drift.
    /// </summary>
    private GraphicsSizeF MeasureRun(IGraphicsContext g, string text, IGraphicsFont font)
    {
        var proposedSize = new GraphicsSizeF(PageSize.Width, _lineHeight + _lineHeight / 2);
        return g.MeasureString(text, font, proposedSize, GraphicsStringFormat, out _, out _);
    }

    private static GraphicsFontStyle GetGraphicsFontStyle(TextMateFontStyle textMateStyle)
    {
        GraphicsFontStyle style = GraphicsFontStyle.Regular;
        if (textMateStyle.HasFlag(TextMateFontStyle.Bold))
        {
            style |= GraphicsFontStyle.Bold;
        }

        if (textMateStyle.HasFlag(TextMateFontStyle.Italic))
        {
            style |= GraphicsFontStyle.Italic;
        }

        return style;
    }

    private void InitializeGrammar()
    {
        ThemeName theme = ParseTheme(ContentSettings?.Style);
        var options = new RegistryOptions(theme);
        _registry = new Registry(new WinPrintRegistryOptions(options));
        _resolvedScopeName = ResolveScopeName(options);
        try
        {
            _grammar = string.IsNullOrEmpty(_resolvedScopeName) ? null : _registry.LoadGrammar(_resolvedScopeName);
        }
        catch (Exception ex)
        {
            // A missing/broken grammar must not abort printing — fall back to plain (unhighlighted) text.
            Log.Warning(ex, "TextMate: failed to load grammar {scope}; rendering as plain text.", _resolvedScopeName);
            _grammar = null;
            _resolvedScopeName = null;
        }

        Log.Debug("TextMate grammar: {scope}", _resolvedScopeName ?? "(plain text)");
    }

    private string? ResolveScopeName(RegistryOptions options)
    {
        string? fileExtension = string.IsNullOrEmpty(_filePath) ? null : Path.GetExtension(_filePath);

        // 1. WinPrint's own grammars (Brainfuck, INTERCAL) resolved from the *content type/language* —
        // this is the normal path (LoadFileAsync sets ContentType from the extension) and, crucially,
        // honors an explicit --content-type / fileTypeMapping override before the extension is consulted.
        string? customByType = WinPrintGrammars.ResolveScope(null, ContentType, Language);
        if (customByType is not null)
        {
            return customByType;
        }

        // 2. Bundled grammar by file extension.
        if (!string.IsNullOrEmpty(fileExtension))
        {
            string? scope = options.GetScopeByExtension(fileExtension);
            if (!string.IsNullOrEmpty(scope))
            {
                return scope;
            }
        }

        // 3. Bundled grammar by content type / language.
        List<Language>? languages = options.GetAvailableLanguages();
        string? normalizedContentType = Normalize(ContentType);
        string? normalizedLanguage = Normalize(Language);
        Language? match = languages.FirstOrDefault(l =>
            Matches(l.Id, normalizedLanguage) ||
            Matches(l.Id, normalizedContentType) ||
            (l.Aliases?.Any(a => Matches(a, normalizedLanguage) || Matches(a, normalizedContentType)) ?? false) ||
            (l.MimeTypes?.Any(m => Matches(m, normalizedContentType)) ?? false));
        if (match is not null)
        {
            return options.GetScopeByLanguageId(match.Id);
        }

        // 4. Esolang fallback by extension — only when no explicit content type/language was given, so an
        // override (even to plain text or another grammar) is never overridden by the file extension.
        return string.IsNullOrEmpty(ContentType) && string.IsNullOrEmpty(Language)
            ? WinPrintGrammars.ResolveScope(fileExtension, null, null)
            : null;
    }

    private static bool Matches(string? value, string? normalized)
    {
        return !string.IsNullOrEmpty(value) && !string.IsNullOrEmpty(normalized) && Normalize(value) == normalized;
    }

    private static string? Normalize(string? value)
    {
        return string.IsNullOrWhiteSpace(value)
            ? null
            : new string([.. value.Where(char.IsLetterOrDigit).Select(char.ToLowerInvariant)]);
    }

    private List<TextMateWrappedLine> TokenizeAndWrap(string document, int maxLineChars)
    {
        var wrapped = new List<TextMateWrappedLine>();
        IStateStack? ruleStack = null;
        int lineNumber = 0;

        using var reader = new StringReader(document);
        string? line;
        while ((line = reader.ReadLine()) != null)
        {
            if (ContentSettings!.TabSpaces > 0)
            {
                line = line.Replace("\t", new string(' ', ContentSettings.TabSpaces));
            }

            lineNumber++;
            if (ContentSettings.NewPageOnFormFeed && line.Contains('\f'))
            {
                string[] parts = line.Split('\f');
                for (int i = 0; i < parts.Length; i++)
                {
                    if (i > 0)
                    {
                        AddBlankLinesToNextPage(wrapped);
                    }

                    AddTokenizedLine(wrapped, parts[i], lineNumber, maxLineChars, ref ruleStack);
                    if (i < parts.Length - 1)
                    {
                        lineNumber++;
                    }
                }
            }
            else
            {
                AddTokenizedLine(wrapped, line, lineNumber, maxLineChars, ref ruleStack);
            }
        }

        if (wrapped.Count == 0)
        {
            wrapped.Add(new TextMateWrappedLine { NonWrappedLineNumber = 1 });
        }

        return wrapped;
    }

    private void AddBlankLinesToNextPage(List<TextMateWrappedLine> wrapped)
    {
        while (_linesPerPage > 0 && wrapped.Count % _linesPerPage != 0)
        {
            wrapped.Add(new TextMateWrappedLine());
        }
    }

    private void AddTokenizedLine(List<TextMateWrappedLine> wrapped, string line, int lineNumber, int maxLineChars,
        ref IStateStack? ruleStack)
    {
        List<(int Start, int End, Color Foreground, TextMateFontStyle FontStyle)> tokens =
            TokenizeLine(line, ref ruleStack);
        if (line.Length == 0)
        {
            wrapped.Add(new TextMateWrappedLine { NonWrappedLineNumber = lineNumber });
            return;
        }

        for (int start = 0; start < line.Length; start += maxLineChars)
        {
            int length = Math.Min(maxLineChars, line.Length - start);
            var wrappedLine = new TextMateWrappedLine
            {
                NonWrappedLineNumber = start == 0 ? lineNumber : 0,
                Text = line.Substring(start, length)
            };

            foreach ((int Start, int End, Color Foreground, TextMateFontStyle FontStyle) token in tokens)
            {
                int intersectionStart = Math.Max(token.Start, start);
                int intersectionEnd = Math.Min(token.End, start + length);
                if (intersectionEnd <= intersectionStart)
                {
                    continue;
                }

                wrappedLine.Runs.Add(new TextMateWrappedRun
                {
                    Start = intersectionStart - start,
                    Length = intersectionEnd - intersectionStart,
                    Foreground = token.Foreground,
                    FontStyle = token.FontStyle
                });
            }

            if (wrappedLine.Runs.Count == 0)
            {
                wrappedLine.Runs.Add(new TextMateWrappedRun { Start = 0, Length = wrappedLine.Text.Length });
            }

            wrapped.Add(wrappedLine);
        }
    }

    private List<(int Start, int End, Color Foreground, TextMateFontStyle FontStyle)> TokenizeLine(string line,
        ref IStateStack? ruleStack)
    {
        if (_grammar is null || _registry is null)
        {
            ruleStack = null;
            return [(0, line.Length, Color.Black, TextMateFontStyle.None)];
        }

        ITokenizeLineResult2? result =
            _grammar.TokenizeLine2(new LineText(line), ruleStack, TimeSpan.FromSeconds(1));
        ruleStack = result.RuleStack;
        int[]? encodedTokens = result.Tokens;
        string[] colorMap = [.. _registry.GetColorMap()];
        var tokens = new List<(int Start, int End, Color Foreground, TextMateFontStyle FontStyle)>();

        for (int i = 0; i < encodedTokens.Length; i += 2)
        {
            int start = encodedTokens[i];
            int end = i + 2 < encodedTokens.Length ? encodedTokens[i + 2] : line.Length;
            if (end <= start)
            {
                continue;
            }

            int metadata = encodedTokens[i + 1];
            tokens.Add((start, end, GetForegroundColor(metadata, colorMap),
                EncodedTokenAttributes.GetFontStyle(metadata)));
        }

        return tokens.Count == 0 ? [(0, line.Length, Color.Black, TextMateFontStyle.None)] : tokens;
    }

    private static Color GetForegroundColor(int metadata, string[] colorMap)
    {
        int colorId = EncodedTokenAttributes.GetForeground(metadata);
        int colorIndex = colorId - 1;
        if (colorIndex < 0 || colorIndex >= colorMap.Length)
        {
            return Color.Black;
        }

        string color = colorMap[colorIndex];
        return ColorTranslator.FromHtml(color);
    }

    private static ThemeName ParseTheme(string? style)
    {
        return Enum.TryParse(style, true, out ThemeName theme) ? theme : ThemeName.VisualStudioLight;
    }

    private static int CountLogicalLines(string document, bool countFormFeeds)
    {
        if (document.Length == 0)
        {
            return 1;
        }

        int lines = 1;
        foreach (char ch in document)
        {
            if (ch == '\n' || (countFormFeeds && ch == '\f'))
            {
                lines++;
            }
        }

        return lines;
    }
}

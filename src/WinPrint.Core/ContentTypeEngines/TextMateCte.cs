using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Printing;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using Serilog;
using TextMateSharp.Grammars;
using TextMateSharp.Internal.Grammars;
using TextMateSharp.Registry;
using TextMateSharp.Themes;
using WinPrint.Core.Models;
using WinPrint.Core.Services;
using DrawingFont = System.Drawing.Font;
using DrawingFontStyle = System.Drawing.FontStyle;
using TextMateFontStyle = TextMateSharp.Themes.FontStyle;

namespace WinPrint.Core.ContentTypeEngines;

internal sealed class TextMateWrappedRun {
    public int Start { get; init; }
    public int Length { get; init; }
    public Color Foreground { get; init; } = Color.Black;
    public TextMateFontStyle FontStyle { get; init; } = TextMateFontStyle.None;
}

internal sealed class TextMateWrappedLine {
    public int NonWrappedLineNumber { get; init; }
    public string Text { get; init; } = string.Empty;
    public List<TextMateWrappedRun> Runs { get; } = [];
}

/// <summary>
///     Syntax-highlighting content engine backed by TextMateSharp bundled grammars.
/// </summary>
public class TextMateCte : ContentTypeEngineBase, IDisposable {
    private static readonly string?[]? _supportedContentTypes = ["text/plain"];

    private DrawingFont? _cachedFont;
    private DrawingFont? _boldFont;
    private DrawingFont? _italicFont;
    private DrawingFont? _boldItalicFont;
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

    public override string?[]? SupportedContentTypes => _supportedContentTypes;

    public static TextMateCte Create() {
        var engine = new TextMateCte();
        engine.CopyPropertiesFrom(ModelLocator.Current.Settings.TextMateContentTypeEngineSettings);
        return engine;
    }

    public void Configure(string? contentType, string? language, string? filePath) {
        ContentType = contentType;
        Language = language;
        _filePath = filePath;
    }

    public void Dispose() {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    private void Dispose(bool disposing) {
        LogService.TraceMessage($"disposing: {disposing}");

        if (_disposed) {
            return;
        }

        if (disposing) {
            _cachedFont?.Dispose();
            _boldFont?.Dispose();
            _italicFont?.Dispose();
            _boldItalicFont?.Dispose();
            _wrappedLines = null;
        }

        _disposed = true;
    }

    public override async Task<bool> SetDocumentAsync(string doc) {
        Document = doc;
        return await Task.FromResult(true);
    }

    public override async Task<int> RenderAsync(PrinterResolution? printerResolution, EventHandler<string>? reflowProgress) {
        LogService.TraceMessage();

        if (Document is null) {
            throw new InvalidOperationException("Document can't be null for RenderAsync");
        }

        if (printerResolution is null) {
            throw new ArgumentNullException(nameof(printerResolution));
        }

        var dpiX = printerResolution.X;
        var dpiY = printerResolution.Y;
        if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows) || dpiX < 0 || dpiY < 0) {
            dpiX = dpiY = 96;
        }

        using var bitmap = new Bitmap(1, 1);
        bitmap.SetResolution(dpiX, dpiY);
        using var g = Graphics.FromImage(bitmap);
        g.PageUnit = GraphicsUnit.Display;
        g.TextRenderingHint = TextRenderingHint;

        _cachedFont?.Dispose();
        _boldFont?.Dispose();
        _italicFont?.Dispose();
        _boldItalicFont?.Dispose();
        _boldFont = null;
        _italicFont = null;
        _boldItalicFont = null;

        _cachedFont = new DrawingFont(ContentSettings!.Font.Family, ContentSettings.Font.Size / 72F * 96,
            ContentSettings.Font.Style, GraphicsUnit.Pixel);
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux)) {
            _cachedFont.Dispose();
            _cachedFont = new DrawingFont(ContentSettings.Font.Family, ContentSettings.Font.Size,
                ContentSettings.Font.Style, GraphicsUnit.Point);
        }

        _lineHeight = _cachedFont.GetHeight(dpiY);
        if (PageSize.Height < _lineHeight) {
            throw new InvalidOperationException("The line height is greater than page height.");
        }

        _linesPerPage = (int)Math.Floor(PageSize.Height / _lineHeight);
        var logicalLineCount = CountLogicalLines(Document, ContentSettings.NewPageOnFormFeed);
        var lineNumberDigits = Math.Max(3, logicalLineCount.ToString(CultureInfo.InvariantCulture).Length);
        _lineNumberWidth = ContentSettings.LineNumbers ? MeasureString(g, new string('0', lineNumberDigits + 1)).Width : 0;

        var charWidth = Math.Max(1, MeasureString(g, "W").Width);
        var maxLineChars = Math.Max(1, (int)Math.Floor((PageSize.Width - _lineNumberWidth) / charWidth));

        InitializeGrammar();
        _wrappedLines = TokenizeAndWrap(Document, maxLineChars);

        var pages = (int)Math.Ceiling(_wrappedLines.Count / (double)_linesPerPage);
        Log.Debug("Rendered {pages} TextMate pages of {linesperpage} lines per page, for a total of {lines} lines.",
            pages, _linesPerPage, _wrappedLines.Count);
        return await Task.FromResult(pages);
    }

    public override void PaintPage(Graphics g, int pageNum) {
        LogService.TraceMessage($"{pageNum}");
        if (_wrappedLines is null || _cachedFont is null) {
            Log.Debug("TextMateCte must be rendered before painting.");
            return;
        }

        g.TextRenderingHint = TextRenderingHint;
        var firstLineOnPage = _linesPerPage * (pageNum - 1);
        int i;
        for (i = firstLineOnPage; i < firstLineOnPage + _linesPerPage && i < _wrappedLines.Count; i++) {
            var line = _wrappedLines[i];
            var yPos = (i - firstLineOnPage) * _lineHeight;

            if (ContentSettings!.LineNumbers && _lineNumberWidth != 0) {
                if (line.NonWrappedLineNumber > 0) {
                    var lineNumber = line.NonWrappedLineNumber.ToString(CultureInfo.InvariantCulture);
                    var x = ContentSettings.LineNumberSeparator
                        ? _lineNumberWidth - 6 - MeasureString(g, lineNumber).Width
                        : 0;
                    g.DrawString(lineNumber, _cachedFont, Brushes.Gray, x, yPos, StringFormat);
                }

                if (ContentSettings.LineNumberSeparator) {
                    g.DrawLine(Pens.Gray, _lineNumberWidth - 4, yPos, _lineNumberWidth - 4, yPos + _lineHeight);
                }
            }

            if (ContentSettings.Diagnostics) {
                g.DrawRectangle(Pens.Red, _lineNumberWidth, yPos, PageSize.Width - _lineNumberWidth, _lineHeight);
            }

            var xPos = _lineNumberWidth;
            foreach (var run in line.Runs) {
                if (run.Start >= line.Text.Length || run.Length <= 0) {
                    continue;
                }

                var text = line.Text.Substring(run.Start, Math.Min(run.Length, line.Text.Length - run.Start));
                var font = GetFont(run.FontStyle);
                using var brush = new SolidBrush(run.Foreground);
                g.DrawString(text, font, brush, xPos, yPos, StringFormat);
                var size = MeasureString(g, font, text);

                if (ContentSettings.Diagnostics) {
                    g.DrawRectangle(new Pen(Color.Orange, 1), xPos, yPos, size.Width, size.Height);
                }

                xPos += size.Width;
            }
        }

        Log.Debug("Painted {lineOnPage} TextMate lines.", i - 1);
    }

    private void InitializeGrammar() {
        var theme = ParseTheme(ContentSettings?.Style);
        var options = new RegistryOptions(theme);
        _registry = new Registry(options);
        _resolvedScopeName = ResolveScopeName(options);
        _grammar = string.IsNullOrEmpty(_resolvedScopeName) ? null : _registry.LoadGrammar(_resolvedScopeName);
        Log.Debug("TextMate grammar: {scope}", _resolvedScopeName ?? "(plain text)");
    }

    private string? ResolveScopeName(RegistryOptions options) {
        if (!string.IsNullOrEmpty(_filePath)) {
            var extension = Path.GetExtension(_filePath);
            if (!string.IsNullOrEmpty(extension)) {
                var scope = options.GetScopeByExtension(extension);
                if (!string.IsNullOrEmpty(scope)) {
                    return scope;
                }
            }
        }

        var languages = options.GetAvailableLanguages();
        var normalizedContentType = Normalize(ContentType);
        var normalizedLanguage = Normalize(Language);
        var match = languages.FirstOrDefault(l =>
            Matches(l.Id, normalizedLanguage) ||
            Matches(l.Id, normalizedContentType) ||
            (l.Aliases?.Any(a => Matches(a, normalizedLanguage) || Matches(a, normalizedContentType)) ?? false) ||
            (l.MimeTypes?.Any(m => Matches(m, normalizedContentType)) ?? false));

        return match is null ? null : options.GetScopeByLanguageId(match.Id);
    }

    private static bool Matches(string? value, string? normalized) {
        return !string.IsNullOrEmpty(value) && !string.IsNullOrEmpty(normalized) && Normalize(value) == normalized;
    }

    private static string? Normalize(string? value) {
        return string.IsNullOrWhiteSpace(value)
            ? null
            : new string(value.Where(char.IsLetterOrDigit).Select(char.ToLowerInvariant).ToArray());
    }

    private List<TextMateWrappedLine> TokenizeAndWrap(string document, int maxLineChars) {
        var wrapped = new List<TextMateWrappedLine>();
        IStateStack? ruleStack = null;
        var lineNumber = 0;

        using var reader = new StringReader(document);
        string? line;
        while ((line = reader.ReadLine()) != null) {
            if (ContentSettings!.TabSpaces > 0) {
                line = line.Replace("\t", new string(' ', ContentSettings.TabSpaces));
            }

            lineNumber++;
            if (ContentSettings.NewPageOnFormFeed && line.Contains('\f')) {
                var parts = line.Split('\f');
                for (var i = 0; i < parts.Length; i++) {
                    if (i > 0) {
                        AddBlankLinesToNextPage(wrapped);
                    }

                    AddTokenizedLine(wrapped, parts[i], lineNumber, maxLineChars, ref ruleStack);
                    if (i < parts.Length - 1) {
                        lineNumber++;
                    }
                }
            }
            else {
                AddTokenizedLine(wrapped, line, lineNumber, maxLineChars, ref ruleStack);
            }
        }

        if (wrapped.Count == 0) {
            wrapped.Add(new TextMateWrappedLine { NonWrappedLineNumber = 1 });
        }

        return wrapped;
    }

    private void AddBlankLinesToNextPage(List<TextMateWrappedLine> wrapped) {
        while (_linesPerPage > 0 && wrapped.Count % _linesPerPage != 0) {
            wrapped.Add(new TextMateWrappedLine());
        }
    }

    private void AddTokenizedLine(List<TextMateWrappedLine> wrapped, string line, int lineNumber, int maxLineChars, ref IStateStack? ruleStack) {
        var tokens = TokenizeLine(line, ref ruleStack);
        if (line.Length == 0) {
            wrapped.Add(new TextMateWrappedLine { NonWrappedLineNumber = lineNumber });
            return;
        }

        for (var start = 0; start < line.Length; start += maxLineChars) {
            var length = Math.Min(maxLineChars, line.Length - start);
            var wrappedLine = new TextMateWrappedLine {
                NonWrappedLineNumber = start == 0 ? lineNumber : 0,
                Text = line.Substring(start, length)
            };

            foreach (var token in tokens) {
                var intersectionStart = Math.Max(token.Start, start);
                var intersectionEnd = Math.Min(token.End, start + length);
                if (intersectionEnd <= intersectionStart) {
                    continue;
                }

                wrappedLine.Runs.Add(new TextMateWrappedRun {
                    Start = intersectionStart - start,
                    Length = intersectionEnd - intersectionStart,
                    Foreground = token.Foreground,
                    FontStyle = token.FontStyle
                });
            }

            if (wrappedLine.Runs.Count == 0) {
                wrappedLine.Runs.Add(new TextMateWrappedRun { Start = 0, Length = wrappedLine.Text.Length });
            }

            wrapped.Add(wrappedLine);
        }
    }

    private List<(int Start, int End, Color Foreground, TextMateFontStyle FontStyle)> TokenizeLine(string line, ref IStateStack? ruleStack) {
        if (_grammar is null || _registry is null) {
            ruleStack = null;
            return [ (0, line.Length, Color.Black, TextMateFontStyle.None) ];
        }

        var result = _grammar.TokenizeLine2(new LineText(line), ruleStack, TimeSpan.FromSeconds(1));
        ruleStack = result.RuleStack;
        var encodedTokens = result.Tokens;
        var colorMap = _registry.GetColorMap().ToArray();
        var tokens = new List<(int Start, int End, Color Foreground, TextMateFontStyle FontStyle)>();

        for (var i = 0; i < encodedTokens.Length; i += 2) {
            var start = encodedTokens[i];
            var end = i + 2 < encodedTokens.Length ? encodedTokens[i + 2] : line.Length;
            if (end <= start) {
                continue;
            }

            var metadata = encodedTokens[i + 1];
            tokens.Add((start, end, GetForegroundColor(metadata, colorMap), EncodedTokenAttributes.GetFontStyle(metadata)));
        }

        return tokens.Count == 0 ? [ (0, line.Length, Color.Black, TextMateFontStyle.None) ] : tokens;
    }

    private static Color GetForegroundColor(int metadata, string[] colorMap) {
        var colorId = EncodedTokenAttributes.GetForeground(metadata);
        var colorIndex = colorId - 1;
        if (colorIndex < 0 || colorIndex >= colorMap.Length) {
            return Color.Black;
        }

        var color = colorMap[colorIndex];
        return ColorTranslator.FromHtml(color);
    }

    private DrawingFont GetFont(TextMateFontStyle textMateStyle) {
        if (ContentSettings!.DisableFontStyles || textMateStyle is TextMateFontStyle.None or TextMateFontStyle.NotSet) {
            return _cachedFont!;
        }

        var style = DrawingFontStyle.Regular;
        if (textMateStyle.HasFlag(TextMateFontStyle.Bold)) {
            style |= DrawingFontStyle.Bold;
        }

        if (textMateStyle.HasFlag(TextMateFontStyle.Italic)) {
            style |= DrawingFontStyle.Italic;
        }

        if (style == (DrawingFontStyle.Bold | DrawingFontStyle.Italic)) {
            return _boldItalicFont ??= new DrawingFont(_cachedFont!.FontFamily, _cachedFont.Size, style, _cachedFont.Unit);
        }

        if (style == DrawingFontStyle.Bold) {
            return _boldFont ??= new DrawingFont(_cachedFont!.FontFamily, _cachedFont.Size, style, _cachedFont.Unit);
        }

        if (style == DrawingFontStyle.Italic) {
            return _italicFont ??= new DrawingFont(_cachedFont!.FontFamily, _cachedFont.Size, style, _cachedFont.Unit);
        }

        return _cachedFont!;
    }

    private SizeF MeasureString(Graphics g, string text) {
        return MeasureString(g, _cachedFont, text);
    }

    private static SizeF MeasureString(Graphics g, DrawingFont? font, string text) {
        var proposedSize = new SizeF(10000, font!.GetHeight() + (font.GetHeight() / 2));
        return g.MeasureString(text, font, proposedSize, StringFormat, out _, out _);
    }

    private static ThemeName ParseTheme(string? style) {
        return Enum.TryParse<ThemeName>(style, ignoreCase: true, out var theme) ? theme : ThemeName.VisualStudioLight;
    }

    private static int CountLogicalLines(string document, bool countFormFeeds) {
        if (document.Length == 0) {
            return 1;
        }

        var lines = 1;
        foreach (var ch in document) {
            if (ch == '\n' || (countFormFeeds && ch == '\f')) {
                lines++;
            }
        }

        return lines;
    }
}

// Copyright Kindel, LLC - http://www.kindel.com
// Published under the MIT License at https://github.com/tig/winprint

using System.Runtime.InteropServices;
using System.Text;
using Serilog;
using WinPrint.Core.Abstractions;
using WinPrint.Core.Models;
using WinPrint.Core.Services;

namespace WinPrint.Core.ContentTypeEngines;

/// <summary>
///     Implements text/plain file type support with word/line wrapping. No formmating other
///     than line numbers.
/// </summary>
public class TextCte : ContentTypeEngineBase, IDisposable
{
    private static readonly string[] s_supportedContentTypes = ["text/plain"];
    private IGraphicsFont? _cachedFont;

    // Protected implementation of Dispose pattern.
    // Flag: Has Dispose already been called?
    private bool _disposed;

    private float _lineHeight;

    private float _lineNumberWidth;
    private int _linesPerPage;
    private int _minLineLen;

    // All of the lines of the text file, after reflow/line-wrap
    private List<WrappedLine>? _wrappedLines;

    /// <summary>
    ///     ContentType identifier (shorthand for class name).
    /// </summary>
    public override string[] SupportedContentTypes => s_supportedContentTypes;

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    public static TextCte Create()
    {
        var engine = new TextCte();
        // Populate it with the common settings
        engine.CopyPropertiesFrom(ModelLocator.Current.Settings.TextContentTypeEngineSettings);
        return engine;
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
            if (_cachedFont != null)
            {
                _cachedFont.Dispose();
            }

            _wrappedLines = null;
        }

        _disposed = true;
    }

    // TODO: Pass doc around by ref to save copies
    public override async Task<bool> SetDocumentAsync(string doc)
    {
        Document = doc;
        return await Task.FromResult(true);
    }

    /// <summary>
    ///     Get total count of pages. Set any local page-size related values (e.g. linesPerPage).
    /// </summary>
    /// <param name="e"></param>
    /// <param name="printerResolution"></param>
    /// <param name="reflowProgress"></param>
    /// <returns></returns>
    public override async Task<int> RenderAsync(PrintResolution? printerResolution,
        EventHandler<string>? reflowProgress)
    {
        LogService.TraceMessage();

        if (Document is null)
        {
            throw new InvalidOperationException("Document can't be null for Render");
        }

        if (printerResolution is null)
        {
            throw new ArgumentNullException(nameof(printerResolution));
        }

        int dpiX = printerResolution.X;
        int dpiY = printerResolution.Y;

        // BUGBUG: On Windows we can use the printer's resolution to be more accurate. But on Linux we
        // have to use 96dpi. See https://github.com/mono/libgdiplus/issues/623, etc...
        if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows) || dpiX < 0 || dpiY < 0)
        {
            dpiX = dpiY = 96;
        }

        // Obtain a graphics context used for determining glyph metrics. Production uses System.Drawing
        // (Windows); tests/non-Windows hosts inject a platform-neutral context via MeasurementContext so
        // reflow can run cross-platform.
        IDisposable? ownedContext = null;
        IGraphicsContext g = ResolveMeasurementContext(dpiX, dpiY, out ownedContext);

        try
        {
            // Calculate the number of lines per page; first we need our font. Keep it around.
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

            // Round down # of lines per page to ensure lines don't clip on bottom
            _linesPerPage = (int)Math.Floor(PageSize.Height / _lineHeight);

            // 3 digits + 1 wide - Will support 999 lines before line numbers start to not fit
            // TODO: Make line number width dynamic
            // Note, MeasureString is actually dependent on lineNumberWidth!
            _lineNumberWidth = ContentSettings.LineNumbers
                ? MeasureString(g, new string('0', 4), _cachedFont).Width
                : 0;

            // This is the shortest line length (in chars) that we think we'll see.
            // This is used as a performance optimization (probably premature) and
            // could be 0 with no functional change.
            _minLineLen = (int)((PageSize.Width - _lineNumberWidth) / MeasureString(g, "W", _cachedFont).Width);

            // Note, MeasureLines may increment numPages due to form feeds and line wrapping
            _wrappedLines = LineWrapDocument(g, Document); // new List<string>();

            int n = (int)Math.Ceiling(_wrappedLines.Count / (double)_linesPerPage);

            Log.Debug("Rendered {pages} pages of {linesperpage} lines per page, for a total of {lines} lines.", n,
                _linesPerPage, _wrappedLines.Count);

            return await Task.FromResult(n);
        }
        finally
        {
            ownedContext?.Dispose();
        }
    }

    /// <summary>
    ///     This does the heavy-weight task of ensuring each line will fit PageSize.Width by
    ///     wrapping them. It also does tab expansion (which is naive for variable-pitched fonts) and
    ///     Supports form-feeds.
    /// </summary>
    /// <param name="g"></param>
    /// <param name="document"></param>
    /// <returns></returns>
    private List<WrappedLine> LineWrapDocument(IGraphicsContext g, string document)
    {
        // TODO: Profile for performance
        // LogService.TraceMessage();
        var wrapped = new List<WrappedLine>();


        int lineCount = 0;

        // convert string to stream
        byte[] byteArray = Encoding.UTF8.GetBytes(document);
        var stream = new MemoryStream(byteArray);
        var reader = new StreamReader(stream);
        while (reader.ReadLine() is { } line)
        {
            // Expand tabs
            if (ContentSettings!.TabSpaces > 0)
            {
                line = line.Replace("\t", new string(' ', ContentSettings.TabSpaces));
            }

            ++lineCount;
            if (ContentSettings.NewPageOnFormFeed && line.Contains("\f"))
            {
                lineCount = ExpandFormFeeds(g, wrapped, line, lineCount);
            }
            else
            {
                //Log.Debug("Line {num}: {line}", lineCount, line);
                lineCount = AddLine(g, wrapped, line, lineCount);
            }
        }

        return wrapped;
    }

    /// <summary>
    ///     Form feeds
    ///     treat a FF the same as the end of a line; next line is first line of next page
    ///     FF at start of line - That line should be at top of next page
    ///     FF in middle of line - Text up to FF should be on current page, text after should be at top of
    ///     next page
    ///     FF at end of line - Next line should be top of next page
    /// </summary>
    /// <param name="g"></param>
    /// <param name="list"></param>
    /// <param name="line"></param>
    /// <param name="lineCount"></param>
    /// <returns></returns>
    private int ExpandFormFeeds(IGraphicsContext g, List<WrappedLine> list, string line, int lineCount)
    {
        string lineToAdd = "";

        for (int i = 0; i < line.Length; i++)
        {
            if (line[i] == '\f')
            {
                if (lineToAdd.Length > 0)
                {
                    // FF was NOT at start of line. Add it.
                    AddLine(g, list, lineToAdd, lineCount);
                    // if we're not at the end of the line t increment line #
                    if (i < line.Length - 1)
                    {
                        lineCount++;
                    }
                }

                // Add blank lines to get to next page
                while (list.Count % _linesPerPage != 0)
                {
                    var newLine = new WrappedLine { Text = "", NonWrappedLineNumber = 0 };
#if DEBUG
                    newLine.TextNonWrapped = line;
#endif
                    list.Add(newLine);
                }

                // Now on next line
                lineToAdd = "";
            }
            else
            {
                lineToAdd += line[i];
            }
        }

        if (lineToAdd.Length > 0)
        {
            AddLine(g, list, lineToAdd, lineCount);
        }

        return lineCount;
    }

    /// <summary>
    ///     Add a 'full length' line to the wrapped lines list. This function (which is recursive)
    ///     parses the passed line, finding the truncated version that will JUST fit in Page.Width
    ///     using GDI+'s MeasureString functionality. It then adds that truncated line to the wrapped line
    ///     list and runs recursively on the remainder.
    /// </summary>
    /// <param name="g"></param>
    /// <param name="wrappedList"></param>
    /// <param name="lineToAdd">The, potentially, too-long line to wrap.</param>
    /// <param name="lineCount"></param>
    /// <returns></returns>
    private int AddLine(IGraphicsContext g, List<WrappedLine> wrappedList, string lineToAdd, int lineCount)
    {
        // TODO: Profile AddLine for performance
        MeasureString(g, lineToAdd, _cachedFont!, out int numCharsThatFit, out int l1);
        //Log.Debug("   AddLine: {lineToAdd} - this line should {not}wrap", lineToAdd, lineToAdd.Length <= numCharsThatFit ? "not " : "");
        if (lineToAdd.Length > numCharsThatFit)
        {
            // TODO: should this be >?
            // This line wraps. Figure out by how much.
            // Starting at minLineLen into the line, keep trying until it wraps again
            // For fixed-pitch fonts, minLineLen will match exactly, so all this is not needed
            // But for variable-pitched fonts, we have to char-by-char
            int start = 0;
            int end = _minLineLen;
            for (int i = _minLineLen; i <= lineToAdd.Length; i++)
            {
                string truncatedLine = lineToAdd[start..end++];
                MeasureString(g, truncatedLine, _cachedFont!, out int numCharsThatFitTruncated, out int l2);
                if (truncatedLine.Length > numCharsThatFitTruncated)
                {
                    // The truncated line now too big, so shorten it by one char and add it
                    truncatedLine = truncatedLine[..^1];
                    var wl = new WrappedLine { Text = truncatedLine, NonWrappedLineNumber = lineCount };
#if DEBUG
                    wl.TextNonWrapped = lineToAdd;
                    //Log.Debug("   Adding shorter line to list: {truncatedLine}, {nonWrappedLineNumber}, {textNonWrapped}", wl.text, wl.nonWrappedLineNumber, wl.textNonWrapped);
#endif
                    wrappedList.Add(wl);

                    // Recurse with the rest of the line
                    AddLine(g, wrappedList, lineToAdd[truncatedLine.Length..], 0);

                    // exit for loop
                    break;
                }
            }
        }
        else
        {
            var wl = new WrappedLine { Text = lineToAdd, NonWrappedLineNumber = lineCount };
#if DEBUG
            wl.TextNonWrapped = lineToAdd;
            //Log.Debug("   Adding passed to list: {truncatedLine}, {nonWrappedLineNumber}, {textNonWrapped}", wl.text, wl.nonWrappedLineNumber, wl.textNonWrapped);
#endif
            wrappedList.Add(wl);
        }

        return lineCount;
    }

    /// <summary>
    ///     Paints a single page with line numbers.
    /// </summary>
    /// <param name="g">Graphics with 0,0 being the origin of the Page</param>
    /// <param name="pageNum">Page number to print</param>
    public override void PaintPage(IGraphicsContext g, int pageNum)
    {
        LogService.TraceMessage($"{pageNum}");
        if (_wrappedLines == null)
        {
            Log.Debug("wrappedLines must not be null");
            return;
        }

        g.SetTextRenderingMode(GraphicsTextRenderingMode);
        using IGraphicsFont paintFont = CreatePaintFont(g);

        // Paint each line of the file (each element of _wrappedLines that go on pageNum
        int firstLineInWrappedLines = _linesPerPage * (pageNum - 1);
        int i;
        for (i = firstLineInWrappedLines;
             i < firstLineInWrappedLines + _linesPerPage && i < _wrappedLines.Count;
             i++)
        {
            float yPos = (i - _linesPerPage * (pageNum - 1)) * _lineHeight;

            // Right justify line number
            int x = ContentSettings!.LineNumberSeparator
                ? (int)(_lineNumberWidth - 6 -
                        MeasureString(g, $"{_wrappedLines[i].NonWrappedLineNumber}", paintFont).Width)
                : 0;

            // Line #s
            if (_wrappedLines[i].NonWrappedLineNumber > 0)
            {
                if (ContentSettings.LineNumbers && _lineNumberWidth != 0)
                {
                    // TOOD: Figure out how to make the spacing around separator more dynamic
                    // TODO: Allow a different (non-monospace) font for line numbers
                    g.DrawString($"{_wrappedLines[i].NonWrappedLineNumber}", paintFont, g.GrayBrush, x, yPos,
                        GraphicsStringFormat);
                }
            }

            // Line # separator (draw even if there's no line number, but stop at end of doc)
            // TODO: Support setting color of line #s and separator
            if (ContentSettings.LineNumbers && ContentSettings.LineNumberSeparator && _lineNumberWidth != 0)
            {
                g.DrawLine(g.GrayPen, _lineNumberWidth - 2, yPos, _lineNumberWidth - 2, yPos + _lineHeight);
            }

            // Text
            g.DrawString(_wrappedLines[i].Text, paintFont, g.BlackBrush, _lineNumberWidth, yPos,
                GraphicsStringFormat);
            if (ContentSettings.Diagnostics)
            {
                g.DrawRectangle(g.RedPen, _lineNumberWidth, yPos, PageSize.Width - _lineNumberWidth, _lineHeight);
            }
        }

        Log.Debug("Painted {lineOnPage} lines.", i - 1);
    }

    private IGraphicsFont CreatePaintFont(IGraphicsContext g)
    {
        GraphicsFontUnit unit = g.IsDisplayUnit ? GraphicsFontUnit.Point : GraphicsFontUnit.Pixel;
        float size = g.IsDisplayUnit ? ContentSettings!.Font.Size : ContentSettings!.Font.Size / 72F * 96F;
        return g.CreateFont(ContentSettings.Font.Family, size,
            (GraphicsFontStyle)ContentSettings.Font.Style, unit);
    }

    private GraphicsSizeF MeasureString(IGraphicsContext g, string text, IGraphicsFont font)
    {
        return MeasureString(g, text, font, out _, out _);
    }

    private GraphicsSizeF MeasureString(IGraphicsContext g, string text, IGraphicsFont font, out int charsFitted,
        out int linesFilled)
    {
        g.SetTextRenderingMode(GraphicsTextRenderingMode);
        var proposedSize = new GraphicsSizeF(PageSize.Width - _lineNumberWidth, _lineHeight + _lineHeight / 2);
        return g.MeasureString(text, font, proposedSize, GraphicsStringFormat, out charsFitted, out linesFilled);
    }
}

// Copyright Kindel, LLC - http://www.kindel.com
// Published under the MIT License at https://github.com/tig/winprint

using System.Drawing;
using System.Runtime.InteropServices;
using libvt100;
using Serilog;
using WinPrint.Core.Abstractions;
using WinPrint.Core.Models;
using WinPrint.Core.Services;

namespace WinPrint.Core.ContentTypeEngines;

/// <summary>
///     Implements <c>text/ansi</c> (e.g. <c>.ans</c> / <c>.ansi</c> ANSI-art and colorized console
///     captures) by decoding the document with the vendored, managed <c>libvt100</c> ANSI decoder into a
///     <see cref="DynamicScreen" /> (lines of styled <see cref="DynamicScreen.Run" />s) and painting
///     through the cross-platform <see cref="IGraphicsContext" /> pipeline: per-run foreground color and
///     bold/italic, with optional line numbers. libvt100 owns the line/word wrapping (unlike
///     <see cref="TextCte" />, which wraps itself). Selected for <c>text/ansi</c>; <c>text/plain</c> is
///     also declared (registry parity with the historical engine) but resolves to the default CTE.
///     NOTE: https://invisible-island.net/xterm/ctlseqs/ctlseqs.html
/// </summary>
public class AnsiCte : ContentTypeEngineBase, IDisposable
{
    // text/plain is listed (as the historical AnsiCte did) so the engine is in the registry for it,
    // but text/plain resolves to the default CTE (TextMate); AnsiCte is selected for text/ansi.
    private static readonly string[] s_supportedContentTypes = ["text/plain", "text/ansi"];

    private GraphicsSizeF _charSize;
    private bool _disposed;
    private int _dpiY = 96;
    private float _lineHeight;
    private float _lineNumberWidth;
    private int _linesPerPage;
    private int _minLineLen;

    // The decoded screen (all lines, after libvt100 reflow/wrapping).
    private DynamicScreen? _screen;

    public override string[] SupportedContentTypes => s_supportedContentTypes;

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    public static AnsiCte Create()
    {
        var engine = new AnsiCte();
        engine.CopyPropertiesFrom(ModelLocator.Current.Settings.AnsiContentTypeEngineSettings);
        return engine;
    }

    private void Dispose(bool disposing)
    {
        if (_disposed)
        {
            return;
        }

        if (disposing)
        {
            _screen = null;
        }

        _disposed = true;
    }

    public override async Task<bool> SetDocumentAsync(string doc)
    {
        Document = doc;
        return await Task.FromResult(true);
    }

    /// <summary>
    ///     Decodes the document into a <see cref="DynamicScreen" /> and returns the page count.
    /// </summary>
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

        // BUGBUG: On Windows we can use the printer's resolution; elsewhere GDI+ forces 96dpi.
        if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows) || dpiX < 0 || dpiY < 0)
        {
            dpiX = dpiY = 96;
        }

        _dpiY = dpiY;

        IGraphicsContext g = ResolveMeasurementContext(dpiX, dpiY, out IDisposable? owner);
        try
        {
            using IGraphicsFont font = CreateFont(g, GraphicsFontStyle.Regular);
            _charSize = MeasureString(g, "W", font);
            _lineHeight = font.GetHeight(dpiY);

            if (PageSize.Height < _lineHeight)
            {
                throw new InvalidOperationException(
                    $"The line height ({_lineHeight:F2}) is greater than page height ({PageSize.Height:F2}).");
            }

            _linesPerPage = (int)Math.Floor(PageSize.Height / _lineHeight);

            // 4 chars wide supports up to 999 line numbers before the gutter gets tight.
            _lineNumberWidth = ContentSettings!.LineNumbers ? _charSize.Width * 4 : 0;

            // Shortest line length (chars) we expect — the wrap width handed to libvt100.
            _minLineLen = Math.Max(1, (int)((PageSize.Width - _lineNumberWidth) / Math.Max(1f, _charSize.Width)));

            _screen = new DynamicScreen(_minLineLen)
            {
                TabSpaces = Math.Max(1, ContentSettings.TabSpaces)
            };

            IAnsiDecoder vt100 = new AnsiDecoder();
            vt100.Encoding = Encoding ?? System.Text.Encoding.UTF8;
            vt100.Subscribe(_screen);

            byte[] bytes = vt100.Encoding.GetBytes(Document);
            if (bytes is { Length: > 0 })
            {
                try
                {
                    vt100.Input(bytes);
                }
                catch (Exception ex)
                {
                    // The decoder is meant to survive bad data on its own; this is a last-resort guard so
                    // a malformed ANSI file degrades to a partial render instead of aborting reflow.
                    Log.Warning(ex, "AnsiCte: ANSI decode aborted early; rendering partial output.");
                }
            }

            int n = (int)Math.Ceiling(_screen.Lines.Count / (double)_linesPerPage);
            Log.Debug("Rendered {pages} ANSI pages of {linesperpage} lines per page, total {lines} lines.", n,
                _linesPerPage, _screen.Lines.Count);
            return await Task.FromResult(n);
        }
        finally
        {
            owner?.Dispose();
        }
    }

    /// <summary>
    ///     Paints a single page by walking the decoded screen lines and their styled runs.
    /// </summary>
    public override void PaintPage(IGraphicsContext g, int pageNum)
    {
        LogService.TraceMessage($"{pageNum}");
        if (_screen is null)
        {
            Log.Debug("_screen must not be null");
            return;
        }

        g.SetTextRenderingMode(GraphicsTextRenderingMode);

        var fontCache = new Dictionary<GraphicsFontStyle, IGraphicsFont>();
        try
        {
            IGraphicsFont GetFont(GraphicsFontStyle style)
            {
                if (!fontCache.TryGetValue(style, out IGraphicsFont? f))
                {
                    f = CreatePaintFont(g, style);
                    fontCache[style] = f;
                }

                return f;
            }

            int firstLineOnPage = _linesPerPage * (pageNum - 1);
            int i;
            for (i = firstLineOnPage; i < firstLineOnPage + _linesPerPage && i < _screen.Lines.Count; i++)
            {
                DynamicScreen.Line line = _screen.Lines[i];
                float yPos = (i - _linesPerPage * (pageNum - 1)) * _lineHeight;

                PaintLineNumber(g, line.LineNumber, yPos, GetFont(GraphicsFontStyle.Regular));

                float xPos = _lineNumberWidth;
                foreach (DynamicScreen.Run run in line.Runs)
                {
                    GraphicsFontStyle style = GraphicsFontStyle.Regular;
                    if (!ContentSettings!.DisableFontStyles)
                    {
                        if (run.Attributes.Bold)
                        {
                            style |= GraphicsFontStyle.Bold;
                        }

                        if (run.Attributes.Italic)
                        {
                            style |= GraphicsFontStyle.Italic;
                        }
                    }

                    IGraphicsFont font = GetFont(style);
                    string text = line.Text[run.Start..(run.Start + run.Length)];
                    using IGraphicsBrush brush = g.CreateSolidBrush(ToGraphicsColor(run.Attributes.ForegroundColor));
                    g.DrawString(text, font, brush, xPos, yPos, GraphicsStringFormat);
                    xPos += MeasureString(g, text, font).Width;
                }
            }

            Log.Debug("Painted {lineOnPage} lines.", i - firstLineOnPage);
        }
        finally
        {
            foreach (IGraphicsFont f in fontCache.Values)
            {
                f.Dispose();
            }
        }
    }

    private void PaintLineNumber(IGraphicsContext g, int lineNumber, float yPos, IGraphicsFont font)
    {
        if (!ContentSettings!.LineNumbers || _lineNumberWidth == 0)
        {
            return;
        }

        if (lineNumber > 0)
        {
            // Right-align the number in the gutter.
            float x = ContentSettings.LineNumberSeparator
                ? _lineNumberWidth - 6 - MeasureString(g, $"{lineNumber}", font).Width
                : 0;
            g.DrawString($"{lineNumber}", font, g.GrayBrush, x, yPos, GraphicsStringFormat);
        }

        if (ContentSettings.LineNumberSeparator)
        {
            g.DrawLine(g.GrayPen, _lineNumberWidth - 2, yPos, _lineNumberWidth - 2, yPos + _lineHeight);
        }
    }

    private static GraphicsColor ToGraphicsColor(Color color)
    {
        // The terminal default foreground is white (white-on-black); print it as black on white paper.
        if (color.R == 0xFF && color.G == 0xFF && color.B == 0xFF)
        {
            return GraphicsColor.FromRgb(0, 0, 0);
        }

        return GraphicsColor.FromArgb(color.A, color.R, color.G, color.B);
    }

    private IGraphicsFont CreateFont(IGraphicsContext g, GraphicsFontStyle style)
    {
        return g.CreateFont(ContentSettings!.Font.Family, ContentSettings.Font.Size / 72F * 96F, style,
            GraphicsFontUnit.Pixel);
    }

    private IGraphicsFont CreatePaintFont(IGraphicsContext g, GraphicsFontStyle style)
    {
        GraphicsFontUnit unit = g.IsDisplayUnit ? GraphicsFontUnit.Point : GraphicsFontUnit.Pixel;
        float size = g.IsDisplayUnit ? ContentSettings!.Font.Size : ContentSettings!.Font.Size / 72F * 96F;
        return g.CreateFont(ContentSettings.Font.Family, size, style, unit);
    }

    private GraphicsSizeF MeasureString(IGraphicsContext g, string text, IGraphicsFont font)
    {
        g.SetTextRenderingMode(GraphicsTextRenderingMode);
        var proposedSize = new GraphicsSizeF(PageSize.Width, _lineHeight + _lineHeight / 2);
        return g.MeasureString(text, font, proposedSize, GraphicsStringFormat, out _, out _);
    }
}

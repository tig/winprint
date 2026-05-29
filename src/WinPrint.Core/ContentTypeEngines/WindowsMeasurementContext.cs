// Copyright Kindel, LLC - http://www.kindel.com
// Published under the MIT License at https://github.com/tig/winprint

using System;
using System.Drawing;
using WinPrint.Core.Abstractions;

namespace WinPrint.Core.ContentTypeEngines;

/// <summary>
///     Owns the System.Drawing <see cref="Bitmap" />/<see cref="Graphics" /> used to back a
///     measurement <see cref="IGraphicsContext" /> during reflow on Windows, disposing them when
///     reflow completes. This is the default measurement context used by <see cref="TextCte" /> when
///     no <see cref="ContentTypeEngineBase.MeasurementContext" /> has been injected.
/// </summary>
internal sealed class WindowsMeasurementContext : IDisposable
{
    private readonly Bitmap _bitmap;
    private readonly Graphics _graphics;

    public WindowsMeasurementContext (int dpiX, int dpiY)
    {
        // A representative 1x1 surface is enough for glyph metrics.
        _bitmap = new Bitmap (1, 1);
        _bitmap.SetResolution (dpiX, dpiY);
        _graphics = Graphics.FromImage (_bitmap);
        _graphics.PageUnit = GraphicsUnit.Display; // Display is 1/100th"
        Context = new SystemDrawingGraphicsContext (_graphics);
    }

    public IGraphicsContext Context { get; }

    public void Dispose ()
    {
        _graphics.Dispose ();
        _bitmap.Dispose ();
    }
}

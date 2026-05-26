using System;
using System.Drawing;

namespace WinPrint.Core.Abstractions;

internal sealed class SystemDrawingPen : IGraphicsPen
{
    private readonly bool _ownsNative;

    public SystemDrawingPen (Pen pen, bool ownsNative = true)
    {
        Pen = pen ?? throw new ArgumentNullException (nameof (pen));
        _ownsNative = ownsNative;
    }

    public Pen Pen { get; }

    public void Dispose ()
    {
        if (_ownsNative)
        {
            Pen.Dispose ();
        }
    }
}

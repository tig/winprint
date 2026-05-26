using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Text;

namespace WinPrint.Core.Abstractions;

internal sealed class SystemDrawingBrush : IGraphicsBrush
{
    private readonly bool _ownsNative;

    public SystemDrawingBrush (Brush brush, bool ownsNative = true)
    {
        Brush = brush ?? throw new ArgumentNullException (nameof (brush));
        _ownsNative = ownsNative;
    }

    public Brush Brush { get; }

    public void Dispose ()
    {
        if (_ownsNative)
        {
            Brush.Dispose ();
        }
    }
}

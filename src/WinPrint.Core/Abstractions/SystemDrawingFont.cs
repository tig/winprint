using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Text;

namespace WinPrint.Core.Abstractions;

internal sealed class SystemDrawingFont : IGraphicsFont
{
    private readonly bool _ownsNative;

    public SystemDrawingFont (Font font, bool ownsNative = true)
    {
        Font = font ?? throw new ArgumentNullException (nameof (font));
        _ownsNative = ownsNative;
    }

    public Font Font { get; }

    public void Dispose ()
    {
        if (_ownsNative)
        {
            Font.Dispose ();
        }
    }
}

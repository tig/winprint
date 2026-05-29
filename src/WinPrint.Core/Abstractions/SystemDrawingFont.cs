using System;
using System.Drawing;

namespace WinPrint.Core.Abstractions;

internal sealed class SystemDrawingFont : IGraphicsFont
{
    private readonly bool _ownsNative;

    public SystemDrawingFont(Font font, bool ownsNative = true)
    {
        Font = font ?? throw new ArgumentNullException(nameof(font));
        _ownsNative = ownsNative;
    }

    public Font Font { get; }

    public float GetHeight(float dpi)
    {
        return Font.GetHeight(dpi);
    }

    public void Dispose()
    {
        if (_ownsNative)
        {
            Font.Dispose();
        }
    }
}

namespace WinPrint.Core.Abstractions;

public interface IGraphicsFont : IGraphicsResource
{
    /// <summary>
    ///     Returns the line height (line spacing) of this font, in pixels, for the given vertical DPI.
    /// </summary>
    float GetHeight(float dpi);
}

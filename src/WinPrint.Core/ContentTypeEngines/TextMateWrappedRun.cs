using System.Drawing;
using TextMateFontStyle = TextMateSharp.Themes.FontStyle;

namespace WinPrint.Core.ContentTypeEngines;

internal sealed class TextMateWrappedRun
{
    public int Start { get; init; }
    public int Length { get; init; }
    public Color Foreground { get; init; } = Color.Black;
    public TextMateFontStyle FontStyle { get; init; } = TextMateFontStyle.None;
}

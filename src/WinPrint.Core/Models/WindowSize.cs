namespace WinPrint.Core.Models;

//
// Summary:
//     Specifies how a form window is displayed.

public class WindowSize
{
    public WindowSize()
    {
    }

    public WindowSize(int width, int height)
    {
        Width = width;
        Height = height;
    }

    public int Width { get; set; }
    public int Height { get; set; }
}

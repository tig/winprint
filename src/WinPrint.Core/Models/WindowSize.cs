using System;
using System.Collections.Generic;
using System.Drawing;
using System.Runtime.InteropServices;
using System.Text.Json.Serialization;
using WinPrint.Core.ContentTypeEngines;

namespace WinPrint.Core.Models;

//
// Summary:
//     Specifies how a form window is displayed.

public class WindowSize
{
    public WindowSize ()
    {
    }

    public WindowSize (int width, int height)
    {
        Width = width;
        Height = height;
    }

    public int Width { get; set; }
    public int Height { get; set; }
}

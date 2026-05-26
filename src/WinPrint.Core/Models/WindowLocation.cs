using System;
using System.Collections.Generic;
using System.Drawing;
using System.Runtime.InteropServices;
using System.Text.Json.Serialization;
using WinPrint.Core.ContentTypeEngines;

namespace WinPrint.Core.Models;

public class WindowLocation
{
    public WindowLocation ()
    {
    }

    public WindowLocation (int x, int y)
    {
        X = x;
        Y = y;
    }

    public int X { get; set; }
    public int Y { get; set; }
}

using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Printing;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using Serilog;
using TextMateSharp.Grammars;
using TextMateSharp.Internal.Grammars;
using TextMateSharp.Registry;
using WinPrint.Core.Abstractions;
using WinPrint.Core.Models;
using WinPrint.Core.Services;
using DrawingFont = System.Drawing.Font;
using DrawingFontStyle = System.Drawing.FontStyle;
using TextMateFontStyle = TextMateSharp.Themes.FontStyle;

namespace WinPrint.Core.ContentTypeEngines;

internal sealed class TextMateWrappedLine
{
    public int NonWrappedLineNumber { get; init; }
    public string Text { get; init; } = string.Empty;
    public List<TextMateWrappedRun> Runs { get; } = [];
}

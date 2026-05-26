// Copyright Kindel, LLC - http://www.kindel.com
// Published under the MIT License at https://github.com/tig/winprint

using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Printing;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using Serilog;
using WinPrint.Core.Abstractions;
using WinPrint.Core.Models;
using WinPrint.Core.Services;
using Font = System.Drawing.Font;

namespace WinPrint.Core.ContentTypeEngines;

/// <summary>
///     This struct keeps track of which lines are 'real' and thus get a printed line number
///     and which are the result of wrapping.
/// </summary>
internal struct WrappedLine
{
    internal int _nonWrappedLineNumber; // 0 if wrapped
    internal string _text; // contents of this part of the line
#if DEBUG
    internal string _textNonWrapped;
#endif
}

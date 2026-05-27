// Copyright Kindel, LLC - http://www.kindel.com
// Published under the MIT License at https://github.com/tig/winprint

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

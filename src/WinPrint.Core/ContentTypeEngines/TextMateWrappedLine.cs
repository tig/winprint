using System.Collections.Generic;

namespace WinPrint.Core.ContentTypeEngines;

internal sealed class TextMateWrappedLine
{
    public int NonWrappedLineNumber { get; init; }
    public string Text { get; init; } = string.Empty;
    public List<TextMateWrappedRun> Runs { get; } = [];
}

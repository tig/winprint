namespace WinPrint.Core.Abstractions;

/// <summary>
///     Outcome of submitting a print job. For backends that hand off to an external spooler
///     (e.g. <c>lpr</c>/CUPS) this reports whether the hand-off succeeded and how many sheets were
///     submitted, so headless callers can surface actionable errors instead of failing silently.
/// </summary>
public sealed class PrintJobResult
{
    public PrintJobResult(bool success, int sheetsPrinted, string? error = null)
    {
        Success = success;
        SheetsPrinted = sheetsPrinted;
        Error = error;
    }

    public bool Success { get; }

    public int SheetsPrinted { get; }

    public string? Error { get; }

    public static PrintJobResult Succeeded(int sheetsPrinted)
    {
        return new PrintJobResult(true, sheetsPrinted);
    }

    public static PrintJobResult Failed(string error, int sheetsPrinted = 0)
    {
        return new PrintJobResult(false, sheetsPrinted, error);
    }
}

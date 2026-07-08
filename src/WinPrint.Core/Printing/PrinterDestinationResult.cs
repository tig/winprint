namespace WinPrint.Core.Printing;

/// <summary>
///     Outcome of resolving a print destination to a concrete CUPS queue name.
/// </summary>
public readonly struct PrinterDestinationResult
{
    public string? PrinterName { get; }
    public string? Error { get; }

    public bool Success => Error is null && !string.IsNullOrEmpty(PrinterName);

    private PrinterDestinationResult(string? printerName, string? error)
    {
        PrinterName = printerName;
        Error = error;
    }

    public static PrinterDestinationResult Ok(string printerName)
    {
        ArgumentException.ThrowIfNullOrEmpty(printerName);
        return new PrinterDestinationResult(printerName, null);
    }

    public static PrinterDestinationResult Fail(string error)
    {
        ArgumentException.ThrowIfNullOrEmpty(error);
        return new PrinterDestinationResult(null, error);
    }
}

using WinPrint.Core.Abstractions;

namespace WinPrint.Core.Printing;

/// <summary>
///     Selects the default <see cref="IPrintService" /> for the current operating system: the
///     System.Drawing backend on Windows, the Skia/CUPS backend elsewhere. Used by headless front-ends
///     (CLI, TUI). MAUI selects its own services so it can present native print dialogs.
/// </summary>
public static class PrintServiceFactory
{
    public static IPrintService Create()
    {
#if WINDOWS
        if (OperatingSystem.IsWindows())
        {
            return new WindowsPrintService();
        }
#endif
        return new UnixPrintService();
    }
}

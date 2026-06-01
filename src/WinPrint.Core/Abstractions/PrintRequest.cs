using WinPrint.Core.ViewModels;

namespace WinPrint.Core.Abstractions;

/// <summary>
///     A platform-neutral description of a print operation. Carries only WinPrint.Core types so the
///     same request can be executed by any <see cref="IPrintService" /> backend (Windows
///     System.Drawing, Unix Skia/lpr, or MAUI-Mac Skia PDF).
/// </summary>
public sealed class PrintRequest
{
    public PrintRequest(SheetViewModel sheetViewModel, PrintPageSetup pageSetup, string documentName)
    {
        SheetViewModel = sheetViewModel ?? throw new ArgumentNullException(nameof(sheetViewModel));
        PageSetup = pageSetup ?? throw new ArgumentNullException(nameof(pageSetup));
        DocumentName = documentName ?? string.Empty;
    }

    public SheetViewModel SheetViewModel { get; }

    public PrintPageSetup PageSetup { get; }

    public string DocumentName { get; }

    /// <summary>First sheet to print (1-based). A value &lt;= 0 means "from the first sheet".</summary>
    public int FromSheet { get; set; }

    /// <summary>Last sheet to print (1-based, inclusive). A value &lt;= 0 means "to the last sheet".</summary>
    public int ToSheet { get; set; }
}

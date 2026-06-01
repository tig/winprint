using WinPrint.Core.Abstractions;
using WinPrint.TUI.UnitTests.Testing;
using WinPrint.TUI.Views.Editors;
using Xunit;

namespace WinPrint.TUI.UnitTests;

/// <summary>
///     Golden + behavior tests for <see cref="PrinterEditor" />: Printer and Paper-size dropdowns that
///     edit the bound mutable <see cref="PrintPageSetup" />, with injectable choice lists and an
///     unlisted-value fallback.
/// </summary>
public class PrinterEditorGoldenTests
{
    [Fact]
    public void InitialRender_MatchesGolden()
    {
        var editor = new PrinterEditor
        {
            Value = new PrintPageSetup { PrinterName = "Microsoft Print to PDF", PaperSizeName = "Letter" }
        };
        var fixture = new AppFixture(editor, 44, 6);

        GridSnapshot.Verify(fixture.Screen, "printer-editor");
    }

    [Fact]
    public void Render_ShowsPrinterAndPaper()
    {
        var editor = new PrinterEditor
        {
            Value = new PrintPageSetup { PrinterName = "Microsoft Print to PDF", PaperSizeName = "A4" }
        };
        var fixture = new AppFixture(editor, 44, 6);

        DriverAssert.ContainsText(fixture.Screen, "Microsoft Print to PDF");
        DriverAssert.ContainsText(fixture.Screen, "A4");
    }

    [Fact]
    public void OnValueChanged_AddsUnlistedValuesSoTheyDisplay()
    {
        // A printer/paper not in the offered lists is still shown (e.g. restored from a profile).
        var editor = new PrinterEditor
        {
            Value = new PrintPageSetup { PrinterName = "Acme LaserJet 9000", PaperSizeName = "Custom 5x7" }
        };
        var fixture = new AppFixture(editor, 44, 6);

        DriverAssert.ContainsText(fixture.Screen, "Acme LaserJet 9000");
        DriverAssert.ContainsText(fixture.Screen, "Custom 5x7");
    }
}

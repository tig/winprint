using WinPrint.TUI.UnitTests.Testing;
using WinPrint.TUI.Views;
using Xunit;

namespace WinPrint.TUI.UnitTests;

/// <summary>
///     Golden + behavior tests for <see cref="SettingsPanel" />: the composed left settings column —
///     Sheet, Margins, Multiple Pages Up, the two fonts, Printer, and About — stacked into one
///     continuous frame with auto-joined borders.
/// </summary>
public class SettingsPanelGoldenTests
{
    [Fact]
    public void InitialRender_MatchesGolden()
    {
        var panel = new SettingsPanel(version: "2.5.0");
        var fixture = new AppFixture(panel, width: 52, height: 24);

        GridSnapshot.Verify(fixture.Screen, "settings-panel");
    }

    [Fact]
    public void Render_ShowsEverySectionInWinFormsOrder()
    {
        var panel = new SettingsPanel(version: "2.5.0");
        var fixture = new AppFixture(panel, width: 52, height: 24);

        string screen = fixture.Screen;
        int sheet = screen.IndexOf("Sheet", StringComparison.Ordinal);
        int margins = screen.IndexOf("Margins", StringComparison.Ordinal);
        int pages = screen.IndexOf("Multiple Pages Up", StringComparison.Ordinal);
        int headerFooter = screen.IndexOf("Header/Footer Font", StringComparison.Ordinal);
        int content = screen.IndexOf("Content Font", StringComparison.Ordinal);
        int printer = screen.IndexOf("Printer", StringComparison.Ordinal);
        int about = screen.IndexOf("About", StringComparison.Ordinal);

        Assert.All(new[] { sheet, margins, pages, headerFooter, content, printer, about },
            index => Assert.True(index >= 0));

        // Sections appear top-to-bottom in the WinForms left-column order.
        Assert.True(sheet < margins);
        Assert.True(margins < pages);
        Assert.True(pages < headerFooter);
        Assert.True(headerFooter < content);
        Assert.True(content < printer);
        Assert.True(printer < about);
    }

    [Fact]
    public void BordersAutoJoin_IntoOneFrame()
    {
        var panel = new SettingsPanel(version: "2.5.0");
        var fixture = new AppFixture(panel, width: 52, height: 24);

        // A joined left tee marks where adjacent section borders merge via the shared LineCanvas.
        DriverAssert.ContainsText(fixture.Screen, "├");
    }
}

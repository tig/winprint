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
        var panel = new SettingsPanel("2.5.0");
        var fixture = new AppFixture(panel, 52, 24);

        GridSnapshot.Verify(fixture.Screen, "settings-panel");
    }

    [Fact]
    public void Render_ShowsEverySectionInWinFormsOrder()
    {
        var panel = new SettingsPanel("2.5.0");
        var fixture = new AppFixture(panel, 52, 24);

        string screen = fixture.Screen;
        int file = screen.IndexOf("File", StringComparison.Ordinal);
        int sheet = screen.IndexOf("Sheet", StringComparison.Ordinal);
        int margins = screen.IndexOf("Margins", StringComparison.Ordinal);
        int pages = screen.IndexOf("Multiple Pages Up", StringComparison.Ordinal);
        int headerFooter = screen.IndexOf("Header/Footer Font", StringComparison.Ordinal);
        int content = screen.IndexOf("Content Font", StringComparison.Ordinal);
        int printer = screen.IndexOf("Printer", StringComparison.Ordinal);

        Assert.All(new[] { file, sheet, margins, pages, headerFooter, content, printer },
            index => Assert.True(index >= 0));

        // Sections appear top-to-bottom in the WinForms left-column order.
        Assert.True(file < sheet);
        Assert.True(sheet < margins);
        Assert.True(margins < pages);
        Assert.True(pages < content);
        Assert.True(content < headerFooter);
        Assert.True(headerFooter < printer);
    }

    [Fact]
    public void BordersAutoJoin_IntoOneFrame()
    {
        var panel = new SettingsPanel("2.5.0");
        var fixture = new AppFixture(panel, 52, 24);

        // A right-tee marks where section title borders join via the shared LineCanvas.
        DriverAssert.ContainsText(fixture.Screen, "┤");
    }
}

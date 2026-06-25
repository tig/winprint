using WinPrint.Core.Models;
using WinPrint.TUI.UnitTests.Testing;
using WinPrint.TUI.Views.Editors;
using Xunit;

namespace WinPrint.TUI.UnitTests;

/// <summary>
///     Golden + behavior tests for <see cref="FontEditor" />: a summary line showing the current
///     family/style/size, plus a button that opens the full <see cref="WinPrint.TUI.Views.FontChooserDialog" />.
/// </summary>
public class FontEditorGoldenTests
{
    [Fact]
    public void InitialRender_MatchesGolden()
    {
        var editor = new FontEditor("Font", "_Font…")
        {
            Value = new Font { Family = "Source Code Pro", Size = 10f, Style = FontStyle.Regular }
        };
        var fixture = new AppFixture(editor, 60, 6);

        GridSnapshot.Verify(fixture.Screen, "font-editor");
    }

    [Fact]
    public void Render_ShowsFamilyStyleSizeSummary_AndButton()
    {
        var editor = new FontEditor("Font", "_Font…")
        {
            Value = new Font { Family = "Source Code Pro", Size = 10f, Style = FontStyle.Bold | FontStyle.Italic }
        };
        var fixture = new AppFixture(editor, 60, 6);

        DriverAssert.ContainsText(fixture.Screen, "Source Code Pro");
        DriverAssert.ContainsText(fixture.Screen, "Bold Italic"); // the summary now reflects style
        DriverAssert.ContainsText(fixture.Screen, "10pt");
        DriverAssert.ContainsText(fixture.Screen, "Font…"); // the chooser button
    }

    [Fact]
    public void OnValueChanged_ShowsAnyFamily()
    {
        // Family is a free-form string at the model level; the summary shows whatever it is.
        var editor = new FontEditor("Font", "_Font…")
        {
            Value = new Font { Family = "Wingdings Deluxe", Size = 10f, Style = FontStyle.Regular }
        };
        var fixture = new AppFixture(editor, 60, 6);

        DriverAssert.ContainsText(fixture.Screen, "Wingdings Deluxe");
    }
}

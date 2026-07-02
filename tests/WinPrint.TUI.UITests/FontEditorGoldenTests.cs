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
        var editor = new FontEditor
        {
            Value = new Font { Family = "Source Code Pro", Size = 10f, Style = FontStyle.Regular }
        };
        var fixture = new AppFixture(editor, 60, 6);

        GridSnapshot.Verify(fixture.Screen, "font-editor");
    }

    [Fact]
    public void Render_ShowsFamilyStyleSizeSummary_AndButton()
    {
        var editor = new FontEditor
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
        var editor = new FontEditor
        {
            Value = new Font { Family = "Wingdings Deluxe", Size = 10f, Style = FontStyle.Regular }
        };
        var fixture = new AppFixture(editor, 60, 6);

        DriverAssert.ContainsText(fixture.Screen, "Wingdings Deluxe");
    }

    [Fact]
    public void ChangingFont_RaisesValueChanged_SoPreviewCanReflow()
    {
        // Regression (#178): changing the font must raise ValueChanged so the SettingsPanel handler writes
        // the new font and reflows. The chooser hands back a fresh Font instance; assigning it through Value
        // is what raises the event (Font has value equality, so an identical selection is a no-op). The old
        // in-place dropdown mutation left Value reference-identical and never reflowed.
        var editor = new FontEditor
        {
            Value = new Font { Family = "Source Code Pro", Size = 10f, Style = FontStyle.Regular }
        };
        _ = new AppFixture(editor, 60, 6);

        Font? changedTo = null;
        editor.ValueChanged += (_, e) => changedTo = e.NewValue;

        editor.Value = new Font { Family = "Courier New", Size = 12f, Style = FontStyle.Bold };

        Assert.NotNull(changedTo);
        Assert.Equal("Courier New", changedTo!.Family);
        Assert.Equal(12f, changedTo.Size);
        Assert.Equal(FontStyle.Bold, changedTo.Style);
    }
}

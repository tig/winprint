using WinPrint.Core.Abstractions;
using WinPrint.TUI.UnitTests.Testing;
using WinPrint.TUI.Views.Editors;
using Xunit;

namespace WinPrint.TUI.UnitTests;

/// <summary>
///     Proof-of-concept tests for the encapsulated <see cref="MarginEditor" /> and the text-grid golden
///     test infrastructure. The editor is exercised with hard-coded values (no settings/sheet hookup)
///     to validate rendering, decimal-inch presentation, clamping, and rebind behavior.
/// </summary>
public class MarginEditorGoldenTests
{
    [Fact]
    public void InitialRender_MatchesGolden()
    {
        // PrintMargins(left, right, top, bottom) in hundredths of an inch.
        var editor = new MarginEditor { Value = new PrintMargins(75, 100, 50, 25) };
        using var fixture = new AppFixture(editor, width: 32, height: 7);

        GridSnapshot.Verify(fixture.Screen, "margin-editor");
    }

    [Fact]
    public void Render_ShowsDecimalInches_NotHundredths()
    {
        var editor = new MarginEditor { Value = new PrintMargins(75, 100, 50, 25) };
        using var fixture = new AppFixture(editor, width: 32, height: 7);

        DriverAssert.ContainsText(fixture.Screen, "0.75"); // left, as decimal inches
        DriverAssert.ContainsText(fixture.Screen, "1.00"); // right
        DriverAssert.ContainsText(fixture.Screen, "0.50"); // top
        DriverAssert.DoesNotContainText(fixture.Screen, "100"); // not raw hundredths
    }

    [Fact]
    public void Value_OutOfRange_IsClampedByCoreConstraint()
    {
        // 99999 hundredths (~1000") and -50 hundredths are both out of the 0..4" range.
        var editor = new MarginEditor { Value = new PrintMargins(99999, -50, 0, 0) };

        Assert.NotNull(editor.Value);
        Assert.Equal(400, editor.Value!.Left); // clamped to 4.00" => 400 hundredths
        Assert.Equal(0, editor.Value.Right); // clamped to 0.00" => 0 hundredths
    }

    [Fact]
    public void Value_Reassigned_RebindsChildrenToRenderNewMargins()
    {
        // Distinct top-margin values render distinctly: this is the sheet-switch rebind guard.
        var editor = new MarginEditor { Value = new PrintMargins(10, 20, 30, 40) };
        editor.Value = new PrintMargins(11, 22, 33, 44);
        using var fixture = new AppFixture(editor, width: 32, height: 7);

        DriverAssert.ContainsText(fixture.Screen, "0.33"); // rebound top value (33 hundredths)
        DriverAssert.DoesNotContainText(fixture.Screen, "0.30"); // stale value is gone
    }
}

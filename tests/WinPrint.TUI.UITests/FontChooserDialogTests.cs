using Terminal.Gui.App;
using Terminal.Gui.Drivers;
using Terminal.Gui.Input;
using WinPrint.Core.Models;
using WinPrint.TUI.UnitTests.Testing;
using WinPrint.TUI.Views;
using Xunit;

namespace WinPrint.TUI.UnitTests;

/// <summary>
///     Behavior tests for <see cref="FontChooserDialog" /> (issue #177): the chooser exposes family,
///     size, and bold/italic controls plus a live preview, and confirming returns the composed font.
///     The family list is system-dependent, so these assert the deterministic chrome and the seeded
///     selection rather than a full golden snapshot.
/// </summary>
public class FontChooserDialogTests
{
    [Fact]
    public void Render_ShowsFamilySizeStyleAndButtons()
    {
        var dialog = new FontChooserDialog(
            new Font { Family = "Source Code Pro", Size = 10f, Style = FontStyle.Regular });
        var fixture = new AppFixture(dialog, 100, 30);

        DriverAssert.ContainsText(fixture.Screen, "Choose Content Font"); // title
        DriverAssert.ContainsText(fixture.Screen, "Fixed Pitch Only"); // top-left toggle
        DriverAssert.ContainsText(fixture.Screen, "Source Code Pro"); // seeded family is always listed
        DriverAssert.ContainsText(fixture.Screen, "Size:");
        DriverAssert.ContainsText(fixture.Screen, "Bold");
        DriverAssert.ContainsText(fixture.Screen, "Italic");
        DriverAssert.ContainsText(fixture.Screen, "OK");
        DriverAssert.ContainsText(fixture.Screen, "Cancel");
    }

    [Fact]
    public void NewDialog_IsNotConfirmed()
    {
        var dialog = new FontChooserDialog(new Font { Family = "Consolas", Size = 11f });

        Assert.False(dialog.Confirmed);
        Assert.Null(dialog.SelectedFont);
    }

    [Fact]
    public void Confirm_ReturnsComposedFont_PreservingFamilySizeAndStyle()
    {
        using IApplication app = Application.Create();
        app.Init(DriverRegistry.Names.ANSI);
        app.Driver!.SetScreenSize(100, 30);

        var dialog = new FontChooserDialog(
            new Font { Family = "Consolas", Size = 11f, Style = FontStyle.Bold | FontStyle.Italic });
        SessionToken? token = app.Begin(dialog);
        app.LayoutAndDraw();

        // The OK button is the default, so Enter confirms.
        dialog.NewKeyDownEvent(Key.Enter);

        Assert.True(dialog.Confirmed);
        Assert.NotNull(dialog.SelectedFont);
        Assert.Equal("Consolas", dialog.SelectedFont!.Family);
        Assert.Equal(11f, dialog.SelectedFont.Size);
        Assert.True(dialog.SelectedFont.Style.HasFlag(FontStyle.Bold));
        Assert.True(dialog.SelectedFont.Style.HasFlag(FontStyle.Italic));

        app.End(token!);
        dialog.Dispose();
    }

    [Fact]
    public void Cancel_LeavesDialogUnconfirmed()
    {
        using IApplication app = Application.Create();
        app.Init(DriverRegistry.Names.ANSI);
        app.Driver!.SetScreenSize(100, 30);

        var dialog = new FontChooserDialog(new Font { Family = "Consolas", Size = 11f });
        SessionToken? token = app.Begin(dialog);
        app.LayoutAndDraw();

        dialog.NewKeyDownEvent(Key.Esc);

        Assert.False(dialog.Confirmed);
        Assert.Null(dialog.SelectedFont);

        app.End(token!);
        dialog.Dispose();
    }
}

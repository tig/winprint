using System.Drawing;
using Terminal.Gui.App;
using Terminal.Gui.Drivers;

namespace WinPrint.TUI;

/// <summary>
///     Helpers for <see cref="CommandKind.Input" /> commands that run without a TUI view tree.
/// </summary>
internal static class HeadlessInlineTeardown
{
    /// <summary>
    ///     Reserves an inline screen region so Terminal.Gui teardown parks the cursor below the
    ///     shell prompt instead of on it (#240).
    /// </summary>
    /// <remarks>
    ///     <see cref="Terminal.Gui.Cli.CliHost" /> wraps every command in an inline
    ///     <see cref="IApplication" /> session. Commands such as <c>wp print</c> never call
    ///     <c>RunAsync</c> on a view, so <see cref="IApplication.Screen" /> height stays zero and
    ///     <c>AnsiOutput.Dispose</c> leaves the cursor at <see cref="IDriver.InlinePosition" /> —
    ///     the row where the prompt was. The host then writes the command result there, overwriting
    ///     earlier terminal output. Claim one row at the inline anchor so dispose advances the
    ///     cursor to the line below the prompt.
    /// </remarks>
    internal static void ReserveInlineRegion(IApplication? app, int rows = 1)
    {
        if (app is not { AppModel: AppModel.Inline } || app.Driver is not { } driver)
        {
            return;
        }

        int width = Math.Max(1, driver.Screen.Width);
        int height = Math.Max(1, rows);
        int startRow = Math.Max(0, driver.InlinePosition.Y);
        app.Screen = new Rectangle(0, startRow, width, height);
    }
}

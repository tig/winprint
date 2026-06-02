using Terminal.Gui.App;
using Terminal.Gui.Drawing;
using Terminal.Gui.Drivers;
using Terminal.Gui.ViewBase;
using Terminal.Gui.Views;

namespace WinPrint.TUI;

/// <summary>
///     Renders a <see cref="View" /> headlessly on the Terminal.Gui ANSI driver and returns the full
///     character grid for the frame. This is the single render path shared by the <c>wp dump</c>
///     command and the golden-test harness, so the binary and the tests can never diverge.
/// </summary>
/// <remarks>
///     <para>
///         The capture happens inside the run loop (via <see cref="IApplication.Iteration" />), because
///         layout/draw happen there. We read <see cref="IDriver.ToString" /> — the <em>complete</em>
///         cell grid for the current frame — rather than <see cref="IDriver.ToAnsi" />, which emits
///         incremental diffs and would yield a partial frame mid-loop.
///     </para>
///     <para>
///         This is exactly the "render a view to a grid string" one-liner that Terminal.Gui could
///         expose as a first-class API (e.g. <c>Application.RenderToString(view, width, height)</c>);
///         having to hand-roll it here is a finding worth feeding back to gui-cs.
///     </para>
/// </remarks>
public static class HeadlessRenderer
{
    /// <summary>Renders <paramref name="content" /> at the given size and returns the LF-normalized grid.</summary>
    /// <param name="content">The view to render.</param>
    /// <param name="width">Viewport width in cells.</param>
    /// <param name="height">Viewport height in cells.</param>
    public static string RenderToGrid(View content, int width, int height)
    {
        ArgumentNullException.ThrowIfNull(content);

        using IApplication app = Application.Create();
        app.AppModel = AppModel.FullScreen;
        app.Init(DriverRegistry.Names.ANSI);
        app.Driver!.SetScreenSize(width, height);

        // Host content edge-to-edge with no outer chrome — the editor views carry their own borders.
        var window = new Window
        {
            X = 0,
            Y = 0,
            Width = Dim.Fill(),
            Height = Dim.Fill(),
            BorderStyle = LineStyle.None
        };
        window.Add(content);

        int iterations = 0;
        // Capture after several iterations so cross-dependent layouts (e.g. Pos.AnchorEnd combined with
        // Dim expressions referencing sibling sizes) fully settle before the grid is read.
        const int stableFrame = 8;
        string grid = string.Empty;

        app.Iteration += OnIteration;
        app.Run(window);
        app.Iteration -= OnIteration;

        return grid;

        void OnIteration(object? sender, EventArgs<IApplication?> e)
        {
            iterations++;
            grid = Canonicalize(app.Driver?.ToString());

            if (iterations >= stableFrame)
            {
                app.RequestStop();
            }
        }
    }

    /// <summary>Normalizes CRLF/CR line endings to LF so grids match across operating systems.</summary>
    public static string Canonicalize(string? text)
    {
        return (text ?? string.Empty).Replace("\r\n", "\n").Replace("\r", "\n");
    }
}

using Terminal.Gui.App;
using Terminal.Gui.Drivers;
using Terminal.Gui.ViewBase;
using Terminal.Gui.Views;

namespace WinPrint.TUI.UnitTests.Testing;

/// <summary>
///     Renders a content <see cref="View" /> headlessly on the Terminal.Gui ANSI driver and captures a
///     deterministic snapshot of the full screen as a character grid (<see cref="Screen" />).
/// </summary>
/// <remarks>
///     <para>
///         The capture happens inside the application run loop (via <see cref="IApplication.Iteration" />),
///         because the view is laid out and drawn by that loop. We read <see cref="IDriver.ToString" />,
///         which always returns the <em>complete</em> cell grid for the current frame (unlike
///         <see cref="IDriver.ToAnsi" />, which emits incremental diffs), then request stop once the
///         frame is stable. This yields a stable, diffable plain-text snapshot.
///     </para>
///     <para>
///         Each fixture uses its own <see cref="Application.Create" /> instance (thread-local isolated)
///         and never touches the process-global <c>Application.Init()</c> state.
///     </para>
/// </remarks>
public sealed class AppFixture : IDisposable
{
    private readonly IApplication _app;

    /// <summary>Renders <paramref name="content" /> and captures a stable full-grid snapshot.</summary>
    /// <param name="content">The view under test.</param>
    /// <param name="width">Test viewport width in cells.</param>
    /// <param name="height">Test viewport height in cells.</param>
    public AppFixture(View content, int width = 40, int height = 12)
    {
        _app = Application.Create();
        _app.AppModel = AppModel.FullScreen;
        _app.Init(DriverRegistry.Names.ANSI);
        _app.Driver!.SetScreenSize(width, height);

        var window = new Window
        {
            X = 0,
            Y = 0,
            Width = Dim.Fill(),
            Height = Dim.Fill()
        };
        window.Add(content);

        var iterations = 0;
        const int stableFrame = 4;

        _app.Iteration += OnIteration;
        _app.Run(window);
        _app.Iteration -= OnIteration;

        return;

        void OnIteration(object? sender, EventArgs<IApplication?> e)
        {
            iterations++;

            // ToString() returns the full character grid for the current frame.
            Screen = Canonicalize(_app.Driver?.ToString());

            if (iterations >= stableFrame)
            {
                _app.RequestStop();
            }
        }
    }

    /// <summary>The captured full-screen render as a newline-separated character grid.</summary>
    public string Screen { get; private set; } = string.Empty;

    private static string Canonicalize(string? text)
    {
        return (text ?? string.Empty).Replace("\r\n", "\n").Replace("\r", "\n");
    }

    /// <inheritdoc />
    public void Dispose()
    {
        _app.Dispose();
    }
}

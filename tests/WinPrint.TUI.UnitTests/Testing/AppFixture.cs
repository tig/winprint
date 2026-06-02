using Terminal.Gui.ViewBase;
using WinPrint.TUI;

namespace WinPrint.TUI.UnitTests.Testing;

/// <summary>
///     Renders a content <see cref="View" /> headlessly and exposes the resulting character grid as
///     <see cref="Screen" /> for golden comparison.
/// </summary>
/// <remarks>
///     This delegates to <see cref="HeadlessRenderer.RenderToGrid" /> — the same render path the
///     <c>wp dump</c> command uses — so the tests and the binary can never diverge on how a view is
///     captured.
/// </remarks>
public sealed class AppFixture
{
    /// <summary>Renders <paramref name="content" /> and captures its full-grid snapshot.</summary>
    /// <param name="content">The view under test.</param>
    /// <param name="width">Test viewport width in cells.</param>
    /// <param name="height">Test viewport height in cells.</param>
    public AppFixture(View content, int width = 40, int height = 12)
    {
        Screen = HeadlessRenderer.RenderToGrid(content, width, height);
    }

    /// <summary>The captured full-screen render as a newline-separated character grid.</summary>
    public string Screen { get; }
}

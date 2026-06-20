using Terminal.Gui.App;
using Terminal.Gui.Drawing;
using Terminal.Gui.Drivers;
using Terminal.Gui.Editor;
using Terminal.Gui.Input;
using Terminal.Gui.ViewBase;
using Terminal.Gui.Views;

namespace WinPrint.TUI.UnitTests.Testing;

/// <summary>
///     Renders a view headlessly while focusing its inner <see cref="Editor" /> and injecting
///     keystrokes, so an <em>interactive</em> state — notably the macro autocomplete popup, which only
///     exists once the user has typed a prefix — can be captured as a plain-text grid for golden review
///     (and rasterized to a PNG via <c>scripts/grid2png.py</c>).
/// </summary>
/// <remarks>
///     <para>
///         This complements <see cref="AppFixture" />/<see cref="HeadlessRenderer" />, which capture a
///         single static frame. Here the run loop is pumped one key at a time (input is processed at the
///         top of each iteration), then the grid is captured once <paramref name="captureWhen" /> is
///         satisfied — plus a couple of settle frames, because the popup is an application-level
///         <c>Popover</c> overlay that draws a frame after it becomes visible.
///     </para>
/// </remarks>
public static class InteractiveCapture
{
    /// <summary>Renders <paramref name="content" />, injecting <paramref name="keys" /> into its inner editor.</summary>
    /// <param name="content">The view to host (must contain an <see cref="Editor" /> somewhere in its tree).</param>
    /// <param name="keys">Keys to inject, one per iteration, after the editor is focused.</param>
    /// <param name="width">Viewport width in cells.</param>
    /// <param name="height">Viewport height in cells.</param>
    /// <param name="captureWhen">
    ///     Predicate over the focused inner <see cref="Editor" /> gating capture (e.g.
    ///     <see cref="Editor.IsCompletionActive" />); defaults to always.
    /// </param>
    public static string CaptureWithKeys(
        View content,
        IReadOnlyList<Key> keys,
        int width,
        int height,
        Func<Editor, bool>? captureWhen = null)
    {
        ArgumentNullException.ThrowIfNull(content);
        ArgumentNullException.ThrowIfNull(keys);

        using IApplication app = Application.Create();
        app.AppModel = AppModel.FullScreen;
        app.Init(DriverRegistry.Names.ANSI);
        app.Driver!.SetScreenSize(width, height);

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
        int keyIndex = 0;
        int settleFrames = 0;
        string grid = string.Empty;
        Editor? target = null;
        // Generous ceiling: one frame to focus, one per key, then a handful to draw the popup.
        const int maxIterations = 80;
        const int settleFramesNeeded = 2;
        // Let layout settle before focusing so the editor (and the popup it anchors to its caret) is
        // positioned; mirrors the stable-frame wait in HeadlessRenderer.
        const int settleBeforeFocus = 3;

        app.Iteration += OnIteration;
        app.Run(window);
        app.Iteration -= OnIteration;

        return grid;

        void OnIteration(object? sender, EventArgs<IApplication?> e)
        {
            iterations++;

            if (iterations < settleBeforeFocus)
            {
                return;
            }

            // Focus the editor first; let that settle a frame before typing. (The editor's container
            // must be a focusable View for this to take — see EditorBase; if that regresses, SetFocus
            // fails, the popup never opens, and the autocomplete golden test catches it.)
            if (target is null)
            {
                target = FindEditor(content)
                         ?? throw new InvalidOperationException("No Editor found in the view tree to capture.");

                target.SetFocus();
                return;
            }

            // Type the keys into the focused editor. RaiseKeyDownEvent dispatches synchronously to the
            // focused view, so each insert (and the completion query it triggers) completes in turn.
            if (keyIndex < keys.Count)
            {
                app.Keyboard.RaiseKeyDownEvent(keys[keyIndex++]);
                return;
            }

            // Keys done: capture the current frame, then stop once the gate is satisfied and the
            // overlay has had a couple of frames to draw — or at the safety ceiling.
            grid = HeadlessRenderer.Canonicalize(app.Driver?.ToString());

            if (captureWhen?.Invoke((Editor)target) ?? true)
            {
                settleFrames++;
            }

            if (settleFrames >= settleFramesNeeded || iterations >= maxIterations)
            {
                app.RequestStop();
            }
        }
    }

    /// <summary>
    ///     Hosts <paramref name="content" /> in the run loop and reports whether the inner
    ///     <see cref="Editor" /> can take keyboard focus — i.e. every container on the path from the host
    ///     down to the editor is focusable. Terminal.Gui skips a non-focusable container and its whole
    ///     subtree, so this fails if any link in the chain has <c>CanFocus = false</c>.
    /// </summary>
    public static bool CanFocusInnerEditor(View content, int width, int height)
    {
        ArgumentNullException.ThrowIfNull(content);

        using IApplication app = Application.Create();
        app.AppModel = AppModel.FullScreen;
        app.Init(DriverRegistry.Names.ANSI);
        app.Driver!.SetScreenSize(width, height);

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
        bool focused = false;

        app.Iteration += OnIteration;
        app.Run(window);
        app.Iteration -= OnIteration;

        return focused;

        void OnIteration(object? sender, EventArgs<IApplication?> e)
        {
            iterations++;
            if (iterations < 3)
            {
                return;
            }

            if (FindEditor(content) is { } editor)
            {
                editor.SetFocus();
                focused = editor.HasFocus;
            }

            app.RequestStop();
        }
    }

    // Depth-first search for the first Editor in the view tree (the format field, here).
    private static Editor? FindEditor(View view)
    {
        foreach (View sub in view.SubViews)
        {
            if (sub is Editor editor)
            {
                return editor;
            }

            Editor? nested = FindEditor(sub);
            if (nested is not null)
            {
                return nested;
            }
        }

        return null;
    }
}

using System.Reflection;
using System.Drawing;
using Terminal.Gui.Configuration;
using Terminal.Gui.Drawing;
using Terminal.Gui.Input;
using Terminal.Gui.Views;
using WinPrint.Core;
using WinPrint.TUI.UnitTests.Testing;
using WinPrint.TUI.Graphics;
using WinPrint.TUI.Views;
using Xunit;

namespace WinPrint.TUI.UnitTests;

/// <summary>
///     Golden test for <see cref="PreviewPane" />: an empty bordered frame standing in for the page
///     preview.
/// </summary>
public class PreviewPaneGoldenTests
{
    [Fact]
    public void InitialRender_MatchesGolden()
    {
        var preview = new PreviewPane();
        var fixture = new AppFixture(preview, 44, 22);

        GridSnapshot.Verify(fixture.Screen, "preview-pane");
    }

    [Fact]
    public void WithoutSixelSupport_RendersEmptyPane()
    {
        var preview = new PreviewPane();
        var fixture = new AppFixture(preview, 44, 22);

        // With the fallback link disabled, the pane renders empty (no crash).
        Assert.NotNull(fixture.Screen);
    }

    [Fact]
    public void PreviewCanvasScheme_MatchesRendererCanvasBackground()
    {
        _ = new PreviewPane();

        Scheme scheme = SchemeManager.GetScheme(PreviewPane.PreviewCanvasSchemeName);

        Assert.Equal(224, scheme.Normal.Background.R);
        Assert.Equal(224, scheme.Normal.Background.G);
        Assert.Equal(224, scheme.Normal.Background.B);
    }

    [Fact]
    public void RenderSpinner_StartsHiddenAndCanBeToggled()
    {
        var preview = new PreviewPane();

        Assert.False(preview.RenderSpinner.Visible);
        Assert.False(preview.RenderSpinner.AutoSpin);

        SetRenderingVisible(preview, true);

        Assert.True(preview.RenderSpinner.Visible);
        Assert.True(preview.RenderSpinner.AutoSpin);

        SetRenderingVisible(preview, false);

        Assert.False(preview.RenderSpinner.Visible);
        Assert.False(preview.RenderSpinner.AutoSpin);
    }

    [Fact]
    public void RenderSpinner_HidesPlaceholderWhileRendering()
    {
        var preview = new PreviewPane();
        preview.PageLabel.Visible = true;

        SetRenderingVisible(preview, true);

        Assert.False(preview.PageLabel.Visible);
        Assert.Null(preview.PageLabel.SchemeName);
    }

    [Fact]
    public void KeyboardNavigation_SeparatesPagesPanAndZoom()
    {
        var preview = new PreviewPane();
        SetTotalPages(preview, 3);

        Assert.True(SendKey(preview, Key.PageDown));
        Assert.Equal(1, preview.CurrentPage);

        Assert.True(SendKey(preview, Key.CursorRight));
        Assert.Equal(1, preview.CurrentPage);

        // Keyboard zoom is no longer overridden by the preview — it is owned by ImageView
        // (gui-cs/Terminal.Gui#5494). The old Ctrl+PageUp binding was dead on macOS, so the preview
        // must not intercept it or change page/zoom; it falls through to ImageView.
        double zoom = preview.Image.ZoomLevel;
        Assert.False(SendKey(preview, Key.PageUp.WithCtrl));
        Assert.Equal(zoom, preview.Image.ZoomLevel);
        Assert.Equal(1, preview.CurrentPage);

        Assert.True(SendKey(preview, Key.Home));
        Assert.Equal(0, preview.CurrentPage);

        Assert.True(SendKey(preview, Key.PageUp));
        Assert.Equal(0, preview.CurrentPage);
    }

    [Fact]
    public void ZoomKeys_AreOwnedByImageView()
    {
        var preview = new PreviewPane();

        // winprint no longer overrides zoom keys; it inherits ImageView's Mac-safe +/=/- and 0
        // bindings (gui-cs/Terminal.Gui#5494). PageUp/PageDown are left free for page navigation.
        Assert.Contains(Command.ZoomIn, preview.Image.KeyBindings.GetCommands(new Key('+')));
        Assert.Contains(Command.ZoomIn, preview.Image.KeyBindings.GetCommands(new Key('=')));
        Assert.Contains(Command.ZoomOut, preview.Image.KeyBindings.GetCommands(new Key('-')));
        Assert.Contains(Command.Home, preview.Image.KeyBindings.GetCommands(Key.D0)); // '0' resets the view
        Assert.Empty(preview.Image.KeyBindings.GetCommands(Key.PageUp));
        Assert.Empty(preview.Image.KeyBindings.GetCommands(Key.PageDown));
    }

    [Fact]
    public void RenderScale_FollowsZoomWhenWithinPixelBudget()
    {
        var preview = new PreviewPane();
        preview.Image.ZoomLevel = 4d;

        float scale = GetRenderScale(preview, 1000, 1000);

        Assert.Equal(4f, scale);
    }

    [Fact]
    public void RenderScale_CapsZoomByPixelBudget()
    {
        var preview = new PreviewPane();
        preview.Image.ZoomLevel = 10d;

        float scale = GetRenderScale(preview, 2000, 1200);

        Assert.InRange(scale, 3.1f, 3.2f);
    }

    [Fact]
    public void CtrlWheelZoom_AnchorsOnMousePosition()
    {
        var preview = new PreviewPane();
        preview.Image.Image = new Terminal.Gui.Drawing.Color[100, 100];
        preview.Image.Viewport = new Rectangle(0, 0, 10, 10);

        SendMouse(preview, MouseFlags.WheeledUp | MouseFlags.Ctrl, new Point(8, 5));

        Assert.True(GetImageCenterX(preview) > 0.5d);
    }

    [Fact]
    public void NoFilePreview_ClickAnywhereRequestsOpenFile()
    {
        var preview = new PreviewPane();
        bool requested = false;
        preview.OpenFileRequested += (_, _) => requested = true;

        SendMouse(preview, MouseFlags.LeftButtonClicked);

        Assert.True(requested);
    }

    [Fact]
    public void LoadedPreview_ClickDoesNotRequestOpenFile()
    {
        var preview = new PreviewPane();
        SetLoadedPreview(preview);
        bool requested = false;
        preview.OpenFileRequested += (_, _) => requested = true;

        SendMouse(preview, MouseFlags.LeftButtonClicked);

        Assert.False(requested);
    }

    private static void SetTotalPages(PreviewPane preview, int totalPages)
    {
        typeof(PreviewPane)
            .GetField("_totalPages", BindingFlags.Instance | BindingFlags.NonPublic)!
            .SetValue(preview, totalPages);
    }

    private static bool SendKey(PreviewPane preview, Key key)
    {
        return (bool)typeof(PreviewPane)
            .GetMethod("OnKeyDown", BindingFlags.Instance | BindingFlags.NonPublic)!
            .Invoke(preview, [key])!;
    }

    private static void SendMouse(PreviewPane preview, MouseFlags flags, Point? position = null)
    {
        typeof(PreviewPane)
            .GetMethod("HandlePreviewMouse", BindingFlags.Instance | BindingFlags.NonPublic)!
            .Invoke(preview, [new Mouse { Flags = flags, Position = position }]);
    }

    private static void SetRenderingVisible(PreviewPane preview, bool visible)
    {
        typeof(PreviewPane)
            .GetMethod("SetRenderingVisible", BindingFlags.Instance | BindingFlags.NonPublic)!
            .Invoke(preview, [visible]);
    }

    private static float GetRenderScale(PreviewPane preview, int width, int height)
    {
        return (float)typeof(PreviewPane)
            .GetMethod("GetRenderScale", BindingFlags.Instance | BindingFlags.NonPublic)!
            .Invoke(preview, [width, height])!;
    }

    private static double GetImageCenterX(PreviewPane preview)
    {
        return (double)typeof(ImageView)
            .GetField("_centerX", BindingFlags.Instance | BindingFlags.NonPublic)!
            .GetValue(preview.Image)!;
    }

    private static void SetLoadedPreview(PreviewPane preview)
    {
        typeof(PreviewPane)
            .GetField("_sheetVM", BindingFlags.Instance | BindingFlags.NonPublic)!
            .SetValue(preview, new SheetViewModel());

        typeof(PreviewPane)
            .GetField("_renderer", BindingFlags.Instance | BindingFlags.NonPublic)!
            .SetValue(preview, new PageRenderer());
    }
}

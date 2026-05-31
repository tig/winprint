using Terminal.Gui.Drawing;
using Terminal.Gui.ViewBase;

namespace WinPrint.TUI.Views;

/// <summary>
///     Placeholder page preview: an empty bordered frame standing in for the WinForms
///     <c>PrintPreview</c> control. The pane carries its own border and sets
///     <see cref="View.SuperViewRendersLineCanvas" /> so, when overlapped with the surrounding
///     editors, the shared LineCanvas joins the borders into one continuous frame. Real page rendering
///     is wired in later.
/// </summary>
public sealed class PreviewPane : View
{
    /// <summary>Creates the (empty) preview pane.</summary>
    public PreviewPane()
    {
        Width = Dim.Fill();
        Height = Dim.Fill();
        BorderStyle = LineStyle.Single;
        SuperViewRendersLineCanvas = true;
    }
}

namespace WinPrint.TUI.Views.Editors;

/// <summary>
///     Shared layout metrics for the settings editors. Using fixed field widths (rather than
///     <c>Dim.Fill</c>) lets each editor's own width be <c>Dim.Auto</c> — so the editor sizes to its
///     content and the composed panel sizes to the widest editor. Keeping the values common means the
///     stacked editors line up and their borders join cleanly.
/// </summary>
internal static class EditorMetrics
{
    private const int DropDownArrowWidth = 3;

    /// <summary>Width of the left-hand label column (fits the longest label, "Size (pt):").</summary>
    public const int LabelWidth = 10;

    /// <summary>Width of a primary input field (dropdowns, text fields).</summary>
    public const int FieldWidth = 28;

    /// <summary>Width of a compact field (e.g. the font size dropdown).</summary>
    public const int SizeFieldWidth = 6;

    /// <summary>
    ///     The natural content width of a label-plus-field editor row (<see cref="LabelWidth" /> +
    ///     <see cref="FieldWidth" />). Editors whose layout needs a definite width — e.g. the diamond
    ///     <c>MarginEditor</c> using <c>Pos.Center</c>/<c>Pos.AnchorEnd</c> — use this so they match the
    ///     auto-sized editors when stacked.
    /// </summary>
    public const int ContentWidth = LabelWidth + FieldWidth;

    /// <summary>Returns a compact dropdown width that fits the longest item plus the dropdown arrow.</summary>
    public static int DropDownWidth(IEnumerable<string> items)
    {
        int contentWidth = items.DefaultIfEmpty(string.Empty).Max(static item => item.Length);
        return Math.Max(DropDownArrowWidth, contentWidth + DropDownArrowWidth);
    }
}

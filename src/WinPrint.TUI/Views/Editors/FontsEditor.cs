using Terminal.Gui.ViewBase;
using WinPrint.Core.Models;

namespace WinPrint.TUI.Views.Editors;

/// <summary>
///     Groups the two font selectors winprint cares about — the Header/Footer font and the Content
///     font — as two <see cref="FontEditor" />s stacked vertically. Each child sets
///     <see cref="View.SuperViewRendersLineCanvas" />, and the lower editor overlaps the upper by one
///     row, so Terminal.Gui's shared <c>LineCanvas</c> auto-joins their borders into one continuous
///     frame (the same technique the UI Catalog <c>AdornmentsEditor</c> uses to stack sub-editors).
/// </summary>
public sealed class FontsEditor : View
{
    /// <summary>Creates the grouped fonts editor.</summary>
    public FontsEditor()
    {
        Width = Dim.Fill();
        Height = Dim.Auto(DimAutoStyle.Content);
        // Focusable container so focus descends into the two child FontEditors (a non-focusable View
        // would have its subviews skipped by Terminal.Gui's focus navigation).
        CanFocus = true;

        HeaderFooterFont = new FontEditor("Header/Footer")
        {
            X = 0,
            Y = 0,
            Value = new Font { Family = "Source Code Pro", Size = 8f, Style = FontStyle.Regular }
        };

        // Overlap the upper editor's bottom border by one row; the joined LineCanvas merges them.
        ContentFont = new FontEditor("Content")
        {
            X = 0,
            Y = Pos.Bottom(HeaderFooterFont) - 1,
            Value = new Font { Family = "Source Code Pro", Size = 10f, Style = FontStyle.Regular }
        };

        Add(HeaderFooterFont, ContentFont);
    }

    /// <summary>Editor for the header/footer font.</summary>
    public FontEditor HeaderFooterFont { get; }

    /// <summary>Editor for the content font.</summary>
    public FontEditor ContentFont { get; }
}

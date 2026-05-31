using Terminal.Gui.ViewBase;
using WinPrint.Core.Abstractions;
using WinPrint.Core.Models;
using WinPrint.TUI.Views.Editors;

namespace WinPrint.TUI;

/// <summary>
///     Named factory for the editor/subview components, so the same view can be instantiated with a
///     representative sample value from anywhere: the golden tests, the scratch render host, the
///     <c>wp render</c>/<c>wp dump</c> commands, and (via tuirec) full-fidelity image capture. Keeping
///     one catalog avoids the sample value drifting between the test harness and the binary.
/// </summary>
public static class ViewCatalog
{
    /// <summary>The names of every catalogued view, for help text and discovery.</summary>
    public static IReadOnlyList<string> Names { get; } = ["margin", "header", "footer", "font", "fonts"];

    /// <summary>Creates the named view populated with a representative sample value.</summary>
    /// <param name="name">A name from <see cref="Names" />.</param>
    /// <returns>The constructed view.</returns>
    /// <exception cref="ArgumentException">If <paramref name="name" /> is not catalogued.</exception>
    public static View Create(string name)
    {
        return name switch
        {
            "margin" => new MarginEditor { Value = new PrintMargins(75, 100, 50, 25) },
            "header" => new HeaderFooterEditor("_Header")
            {
                Value = new Header { Enabled = true, Text = "{FileName}|{Title}|Page {Page}" }
            },
            "footer" => new HeaderFooterEditor("_Footer")
            {
                Value = new Footer { Enabled = true, Text = "{FilePath}||{DatePrinted}" }
            },
            "font" => new FontEditor("_Font")
            {
                Value = new Font { Family = "Source Code Pro", Size = 10f, Style = FontStyle.Regular }
            },
            "fonts" => new FontsEditor(),
            _ => throw new ArgumentException(
                $"Unknown view '{name}'. Known views: {string.Join(", ", Names)}.", nameof(name))
        };
    }
}

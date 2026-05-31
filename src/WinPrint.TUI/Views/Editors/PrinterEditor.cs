using System.Collections.ObjectModel;
using Terminal.Gui.Drawing;
using Terminal.Gui.ViewBase;
using Terminal.Gui.Views;
using WinPrint.Core.Abstractions;

namespace WinPrint.TUI.Views.Editors;

/// <summary>
///     Picks the target printer and paper size, mirroring the original WinForms <c>printersCB</c> and
///     <c>paperSizesCB</c> combos. The bound value is a <see cref="PrintPageSetup" />; the two
///     dropdowns edit its <see cref="PrintPageSetup.PrinterName" /> and
///     <see cref="PrintPageSetup.PaperSizeName" />.
///     <para>
///         Choice lists are injectable because the real lists are Windows-only
///         (<c>PrinterSettings.InstalledPrinters</c> / <c>.PaperSizes</c>); they default to
///         <see cref="PrinterChoices" /> so the editor renders cross-platform and in tests. A bound
///         value not in its list is added so the dropdown can display it. <see cref="PrintPageSetup" />
///         is mutable; editing a child mutates the bound instance in place.
///     </para>
/// </summary>
public sealed class PrinterEditor : EditorBase<PrintPageSetup>
{
    private readonly DropDownList _printer;
    private readonly ObservableCollection<string> _printers;
    private readonly DropDownList _paper;
    private readonly ObservableCollection<string> _papers;

    /// <summary>Creates a printer/paper-size editor.</summary>
    /// <param name="printers">Available printers; defaults to <see cref="PrinterChoices.DefaultPrinters" />.</param>
    /// <param name="paperSizes">Available paper sizes; defaults to <see cref="PrinterChoices.DefaultPaperSizes" />.</param>
    public PrinterEditor(IEnumerable<string>? printers = null, IEnumerable<string>? paperSizes = null)
    {
        Width = Dim.Fill();
        Height = Dim.Auto(DimAutoStyle.Content);
        BorderStyle = LineStyle.Single;
        SuperViewRendersLineCanvas = true;
        Title = "_Printer";

        _printers = new ObservableCollection<string>(printers ?? PrinterChoices.DefaultPrinters);
        var printerLabel = new Label { X = 0, Y = 0, Text = "Printer:" };
        _printer = new DropDownList
        {
            X = Pos.Right(printerLabel) + 1,
            Y = 0,
            Width = Dim.Fill(),
            Source = new ListWrapper<string>(_printers)
        };

        _papers = new ObservableCollection<string>(paperSizes ?? PrinterChoices.DefaultPaperSizes);
        var paperLabel = new Label { X = 0, Y = Pos.Bottom(printerLabel), Text = "Paper:  " };
        _paper = new DropDownList
        {
            X = Pos.Right(paperLabel) + 1,
            Y = Pos.Top(paperLabel),
            Width = Dim.Fill(),
            Source = new ListWrapper<string>(_papers)
        };

        _printer.ValueChanged += (_, _) => PushFromChildren();
        _paper.ValueChanged += (_, _) => PushFromChildren();

        Add(printerLabel, _printer, paperLabel, _paper);
    }

    /// <inheritdoc />
    protected override void OnValueChanged(PrintPageSetup? newValue)
    {
        PrintPageSetup setup = newValue ?? new PrintPageSetup();
        _printer.Value = Ensure(_printers, setup.PrinterName);
        _paper.Value = Ensure(_papers, setup.PaperSizeName);
    }

    private void PushFromChildren()
    {
        if (Suppressing || Value is null)
        {
            return;
        }

        // PrintPageSetup is mutable; mutate the bound instance directly.
        Value.PrinterName = _printer.Value ?? string.Empty;
        Value.PaperSizeName = _paper.Value ?? string.Empty;
    }

    // A bound printer/paper may not be in the offered list (e.g. set from a saved profile); add it so
    // the dropdown can show it as the current selection.
    private static string Ensure(ObservableCollection<string> items, string value)
    {
        if (!string.IsNullOrEmpty(value) && !items.Contains(value))
        {
            items.Insert(0, value);
        }

        return value;
    }
}

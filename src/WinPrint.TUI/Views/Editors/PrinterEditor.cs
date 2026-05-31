using System.Collections.ObjectModel;
using Terminal.Gui.Drawing;
using Terminal.Gui.ViewBase;
using Terminal.Gui.Views;
using WinPrint.Core.Abstractions;

namespace WinPrint.TUI.Views.Editors;

/// <summary>
///     Picks the target printer, paper size, and sheet range (From/To), mirroring the original WinForms
///     <c>printerGroup</c> — which stacks the printer combo, paper combo, and the From/To inputs under a
///     single "Printer" group box. The bound value is a <see cref="PrintPageSetup" />; an optional
///     <see cref="PageRange" /> carries From/To.
///     <para>
///         Printer/paper choice lists are injectable because the real lists are Windows-only
///         (<c>PrinterSettings.InstalledPrinters</c> / <c>.PaperSizes</c>); they default to
///         <see cref="PrinterChoices" /> so the editor renders cross-platform and in tests. A bound
///         value not in its list is added so the dropdown can display it. Both bound models are mutable;
///         editing a child mutates them in place.
///     </para>
/// </summary>
public sealed class PrinterEditor : EditorBase<PrintPageSetup>
{
    private const int MinFrom = 1;
    private const int MaxPage = 9999;

    // Width of the left-hand label column, so Printer/Paper/From/To align like the other editors.
    private const int LabelWidth = 9;

    private readonly DropDownList _printer;
    private readonly ObservableCollection<string> _printers;
    private readonly DropDownList _paper;
    private readonly ObservableCollection<string> _papers;
    private readonly NumericUpDown<int> _from;
    private readonly NumericUpDown<int> _to;

    /// <summary>Creates a printer editor.</summary>
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
        var printerLabel = new Label { X = 0, Y = 0, Width = LabelWidth, Text = "Printer:" };
        _printer = new DropDownList
        {
            X = LabelWidth,
            Y = 0,
            Width = Dim.Fill(),
            Source = new ListWrapper<string>(_printers)
        };

        _papers = new ObservableCollection<string>(paperSizes ?? PrinterChoices.DefaultPaperSizes);
        var paperLabel = new Label { X = 0, Y = 1, Width = LabelWidth, Text = "Paper:" };
        _paper = new DropDownList
        {
            X = LabelWidth,
            Y = 1,
            Width = Dim.Fill(),
            Source = new ListWrapper<string>(_papers)
        };

        // "Pages" (printer-driver terminology): the From/To range the driver is told to print.
        var pagesLabel = new Label { X = 0, Y = 2, Width = LabelWidth, Text = "Pages:" };
        var fromLabel = new Label { X = LabelWidth, Y = 2, Text = "From" };
        _from = new NumericUpDown<int>
        {
            X = Pos.Right(fromLabel) + 1,
            Y = 2,
            Increment = 1,
            Value = MinFrom
        };
        _from.ValueChanging += (_, args) => args.NewValue = Math.Clamp(args.NewValue, MinFrom, MaxPage);

        var toLabel = new Label { X = Pos.Right(_from) + 2, Y = 2, Text = "To" };
        _to = new NumericUpDown<int>
        {
            X = Pos.Right(toLabel) + 1,
            Y = 2,
            Increment = 1,
            Value = 0
        };
        // 0 means "to the end".
        _to.ValueChanging += (_, args) => args.NewValue = Math.Clamp(args.NewValue, 0, MaxPage);

        _printer.ValueChanged += (_, _) => PushFromChildren();
        _paper.ValueChanged += (_, _) => PushFromChildren();
        _from.ValueChanged += (_, _) => PushFromChildren();
        _to.ValueChanged += (_, _) => PushFromChildren();

        Add(printerLabel, _printer, paperLabel, _paper, pagesLabel, fromLabel, _from, toLabel, _to);
    }

    /// <summary>The sheet range (From/To) being edited. Editing From/To mutates this instance.</summary>
    public PageRange Range { get; private set; } = new();

    /// <inheritdoc />
    protected override void OnValueChanged(PrintPageSetup? newValue)
    {
        PrintPageSetup setup = newValue ?? new PrintPageSetup();
        _printer.Value = Ensure(_printers, setup.PrinterName);
        _paper.Value = Ensure(_papers, setup.PaperSizeName);
        RebindRange();
    }

    /// <summary>Binds the sheet range model edited by the From/To fields.</summary>
    public void SetRange(PageRange range)
    {
        Range = range ?? new PageRange();
        RebindRange();
    }

    private void RebindRange()
    {
        Range.From = Math.Clamp(Range.From, MinFrom, MaxPage);
        Range.To = Math.Clamp(Range.To, 0, MaxPage);

        bool wasSuppressing = SuppressChildEcho;
        SuppressChildEcho = true;
        _from.Value = Range.From;
        _to.Value = Range.To;
        SuppressChildEcho = wasSuppressing;
    }

    // The range fields can change independently of Value, so guard their echo separately from the
    // base Suppressing flag (which only covers Value rebinds).
    private bool SuppressChildEcho { get; set; }

    private void PushFromChildren()
    {
        if (Suppressing || SuppressChildEcho)
        {
            return;
        }

        if (Value is not null)
        {
            // PrintPageSetup is mutable; mutate the bound instance directly.
            Value.PrinterName = _printer.Value ?? string.Empty;
            Value.PaperSizeName = _paper.Value ?? string.Empty;
        }

        Range.From = _from.Value;
        Range.To = _to.Value;
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

using System.Collections.ObjectModel;
using Terminal.Gui.Drawing;
using Terminal.Gui.ViewBase;
using Terminal.Gui.Views;
using WinPrint.Core.Abstractions;

namespace WinPrint.TUI.Views.Editors;

/// <summary>
///     Edits the target printer, paper size, and sheet range (From/To).
///     The bound value is a <see cref="PrintPageSetup" />; an optional
///     <see cref="PageRange" /> carries From/To.
/// </summary>
public sealed class PrinterEditor : EditorBase<PrintPageSetup>
{
    private const int MinFrom = 1;
    private const int MaxPage = 9999;

    private const int LabelWidth = EditorMetrics.LabelWidth;

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
        BorderStyle = LineStyle.Rounded;
        Border.Thickness = new Thickness(0, 2, 0, 0);
        SuperViewRendersLineCanvas = true;
        Title = "Printer";

        _printers = new ObservableCollection<string>(printers ?? PrinterChoices.DefaultPrinters);
        _printer = new DropDownList
        {
            Width = Dim.Fill(),
            Source = new ListWrapper<string>(_printers)
        };

        _papers = new ObservableCollection<string>(paperSizes ?? PrinterChoices.DefaultPaperSizes);
        var paperLabel = new Label { Y = Pos.Bottom(_printer), Text = "Paper:" };
        _paper = new DropDownList
        {
            X = Pos.Right(paperLabel) + 1,
            Y = Pos.Top(paperLabel),
            Width = Dim.Fill(),
            Source = new ListWrapper<string>(_papers)
        };

        var pagesLabel = new Label { Y = Pos.Bottom(_paper), Text = "Pages From:" };
        _from = new NumericUpDown<int>
        {
            X = Pos.Right(pagesLabel) + 1,
            Y = Pos.Top(pagesLabel),
            Increment = 1,
            Value = MinFrom
        };
        _from.ValueChanging += (_, args) => args.NewValue = Math.Clamp(args.NewValue, MinFrom, MaxPage);

        var toLabel = new Label { X = Pos.Right(_from) + 2, Y = Pos.Top(pagesLabel), Text = "To" };
        _to = new NumericUpDown<int>
        {
            X = Pos.Right(toLabel) + 1,
            Y = Pos.Top(pagesLabel),
            Increment = 1,
            Value = 0
        };
        _to.ValueChanging += (_, args) => args.NewValue = Math.Clamp(args.NewValue, 0, MaxPage);

        _printer.ValueChanged += (_, _) => PushFromChildren();
        _paper.ValueChanged += (_, _) => PushFromChildren();
        _from.ValueChanged += (_, _) => PushFromChildren();
        _to.ValueChanged += (_, _) => PushFromChildren();

        Add(_printer, paperLabel, _paper, pagesLabel, _from, toLabel, _to);
    }

    /// <summary>The sheet range (From/To) being edited. Editing From/To mutates this instance.</summary>
    public PageRange Range { get; private set; } = new();

    /// <summary>Replaces the printer list with the given names.</summary>
    public void SetPrinters(IEnumerable<string> printers)
    {
        _printers.Clear();
        foreach (string p in printers)
        {
            _printers.Add(p);
        }
    }

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
        SyncRangeToValue();
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

    /// <summary>Propagates the current <see cref="Range"/> back to the bound <see cref="Value"/>.</summary>
    private void SyncRangeToValue()
    {
        if (Value is not null)
        {
            Value.FromSheet = Range.From;
            Value.ToSheet = Range.To;
        }
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
        SyncRangeToValue();
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

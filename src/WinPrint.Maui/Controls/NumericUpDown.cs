using System.Globalization;

namespace WinPrint.Maui.Controls;

/// <summary>
/// A reusable numeric input control: a text entry with up/down (increment/decrement)
/// buttons placed below it. Supports a configurable step, integer-vs-decimal mode,
/// and minimum/maximum bounds. Built only on <c>Microsoft.Maui.Controls</c> primitives
/// (<see cref="Entry"/> + <see cref="Button"/>) so it carries no third-party dependency.
/// </summary>
public class NumericUpDown : ContentView
{
    private readonly Entry _entry;
    private readonly Button _upButton;
    private readonly Button _downButton;

    // Guards against re-entrancy when we push a coerced value back into the Entry.
    private bool _updatingText;

    public NumericUpDown()
    {
        _entry = new Entry
        {
            Keyboard = Keyboard.Numeric,
            HorizontalTextAlignment = TextAlignment.End,
            VerticalOptions = LayoutOptions.Center,
        };
        _entry.Unfocused += OnEntryUnfocused;
        _entry.Completed += OnEntryCompleted;

        _downButton = new Button { Text = "▼", HorizontalOptions = LayoutOptions.Fill }; // ▼
        _upButton = new Button { Text = "▲", HorizontalOptions = LayoutOptions.Fill };   // ▲
        _downButton.Clicked += (_, _) => Decrement();
        _upButton.Clicked += (_, _) => Increment();

        // Two columns of buttons sitting below the entry.
        var buttons = new Grid
        {
            ColumnSpacing = 4,
            ColumnDefinitions =
            {
                new ColumnDefinition { Width = GridLength.Star },
                new ColumnDefinition { Width = GridLength.Star },
            },
        };
        buttons.Add(_downButton, 0, 0);
        buttons.Add(_upButton, 1, 0);

        // Entry on top, buttons below.
        var root = new Grid
        {
            RowSpacing = 4,
            RowDefinitions =
            {
                new RowDefinition { Height = GridLength.Auto },
                new RowDefinition { Height = GridLength.Auto },
            },
        };
        root.Add(_entry, 0, 0);
        root.Add(buttons, 0, 1);

        Content = root;
        UpdateText();
    }

    public static readonly BindableProperty ValueProperty = BindableProperty.Create(
        nameof(Value), typeof(double), typeof(NumericUpDown), 0.0,
        defaultBindingMode: BindingMode.TwoWay,
        propertyChanged: OnValueChanged,
        coerceValue: CoerceValue);

    public static readonly BindableProperty MinimumProperty = BindableProperty.Create(
        nameof(Minimum), typeof(double), typeof(NumericUpDown), double.MinValue,
        propertyChanged: OnBoundsChanged);

    public static readonly BindableProperty MaximumProperty = BindableProperty.Create(
        nameof(Maximum), typeof(double), typeof(NumericUpDown), double.MaxValue,
        propertyChanged: OnBoundsChanged);

    public static readonly BindableProperty IncrementProperty = BindableProperty.Create(
        nameof(Increment), typeof(double), typeof(NumericUpDown), 1.0);

    public static readonly BindableProperty IsIntegerProperty = BindableProperty.Create(
        nameof(IsInteger), typeof(bool), typeof(NumericUpDown), false,
        propertyChanged: OnIsIntegerChanged);

    /// <summary>The current numeric value. Coerced into [<see cref="Minimum"/>, <see cref="Maximum"/>]
    /// and rounded to a whole number when <see cref="IsInteger"/> is set.</summary>
    public double Value
    {
        get => (double)GetValue(ValueProperty);
        set => SetValue(ValueProperty, value);
    }

    /// <summary>Smallest allowed value. Defaults to <see cref="double.MinValue"/>.</summary>
    public double Minimum
    {
        get => (double)GetValue(MinimumProperty);
        set => SetValue(MinimumProperty, value);
    }

    /// <summary>Largest allowed value. Defaults to <see cref="double.MaxValue"/>.</summary>
    public double Maximum
    {
        get => (double)GetValue(MaximumProperty);
        set => SetValue(MaximumProperty, value);
    }

    /// <summary>Amount added or subtracted by the up/down buttons. Defaults to 1.</summary>
    public double Increment
    {
        get => (double)GetValue(IncrementProperty);
        set => SetValue(IncrementProperty, value);
    }

    /// <summary>When true, the value is constrained to whole numbers and displayed without decimals.</summary>
    public bool IsInteger
    {
        get => (bool)GetValue(IsIntegerProperty);
        set => SetValue(IsIntegerProperty, value);
    }

    public void Increment() => Value += Increment;

    public void Decrement() => Value -= Increment;

    private static object CoerceValue(BindableObject bindable, object value)
    {
        var control = (NumericUpDown)bindable;
        var v = (double)value;

        if (control.IsInteger)
        {
            v = Math.Round(v, MidpointRounding.AwayFromZero);
        }

        // Clamp into bounds (guarding against a caller setting Minimum > Maximum).
        var min = control.Minimum;
        var max = control.Maximum;
        if (min <= max)
        {
            v = Math.Clamp(v, min, max);
        }

        return v;
    }

    private static void OnValueChanged(BindableObject bindable, object oldValue, object newValue)
        => ((NumericUpDown)bindable).UpdateText();

    private static void OnBoundsChanged(BindableObject bindable, object oldValue, object newValue)
        => bindable.CoerceValue(ValueProperty); // re-clamp current value against new bounds

    private static void OnIsIntegerChanged(BindableObject bindable, object oldValue, object newValue)
    {
        var control = (NumericUpDown)bindable;
        control.CoerceValue(ValueProperty); // re-round if switching to integer mode
        control.UpdateText();
    }

    private void OnEntryCompleted(object? sender, EventArgs e) => CommitText();

    private void OnEntryUnfocused(object? sender, FocusEventArgs e) => CommitText();

    /// <summary>Parses the text the user typed and pushes it through coercion.</summary>
    private void CommitText()
    {
        if (_updatingText)
        {
            return;
        }

        if (double.TryParse(_entry.Text, NumberStyles.Any, CultureInfo.CurrentCulture, out var parsed))
        {
            Value = parsed; // coercion handles clamping/rounding
        }

        // Whether parse succeeded or not, snap the text back to the canonical value.
        UpdateText();
    }

    private void UpdateText()
    {
        _updatingText = true;
        try
        {
            _entry.Text = IsInteger
                ? Value.ToString("0", CultureInfo.CurrentCulture)
                : Value.ToString(CultureInfo.CurrentCulture);
        }
        finally
        {
            _updatingText = false;
        }
    }
}

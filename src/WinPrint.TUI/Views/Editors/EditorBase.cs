using Terminal.Gui.App;
using Terminal.Gui.ViewBase;

namespace WinPrint.TUI.Views.Editors;

/// <summary>
///     Base class for composed editor views that expose a single strongly-typed value via
///     Terminal.Gui's <see cref="IValue{TValue}" />. Setting <see cref="Value" /> raises the
///     cancelable <see cref="ValueChanging" /> event, then pushes the new model into the child
///     controls (via <see cref="OnValueChanged" />) and raises <see cref="ValueChanged" />.
///     <para>
///         Rebinding to a different model is therefore just <c>editor.Value = newModel</c> — the same
///         mechanism the UI uses for edits. While <see cref="OnValueChanged" /> runs,
///         <see cref="Suppressing" /> is <see langword="true" /> so child-change handlers can avoid
///         echoing the value straight back.
///     </para>
/// </summary>
/// <typeparam name="TValue">The value type the editor edits.</typeparam>
public abstract class EditorBase<TValue> : View, IValue<TValue>
{
    private TValue? _value;

    /// <summary>Initializes the editor as a focusable container so its child controls are keyboard-reachable.</summary>
    protected EditorBase()
    {
        // A View defaults to CanFocus=false, and Terminal.Gui's focus navigation skips a
        // non-focusable container *and all of its subviews* — so the inner controls (spinners, text
        // fields, the header/footer format editor with its macro autocomplete) would be unreachable
        // by keyboard. Mark the container focusable so Tab/focus descends into the child controls.
        CanFocus = true;
    }

    /// <inheritdoc />
    public TValue? Value
    {
        get => _value;
        set => SetValue(value);
    }

    /// <inheritdoc />
    public event EventHandler<ValueChangingEventArgs<TValue?>>? ValueChanging;

    /// <inheritdoc />
    public event EventHandler<ValueChangedEventArgs<TValue?>>? ValueChanged;

    /// <inheritdoc />
    public event EventHandler<ValueChangedEventArgs<object?>>? ValueChangedUntyped;

    /// <summary>
    ///     <see langword="true" /> while the editor is pushing <see cref="Value" /> into its child
    ///     controls. Subclasses check this in child-change handlers to avoid feedback loops.
    /// </summary>
    protected bool Suppressing { get; private set; }

    /// <summary>
    ///     Called after <see cref="Value" /> changes so the subclass can update its child controls to
    ///     reflect <paramref name="newValue" />.
    /// </summary>
    protected abstract void OnValueChanged(TValue? newValue);

    private void SetValue(TValue? newValue)
    {
        TValue? oldValue = _value;

        if (EqualityComparer<TValue?>.Default.Equals(oldValue, newValue))
        {
            return;
        }

        ValueChangingEventArgs<TValue?> changing = new(oldValue, newValue);
        ValueChanging?.Invoke(this, changing);

        if (changing.Handled)
        {
            return;
        }

        _value = changing.NewValue;

        Suppressing = true;

        try
        {
            OnValueChanged(_value);
        }
        finally
        {
            Suppressing = false;
        }

        ValueChanged?.Invoke(this, new ValueChangedEventArgs<TValue?>(oldValue, _value));
        ValueChangedUntyped?.Invoke(this, new ValueChangedEventArgs<object?>(oldValue, _value));
    }
}

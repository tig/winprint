using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace WinPrint.Core.Models;

public abstract class ModelBase : INotifyPropertyChanged
{
    public event PropertyChangedEventHandler? PropertyChanged;

    protected void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }

    protected bool SetField<T>(ref T field, T value, [CallerMemberName] string? propertyName = null)
    {
        if (EqualityComparer<T>.Default.Equals(field, value))
        {
            return false;
        }

        field = value;
        OnPropertyChanged(propertyName);
        return true;
    }

    public override string ToString()
    {
        return this switch
        {
            Settings settings => Serialization.WinPrintJson.SerializeSettings(settings),
            SheetSettings sheetSettings => Serialization.WinPrintJson.SerializeSheetSettings(sheetSettings),
            _ => base.ToString() ?? GetType().Name
        };
    }

    /// <summary>
    ///     Returns a dictionary of telemetry-safe values for this model. Derived types override and
    ///     call <see cref="TelemetryCollector" /> helpers to emit explicit fields (no reflection).
    /// </summary>
    /// <returns>A dictionary with the properties and values (as strings). Suitable for calling TrackEvent().</returns>
    public virtual IDictionary<string, string?> GetTelemetryDictionary()
    {
        return TelemetryCollector.Create();
    }

    /// <summary>
    ///     System.Text.Json does not support copying a deserialized object to an existing instance.
    ///     To work around this, ModelBase implements a 'deep, memberwise clone' method.
    ///     `Named CopyPropertiesFrom` to make it clear what it does.
    /// </summary>
    public virtual void CopyPropertiesFrom(ModelBase? source)
    {
    }
}

using System.ComponentModel;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text.Json;

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
        return JsonSerializer.Serialize(this, GetType());
        //return base.ToString();
    }

    /// <summary>
    ///     Returns a dictionary containing all the properties of the object that
    ///     are safe to track via telemetry. Use the [SafeForTelemetry] attribute on any
    ///     property of a class derived from ModelBase to enable emitting to telemetry.
    /// </summary>
    /// <returns>A dictionary with the properties and values (as strings). Suitable for calling TrackEvent().</returns>
    public virtual IDictionary<string, string?> GetTelemetryDictionary()
    {
        var dictionary = new Dictionary<string, string?>();
        foreach (PropertyDescriptor property in TypeDescriptor.GetProperties(this))
        {
            if (property.Attributes.Contains(new SafeForTelemetry()))
            {
                object? value = property.GetValue(this);
                if (value != null)
                {
                    if (property.PropertyType.IsSubclassOf(typeof(ModelBase)))
                    {
                        // Go deep
                        IDictionary<string, string?> propDict = ((ModelBase)value).GetTelemetryDictionary();
                        dictionary.Add(property.Name, JsonSerializer.Serialize(propDict, propDict.GetType()));
                    }
                    else
                    {
                        dictionary.Add(property.Name, JsonSerializer.Serialize(value, value.GetType()));
                    }
                }
            }
        }

        return dictionary;
    }

    /// <summary>
    ///     System.Text.Json does not support copying a deserialized object to an existing instance.
    ///     To work around this, ModelBase implements a 'deep, memberwise clone' method.
    ///     `Named CopyPropertiesFrom` to make it clear what it does.
    ///     TODO: When System.Text.Json implements `PopulateObject` revisit
    ///     https://github.com/dotnet/corefx/issues/37627
    /// </summary>
    /// <param name="source"></param>
    public virtual void CopyPropertiesFrom(ModelBase? source)
    {
        if (source is null)
        {
            return;
        }

        var sourceProps = source.GetType().GetProperties().Where(x => x.CanRead).ToList();
        var destProps = GetType().GetProperties().Where(x => x.CanWrite).ToList();
        foreach ((PropertyInfo? sourceProp, PropertyInfo? destProp) in
                 // check if the property can be set or no.
                 from sourceProp in sourceProps
                 where destProps.Any(x => x.Name == sourceProp.Name)
                 let destProp = destProps.First(x => x.Name == sourceProp.Name)
                 where destProp.CanWrite
                 select (sourceProp, destProp))
        {
            // "System.Collections.Generic.IList`
            if (sourceProp.Name != "Sheets")
            {
                if (sourceProp.PropertyType.IsSubclassOf(typeof(ModelBase)))
                {
                    // Property is subclass of ModelBase - Recurse through sub-objects
                    if (sourceProp.GetValue(source, null) is ModelBase sourceValue)
                    {
                        if (destProp.GetValue(this) is not ModelBase destValue)
                        {
                            // Destination is null. Create it.
                            destValue = (ModelBase)Activator.CreateInstance(destProp.PropertyType)!;
                            destProp.SetValue(this, destValue);
                        }

                        destValue.CopyPropertiesFrom(sourceValue);
                    }
                    else
                    {
                        destProp.SetValue(this, null);
                    }
                }
                else
                {
                    destProp.SetValue(this, sourceProp.GetValue(source, null), null);
                }
            }
            else
            {
                Dictionary<string, SheetSettings> sourceList =
                    sourceProp.GetValue(source) as Dictionary<string, SheetSettings> ??
                    [];

                Dictionary<string, SheetSettings> destList =
                    destProp.GetValue(this) as Dictionary<string, SheetSettings> ??
                    [];

                foreach (KeyValuePair<string, SheetSettings> src in sourceList)
                {
                    if (destList.ContainsKey(src.Key))
                    {
                        destList[src.Key].CopyPropertiesFrom(src.Value);
                    }
                    else
                    {
                        destList[src.Key] = new SheetSettings();
                        destList[src.Key].CopyPropertiesFrom(src.Value);
                    }
                }

                if (destProp.GetValue(this) is null)
                {
                    destProp.SetValue(this, destList, null);
                }
            }
        }
    }
}

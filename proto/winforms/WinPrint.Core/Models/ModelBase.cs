using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using GalaSoft.MvvmLight;

namespace WinPrint.Core.Models {
    public abstract class ModelBase : INotifyPropertyChanged {
        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string propertyName = null) =>
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));

        protected bool SetField<T>(ref T field, T value, [CallerMemberName] string propertyName = null) {
            if (EqualityComparer<T>.Default.Equals(field, value)) return false;
            field = value;
            OnPropertyChanged(propertyName);
            return true;
        }

        /// <summary>
        /// System.Text.Json does not support copying a deserialized object to an existing instance.
        /// To work around this, ModelBase implements a 'deep, memberwise clone' method. 
        /// `Named CopyPropertiesFrom` to make it clear what it does. 
        /// TOOD: When System.Text.Json implements `PopulateObject` revisit
        /// https://github.com/dotnet/corefx/issues/37627
        /// </summary>
        /// <param name="source"></param>
        public virtual void CopyPropertiesFrom(ModelBase source) {
            var sourceProps = source.GetType().GetProperties().Where(x => x.CanRead).ToList();
            var destProps = this.GetType().GetProperties().Where(x => x.CanWrite).ToList();
            foreach (var (sourceProp, destProp) in
            // check if the property can be set or no.
            from sourceProp in sourceProps
            where destProps.Any(x => x.Name == sourceProp.Name)
            let destProp = destProps.First(x => x.Name == sourceProp.Name)
            where destProp.CanWrite
            select (sourceProp, destProp)) {
                // "System.Collections.Generic.IList`
                if (sourceProp.Name != "Sheets") {
                    if (sourceProp.PropertyType.IsSubclassOf(typeof(ModelBase)))
                        // Property is subclass of ModelBase - Recurse through sub-objects
                        ((ModelBase)destProp.GetValue(this)).CopyPropertiesFrom((ModelBase)sourceProp.GetValue(source, null));
                    else
                        destProp.SetValue(this, sourceProp.GetValue(source, null), null);
                }
                else {
                    IList<Sheet> sourceList = (IList<Sheet>)sourceProp.GetValue(source);
                    IList<Sheet> destList = (IList<Sheet>)destProp.GetValue(this);
                    // Copy list item by item. If source ha more than dest, ...
                    for (int i = 0; i < sourceList.Count; i++) {
                        if (i > destList.Count - 1) destList.Add(new Sheet());
                        destList[i].CopyPropertiesFrom((Sheet)sourceList[i]);
                    }
                }
            }
        }
    }
}

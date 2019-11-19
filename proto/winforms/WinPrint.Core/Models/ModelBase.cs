using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using GalaSoft.MvvmLight;

namespace WinPrint.Core.Models {
    public abstract class ModelBase : ObservableObject {

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
                if (sourceProp.PropertyType.IsSubclassOf(typeof(ModelBase)))
                    // Property is subclass of ModelBase - Recurse through sub-objects
                    ((ModelBase)destProp.GetValue(this)).CopyPropertiesFrom((ModelBase)sourceProp.GetValue(source, null));
                else
                    destProp.SetValue(this, sourceProp.GetValue(source, null), null);
            }
        }
    }
}

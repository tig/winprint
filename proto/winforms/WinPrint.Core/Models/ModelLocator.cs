using System;

using GalaSoft.MvvmLight.Ioc;
using WinPrint.Core.Services;

//using WinPrint.Services;
//using WinPrint.Views;

namespace WinPrint.Core.Models {
    public class ModelLocator {
        private static ModelLocator _current;

        public static ModelLocator Current => _current ?? (_current = new ModelLocator());

        private ModelLocator() {

            // Register the Document model via the SettingsService Factory
            SimpleIoc.Default.Register<Document>(SettingsService.Create);
            //SimpleIoc.Default.Register<WinPrint.Core.Models.Header>();
            //SimpleIoc.Default.Register<WinPrint.Core.Models.Footer>();

        }

        public Models.Document Document => SimpleIoc.Default.GetInstance<Models.Document>();

        public void Register<VM, V>()
            where VM : class {
            SimpleIoc.Default.Register<VM>();
        }

        
    }
}

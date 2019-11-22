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
            SimpleIoc.Default.Register<Settings>(SettingsService.Create);
            SimpleIoc.Default.Register<Options>();
        }

        public Models.Settings Settings => SimpleIoc.Default.GetInstance<Models.Settings>();
        public Models.Options Options => SimpleIoc.Default.GetInstance<Models.Options>();

        public void Register<VM, V>()
            where VM : class {
            SimpleIoc.Default.Register<VM>();
        }

        
    }
}

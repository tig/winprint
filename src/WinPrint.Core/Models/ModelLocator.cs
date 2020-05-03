
using GalaSoft.MvvmLight.Ioc;
using WinPrint.Core.Services;

//using WinPrint.Services;
//using WinPrint.Views;

namespace WinPrint.Core.Models {
    public class ModelLocator {
        private static ModelLocator _current;

        public static ModelLocator Current => _current ?? (_current = new ModelLocator());

        private ModelLocator() {
            // Register the models via the Servcies Factory
            SimpleIoc.Default.Register<Settings>(SettingsService.Create);
            SimpleIoc.Default.Register<FileTypeMapping>(FileTypeMappingService.Create);
            SimpleIoc.Default.Register<Options>();
        }

        public Models.Settings Settings => SimpleIoc.Default.GetInstance<Models.Settings>();

        public Models.Options Options => SimpleIoc.Default.GetInstance<Models.Options>();
        public Models.FileTypeMapping FileTypeMapping => SimpleIoc.Default.GetInstance<Models.FileTypeMapping>();

        public void Register<VM, V>()
            where VM : class {
            SimpleIoc.Default.Register<VM>();
        }

        public static void Reset() {
            _current = null;
            SimpleIoc.Default.Unregister<Settings>();
            SimpleIoc.Default.Unregister<FileTypeMapping>();
            SimpleIoc.Default.Unregister<Options>();
        }
    }
}

using System;

using GalaSoft.MvvmLight.Ioc;

//using WinPrint.Services;
//using WinPrint.Views;

namespace WinPrint.Core.Services {
    public class ServiceLocator {
        private static ServiceLocator _current;

        public static ServiceLocator Current => _current ?? (_current = new ServiceLocator());

        private ServiceLocator() {
            SimpleIoc.Default.Register<WinPrint.Core.Services.SettingsService>();
            SimpleIoc.Default.Register<WinPrint.Core.Services.FileAssociationsService>();
        }

        public SettingsService SettingsService => SimpleIoc.Default.GetInstance<Services.SettingsService>();
        public FileAssociationsService FileAssociationsService => SimpleIoc.Default.GetInstance<Services.FileAssociationsService>();

        public void Register<VM, V>()
            where VM : class {
            SimpleIoc.Default.Register<VM>();
        }
    }
}

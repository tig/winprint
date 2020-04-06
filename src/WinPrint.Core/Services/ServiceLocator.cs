﻿using GalaSoft.MvvmLight.Ioc;

namespace WinPrint {
    //public class Logger {
    //    static WinPrint.Core.Services.LogService Log = ServiceLocator.Current.LogService;
    //}
}

namespace WinPrint.Core.Services {
    public class ServiceLocator {
        private static ServiceLocator _current;

        public static ServiceLocator Current => _current ?? (_current = new ServiceLocator());

        private ServiceLocator() {
            SimpleIoc.Default.Register<SettingsService>();
            SimpleIoc.Default.Register<FileAssociationsService>();
            SimpleIoc.Default.Register<NodeService>();
            SimpleIoc.Default.Register<LogService>(true);
            SimpleIoc.Default.Register<TelemetryService>(true);
            SimpleIoc.Default.Register<UpdateService>();
        }

        public LogService LogService => SimpleIoc.Default.GetInstance<Services.LogService>();
        public TelemetryService TelemetryService => SimpleIoc.Default.GetInstance<Services.TelemetryService>();
        public SettingsService SettingsService => SimpleIoc.Default.GetInstance<Services.SettingsService>();
        public FileAssociationsService FileAssociationsService => SimpleIoc.Default.GetInstance<Services.FileAssociationsService>();
        public NodeService NodeService => SimpleIoc.Default.GetInstance<Services.NodeService>();
        public UpdateService UpdateService => SimpleIoc.Default.GetInstance<UpdateService>();

        public void Register<VM, V>()
            where VM : class {
            SimpleIoc.Default.Register<VM>();
        }

        public static void Reset() {
            _current = null;
            SimpleIoc.Default.Unregister<SettingsService>();
            SimpleIoc.Default.Unregister<FileAssociationsService>();
            SimpleIoc.Default.Unregister<NodeService>();
            SimpleIoc.Default.Unregister<LogService>();
            SimpleIoc.Default.Unregister<TelemetryService>();
            SimpleIoc.Default.Unregister<UpdateService>();
        }
    }
}

using GalaSoft.MvvmLight.Ioc;
using WinPrint.Core.Services;

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
            SimpleIoc.Default.Register<UpdateService>();
        }

        public LogService LogService => SimpleIoc.Default.GetInstance<Services.LogService>();
        public SettingsService SettingsService => SimpleIoc.Default.GetInstance<Services.SettingsService>();
        public FileAssociationsService FileAssociationsService => SimpleIoc.Default.GetInstance<Services.FileAssociationsService>();
        public NodeService NodeService => SimpleIoc.Default.GetInstance<Services.NodeService>();

        public UpdateService UpdateService => SimpleIoc.Default.GetInstance<UpdateService>();

        public void Register<VM, V>()
            where VM : class {
            SimpleIoc.Default.Register<VM>();
        }
    }
}

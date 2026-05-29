using GalaSoft.MvvmLight.Ioc;

namespace WinPrint.Core.Services;

public class ServiceLocator
{
    private static ServiceLocator? _current;

    private ServiceLocator()
    {
        SimpleIoc.Default.Register<SettingsService>();
        SimpleIoc.Default.Register<FileTypeMappingService>();
        SimpleIoc.Default.Register<LogService>(true);
        SimpleIoc.Default.Register<TelemetryService>(true);
        SimpleIoc.Default.Register<UpdateService>();
        SimpleIoc.Default.Register<PygmentsConverterService>();
    }

    public static ServiceLocator Current => _current ??= new ServiceLocator();

    public LogService LogService => SimpleIoc.Default.GetInstance<LogService>();
    public TelemetryService TelemetryService => SimpleIoc.Default.GetInstance<TelemetryService>();
    public SettingsService SettingsService => SimpleIoc.Default.GetInstance<SettingsService>();
    public FileTypeMappingService FileTypeMappingService => SimpleIoc.Default.GetInstance<FileTypeMappingService>();
    public UpdateService UpdateService => SimpleIoc.Default.GetInstance<UpdateService>();

    public PygmentsConverterService PygmentsConverterService =>
        SimpleIoc.Default.GetInstance<PygmentsConverterService>();

    public void Register<VM, V>()
        where VM : class
    {
        SimpleIoc.Default.Register<VM>();
    }

    public static void Reset()
    {
        _current = null;
        SimpleIoc.Default.Unregister<SettingsService>();
        SimpleIoc.Default.Unregister<FileTypeMappingService>();
        SimpleIoc.Default.Unregister<LogService>();
        SimpleIoc.Default.Unregister<TelemetryService>();
        SimpleIoc.Default.Unregister<UpdateService>();
        SimpleIoc.Default.Unregister<PygmentsConverterService>();
    }
}

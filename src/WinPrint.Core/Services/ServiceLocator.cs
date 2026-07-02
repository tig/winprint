namespace WinPrint.Core.Services;

public class ServiceLocator
{
    private static ServiceLocator? s_current;

    private ServiceLocator()
    {
    }

    public static ServiceLocator Current => s_current ??= new ServiceLocator();

    private static WinPrintServices Services => WinPrintServices.Current;

    public LogService LogService => Services.LogService;

    public TelemetryService TelemetryService => Services.TelemetryService;

    public SettingsService SettingsService => Services.SettingsService;

    public FileTypeMappingService FileTypeMappingService => Services.FileTypeMappingService;

    public UpdateService UpdateService => Services.UpdateService;

    public IFontEnumerationService FontEnumerationService => Services.FontEnumerationService;

    public static void Reset()
    {
        s_current = null;
        WinPrintServices.Reset();
    }
}

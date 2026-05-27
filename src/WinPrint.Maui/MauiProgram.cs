using Microsoft.Extensions.Logging;
#if WINDOWS
using WinPrint.Core.Models;
using WinPrint.Core.Services;
#endif

namespace WinPrint.Maui;

public static class MauiProgram
{
    public static MauiApp CreateMauiApp ()
    {
        MauiAppBuilder builder = MauiApp.CreateBuilder ();
        builder
            .UseMauiApp<App> ()
            .ConfigureFonts (fonts =>
            {
                fonts.AddFont ("OpenSans-Regular.ttf", "OpenSansRegular");
                fonts.AddFont ("OpenSans-Semibold.ttf", "OpenSansSemibold");
            });

        builder.Services.AddSingleton<AppShell> ();
        builder.Services.AddSingleton<MainPage> ();
#if WINDOWS
        builder.Services.AddSingleton (_ => ServiceLocator.Current);
        builder.Services.AddSingleton (_ => ModelLocator.Current!);
#endif

#if DEBUG
        builder.Logging.AddDebug ();
#endif

        return builder.Build ();
    }
}

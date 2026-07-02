using UIKit;

namespace WinPrint.Maui;

public class Program
{
    // This is the main entry point of the application.
    private static void Main(string[] args)
    {
        // The Catalyst startup code chdirs to the .app bundle before managed Main
        // runs, so Directory.GetCurrentDirectory() never sees the directory the
        // command was typed in. liblaunchcwd.dylib (a dyld constructor, which runs
        // before that chdir) records the true launch directory in WINPRINT_LAUNCH_CWD.
        // PWD is only a fallback: POSIX shells maintain it, but pwsh does not.
        MauiProgram.LaunchCwd = Environment.GetEnvironmentVariable("WINPRINT_LAUNCH_CWD")
                                ?? Environment.GetEnvironmentVariable("PWD")
                                ?? Directory.GetCurrentDirectory();

        // if you want to use a different Application Delegate class from "AppDelegate"
        // you can specify it here.
        UIApplication.Main(args, null, typeof(AppDelegate));
    }
}

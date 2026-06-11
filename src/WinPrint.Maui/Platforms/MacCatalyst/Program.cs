using UIKit;

namespace WinPrint.Maui;

public class Program
{
    // This is the main entry point of the application.
    private static void Main(string[] args)
    {
        // The Catalyst host chdirs to the .app bundle before managed Main even runs,
        // so Directory.GetCurrentDirectory() is useless for resolving relative file
        // arguments. The PWD environment variable still holds the shell's invocation
        // directory (chdir doesn't rewrite it).
        MauiProgram.LaunchCwd =
            Environment.GetEnvironmentVariable("PWD") ?? Directory.GetCurrentDirectory();

        // if you want to use a different Application Delegate class from "AppDelegate"
        // you can specify it here.
        UIApplication.Main(args, null, typeof(AppDelegate));
    }
}

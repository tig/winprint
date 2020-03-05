using System;
using System.Threading.Tasks;
using System.Windows.Forms;
using CommandLine;
using CommandLine.Text;
using Serilog;
using Serilog.Events;
using WinPrint.Core.Models;
using WinPrint.Core.Services;

namespace WinPrint.Winforms {
    static class Program {
        /// <summary>
        ///  The main entry point for the application.
        /// </summary>
        [STAThread]
        static void Main(string[] args) {
            ServiceLocator.Current.LogService.Start(SettingsService.SettingsPath);

            AppDomain.CurrentDomain.UnhandledException += CurrentDomain_UnhandledException;
            TaskScheduler.UnobservedTaskException += TaskScheduler_UnobservedTaskException;

            //var settings = new CefSettings();
            //settings.BrowserSubprocessPath = @"x86\CefSharp.BrowserSubprocess.exe";
            //Cef.Initialize(settings, performDependencyCheck: false, browserProcessHandler: null);

            if (args.Length > 0) {
                var parser = new Parser(with => with.EnableDashDash = true);
                var result = parser.ParseArguments<Options>(args);
                result
                    .WithParsed<Options>(o => {
                        if (o.Debug) {
                            ServiceLocator.Current.LogService.ConsoleLevelSwitch.MinimumLevel = LogEventLevel.Debug;
                            ServiceLocator.Current.LogService.MasterLevelSwitch.MinimumLevel = LogEventLevel.Debug;
                        }
                        else {
                            ServiceLocator.Current.LogService.ConsoleLevelSwitch.MinimumLevel = LogEventLevel.Information;
                        }
                        ServiceLocator.Current.LogService.TrackEvent("Command Line Options", properties: o.GetTelemetryDictionary());
                        Log.Information("Command Line: {cmd}", Parser.Default.FormatCommandLine(o));
                        ModelLocator.Current.Options.CopyPropertiesFrom(o);
                    })
                    .WithNotParsed((errs) => DisplayHelp(result));
                parser.Dispose();
            }

            Application.SetHighDpiMode(HighDpiMode.SystemAware);
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);

#pragma warning disable CA2000 // Dispose objects before losing scope
            Application.Run(new MainWindow());
#pragma warning restore CA2000 // Dispose objects before losing scope
        }

        static void DisplayHelp<T>(ParserResult<T> result) {
            var helpText = HelpText.AutoBuild(result, h => {
                h.AutoHelp = true;
                h.AutoVersion = true;
             
                //h.AddPostOptionsLine("Files\tOne or more filenames of files to be printed.");
                return HelpText.DefaultParsingErrorsHandler(result, h);
            }, e => e);
            MessageBox.Show(helpText);
            Environment.Exit(0);
        }

        private static void TaskScheduler_UnobservedTaskException(object sender, UnobservedTaskExceptionEventArgs e) {
            ServiceLocator.Current.LogService.TrackException(e.Exception);
        }

        private static void CurrentDomain_UnhandledException(object sender, UnhandledExceptionEventArgs e) {
            var ex = e.ExceptionObject as Exception;
            ServiceLocator.Current.LogService.TrackException(ex);
        }
    }
}

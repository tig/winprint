// Copyright Kindel Systems, LLC - http://www.kindel.com
// Published under the MIT License at https://github.com/tig/winprint

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
    internal static class Program {
        /// <summary>
        ///  The main entry point for the winprint GUI application.
        /// </summary>
        [STAThread]
        private static void Main(string[] args) {
            ServiceLocator.Current.TelemetryService.Start(AppDomain.CurrentDomain.FriendlyName);
            ServiceLocator.Current.LogService.Start(AppDomain.CurrentDomain.FriendlyName);

            AppDomain.CurrentDomain.UnhandledException += CurrentDomain_UnhandledException;
            TaskScheduler.UnobservedTaskException += TaskScheduler_UnobservedTaskException;

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
                        ServiceLocator.Current.TelemetryService.TrackEvent("Command Line Options", properties: o.GetTelemetryDictionary());
                        Log.Information("Command Line: {cmd}", Parser.Default.FormatCommandLine(o));
                        ModelLocator.Current.Options.CopyPropertiesFrom(o);
                    })
                    .WithNotParsed((errs) => DisplayHelp(result));
                parser.Dispose();
            }

            Application.SetHighDpiMode(HighDpiMode.SystemAware);
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);

            var main = new MainWindow();
            Application.Run(main);
            main.Dispose();

            ServiceLocator.Current.TelemetryService.Stop();
        }

        private static void DisplayHelp<T>(ParserResult<T> result) {
            var helpText = HelpText.AutoBuild(result, h => {
                h.AutoHelp = true;
                h.AutoVersion = true;

                //h.AddPostOptionsLine("Files\tOne or more filenames of files to be printed.");
                return HelpText.DefaultParsingErrorsHandler(result, h);
            }, e => e);
            MessageBox.Show(helpText);
            ServiceLocator.Current.TelemetryService.TrackEvent("Display Help");
            Environment.Exit(0);
        }

        private static void TaskScheduler_UnobservedTaskException(object sender, UnobservedTaskExceptionEventArgs e) {
            ServiceLocator.Current.TelemetryService.TrackException(e.Exception);
        }

        private static void CurrentDomain_UnhandledException(object sender, UnhandledExceptionEventArgs e) {
            var ex = e.ExceptionObject as Exception;
            ServiceLocator.Current.TelemetryService.TrackException(ex);
        }
    }
}

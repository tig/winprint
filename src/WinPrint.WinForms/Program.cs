// Copyright Kindel, LLC - http://www.kindel.com
// Published under the MIT License at https://github.com/tig/winprint

using System;
using System.Diagnostics;
using System.Threading.Tasks;
using System.Windows.Forms;
using CommandLine;
using CommandLine.Text;
using Serilog;
using Serilog.Events;
using WinPrint.Core.Models;
using WinPrint.Core.Services;
using WinPrint.WinForms;

namespace WinPrint.Winforms {
    internal static class Program {
        /// <summary>
        ///  The main entry point for the winprint GUI application.
        /// </summary>
        [STAThread]
        private static void Main(string[] args) {
            Application.SetHighDpiMode(HighDpiMode.SystemAware);
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);

            ServiceLocator.Current.TelemetryService.Start(AppDomain.CurrentDomain.FriendlyName);

            AppDomain.CurrentDomain.UnhandledException += CurrentDomain_UnhandledException;
            TaskScheduler.UnobservedTaskException += TaskScheduler_UnobservedTaskException;

            bool debug = false;

            var main = new MainWindow();
            GuiLogSink.Instance.OutputWindow = main;

            if (args.Length > 0) {
                var parser = new Parser(with => with.EnableDashDash = true);
                var result = parser.ParseArguments<Options>(args);
                result
                    .WithParsed<Options>(o => {
                        if (o.Debug) {
                            ServiceLocator.Current.LogService.Start(AppDomain.CurrentDomain.FriendlyName, GuiLogSink.Instance, debug: true, verbose: true);
                            ServiceLocator.Current.LogService.ConsoleLevelSwitch.MinimumLevel = LogEventLevel.Debug;
                            ServiceLocator.Current.LogService.MasterLevelSwitch.MinimumLevel = LogEventLevel.Debug;
                        }
                        else {
                            ServiceLocator.Current.LogService.Start(AppDomain.CurrentDomain.FriendlyName, GuiLogSink.Instance, debug: false, verbose: true);
                            ServiceLocator.Current.LogService.ConsoleLevelSwitch.MinimumLevel = LogEventLevel.Information;
                        }
                        ServiceLocator.Current.TelemetryService.TrackEvent("Command Line Options", properties: o.GetTelemetryDictionary());
                        Log.Information("Command Line: {cmd}", Parser.Default.FormatCommandLine(o));
                        ModelLocator.Current.Options.CopyPropertiesFrom(o);
                    })
                    .WithNotParsed((errs) => DisplayHelp(result));
                parser.Dispose();
            }

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

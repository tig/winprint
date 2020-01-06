using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
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
            ServiceLocator.Current.LogService.Start(Path.GetDirectoryName(Assembly.GetExecutingAssembly().CodeBase));

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
                        Log.Information("Command Line: {cmd}", Parser.Default.FormatCommandLine(o));
                        ModelLocator.Current.Options.CopyPropertiesFrom(o);
                    })
                    .WithNotParsed((errs) => DisplayHelp(result, errs));
                parser.Dispose();
            }

            Application.SetHighDpiMode(HighDpiMode.SystemAware);
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);

#pragma warning disable CA2000 // Dispose objects before losing scope
            Application.Run(new MainWindow());
#pragma warning restore CA2000 // Dispose objects before losing scope
        }

        static void DisplayHelp<T>(ParserResult<T> result, IEnumerable<Error> errs) {
            var helpText = HelpText.AutoBuild(result, h => {
                h.AutoHelp = true;
                h.AutoVersion = true;
                //h.AddPostOptionsLine("Files\tOne or more filenames of files to be printed.");
                return HelpText.DefaultParsingErrorsHandler(result, h);
            }, e => e);
            MessageBox.Show(helpText);
            Environment.Exit(0);
        }
    }
}

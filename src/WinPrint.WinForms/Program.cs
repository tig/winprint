using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Forms;
using CommandLine;
using CommandLine.Text;
using WinPrint.Core.Models;

namespace WinPrint.Winforms {
    static class Program {
        /// <summary>
        ///  The main entry point for the application.
        /// </summary>
        [STAThread]
        static void Main(string[] args) {
            //var settings = new CefSettings();
            //settings.BrowserSubprocessPath = @"x86\CefSharp.BrowserSubprocess.exe";
            //Cef.Initialize(settings, performDependencyCheck: false, browserProcessHandler: null);

            if (args.Length > 0) {
                var parser = new Parser(with => with.EnableDashDash = true);
                var result = parser.ParseArguments<Options>(args);
                result
                    .WithParsed<Options>(o => {
                        // copy Files
                        ModelLocator.Current.Options.Files = o.Files.ToList();
                        ModelLocator.Current.Options.Landscape = o.Landscape;
                        ModelLocator.Current.Options.Printer = o.Printer;
                        ModelLocator.Current.Options.PaperSize = o.PaperSize;
                        ModelLocator.Current.Options.Gui = o.Gui;
                        // TODO: Add other command line options supported by command line version
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
            System.Environment.Exit(0);
        }
    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Forms;
using CommandLine;
using CommandLine.Text;
using WinPrint.Core.Models;
using WinPrint.Core.Services;

namespace WinPrint {
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
                        WinPrintServices.Current.Options.Files = o.Files.ToList();
                        WinPrintServices.Current.Options.Landscape = o.Landscape;
                        WinPrintServices.Current.Options.Printer = o.Printer;
                        WinPrintServices.Current.Options.PaperSize = o.PaperSize;
                        WinPrintServices.Current.Options.Gui = o.Gui;
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
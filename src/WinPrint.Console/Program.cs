using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CommandLine;
using CommandLine.Text;
using WinPrint.Core;
using WinPrint.Core.Models;

/// <summary>
/// Implements the WinPrint console/command line app. 
/// </summary>
namespace WinPrintConsole {
    class Program {
        private Print print;
        private Options options;
        private string cmdLine;
        private static ParserResult<Options> result;

        static void Main(string[] args) {
            var program = new Program();

            var parser = new Parser(with => {
                with.EnableDashDash = true;
                with.HelpWriter = null;
            });

            result = parser.ParseArguments<Options>(args);
            result
                .WithParsed(opts => {
                    var sb = new StringBuilder();
                    foreach (var s in args)
                        sb.Append($"{s} ");
                    program.cmdLine = sb.ToString();
                    program.options = opts;
                    Task.WaitAll(program.Go());
                })
                .WithNotParsed((errs) => DisplayHelp(result, errs));

            parser.Dispose();
            if (program.options != null & program.options.Verbose)
                Console.WriteLine("Exiting sucessfully.");
            WinPrint.Core.Helpers.Logging.TraceMessage($"System.Environment.Exit(0)");
            System.Environment.Exit(0);
        }

        private async Task Go() {
            print = new Print();

            print.PrintingPage += (s, pageNum) => Console.WriteLine($"Printing page {pageNum}...");

            print.SheetViewModel.PropertyChanged += PropertyChangedEventHandler;
            print.SheetViewModel.SettingsChanged += SettingsChangedEventHandler;

            print.SheetViewModel.Reflowed += SheetViewModel_Reflowed;

            // -g
            if (options.Gui) {
                // TODO Spawn WinPrint GUI App with args
                if (options.Verbose)
                    Console.WriteLine($"Starting WinPrint GUI App...");
                ProcessStartInfo psi = new ProcessStartInfo();
                psi.UseShellExecute = false;   // This is important
                psi.CreateNoWindow = true;     // This is what hides the command window.
                psi.FileName = @"winprintgui.exe";
                psi.Arguments = cmdLine;
                using (var gui = Process.Start(psi)) {
                    System.Environment.Exit(0);
                }
                Console.WriteLine($"Something went wrong.");
            }

            try {
                // --s
                string sheetID;
                Sheet sheet = print.SheetViewModel.FindSheet(options.Sheet, out sheetID);

                // --l and --o
                if (options.Landscape) sheet.Landscape = true;
                if (options.Portrait) sheet.Landscape = false;

                // --p
                print.SetPrinter(options.Printer);

                // --z
                print.SetPaperSize(options.PaperSize);

                // --v
                if (options.Verbose) {
                    Console.WriteLine($"Printing {options.Files.ToList().FirstOrDefault()}");
                    Console.WriteLine($"    Printer: {print.PrintDocument.PrinterSettings.PrinterName}");
                    Console.WriteLine($"    Paper Size: {print.PrintDocument.DefaultPageSettings.PaperSize.PaperName}");
                    string s = print.PrintDocument.DefaultPageSettings.Landscape ? $"Landscape" : $"Portrait";
                    Console.WriteLine($"    Orientation: {s}");
                    Console.WriteLine($"    Sheet Definition: {sheet.Name} ({sheetID})");
                }

                // Must set landsacpe after printer/paper selection
                print.PrintDocument.DefaultPageSettings.Landscape = sheet.Landscape;
                WinPrint.Core.Helpers.Logging.TraceMessage($"Calling SetSheet");
                print.SheetViewModel.SetSheet(sheet);

                int pagesCounted = 0;
                foreach (var file in options.Files.ToList()) {
                    WinPrint.Core.Helpers.Logging.TraceMessage($"awaiting LoadAsync {file}");
                    var type = await print.SheetViewModel.LoadAsync(file).ConfigureAwait(false);
                    WinPrint.Core.Helpers.Logging.TraceMessage($"back from LoadAsync. Type is {type}");

                    // --c
                    if (options.CountPages) {
                        int n = 0;
                        pagesCounted += n = await print.CountPages(fromSheet: options.FromPage, toSheet: options.ToPage);
                        if (options.Verbose)
                            Console.WriteLine($"Would print {n} pages of {file}.");
                    }
                    else {
                        bool pageRangeSet = false;
                        if (options.FromPage != 0) {
                            print.PrintDocument.PrinterSettings.FromPage = options.FromPage;
                            pageRangeSet = true;
                        }

                        if (options.ToPage != 0) {
                            print.PrintDocument.PrinterSettings.ToPage = options.ToPage;
                            pageRangeSet = true;
                        }

                        if (pageRangeSet)
                            Console.Write($"Printing from page {print.PrintDocument.PrinterSettings.FromPage} to page {print.PrintDocument.PrinterSettings.ToPage}");
                        else
                            Console.Write($"Printing all pages");
                        Console.WriteLine($" on {print.PrintDocument.PrinterSettings.PrinterName}");
                        await print.DoPrint();
                    }
                }
                if (options.Verbose) {
                    if (options.CountPages)
                        Console.WriteLine($"Would have printed a total of {pagesCounted}.");
                    Console.WriteLine($"Done.");
                }
            }
            catch (System.IO.FileNotFoundException fnfe) {
                Console.WriteLine($"{fnfe.Message}");
                System.Environment.Exit(-1);
            }
            catch (Exception e) {
                Console.WriteLine($"{e.Message}");
                //var result = new ParserResult<Options>();
                var helpText = HelpText.AutoBuild(result, h => {
                    h.AutoHelp = true;
                    h.AutoVersion = true;
                    //h.AddPostOptionsLine("Files\tOne or more filenames of files to be printed.");
                    return HelpText.DefaultParsingErrorsHandler(result, h);
                }, e => e);
                Console.WriteLine(helpText);
                System.Environment.Exit(-1);
            }
        }

        private void SheetViewModel_Reflowed(object sender, EventArgs e) {
            WinPrint.Core.Helpers.Logging.TraceMessage();
        }

        private void PropertyChangedEventHandler(object o, PropertyChangedEventArgs e) {
            WinPrint.Core.Helpers.Logging.TraceMessage($"SheetViewModel.PropertyChanged: {e.PropertyName}");
            switch (e.PropertyName) {
                case "Landscape":
                    if (options.Verbose) Console.WriteLine($"Page Orientation: {print.SheetViewModel.Landscape}");
                    break;

                case "Header":
                    if (options.Verbose) Console.WriteLine($"Header Text: {print.SheetViewModel.Header.Text}");
                    break;

                case "Footer":
                    if (options.Verbose) Console.WriteLine($"Footer Text: {print.SheetViewModel.Footer.Text}");
                    break;

                case "Margins":
                    if (options.Verbose) Console.WriteLine($"Margins: Top: {print.SheetViewModel.Margins.Top / 100M}\", " +
                        $"Left: {print.SheetViewModel.Margins.Left / 100M}\", " +
                        $"Right: {print.SheetViewModel.Margins.Right / 100M}\", " +
                        $"Bottom: {print.SheetViewModel.Margins.Bottom / 100M}\", ");
                    break;

                case "PageSeparator":
                    if (options.Verbose) Console.WriteLine($"PageSeparator: {print.SheetViewModel.PageSeparator}");
                    break;

                case "Rows":
                    if (options.Verbose) Console.WriteLine($"Rows: {print.SheetViewModel.Rows}");
                    break;

                case "Columns":
                    if (options.Verbose) Console.WriteLine($"Columns: {print.SheetViewModel.Columns}");
                    break;

                case "Padding":
                    if (options.Verbose) Console.WriteLine($"Padding: {print.SheetViewModel.Padding / 100M}\"");
                    break;

                case "File":
                    if (options.Verbose) Console.WriteLine($"File: {print.SheetViewModel.File}");
                    break;

                case "Type":
                    if (options.Verbose) Console.WriteLine($"Type: {print.SheetViewModel.Type}");
                    break;

                case "Content":
                    if (options.Verbose) Console.WriteLine($"Content loaded.");
                    break;

                case "Loading":
                    if (print.SheetViewModel.Loading)
                        Console.WriteLine($"Reading {print.SheetViewModel.File}...");
                    else if (options.Verbose)
                        Console.WriteLine($"File read.");
                    break;

                case "Reflowing":
                    if (print.SheetViewModel.Reflowing)
                        Console.WriteLine($"Formatting {print.SheetViewModel.Type}...");
                    else if (options.Verbose)
                        Console.WriteLine($"Formating complete.");
                    break;
            }
        }

        private void SettingsChangedEventHandler(object o, bool reflow) {
            WinPrint.Core.Helpers.Logging.TraceMessage($"SheetViewModel.SettingsChanged: {reflow}");
        }

        static void DisplayHelp<T>(ParserResult<T> result, IEnumerable<Error> errs) {
            var helpText = HelpText.AutoBuild(result, h => {
                h.AutoHelp = true;
                h.AutoVersion = true;
                //h.AddPostOptionsLine("Files\tOne or more filenames of files to be printed.");
                return HelpText.DefaultParsingErrorsHandler(result, h);
            }, e => e);
            Console.WriteLine(helpText);
        }
    }
}

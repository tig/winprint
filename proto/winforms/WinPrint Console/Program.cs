using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing.Printing;
using System.Linq;
using CommandLine;
using CommandLine.Text;
using WinPrint.Core;
using WinPrint.Core.Models;

namespace WinPrintConsole {
    class Program {
        private static ParserResult<Options> result;
        static void Main(string[] args) {
            var parser = new Parser(with => {
                with.EnableDashDash = true;
                with.HelpWriter = null;
            });

            result = parser.ParseArguments<Options>(args);
            result
                .WithParsed(opts => Go(opts))
                .WithNotParsed((errs) => DisplayHelp(result, errs));

            parser.Dispose();
            System.Environment.Exit(0);
        }

        private static void Go(Options opts) {
            var print = new Print();

            // -g
            if (opts.Gui) {
                // TODO Spawn WinPrint GUI App with args
                Console.WriteLine($"Starting WinPrint GUI App...");
                System.Environment.Exit(0);
            }

            try {
                // --s
                string sheetID;
                Sheet sheet = FindSheet(opts, out sheetID);

                // --l and --o
                if (opts.Landscape) sheet.Landscape = true;
                if (opts.Portrait) sheet.Landscape = false;

                // --p
                print.SetPrinter(opts.Printer);

                // --z
                print.SetPaperSize(opts.PaperSize);

                // Must set landsacpe after printer/paper selection
                print.PrintDocument.DefaultPageSettings.Landscape = sheet.Landscape;
                print.SheetVM.File = opts.Files.ToList().FirstOrDefault();
                print.SheetVM.SetSettings(sheet);

                // --v
                if (opts.Verbose) {
                    Console.WriteLine($"Printing {opts.Files.ToList().FirstOrDefault()}");
                    Console.WriteLine($"    Printer: {print.PrintDocument.PrinterSettings.PrinterName}");
                    Console.WriteLine($"    Paper Size: {print.PrintDocument.DefaultPageSettings.PaperSize.PaperName}");
                    string s = print.PrintDocument.DefaultPageSettings.Landscape ? $"Landscape" : $"Portrait";
                    Console.WriteLine($"    Orientation: {s}");
                    Console.WriteLine($"    Sheet Definition: {sheet.Name} ({sheetID})");
                }

                // --c
                if (opts.CountPages) {
                    int n = print.CountPages(fromSheet: opts.FromPage, toSheet: opts.ToPage);
                    if (opts.Verbose)
                        Console.WriteLine($"Would print {n} pages.");
                    System.Environment.Exit(n);
                }

                if (opts.FromPage != 0)
                    print.PrintDocument.PrinterSettings.FromPage = opts.FromPage;

                if (opts.ToPage != 0) {
                    print.PrintDocument.PrinterSettings.ToPage = opts.ToPage;
                }

                print.DoPrint();
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


        private static Sheet FindSheet(Options opts, out string sheetID) {
            Sheet sheet = null;
            sheetID = ModelLocator.Current.Settings.DefaultSheet.ToString();
            if (!string.IsNullOrEmpty(opts.Sheet) && !opts.Sheet.Equals("default", StringComparison.InvariantCultureIgnoreCase)) {
                if (!ModelLocator.Current.Settings.Sheets.TryGetValue(opts.Sheet, out sheet)) {
                    // Wasn't a GUID or isn't valid
                    var s = ModelLocator.Current.Settings.Sheets
                    .Where(s => s.Value.Name.Equals(opts.Sheet, StringComparison.InvariantCultureIgnoreCase))
                    .FirstOrDefault();

                    if (s.Value is null) {
                        Console.WriteLine($"Sheet definiton not found.");
                        System.Environment.Exit(-1);
                    }
                    sheetID = s.Key;
                    sheet = s.Value;
                }
            }
            else
                sheet = ModelLocator.Current.Settings.Sheets.GetValueOrDefault(sheetID);
            return sheet;
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

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Drawing.Printing;
using System.Linq;
using System.Threading.Tasks;
using CommandLine;
using CommandLine.Text;
using Serilog;
using Serilog.Events;
using WinPrint.Core;
using WinPrint.Core.Models;
using WinPrint.Core.Services;

/// <summary>
/// Implements the WinPrint console/command line app. 
/// </summary>
namespace WinPrint.Console {
    class Program {
        private Print print;
        private static ParserResult<Options> result;

        static void Main(string[] args) {
            ServiceLocator.Current.LogService.Start(SettingsService.SettingsPath);

            // Parse command line
            using var parser = new Parser(with => {
                with.EnableDashDash = true;
                with.HelpWriter = null;
            });
            result = parser.ParseArguments<Options>(args);
            result.WithParsed(o => {
                    ModelLocator.Current.Options.CopyPropertiesFrom(o);

                    if (o.Debug) {
                        ServiceLocator.Current.LogService.ConsoleLevelSwitch.MinimumLevel = LogEventLevel.Debug;
                        ServiceLocator.Current.LogService.MasterLevelSwitch.MinimumLevel = LogEventLevel.Debug;
                    }
                    else {
                        ServiceLocator.Current.LogService.ConsoleLevelSwitch.MinimumLevel = LogEventLevel.Information;
                    }
                    Log.Debug("Command Line: {CmdLine}", Parser.Default.FormatCommandLine(o));
                    if (o.Verbose) Log.Information("Command Line: {CmdLine}", Parser.Default.FormatCommandLine(o));
                    var program = new Program();
                    Task.WaitAll(program.Go());
                })
                .WithNotParsed((errs) => DisplayHelp(result, errs));

            Log.Debug($"Exiting Main - This should never happen.");
            Environment.Exit(-1);
        }

        private async Task Go() {
            int exitCode = -1;

            Log.Debug(LogService.GetTraceMsg(), "Go");
            print = new Print();

            print.PrintingSheet += (s, sheetNum) => Log.Information("Printing sheet {pageNum}", sheetNum);
            print.SheetViewModel.PropertyChanged += PropertyChangedEventHandler;
            print.SheetViewModel.SettingsChanged += SettingsChangedEventHandler;
            print.SheetViewModel.Reflowed += SheetViewModel_Reflowed;
            print.SheetViewModel.ReflowProgress += (s, msg) => Log.Debug("{DateTime:mm:ss.fff}:Reflow Progress {msg}", DateTime.Now, msg);

            // -g
            if (ModelLocator.Current.Options.Gui) 
                // This will exit this app
                StartGui();

            try {
                // --s
                string sheetID;
                Sheet sheet = print.SheetViewModel.FindSheet(ModelLocator.Current.Options.Sheet, out sheetID);

                // --l and --o
                if (ModelLocator.Current.Options.Landscape) sheet.Landscape = true;
                if (ModelLocator.Current.Options.Portrait) sheet.Landscape = false;

                // --p
                print.SetPrinter(ModelLocator.Current.Options.Printer);

                // --z
                print.SetPaperSize(ModelLocator.Current.Options.PaperSize);

                // --v
                if (ModelLocator.Current.Options.Verbose) {
                    Log.Information("    Printer:          {printer}", print.PrintDocument.PrinterSettings.PrinterName);
                    Log.Information("    Paper Size:       {size}", print.PrintDocument.DefaultPageSettings.PaperSize.PaperName);
                    Log.Information("    Orientation:      {s}", print.PrintDocument.DefaultPageSettings.Landscape ? $"Landscape" : $"Portrait");
                    Log.Information("    Sheet Definition: {name} ({id})", sheet.Name, sheetID);
                }

                // Must set landsacpe after printer/paper selection
                print.PrintDocument.DefaultPageSettings.Landscape = sheet.Landscape;
                Log.Debug("Calling SetSheet");
                print.SheetViewModel.SetSheet(sheet);

                // Go through each file on command line and print them
                // TODO: Handle wildcards
                int sheetsCounted = 0;
                foreach (var file in ModelLocator.Current.Options.Files.ToList())
                    sheetsCounted += await Print(file).ConfigureAwait(false);

                if (ModelLocator.Current.Options.Verbose) {
                    if (ModelLocator.Current.Options.CountPages)
                        Log.Information("Would have printed a total of {pagesCounted} sheets.", sheetsCounted);
                    else
                        Log.Information("Printed a total of {pagesCounted} sheets.", sheetsCounted);
                }

                exitCode = sheetsCounted;
            }
            catch (InvalidPrinterException e) {
                LogException(e);
                Log.Information("Installed printers:");
                foreach (string printer in PrinterSettings.InstalledPrinters)
                    Log.Information($"   {printer}");                  
            }
            catch (InvalidOperationException e) {
                LogException(e);
                if (e.Message.Contains("Sheet definiton not found")) {
                    // show list of available sheets
                    Log.Information("Sheet definitons:");
                    foreach (var sheet in ModelLocator.Current.Settings.Sheets)
                        Log.Information($"   {sheet.Value.Name}");
                }
            }
            catch (Exception e) {
                LogException(e);
                // TODO: Should we show usage info on error? 
                //var helpText = HelpText.AutoBuild(result, h => {
                //    h.AutoHelp = true;
                //    h.AutoVersion = true;
                //    //h.AddPostOptionsLine("Files\tOne or more filenames of files to be printed.");
                //    return HelpText.DefaultParsingErrorsHandler(result, h);
                //}, e => e);
                //foreach (var line in helpText.ToString().Split(Environment.NewLine))
                //    Log.Information(line);
            }
            finally {
                if (ModelLocator.Current.Options.Verbose) 
                    Log.Information($"Exiting with exit code {exitCode}.");
                Log.Debug($"Environment.Exit({exitCode}");
                Environment.Exit(exitCode);
            }
        }

        private static void LogException(Exception e) {
            if (ModelLocator.Current.Options.Debug)
                Log.Error(e, "{msg}", e.Message);
            else
                Log.Error("{msg}", e.Message);
        }

        private async Task<int> Print(string file) {
            int pagesCounted = 0;

            Log.Debug("awaiting LoadAsync {file}", file);
            var type = await print.SheetViewModel.LoadAsync(file).ConfigureAwait(false);
            Log.Debug("back from LoadAsync. Type is {type}", type);

            // --c
            if (ModelLocator.Current.Options.CountPages) {
                int n = 0;
                pagesCounted += n = await print.CountSheets(fromSheet: ModelLocator.Current.Options.FromPage, toSheet: ModelLocator.Current.Options.ToPage);
            }
            else {
                bool pageRangeSet = false;
                if (ModelLocator.Current.Options.FromPage != 0) {
                    print.PrintDocument.PrinterSettings.FromPage = ModelLocator.Current.Options.FromPage;
                    pageRangeSet = true;
                }
                else
                    print.PrintDocument.PrinterSettings.FromPage = 0;

                if (ModelLocator.Current.Options.ToPage != 0) {
                    print.PrintDocument.PrinterSettings.ToPage = ModelLocator.Current.Options.ToPage;
                    pageRangeSet = true;
                }
                else
                    print.PrintDocument.PrinterSettings.ToPage = 0;

                if (pageRangeSet)
                    Log.Information("Printing from sheet {from} to sheet {to}.", print.PrintDocument.PrinterSettings.FromPage, print.PrintDocument.PrinterSettings.ToPage);
                else
                    Log.Information("Printing all sheets.");
                await print.DoPrint();
            }
            return pagesCounted;
        }

        private static void StartGui() {
            // TODO Spawn WinPrint GUI App with args
            if (ModelLocator.Current.Options.Verbose)
                Log.Information("Starting WinPrint GUI App");

            Process gui = null;
            var psi = new ProcessStartInfo();
            try {

                psi.UseShellExecute = false;   // This is important
                psi.CreateNoWindow = true;     // This is what hides the command window.
                psi.FileName = @"winprintgui.exe";
                psi.Arguments = Parser.Default.FormatCommandLine(ModelLocator.Current.Options);
                gui = Process.Start(psi);
                Log.Debug("{file} started", psi.FileName);
            }
            catch (Exception e) {
                Log.Error(e, "Could not start WinPrint GUI App ({app})", psi.FileName);
                Environment.Exit(-1);
            }
            finally {
                gui?.Dispose();
                Environment.Exit(0);
            }
        }

        private void SheetViewModel_Reflowed(object sender, EventArgs e) {
            LogService.TraceMessage();
        }

        private void PropertyChangedEventHandler(object o, PropertyChangedEventArgs e) {
            Log.Debug("SheetViewModel.PropertyChanged: {s}", e.PropertyName);
            switch (e.PropertyName) {
                case "Landscape":
                    if (ModelLocator.Current.Options.Verbose)
                        Log.Information("Paper Orientation: {s}", print.SheetViewModel.Landscape ? "Landscape" : "Portrait");
                    break;

                case "Header":
                    if (ModelLocator.Current.Options.Verbose) Log.Information("Header Text: {s}", print.SheetViewModel.Header.Text);
                    break;

                case "Footer":
                    if (ModelLocator.Current.Options.Verbose) Log.Information("Footer Text: {s}", print.SheetViewModel.Footer.Text);
                    break;

                case "Margins":
                    if (ModelLocator.Current.Options.Verbose) Log.Information("Margins: {v}", print.SheetViewModel.Margins);
                    break;

                case "PageSeparator":
                    if (ModelLocator.Current.Options.Verbose) Log.Information("PageSeparator {s}:", print.SheetViewModel.PageSeparator);
                    break;

                case "Rows":
                    if (ModelLocator.Current.Options.Verbose) Log.Information("Rows: {s}", print.SheetViewModel.Rows);
                    break;

                case "Columns":
                    if (ModelLocator.Current.Options.Verbose) Log.Information("Columns: {s}", print.SheetViewModel.Columns);
                    break;

                case "Padding":
                    if (ModelLocator.Current.Options.Verbose) Log.Information("Padding: {s}", print.SheetViewModel.Padding / 100M);
                    break;

                case "File":
                    if (ModelLocator.Current.Options.Verbose) Log.Information("File: {s}", print.SheetViewModel.File);
                    break;

                case "Type":
                    if (ModelLocator.Current.Options.Verbose) Log.Information("Type: {s}", print.SheetViewModel.Type);
                    break;

                case "Content":
                    if (ModelLocator.Current.Options.Verbose) Log.Information("Content loaded.");
                    break;

                case "Loading":
                    if (print.SheetViewModel.Loading)
                        Log.Information("Reading {f}", print.SheetViewModel.File);
                    else if (ModelLocator.Current.Options.Verbose)
                        Log.Information("File read.");
                    break;

                case "Reflowing":
                    if (print.SheetViewModel.Reflowing)
                        Log.Information("Formatting as {t}", print.SheetViewModel.Type);
                    else if (ModelLocator.Current.Options.Verbose)
                        Log.Information("Formating complete.");
                    break;
            }
        }

        private void SettingsChangedEventHandler(object o, bool reflow) {
            Log.Debug(LogService.GetTraceMsg(), reflow);
        }

        static void DisplayHelp<T>(ParserResult<T> result, IEnumerable<Error> errs) {
            LogService.TraceMessage();
            var helpText = HelpText.AutoBuild(result, h => h);
            System.Console.WriteLine(helpText);
        }
    }
}

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Drawing.Printing;
using System.Linq;
using System.Reflection;
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
            ServiceLocator.Current.TelemetryService.Start(AppDomain.CurrentDomain.FriendlyName);
            ServiceLocator.Current.LogService.Start(AppDomain.CurrentDomain.FriendlyName);

            AppDomain.CurrentDomain.UnhandledException += CurrentDomain_UnhandledException;
            TaskScheduler.UnobservedTaskException += TaskScheduler_UnobservedTaskException;

            // Parse command line
            using var parser = new Parser(with => {
                with.EnableDashDash = true;
                with.HelpWriter = null;
            });
            result = parser.ParseArguments<Options>(args);
            result.WithParsed(o => {
                ServiceLocator.Current.TelemetryService.TrackEvent("Command Line Options", properties: o.GetTelemetryDictionary());
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

        private static void TaskScheduler_UnobservedTaskException(object sender, UnobservedTaskExceptionEventArgs e) {
            ServiceLocator.Current.TelemetryService.TrackException(e.Exception);
        }

        private static void CurrentDomain_UnhandledException(object sender, UnhandledExceptionEventArgs e) {
            var ex = e.ExceptionObject as Exception;
            ServiceLocator.Current.TelemetryService.TrackException(ex);
        }

        private async Task Go() {
            int exitCode = -1;

            Log.Debug(LogService.GetTraceMsg(), "Go");
            print = new Print();

            print.PrintingSheet += (s, sheetNum) => Log.Information("Printing sheet {pageNum}", sheetNum);
            print.SheetViewModel.PropertyChanged += PropertyChangedEventHandler;
            print.SheetViewModel.SettingsChanged += SettingsChangedEventHandler;
            print.SheetViewModel.ReflowProgress += (s, msg) => Log.Debug("Reflow Progress {msg}", msg);

            // -g
            if (ModelLocator.Current.Options.Gui) 
                // This will exit this app
                StartGui();

            ServiceLocator.Current.UpdateService.GotLatestVersion += (s, v) => {
                var cur = new Version(FileVersionInfo.GetVersionInfo(Assembly.GetAssembly(typeof(LogService)).Location).FileVersion);
                Log.Debug("Got new version info. Current: {cur}, Available: {v}", cur, v);
                if (v != null && v.CompareTo(cur) > 0) {
                    Log.Information("A newer version ({v}) of winprint is available at {l}.", v, ServiceLocator.Current.UpdateService.DownloadUri);
                }
            };

            await ServiceLocator.Current.UpdateService.GetLatestStableVersionAsync();

            try {
                // --s
                string sheetID;
                SheetSettings sheet = print.SheetViewModel.FindSheet(ModelLocator.Current.Options.Sheet, out sheetID);

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
                ServiceLocator.Current.TelemetryService.Stop();

                if (ModelLocator.Current.Options.Verbose) 
                    Log.Information($"Exiting with exit code {exitCode}.");
                Log.Debug($"Environment.Exit({exitCode})");
                Environment.Exit(exitCode);
            }
        }

        private static void LogException(Exception e) {
            ServiceLocator.Current.TelemetryService.TrackException(e, false);

            if (ModelLocator.Current.Options.Debug)
                Log.Error(e, "{msg}", e.Message);
            else
                Log.Error("{msg}", e.Message);
        }

        private async Task<int> Print(string file) {
            int sheetsCounted = 0;

            Log.Debug("awaiting LoadAsync {file}, contentType = {t}.", file, ModelLocator.Current.Options.ContentType);
            var type = await print.SheetViewModel.LoadAsync(file, ModelLocator.Current.Options.ContentType).ConfigureAwait(false);
            Log.Debug("back from LoadAsync. Type is {type}", type);

            // --c
            if (ModelLocator.Current.Options.CountPages) {
                int n = 0;
                sheetsCounted += n = await print.CountSheets(fromSheet: ModelLocator.Current.Options.FromPage, toSheet: ModelLocator.Current.Options.ToPage);
            }
            else {
                bool sheetRangeSet = false;
                if (ModelLocator.Current.Options.FromPage != 0) {
                    print.PrintDocument.PrinterSettings.FromPage = ModelLocator.Current.Options.FromPage;
                    sheetRangeSet = true;
                }
                else
                    print.PrintDocument.PrinterSettings.FromPage = 0;

                if (ModelLocator.Current.Options.ToPage != 0) {
                    print.PrintDocument.PrinterSettings.ToPage = ModelLocator.Current.Options.ToPage;
                    sheetRangeSet = true;
                }
                else
                    print.PrintDocument.PrinterSettings.ToPage = 0;

                if (sheetRangeSet)
                    Log.Information("Printing from sheet {from} to sheet {to}.", print.PrintDocument.PrinterSettings.FromPage, print.PrintDocument.PrinterSettings.ToPage);
                //else
                //    Log.Information("Printing all sheets.");
                sheetsCounted += await print.DoPrint();
            }
            return sheetsCounted;
        }

        private static void StartGui() {
            // TODO Spawn WinPrint GUI App with args
            if (ModelLocator.Current.Options.Verbose)
                Log.Information("Starting WinPrint GUI App");

            int exitCode = 0;
            Process gui = null;
            var psi = new ProcessStartInfo();
            try {

                psi.UseShellExecute = false;   // This is important
                psi.CreateNoWindow = true;     // This is what hides the command window.
                psi.FileName = @"winprintgui.exe";
                psi.Arguments = Parser.Default.FormatCommandLine(ModelLocator.Current.Options);
                gui = Process.Start(psi);
                exitCode = gui.ExitCode;
                Log.Debug("{file} started", psi.FileName);
            }
            catch (Exception e) {
                ServiceLocator.Current.TelemetryService.TrackException(e, false);

                Log.Error(e, "Could not start WinPrint GUI App ({app})", psi.FileName);
                if (gui != null)
                    exitCode = gui.ExitCode;
                else
                    exitCode = -1;
            }
            finally {
                gui?.Dispose();

                ServiceLocator.Current.TelemetryService.Stop();

                Environment.Exit(exitCode);
            }
        }

        private void PropertyChangedEventHandler(object o, PropertyChangedEventArgs e) {
            Log.Debug("SheetViewModel.PropertyChanged: {s}", e.PropertyName);
            switch (e.PropertyName) {
                case "Landscape":
                    if (ModelLocator.Current.Options.Verbose)
                        Log.Information("    Paper Orientation: {s}", print.SheetViewModel.Landscape ? "Landscape" : "Portrait");
                    break;

                case "Header":
                    if (ModelLocator.Current.Options.Verbose) Log.Information("    Header Text:      {s}", print.SheetViewModel.Header.Text);
                    break;

                case "Footer":
                    if (ModelLocator.Current.Options.Verbose) Log.Information("    Footer Text:      {s}", print.SheetViewModel.Footer.Text);
                    break;

                case "Margins":
                    if (ModelLocator.Current.Options.Verbose) Log.Information("    Margins:          {v}", print.SheetViewModel.Margins);
                    break;

                case "PageSeparator":
                    if (ModelLocator.Current.Options.Verbose) Log.Information("    PageSeparator     {s}", print.SheetViewModel.PageSeparator);
                    break;

                case "Rows":
                    if (ModelLocator.Current.Options.Verbose) Log.Information("    Rows:             {s}", print.SheetViewModel.Rows);
                    break;

                case "Columns":
                    if (ModelLocator.Current.Options.Verbose) Log.Information("    Columns:          {s}", print.SheetViewModel.Columns);
                    break;

                // TODO: Add INF logging of other sheet properties
                case "Padding":
                    if (ModelLocator.Current.Options.Verbose) Log.Information("    Padding:          {s}", print.SheetViewModel.Padding / 100M);
                    break;

                case "ContentSettings":
                    if (ModelLocator.Current.Options.Verbose) Log.Information("    ContentSettings:  {s}", print.SheetViewModel.ContentSettings);
                    break;

                case "Loading":
                    if (print.SheetViewModel.Loading)
                        Log.Information("Reading {f}", print.SheetViewModel.File);
                    else if (ModelLocator.Current.Options.Verbose)
                        Log.Information("File read.");
                    break;

                case "Reflowing":
                    if (print.SheetViewModel.Reflowing)
                        Log.Information("Formatting as {t}.", print.SheetViewModel.ContentEngine.GetContentType());
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
            ServiceLocator.Current.TelemetryService.TrackEvent("Display Help");
        }
    }
}

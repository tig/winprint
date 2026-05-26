using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Drawing.Printing;
using System.IO;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Serilog;
using Terminal.Gui.App;
using Terminal.Gui.Cli;
using WinPrint.Core;
using WinPrint.Core.ContentTypeEngines;
using WinPrint.Core.Models;
using WinPrint.Core.Services;

namespace WinPrint.cli;

public sealed class PrintCommand : ICliCommand {
    public string PrimaryAlias => "print";

    public IReadOnlyList<string> Aliases { get; } = ["print"];

    public string Description => "Print a file or redirected text with WinPrint.";

    public CommandKind Kind => CommandKind.Input;

    public Type ResultType => typeof(PrintResult);

    public bool AcceptsPositionalArgs => true;

    public IReadOnlyList<CommandOptionDescriptor> Options { get; } = [
        new("printer", "P", typeof(string), "Printer name. Defaults to the system default printer.", false, null),
        new("paper-size", null, typeof(string), "Paper size supported by the selected printer.", false, null),
        new("sheet", "s", typeof(string), "WinPrint sheet definition name or ID.", false, null),
        new("content-type", "c", typeof(string), "Content type engine, content type, or language override.", false, null),
        new("language", "l", typeof(string), "Language or content type override for syntax highlighting.", false, null),
        new("orientation", null, typeof(string), "Page orientation: portrait or landscape.", false, null),
        new("line-numbers", null, typeof(string), "Line number setting: yes or no.", false, null),
        new("from-sheet", null, typeof(int), "First sheet to print.", false, null),
        new("to-sheet", null, typeof(int), "Last sheet to print.", false, null),
        new("what-if", "w", typeof(bool), "Count sheets without printing.", false, null),
        new("gui", "g", typeof(bool), "Open the WinPrint GUI for the file instead of printing from the CLI.", false, null),
        new("config", null, typeof(bool), "Open WinPrint.config.json in the default editor.", false, null)
    ];

    public async Task<CommandResult> RunAsync(
        IApplication app,
        string? initial,
        CommandRunOptions options,
        CancellationToken cancellationToken) {
        bool verbose = options.HasExtension("verbose");
        bool debug = options.HasExtension("debug");
        ServiceLocator.Current.TelemetryService.Start("winprint.cli");
        ServiceLocator.Current.LogService.Start("winprint.cli", null, debug, verbose);

        FileVersionInfo version = FileVersionInfo.GetVersionInfo(Assembly.GetAssembly(typeof(UpdateService))!.Location);
        WriteVerbose(options, $"winprint.cli {version.ProductVersion} - {version.LegalCopyright} - https://tig.github.io/winprint");

        try {
            if (GetFlag(options, "config")) {
                OpenSettingsFile();
                return Ok(PrintResult.NoPrint("Opened WinPrint configuration."), options);
            }

            string? fileName = options.Arguments.Count > 0 ? options.Arguments[0] : null;
            if (GetFlag(options, "gui")) {
                OpenGui(fileName);
                return Ok(PrintResult.NoPrint("Opened WinPrint GUI."), options);
            }

            using Print print = new();
            ConfigurePrinter(print, options);
            SheetSettings sheet = ConfigureSheet(print, options);
            string title = options.Title ?? fileName ?? "winprint.cli";
            string? contentType = GetOption(options, "language")
                                  ?? GetOption(options, "content-type")
                                  ?? ContentTypeEngineBase.GetContentType(fileName ?? "");

            print.SheetViewModel.File = fileName ?? "";
            print.SheetViewModel.Title = title;
            ConfigureSheetRange(print, options);
            await LoadContentAsync(print, fileName, contentType, initial, cancellationToken).ConfigureAwait(false);

            if (verbose) {
                WriteVerbose(options, $"FileName:            {fileName ?? ""}");
                WriteVerbose(options, $"Title:               {title}");
                WriteVerbose(options, $"Content Type:        {print.SheetViewModel.ContentType}");
                WriteVerbose(options, $"Language:            {print.SheetViewModel.Language}");
                WriteVerbose(options, $"Content Type Engine: {print.SheetViewModel.ContentEngine?.GetType().Name ?? ""}");
                WriteVerbose(options, $"Printer:             {print.PrintDocument.PrinterSettings.PrinterName}");
                WriteVerbose(options, $"Paper Size:          {print.PrintDocument.DefaultPageSettings.PaperSize.PaperName}");
                WriteVerbose(options, $"Orientation:         {(print.PrintDocument.DefaultPageSettings.Landscape ? "Landscape" : "Portrait")}");
                WriteVerbose(options, $"Sheet Definition:    {sheet.Name}");
            }

            int sheets = GetFlag(options, "what-if")
                ? await print.CountSheets(GetInt(options, "from-sheet"), GetInt(options, "to-sheet")).ConfigureAwait(false)
                : await print.DoPrint().ConfigureAwait(false);

            PrintResult result = new(
                GetFlag(options, "what-if") ? "counted" : "printed",
                sheets,
                print.SheetViewModel.ContentType ?? "",
                print.SheetViewModel.Language ?? "",
                print.SheetViewModel.ContentEngine?.GetType().Name ?? "",
                print.PrintDocument.PrinterSettings.PrinterName,
                print.PrintDocument.DefaultPageSettings.PaperSize.PaperName,
                print.PrintDocument.DefaultPageSettings.Landscape ? "Landscape" : "Portrait",
                sheet.Name);

            return Ok(result, options);
        }
        catch (Exception ex) when (ex is InvalidPrinterException or InvalidOperationException or IOException or Win32Exception) {
            Log.Error(ex, "winprint.cli failed.");
            return Error(ex);
        }
    }

    private static async Task LoadContentAsync(
        Print print,
        string? fileName,
        string? contentType,
        string? initial,
        CancellationToken cancellationToken) {
        cancellationToken.ThrowIfCancellationRequested();

        if (!string.IsNullOrEmpty(fileName)) {
            string path = Path.IsPathFullyQualified(fileName) ? fileName : Path.GetFullPath(fileName);
            print.SheetViewModel.File = path;
            await print.SheetViewModel.LoadFileAsync(path, contentType).ConfigureAwait(false);
            return;
        }

        string document = initial ?? "";
        if (string.IsNullOrEmpty(document) && Console.IsInputRedirected) {
            document = await Console.In.ReadToEndAsync(cancellationToken).ConfigureAwait(false);
        }

        if (string.IsNullOrEmpty(document)) {
            throw new InvalidOperationException("Specify a file, pipe text to stdin, or pass --initial text.");
        }

        print.SheetViewModel.Encoding = Encoding.UTF8;
        await print.SheetViewModel.LoadStringAsync(document, contentType).ConfigureAwait(false);
    }

    private static void ConfigurePrinter(Print print, CommandRunOptions options) {
        if (GetOption(options, "printer") is { Length: > 0 } printer) {
            print.SetPrinter(printer);
        }

        if (GetOption(options, "paper-size") is { Length: > 0 } paperSize) {
            print.SetPaperSize(paperSize);
        }
    }

    private static SheetSettings ConfigureSheet(Print print, CommandRunOptions options) {
        string? sheetDefinition = GetOption(options, "sheet");
        SheetSettings sheet = print.SheetViewModel.FindSheet(sheetDefinition ?? "", out _);

        if (GetOption(options, "orientation") is { Length: > 0 } orientation) {
            sheet.Landscape = orientation.Equals("landscape", StringComparison.OrdinalIgnoreCase)
                              || orientation.Equals("yes", StringComparison.OrdinalIgnoreCase);
        }

        if (GetOption(options, "line-numbers") is { Length: > 0 } lineNumbers) {
            sheet.ContentSettings ??= new ContentSettings();
            sheet.ContentSettings.LineNumbers = lineNumbers.Equals("yes", StringComparison.OrdinalIgnoreCase)
                                                || lineNumbers.Equals("true", StringComparison.OrdinalIgnoreCase);
        }

        print.PrintDocument.DefaultPageSettings.Landscape = sheet.Landscape;
        print.SheetViewModel.SetSheet(sheet);
        return sheet;
    }

    private static void ConfigureSheetRange(Print print, CommandRunOptions options) {
        print.PrintDocument.PrinterSettings.FromPage = GetInt(options, "from-sheet");
        print.PrintDocument.PrinterSettings.ToPage = GetInt(options, "to-sheet");
    }

    private static void OpenSettingsFile() {
        using Process? proc = Process.Start(new ProcessStartInfo {
            UseShellExecute = true,
            FileName = ServiceLocator.Current.SettingsService.SettingsFileName
        });
    }

    private static void OpenGui(string? fileName) {
        string path = Path.GetDirectoryName(Assembly.GetAssembly(typeof(SettingsService))!.Location)!;
        using Process? proc = Process.Start(new ProcessStartInfo {
            UseShellExecute = true,
            Arguments = fileName ?? "",
            FileName = Path.Combine(path, "winprintgui.exe")
        });
    }

    private static string? GetOption(CommandRunOptions options, string name) {
        return options.CommandOptions.TryGetValue(name, out string? value) ? value : null;
    }

    private static bool GetFlag(CommandRunOptions options, string name) {
        return options.CommandOptions.TryGetValue(name, out string? value)
               && value.Equals("true", StringComparison.OrdinalIgnoreCase);
    }

    private static int GetInt(CommandRunOptions options, string name) {
        return options.CommandOptions.TryGetValue(name, out string? value) && int.TryParse(value, out int result)
            ? result
            : 0;
    }

    private static void WriteVerbose(CommandRunOptions options, string message) {
        if (options.HasExtension("verbose")) {
            Console.Error.WriteLine(message);
        }
    }

    private static CommandResult Ok(PrintResult result, CommandRunOptions options) {
        return new CommandResult(CommandStatus.Ok, result.ToString(), null, null);
    }

    private static CommandResult Error(Exception ex) {
        return new CommandResult(CommandStatus.Error, null, ex.GetType().Name, ex.Message);
    }
}

using System.Text;
using Terminal.Gui.App;
using Terminal.Gui.Cli;
using WinPrint.Core;
using WinPrint.Core.Abstractions;
using WinPrint.Core.Models;
using WinPrint.Core.Printing;

namespace WinPrint.TUI;

/// <summary>
///     The <c>print</c> command: prints one or more files directly to a printer without opening the
///     TUI. It loads each file and applies the shared print options through the same
///     <c>AppViewModel.ApplyOptions</c> / <see cref="PrintOrchestrator" /> path the interactive UI
///     uses, so headless output matches the preview. <c>--what-if</c> reports the sheet count without
///     touching a printer; <c>--pdf &lt;file&gt;</c> writes a PDF file instead of printing (no printer
///     involved on any platform; named <c>--pdf</c> because the host owns <c>--output</c> for
///     redirecting a command's text output).
/// </summary>
public sealed class PrintCommand : ICliCommand
{
    /// <inheritdoc />
    public string PrimaryAlias => "print";

    /// <inheritdoc />
    public IReadOnlyList<string> Aliases { get; } = ["print"];

    /// <inheritdoc />
    public string Description => "Print one or more files without opening the UI.";

    /// <inheritdoc />
    public CommandKind Kind => CommandKind.Input;

    /// <inheritdoc />
    public Type ResultType => typeof(void);

    /// <inheritdoc />
    public bool AcceptsPositionalArgs => true;

    /// <inheritdoc />
    // The shared canonical print options plus the print-only --what-if (count sheets, don't print).
    public IReadOnlyList<CommandOptionDescriptor> Options { get; } =
    [
        .. WinPrintOptions.Shared.Select(o =>
            new CommandOptionDescriptor(o.Name, o.Short?.ToString(), o.ValueType, o.Help, false, null)),
        new("what-if", "w", typeof(bool), "Report how many sheets would print, without printing.", false, null),
        new("pdf", null, typeof(string),
            "Write the output to a PDF file instead of printing (no printer involved).", false, null)
    ];

    /// <inheritdoc />
    public async Task<CommandResult> RunAsync(
        IApplication app,
        string? initial,
        CommandRunOptions options,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(options);

        try
        {
            if (options.Arguments.Count == 0)
            {
                return new CommandResult(CommandStatus.Error, null, "NoFiles",
                    "Specify at least one file to print, e.g. `wp print Program.cs`.");
            }

            bool whatIf = CommandOptionsBinder.GetFlag(options, "what-if");
            string? pdfPath = CommandOptionsBinder.GetString(options, "pdf");
            if (pdfPath is not null)
            {
                if (CommandOptionsBinder.GetString(options, "printer") is not null)
                {
                    return new CommandResult(CommandStatus.Error, null, "PdfAndPrinter",
                        "--pdf writes a file instead of printing; it cannot be combined with --printer.");
                }

                if (options.Arguments.Count > 1)
                {
                    return new CommandResult(CommandStatus.Error, null, "PdfOneFile",
                        "--pdf writes one PDF; specify exactly one input file.");
                }
            }

            var output = new StringBuilder();
            int totalSheets = 0;

            try
            {
                foreach (string file in options.Arguments)
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    totalSheets += await PrintOneAsync(file, options, whatIf, pdfPath, output).ConfigureAwait(false);
                }
            }
            catch (Exception ex) when (ex is InvalidOperationException or IOException or UnauthorizedAccessException)
            {
                return new CommandResult(CommandStatus.Error, output.ToString().TrimEnd(), ex.GetType().Name,
                    ex.Message);
            }

            string verb = whatIf ? "would print" : "printed";
            output.Append($"{options.Arguments.Count} file(s) {verb} {totalSheets} sheet(s).");
            return new CommandResult(CommandStatus.Ok, output.ToString().TrimEnd(), null, null);
        }
        finally
        {
            HeadlessInlineTeardown.ReserveInlineRegion(app);
        }
    }

    // Loads one file, applies the options, and either prints it, writes it to a PDF (--pdf), or
    // (for --what-if) counts its sheets. Returns the number of sheets printed / that would print,
    // and appends a per-file line to output.
    private static async Task<int> PrintOneAsync(
        string file, CommandRunOptions options, bool whatIf, string? pdfPath, StringBuilder output)
    {
        var bound = CommandOptionsBinder.ToOptions(options, [file]);
        var context = SettingsContext.Create(bound,
            pdfPath is null ? null : new PdfFilePrintService(pdfPath));

        if (!await context.App.LoadFileAsync(file).ConfigureAwait(false))
        {
            throw new IOException($"Could not load '{file}'.");
        }

        if (whatIf)
        {
            PrintPlan plan = await PrintOrchestrator.PlanAsync(context.PrintService, context).ConfigureAwait(false);
            output.AppendLine($"{file}: {plan.SelectedSheets} of {plan.TotalSheets} sheet(s) would print.");
            return plan.SelectedSheets;
        }

        PrintJobResult result = await PrintOrchestrator.PrintAsync(context.PrintService, context).ConfigureAwait(false);
        if (!result.Success)
        {
            throw new InvalidOperationException($"{file}: {result.Error ?? "print failed."}");
        }

        output.AppendLine(pdfPath is null
            ? $"{file}: printed {result.SheetsPrinted} sheet(s)."
            : $"{file}: wrote {result.SheetsPrinted} sheet(s) to {Path.GetFullPath(pdfPath)}.");
        return result.SheetsPrinted;
    }
}

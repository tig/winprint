// Copyright Kindel, LLC - http://www.kindel.com
// Published under the MIT License at https://github.com/tig/winprint

using Terminal.Gui.Cli;
using WinPrint.TUI;
using Xunit;

namespace WinPrint.TUI.UnitTests;

/// <summary>
///     Print must not touch a printer when options/files are invalid — including the glued-flag case
///     that used to print the first real file to the default (PDF) then fail on the "Brother" "file".
/// </summary>
public class PrintCommandValidationTests
{
    private static CommandRunOptions Run(IReadOnlyList<string> args, params (string Key, string Value)[] opts)
    {
        return new CommandRunOptions
        {
            Arguments = args,
            CommandOptions = opts.ToDictionary(o => o.Key, o => o.Value)
        };
    }

    [Fact]
    public async Task GluedToSheetValue_ErrorsBeforeAnyPrint()
    {
        string path = Path.Combine(Path.GetTempPath(), $"wp-print-{Guid.NewGuid():N}.md");
        await File.WriteAllTextAsync(path, "# hi\n");
        try
        {
            // Mirrors: --to-sheet 2--printer "Brother…"
            CommandResult result = await new PrintCommand().RunHeadlessAsync(
                Run([path, "Brother HL-L3230CDW series Printer"],
                    ("from-sheet", "1"),
                    ("to-sheet", "2--printer"),
                    ("what-if", "true")),
                CancellationToken.None);

            Assert.Equal(CommandStatus.Error, result.Status);
            Assert.Contains("to-sheet", result.ErrorMessage ?? result.ErrorCode ?? "", StringComparison.OrdinalIgnoreCase);
            // Must not report a successful what-if line for the real file.
            Assert.DoesNotContain("would print", result.Value as string ?? "");
        }
        finally
        {
            File.Delete(path);
        }
    }

    [Fact]
    public async Task MissingSecondFile_ErrorsBeforePrintingFirst()
    {
        string path = Path.Combine(Path.GetTempPath(), $"wp-print-{Guid.NewGuid():N}.md");
        await File.WriteAllTextAsync(path, "# hi\n");
        try
        {
            CommandResult result = await new PrintCommand().RunHeadlessAsync(
                Run([path, "NotARealFile-xyz.md"], ("what-if", "true")),
                CancellationToken.None);

            Assert.Equal(CommandStatus.Error, result.Status);
            string err = result.ErrorMessage ?? "";
            Assert.Contains("NotARealFile", err);
            Assert.Contains("--printer", err);
            Assert.DoesNotContain("would print", result.Value as string ?? "");
            Assert.DoesNotContain("Brother", err); // no vendor-name heuristic
        }
        finally
        {
            File.Delete(path);
        }
    }
}

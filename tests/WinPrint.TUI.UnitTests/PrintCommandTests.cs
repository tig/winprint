using Terminal.Gui.Cli;
using WinPrint.TUI;
using Xunit;

namespace WinPrint.TUI.UnitTests;

/// <summary>
///     Verifies the headless <see cref="PrintCommand" /> (<c>wp print</c>): its CLI surface, the
///     no-file usage error, the <c>--what-if</c> path that counts sheets without touching a
///     printer, and the <c>--pdf</c> validation rules (mutually exclusive with <c>--printer</c>,
///     single input file). <c>--what-if</c> and the validation paths run cross-platform without
///     print hardware; the real <c>--pdf</c> write is covered by
///     <c>PdfFilePrintJobTests</c> and by the Linux cups-pdf verification in issue #244.
/// </summary>
public class PrintCommandTests
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
    public void AdvertisesPrintSurface()
    {
        var command = new PrintCommand();

        Assert.Equal("print", command.PrimaryAlias);
        Assert.Equal(CommandKind.Input, command.Kind);
        Assert.True(command.AcceptsPositionalArgs);
        Assert.Contains(command.Options, o => o.Name == "sheet");
        Assert.Contains(command.Options, o => o is { Name: "what-if", ShortName: "w" });
        Assert.Contains(command.Options, o => o.Name == "pdf");
    }

    [Fact]
    public async Task NoFile_ReturnsUsageError()
    {
        CommandResult result = await new PrintCommand()
            .RunAsync(null!, null, Run([]), CancellationToken.None);

        Assert.Equal(CommandStatus.Error, result.Status);
        Assert.Equal("NoFiles", result.ErrorCode);
    }

    [Fact]
    public async Task WhatIf_CountsSheetsWithoutPrinting()
    {
        string path = Path.Combine(Path.GetTempPath(), $"wp-print-{Guid.NewGuid():N}.cs");
        await File.WriteAllTextAsync(path, "class Program { static void Main() { } }\n");
        try
        {
            CommandResult result = await new PrintCommand()
                .RunAsync(null!, null, Run([path], ("what-if", "true")), CancellationToken.None);

            Assert.Equal(CommandStatus.Ok, result.Status);
            Assert.Contains("would print", result.Value as string);
        }
        finally
        {
            File.Delete(path);
        }
    }

    [Fact]
    public async Task Pdf_AndPrinter_AreMutuallyExclusive()
    {
        string path = Path.Combine(Path.GetTempPath(), $"wp-print-{Guid.NewGuid():N}.cs");
        await File.WriteAllTextAsync(path, "class Program { static void Main() { } }\n");
        try
        {
            CommandResult result = await new PrintCommand()
                .RunAsync(null!, null,
                    Run([path], ("pdf", "out.pdf"), ("printer", "PDF")),
                    CancellationToken.None);

            Assert.Equal(CommandStatus.Error, result.Status);
            Assert.Equal("PdfAndPrinter", result.ErrorCode);
        }
        finally
        {
            File.Delete(path);
        }
    }

    [Fact]
    public async Task Pdf_RequiresExactlyOneInputFile()
    {
        string a = Path.Combine(Path.GetTempPath(), $"wp-print-{Guid.NewGuid():N}.cs");
        string b = Path.Combine(Path.GetTempPath(), $"wp-print-{Guid.NewGuid():N}.cs");
        await File.WriteAllTextAsync(a, "class A {}\n");
        await File.WriteAllTextAsync(b, "class B {}\n");
        try
        {
            CommandResult result = await new PrintCommand()
                .RunAsync(null!, null, Run([a, b], ("pdf", "out.pdf")), CancellationToken.None);

            Assert.Equal(CommandStatus.Error, result.Status);
            Assert.Equal("PdfOneFile", result.ErrorCode);
        }
        finally
        {
            File.Delete(a);
            File.Delete(b);
        }
    }
}

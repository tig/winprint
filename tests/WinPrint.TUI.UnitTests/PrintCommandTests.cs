using Terminal.Gui.Cli;
using WinPrint.TUI;
using Xunit;

namespace WinPrint.TUI.UnitTests;

/// <summary>
///     Verifies the headless <see cref="PrintCommand" /> (<c>wp print</c>): its CLI surface, the
///     no-file usage error, and the <c>--what-if</c> path that counts sheets without touching a
///     printer (so it runs cross-platform without print hardware).
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
}

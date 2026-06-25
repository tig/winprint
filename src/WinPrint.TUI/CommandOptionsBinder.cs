using Terminal.Gui.Cli;
using WinPrint.Core.Models;

namespace WinPrint.TUI;

/// <summary>
///     Maps a parsed <see cref="CommandRunOptions" /> onto the shared <see cref="Options" /> model so
///     every <c>wp</c> command (tui, print) applies the same canonical print options through the same
///     <c>AppViewModel.ApplyOptions</c> path MAUI uses. Keeps the option names in exactly one place.
/// </summary>
internal static class CommandOptionsBinder
{
    public static Options ToOptions(CommandRunOptions options, IEnumerable<string>? files)
    {
        var fileList = files?.Where(f => !string.IsNullOrEmpty(f)).ToList();
        return new Options
        {
            Files = fileList is { Count: > 0 } ? fileList : null,
            Sheet = GetString(options, "sheet"),
            Landscape = GetFlag(options, "landscape"),
            Portrait = GetFlag(options, "portrait"),
            Printer = GetString(options, "printer"),
            PaperSize = GetString(options, "paper-size"),
            FromPage = GetInt(options, "from-sheet"),
            ToPage = GetInt(options, "to-sheet"),
            ContentType = GetString(options, "content-type")
        };
    }

    public static string? GetString(CommandRunOptions options, string name)
    {
        return options.CommandOptions.TryGetValue(name, out string? value) ? value : null;
    }

    public static bool GetFlag(CommandRunOptions options, string name)
    {
        return options.CommandOptions.TryGetValue(name, out string? value)
               && value.Equals("true", StringComparison.OrdinalIgnoreCase);
    }

    public static int GetInt(CommandRunOptions options, string name)
    {
        return options.CommandOptions.TryGetValue(name, out string? value) && int.TryParse(value, out int result)
            ? result
            : 0;
    }
}

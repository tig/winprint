using System.Globalization;
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
            FromPage = GetIntOrThrow(options, "from-sheet"),
            ToPage = GetIntOrThrow(options, "to-sheet"),
            ContentType = GetString(options, "content-type"),
            Rows = GetIntOrThrow(options, "rows"),
            Columns = GetIntOrThrow(options, "columns"),
            HeaderOn = GetFlag(options, "header-on"),
            HeaderOff = GetFlag(options, "header-off"),
            FooterOn = GetFlag(options, "footer-on"),
            FooterOff = GetFlag(options, "footer-off"),
            HeaderText = GetString(options, "header-text"),
            FooterText = GetString(options, "footer-text"),
            HeaderFont = GetString(options, "header-font"),
            FooterFont = GetString(options, "footer-font"),
            HeaderBorders = GetString(options, "header-borders"),
            FooterBorders = GetString(options, "footer-borders")
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

    /// <summary>
    ///     Parses an optional int option. Absent → 0 (meaning "unset / all"). Present but not a valid
    ///     integer → <see cref="InvalidOperationException" /> so glued typos like
    ///     <c>--to-sheet 2--printer</c> fail before any print job starts (defense for tui-cs/cli#42).
    /// </summary>
    public static int GetIntOrThrow(CommandRunOptions options, string name)
    {
        if (!options.CommandOptions.TryGetValue(name, out string? value) || value is null)
        {
            return 0;
        }

        if (int.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out int result))
        {
            return result;
        }

        string hint = value.Contains("--", StringComparison.Ordinal)
            ? " A value that contains '--' usually means a missing space before the next flag" +
              " (e.g. `--to-sheet 2 --printer \"Brother Laser\"`)."
            : string.Empty;

        throw new InvalidOperationException(
            $"Invalid value for --{name}: '{value}' (expected an integer).{hint}");
    }

    /// <summary>Legacy soft parse (absent or invalid → 0). Prefer <see cref="GetIntOrThrow" />.</summary>
    public static int GetInt(CommandRunOptions options, string name)
    {
        return options.CommandOptions.TryGetValue(name, out string? value)
               && int.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out int result)
            ? result
            : 0;
    }
}

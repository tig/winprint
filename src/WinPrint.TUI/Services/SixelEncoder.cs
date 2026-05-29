using System.Text;

namespace WinPrint.TUI.Services;

/// <summary>
///     Encodes image data to sixel format for terminals that support it.
///     Falls back to a text placeholder when sixel is not available.
/// </summary>
public static class SixelEncoder
{
    /// <summary>
    ///     Detects whether the current terminal supports sixel graphics by checking
    ///     the TERM and TERM_PROGRAM environment variables.
    /// </summary>
    public static bool IsSupported()
    {
        // Check common sixel-capable terminal indicators
        string? term = Environment.GetEnvironmentVariable("TERM");
        string? termProgram = Environment.GetEnvironmentVariable("TERM_PROGRAM");

        if (term is not null && term.Contains("sixel", StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        // Known sixel-capable terminals
        return termProgram is "mlterm" or "mintty" or "WezTerm" or "foot" or "contour";
    }

    /// <summary>
    ///     Encodes a PNG image to sixel escape sequence. This is a placeholder implementation
    ///     that documents the intended pipeline; full sixel encoding will be added in a future phase.
    /// </summary>
    /// <param name="pngBytes">PNG image data.</param>
    /// <returns>Sixel escape sequence string, or null if encoding fails.</returns>
    public static string? Encode(byte[] pngBytes)
    {
        // Phase 1: stub — sixel encoding will be implemented when the PNG render pipeline is wired up.
        // The intended sequence is: ESC P q <sixel-data> ESC \
        _ = pngBytes;
        return null;
    }

    /// <summary>
    ///     Returns a text-based fallback representation of a page for terminals without sixel support.
    /// </summary>
    public static string GetFallbackText(int pageNumber, int totalPages, string? fileName)
    {
        var sb = new StringBuilder();
        sb.AppendLine("┌───────────────────────────┐");
        sb.AppendLine("│    Print Preview Page      │");
        sb.AppendLine($"│    Page {pageNumber}/{totalPages,-16}  │");

        if (fileName is not null)
        {
            string display = fileName.Length > 22 ? string.Concat(fileName.AsSpan(0, 19), "...") : fileName;
            sb.AppendLine($"│    {display,-23} │");
        }

        sb.AppendLine("│                           │");
        sb.AppendLine("│  [Sixel not available]    │");
        sb.AppendLine("└───────────────────────────┘");
        return sb.ToString();
    }
}

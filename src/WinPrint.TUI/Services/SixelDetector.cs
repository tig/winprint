namespace WinPrint.TUI.Services;

/// <summary>
///     Detects whether the current terminal supports sixel graphics.
///     Checks the TERM, TERM_PROGRAM environment variables and attempts
///     a Device Attributes query where practical.
/// </summary>
public static class SixelDetector
{
    private static bool? s_cached;

    /// <summary>
    ///     Returns true if the terminal likely supports sixel image rendering.
    /// </summary>
    public static bool IsSupported()
    {
        if (s_cached.HasValue)
        {
            return s_cached.Value;
        }

        s_cached = Detect();
        return s_cached.Value;
    }

    /// <summary>Resets the cached detection result (for testing).</summary>
    public static void Reset() => s_cached = null;

    private static bool Detect()
    {
        // Check known sixel-capable terminals via environment
        string? termProgram = Environment.GetEnvironmentVariable("TERM_PROGRAM");
        if (!string.IsNullOrEmpty(termProgram))
        {
            string lower = termProgram.ToLowerInvariant();
            if (lower.Contains("wezterm") ||
                lower.Contains("iterm") ||
                lower.Contains("mlterm") ||
                lower.Contains("contour") ||
                lower.Contains("foot"))
            {
                return true;
            }
        }

        string? term = Environment.GetEnvironmentVariable("TERM");
        if (!string.IsNullOrEmpty(term))
        {
            if (term.Contains("sixel", StringComparison.OrdinalIgnoreCase) ||
                term.Contains("mlterm", StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }
        }

        // Check for SIXEL environment hint (some terminals set this)
        string? sixelEnv = Environment.GetEnvironmentVariable("SIXEL_SUPPORT");
        if (!string.IsNullOrEmpty(sixelEnv) &&
            sixelEnv.Equals("1", StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        // Default: not supported. A full implementation would use DA1/DA2 escape sequences
        // to query the terminal, but that requires async terminal I/O coordination.
        return false;
    }
}

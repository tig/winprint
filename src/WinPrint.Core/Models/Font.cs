using System.Globalization;

namespace WinPrint.Core.Models;

public class Font : ICloneable
{
    private const FontStyle AllStyles = FontStyle.Bold | FontStyle.Italic | FontStyle.Underline | FontStyle.Strikeout;

    private FontStyle _style = FontStyle.Regular;

    /// <summary>
    ///     Font name or font family name (e.g. "Courier New" or "monospace"
    /// </summary>
    [SafeForTelemetry]
    public string Family
    {
        get;
        set;
        //SetField(ref family, value); 
    } = "sansserif";

    /// <summary>
    ///     Font style (Regular, Bold, Italic, Underline, Strikeout)
    /// </summary>
    [SafeForTelemetry]
    public FontStyle Style
    {
        get => _style;
        set => _style = value & AllStyles;
        //                SetField(ref style, value);
    }

    /// <summary>
    ///     Font size in points.
    /// </summary>
    [SafeForTelemetry]
    public float Size
    {
        get;
        set;
        //SetField(ref size, value);
    } = 8F;

    public object Clone()
    {
        return MemberwiseClone();
    }

    public override int GetHashCode()
    {
        return HashCode.Combine(Family, Size, Style);
    }

    public override bool Equals(object? obj)
    {
        if (obj is not Font font)
        {
            return false;
        }

        return font.Family == Family
               && font.Size == Size
               && font.Style == Style;
    }

    public static bool operator ==(Font? m1, Font? m2)
    {
        if (m1 is null)
        {
            return m2 is null;
        }

        if (m2 is null)
        {
            return false;
        }

        return m1.Equals(m2);
    }

    /// <summary>
    ///     Tests whether two <see cref='Font' /> objects are different.
    /// </summary>
    public static bool operator !=(Font? m1, Font? m2)
    {
        return !(m1 == m2);
    }

    /// <summary>
    ///     Provides some interesting information for the Font in String form.
    /// </summary>
    public override string ToString()
    {
        return $"{Family}, {Size.ToString(CultureInfo.InvariantCulture)}pt, {Style.ToString()}";
    }

    /// <summary>
    ///     Parses a CLI font string (#3), e.g. <c>"Comic Sans MS, 10, bold"</c> or
    ///     <c>"Cascadia Code, 9pt, Italic"</c>. Family may contain commas only if size/style are last.
    /// </summary>
    public static bool TryParse(string? text, out Font? font)
    {
        font = null;
        if (string.IsNullOrWhiteSpace(text))
        {
            return false;
        }

        string[] parts = text.Split(',', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);
        if (parts.Length == 0)
        {
            return false;
        }

        string family;
        float size = 8f;
        FontStyle style = FontStyle.Regular;

        if (parts.Length == 1)
        {
            family = parts[0];
        }
        else if (parts.Length == 2)
        {
            family = parts[0];
            if (!TryParseSize(parts[1], out size) && !TryParseStyle(parts[1], out style))
            {
                // Second token is neither size nor style — treat whole string as family.
                family = text.Trim();
                size = 8f;
                style = FontStyle.Regular;
            }
        }
        else
        {
            // Last = style, penultimate = size, rest = family (rejoined).
            if (!TryParseSize(parts[^2], out size))
            {
                return false;
            }

            if (!TryParseStyle(parts[^1], out style))
            {
                return false;
            }

            family = string.Join(", ", parts.Take(parts.Length - 2));
        }

        if (string.IsNullOrWhiteSpace(family))
        {
            return false;
        }

        font = new Font { Family = family.Trim(), Size = size, Style = style };
        return true;
    }

    private static bool TryParseSize(string token, out float size)
    {
        string t = token.Trim();
        if (t.EndsWith("pt", StringComparison.OrdinalIgnoreCase))
        {
            t = t[..^2].Trim();
        }

        return float.TryParse(t, NumberStyles.Float, CultureInfo.InvariantCulture, out size) && size > 0;
    }

    private static bool TryParseStyle(string token, out FontStyle style)
    {
        style = FontStyle.Regular;
        string t = token.Trim();
        if (t.Length == 0)
        {
            return false;
        }

        // Support "bold italic" / "Bold,Italic" as a single comma-split token ("bold italic").
        FontStyle combined = FontStyle.Regular;
        bool any = false;
        foreach (string word in t.Split([' ', '|', '+'],
                     StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
        {
            if (Enum.TryParse(word, true, out FontStyle one) && one != FontStyle.Regular)
            {
                combined |= one;
                any = true;
            }
            else if (word.Equals("regular", StringComparison.OrdinalIgnoreCase))
            {
                any = true;
            }
            else
            {
                return false;
            }
        }

        style = combined;
        return any || t.Equals("regular", StringComparison.OrdinalIgnoreCase);
    }

    //public Font() {
    //}
    //public void Dispose() {
    //    Dispose(true);
    //    GC.SuppressFinalize(this);
    //}

    //// Protected implementation of Dispose pattern.
    //// Flag: Has Dispose already been called?
    //private bool disposed = false;
    //protected virtual void Dispose(bool disposing) {
    //    if (disposed)
    //        return;

    //    if (disposing) {
    //        //if (font != null) font.Dispose();
    //    }
    //    disposed = true;
    //}
}

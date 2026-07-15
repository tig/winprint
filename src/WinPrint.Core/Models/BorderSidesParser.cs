// Copyright Kindel, LLC - http://www.kindel.com
// Published under the MIT License at https://github.com/tig/winprint

namespace WinPrint.Core.Models;

/// <summary>
///     Parses compact border side lists: <c>none</c>, <c>all</c>, <c>top,bottom</c>, <c>left|right</c>.
/// </summary>
public static class BorderSidesParser
{
    public static bool TryParse(string? text, out BorderSides sides)
    {
        sides = BorderSides.None;
        if (string.IsNullOrWhiteSpace(text))
        {
            return false;
        }

        string raw = text.Trim();
        if (raw.Equals("none", StringComparison.OrdinalIgnoreCase))
        {
            sides = BorderSides.None;
            return true;
        }

        if (raw.Equals("all", StringComparison.OrdinalIgnoreCase))
        {
            sides = BorderSides.All;
            return true;
        }

        BorderSides combined = BorderSides.None;
        string[] tokens = raw.Split([',', '|', '+', ' '],
            StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        if (tokens.Length == 0)
        {
            return false;
        }

        foreach (string token in tokens)
        {
            if (!TryParseOne(token, out BorderSides one))
            {
                return false;
            }

            combined |= one;
        }

        sides = combined;
        return true;
    }

    private static bool TryParseOne(string token, out BorderSides side)
    {
        side = token.ToLowerInvariant() switch
        {
            "left" or "l" => BorderSides.Left,
            "top" or "t" => BorderSides.Top,
            "right" or "r" => BorderSides.Right,
            "bottom" or "b" => BorderSides.Bottom,
            _ => BorderSides.None
        };
        return side != BorderSides.None || token.Equals("none", StringComparison.OrdinalIgnoreCase);
    }
}

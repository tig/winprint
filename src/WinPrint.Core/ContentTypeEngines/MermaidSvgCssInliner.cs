// Copyright Kindel, LLC - http://www.kindel.com
// Published under the MIT License at https://github.com/tig/winprint

using System.Globalization;
using System.Text.RegularExpressions;

namespace WinPrint.Core.ContentTypeEngines;

/// <summary>
///     Resolves the CSS custom properties in Mermaider's SVG output into literal values.
///     Mermaider styles every element through presentation attributes that reference
///     <c>var(--…)</c> custom properties (declared on the root <c>style</c> attribute and derived in a
///     <c>&lt;style&gt;</c> block via <c>var()</c> fallbacks and <c>color-mix(in srgb, …)</c>).
///     Svg.Skia does not evaluate custom properties, so without this pass nodes rasterize as black
///     boxes and edges vanish. Handles exactly the constructs Mermaider emits: <c>var(--x)</c>,
///     <c>var(--x, fallback)</c>, <c>color-mix(in srgb, #hex N%, #hex)</c>, and <c>rem</c> font sizes
///     (converted at 16 px/rem).
/// </summary>
internal static partial class MermaidSvgCssInliner
{
    [GeneratedRegex(@"(--[\w-]+)\s*:\s*([^;]+);", RegexOptions.CultureInvariant)]
    private static partial Regex DeclRegex();

    [GeneratedRegex(@"var\((--[\w-]+)(?:\s*,\s*((?:[^()]|\((?:[^()]|\([^()]*\))*\))*))?\)", RegexOptions.CultureInvariant)]
    private static partial Regex VarRegex();

    [GeneratedRegex(@"color-mix\(\s*in\s+srgb\s*,\s*(#[0-9a-fA-F]{3,8})\s+([\d.]+)%\s*,\s*(#[0-9a-fA-F]{3,8})\s*\)", RegexOptions.CultureInvariant)]
    private static partial Regex ColorMixRegex();

    [GeneratedRegex(@"([\d.]+)rem", RegexOptions.CultureInvariant)]
    private static partial Regex RemRegex();

    [GeneratedRegex(@"<svg[^>]*\sstyle=""([^""]*)""", RegexOptions.CultureInvariant)]
    private static partial Regex RootStyleRegex();

    [GeneratedRegex(@"<style>(.*?)</style>", RegexOptions.Singleline | RegexOptions.CultureInvariant)]
    private static partial Regex StyleBlockRegex();

    public static string Inline(string svg)
    {
        var vars = new Dictionary<string, string>(StringComparer.Ordinal);

        // Root vars from the <svg style="--bg:#fff;…"> attribute (reflects RenderOptions colors).
        Match rootStyle = RootStyleRegex().Match(svg);
        if (rootStyle.Success)
        {
            CollectDeclarations(rootStyle.Groups[1].Value + ";", vars);
        }

        // Derived vars from <style> blocks, resolved in declaration order (later ones reference earlier).
        foreach (Match style in StyleBlockRegex().Matches(svg))
        {
            CollectDeclarations(style.Groups[1].Value, vars);
        }

        string result = VarRegex().Replace(svg, m => Resolve(m.Value, vars));
        // The <style> declarations keep their (now var-free) color-mix() text; evaluate those too so
        // the output contains only literals.
        result = ColorMixRegex().Replace(result, m => Mix(
            m.Groups[1].Value,
            double.Parse(m.Groups[2].Value, CultureInfo.InvariantCulture) / 100.0,
            m.Groups[3].Value));
        return RemRegex().Replace(result, m =>
            (double.Parse(m.Groups[1].Value, CultureInfo.InvariantCulture) * 16).ToString(CultureInfo.InvariantCulture));
    }

    private static void CollectDeclarations(string css, Dictionary<string, string> vars)
    {
        foreach (Match decl in DeclRegex().Matches(css))
        {
            vars[decl.Groups[1].Value] = Resolve(decl.Groups[2].Value.Trim(), vars);
        }
    }

    private static string Resolve(string value, Dictionary<string, string> vars)
    {
        // Bounded: Mermaider nests var() inside color-mix() inside var() fallbacks a few levels deep.
        for (int i = 0; i < 8; i++)
        {
            string before = value;
            value = VarRegex().Replace(value, m =>
            {
                if (vars.TryGetValue(m.Groups[1].Value, out string? v))
                {
                    return v;
                }

                return m.Groups[2].Success ? m.Groups[2].Value.Trim() : "#000000";
            });
            value = ColorMixRegex().Replace(value, m => Mix(
                m.Groups[1].Value,
                double.Parse(m.Groups[2].Value, CultureInfo.InvariantCulture) / 100.0,
                m.Groups[3].Value));
            if (value == before)
            {
                break;
            }
        }

        return value;
    }

    private static string Mix(string colorA, double weightA, string colorB)
    {
        (byte r1, byte g1, byte b1) = ParseHex(colorA);
        (byte r2, byte g2, byte b2) = ParseHex(colorB);
        byte r = (byte)Math.Round((r1 * weightA) + (r2 * (1 - weightA)));
        byte g = (byte)Math.Round((g1 * weightA) + (g2 * (1 - weightA)));
        byte b = (byte)Math.Round((b1 * weightA) + (b2 * (1 - weightA)));
        return $"#{r:X2}{g:X2}{b:X2}";
    }

    private static (byte R, byte G, byte B) ParseHex(string hex)
    {
        hex = hex.TrimStart('#');
        if (hex.Length == 3)
        {
            hex = $"{hex[0]}{hex[0]}{hex[1]}{hex[1]}{hex[2]}{hex[2]}";
        }

        return (Convert.ToByte(hex[..2], 16), Convert.ToByte(hex.Substring(2, 2), 16),
            Convert.ToByte(hex.Substring(4, 2), 16));
    }
}

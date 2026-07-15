// Copyright Kindel, LLC - http://www.kindel.com
// Published under the MIT License at https://github.com/tig/winprint

using WinPrint.Core.Models;

namespace WinPrint.Core.ViewModels;

/// <summary>
///     Applies already-resolved <see cref="Options" /> to an <see cref="AppViewModel" />.
///     Extracted from the view model so CLI application stays under a healthy file size and
///     can evolve without growing <c>AppViewModel</c> past 1k lines.
/// </summary>
public static class CliOptionsApplier
{
    /// <summary>
    ///     Applies print/layout/header-footer CLI options. Call
    ///     <see cref="CliOptionsResolver.ResolveInPlace" /> first for printer/paper names.
    /// </summary>
    /// <returns>The first file argument, or <c>null</c>.</returns>
    public static string? Apply(AppViewModel app, Options options)
    {
        ArgumentNullException.ThrowIfNull(app);
        if (options is null)
        {
            return null;
        }

        // Sheet first so subsequent overrides land on the right definition.
        if (!string.IsNullOrEmpty(options.Sheet))
        {
            if (app.SelectSheetByNameOrId(options.Sheet))
            {
                app.LockSheetFromCliOptions();
            }
        }

        app.BeginSuppressReflow();
        try
        {
            if (options.Landscape)
            {
                app.SetLandscape(true);
            }
            else if (options.Portrait)
            {
                app.SetLandscape(false);
            }

            if (!string.IsNullOrEmpty(options.Printer))
            {
                app.SetPrinterName(options.Printer);
            }

            if (!string.IsNullOrEmpty(options.PaperSize))
            {
                app.SetPaperSize(options.PaperSize);
            }

            if (options.FromPage > 0)
            {
                app.SetFromToSheets(options.FromPage, options.ToPage > 0 ? options.ToPage : null);
            }
            else if (options.ToPage > 0)
            {
                app.SetFromToSheets(null, options.ToPage);
            }

            if (options.Rows > 0)
            {
                app.SetRows(options.Rows);
            }

            if (options.Columns > 0)
            {
                app.SetColumns(options.Columns);
            }

            ApplyHeaderFooter(app, options);
        }
        finally
        {
            app.EndSuppressReflow();
        }

        return options.Files?.FirstOrDefault();
    }

    private static void ApplyHeaderFooter(AppViewModel app, Options options)
    {
        if (options.HeaderOn)
        {
            app.SetHeaderEnabled(true);
        }
        else if (options.HeaderOff)
        {
            app.SetHeaderEnabled(false);
        }

        if (options.FooterOn)
        {
            app.SetFooterEnabled(true);
        }
        else if (options.FooterOff)
        {
            app.SetFooterEnabled(false);
        }

        if (options.HeaderText is not null)
        {
            app.SetHeaderText(options.HeaderText);
        }

        if (options.FooterText is not null)
        {
            app.SetFooterText(options.FooterText);
        }

        if (!string.IsNullOrEmpty(options.HeaderFont))
        {
            if (!Font.TryParse(options.HeaderFont, out Font? font) || font is null)
            {
                throw new InvalidOperationException(
                    $"Invalid value for --header-font: '{options.HeaderFont}' " +
                    "(expected e.g. \"Cascadia Code, 9, bold\").");
            }

            app.SetHeaderFont(font);
        }

        if (!string.IsNullOrEmpty(options.FooterFont))
        {
            if (!Font.TryParse(options.FooterFont, out Font? font) || font is null)
            {
                throw new InvalidOperationException(
                    $"Invalid value for --footer-font: '{options.FooterFont}' " +
                    "(expected e.g. \"Comic Sans MS, 10, bold\").");
            }

            app.SetFooterFont(font);
        }

        if (options.HeaderBorders is not null)
        {
            ApplyBorders(app, true, options.HeaderBorders, "--header-borders");
        }

        if (options.FooterBorders is not null)
        {
            ApplyBorders(app, false, options.FooterBorders, "--footer-borders");
        }
    }

    private static void ApplyBorders(AppViewModel app, bool header, string value, string optionName)
    {
        if (!BorderSidesParser.TryParse(value, out BorderSides sides))
        {
            throw new InvalidOperationException(
                $"Invalid value for {optionName}: '{value}' " +
                "(use none, all, or a list such as top,bottom).");
        }

        if (header)
        {
            app.SetHeaderBorders(sides);
        }
        else
        {
            app.SetFooterBorders(sides);
        }
    }
}

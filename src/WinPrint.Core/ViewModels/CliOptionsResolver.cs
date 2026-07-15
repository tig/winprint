// Copyright Kindel, LLC - http://www.kindel.com
// Published under the MIT License at https://github.com/tig/winprint

using WinPrint.Core.Models;

namespace WinPrint.Core.ViewModels;

/// <summary>
///     CLI-edge resolution for <c>--printer</c> / <c>--paper-size</c>: rewrites
///     <see cref="Options" /> fields to canonical installed names or throws
///     <see cref="InvalidOperationException" />. Call before
///     <see cref="AppViewModel.ApplyOptions" />, which only applies already-resolved values (#264).
/// </summary>
public static class CliOptionsResolver
{
    /// <summary>
    ///     Resolves partial names in place. Printer always validates against
    ///     <paramref name="printers" /> when set. Paper validates only when
    ///     <paramref name="paperSizes" /> is non-null (TUI may omit a paper list).
    /// </summary>
    public static void ResolveInPlace(
        Options options,
        IReadOnlyList<string> printers,
        IReadOnlyList<string>? paperSizes = null)
    {
        ArgumentNullException.ThrowIfNull(options);
        ArgumentNullException.ThrowIfNull(printers);

        if (!string.IsNullOrEmpty(options.Printer))
        {
            NamedChoiceMatch match = NamedChoiceResolver.Resolve(options.Printer, printers, "printer");
            if (!match.Success)
            {
                throw new InvalidOperationException(match.Error);
            }

            options.Printer = match.Name;
        }

        if (!string.IsNullOrEmpty(options.PaperSize) && paperSizes is not null)
        {
            NamedChoiceMatch match = NamedChoiceResolver.Resolve(options.PaperSize, paperSizes, "paper size");
            if (!match.Success)
            {
                throw new InvalidOperationException(match.Error);
            }

            options.PaperSize = match.Name;
        }
    }
}

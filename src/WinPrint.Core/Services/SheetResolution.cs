using WinPrint.Core.Models;

namespace WinPrint.Core.Services;

/// <summary>
///     Resolves which sheet definition to apply when opening a file.
/// </summary>
public static class SheetResolution
{
    /// <summary>
    ///     Chooses a sheet key for a fresh file open when no explicit <c>--sheet</c> override is in play.
    ///     Mapped content types win; otherwise the user's persisted <see cref="Settings.DefaultSheet" />.
    /// </summary>
    public static Guid ResolveSheetForOpen(Settings settings, string contentTypeId)
    {
        ArgumentNullException.ThrowIfNull(settings);

        if (!string.IsNullOrEmpty(contentTypeId)
            && settings.DefaultSheetByContentType.TryGetValue(contentTypeId, out string? sheetKey)
            && settings.Sheets.ContainsKey(sheetKey)
            && Guid.TryParse(sheetKey, out Guid mapped))
        {
            return mapped;
        }

        return settings.DefaultSheet;
    }
}
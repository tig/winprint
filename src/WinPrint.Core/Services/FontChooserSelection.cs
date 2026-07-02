namespace WinPrint.Core.Services;

public static class FontChooserSelection
{
    public static string SelectVisibleFamily(IReadOnlyList<string> visibleFamilies, string currentFamily)
    {
        string? visibleCurrent = visibleFamilies.FirstOrDefault(f =>
            string.Equals(f, currentFamily, StringComparison.OrdinalIgnoreCase));

        if (visibleCurrent is not null)
        {
            return visibleCurrent;
        }

        return visibleFamilies.Count > 0 ? visibleFamilies[0] : string.Empty;
    }
}

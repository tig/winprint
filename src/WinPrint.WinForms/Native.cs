using System.Diagnostics.CodeAnalysis;
using System.Runtime.InteropServices;
using System.Text;

namespace WinPrint.WinForms;

internal class NativeMethods
{
    [DllImport("Shlwapi.dll", SetLastError = true, CharSet = CharSet.Unicode)]
    private static extern uint AssocQueryString(AssocF flags, AssocStr str, string pszAssoc, string? pszExtra,
        [Out] StringBuilder? pszOut, [In][Out] ref uint pcchOut);

    [SuppressMessage("Performance", "CA1806:Do not ignore method results", Justification = "<Pending>")]
    public static string FileExtentionInfo(AssocStr assocStr, string doctype)
    {
        uint pcchOut = 0;
        AssocQueryString(AssocF.Verify, assocStr, doctype, null, null, ref pcchOut);

        var pszOut = new StringBuilder((int)pcchOut);
        AssocQueryString(AssocF.Verify, assocStr, doctype, null, pszOut, ref pcchOut);
        return pszOut.ToString();
    }
}

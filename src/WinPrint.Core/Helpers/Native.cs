using System;
using System.Runtime.InteropServices;
using System.Text;

namespace WinPrint {
    class NativeMethods {

        [DllImport("Shlwapi.dll", SetLastError = true, CharSet = CharSet.Unicode)]
        static extern uint AssocQueryString(AssocF flags, AssocStr str, string pszAssoc, string pszExtra, [Out] StringBuilder pszOut, [In][Out] ref uint pcchOut);

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Performance", "CA1806:Do not ignore method results", Justification = "<Pending>")]
        public static string FileExtentionInfo(AssocStr assocStr, string doctype) {
            uint pcchOut = 0;
            AssocQueryString(AssocF.Verify, assocStr, doctype, null, null, ref pcchOut);

            StringBuilder pszOut = new StringBuilder((int)pcchOut);
            AssocQueryString(AssocF.Verify, assocStr, doctype, null, pszOut, ref pcchOut);
            return pszOut.ToString();
        }

        [Flags]
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Naming", "CA1714:Flags enums should have plural names", Justification = "<Pending>")]
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Naming", "CA1707:Identifiers should not contain underscores", Justification = "<Pending>")]
        public enum AssocF {
            Init_NoRemapCLSID = 0x1,
            Init_ByExeName = 0x2,
            Open_ByExeName = 0x2,
            Init_DefaultToStar = 0x4,
            Init_DefaultToFolder = 0x8,
            NoUserSettings = 0x10,
            NoTruncate = 0x20,
            Verify = 0x40,
            RemapRunDll = 0x80,
            NoFixUps = 0x100,
            IgnoreBaseClass = 0x200
        }

        public enum AssocStr {
            Command = 1,
            Executable,
            FriendlyDocName,
            FriendlyAppName,
            NoOpen,
            ShellNewValue,
            DDECommand,
            DDEIfExec,
            DDEApplication,
            DDETopic
        }
    }
}

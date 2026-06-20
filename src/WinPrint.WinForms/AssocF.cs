using System.Diagnostics.CodeAnalysis;

namespace WinPrint.WinForms;

[Flags]
[SuppressMessage("Naming", "CA1714:Flags enums should have plural names", Justification = "<Pending>")]
[SuppressMessage("Naming", "CA1707:Identifiers should not contain underscores", Justification = "<Pending>")]
public enum AssocF
{
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

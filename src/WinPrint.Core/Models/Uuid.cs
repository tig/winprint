namespace WinPrint.Core;

// WinPrint 2 has been assigned the following GUIDs by Microsoft
//
// Available range: 0002A5xx-0000-0000-C000-000000000046
//
// {0002A500-0000-0000-C000-000000000046} - Wix Installer WinPrint UpgradeCode (product family code), Default Sheet ID 
// {0002A501-0000-0000-C000-000000000046} - Wix Installer Product Code, Default 1SheetUp ID 
// {0002A502-0000-0000-C000-000000000046} - Proportional 2-Up sheet ID
// {0002A503-0000-0000-C000-000000000046} - Proportional 1-Up sheet ID
//
public static class Uuid
{
    public static readonly Guid UpgradeCode = Guid.Parse("{0002A500-0000-0000-C000-000000000046}");
    public static readonly Guid ProductCode = Guid.Parse("{0002A501-0000-0000-C000-000000000046}");

    public static readonly Guid DefaultSheet = Guid.Parse("{0002A500-0000-0000-C000-000000000046}");
    public static readonly Guid DefaultSheet1Up = Guid.Parse("{0002A501-0000-0000-C000-000000000046}");
    public static readonly Guid ProportionalSheet2Up = Guid.Parse("{0002A502-0000-0000-C000-000000000046}");
    public static readonly Guid ProportionalSheet1Up = Guid.Parse("{0002A503-0000-0000-C000-000000000046}");
}

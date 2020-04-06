using System;

namespace WinPrint.Core {

    // WinPrint 2.0 has been assigned the following GUIDs by Microsoft
    //
    // Available range: 0002A5xx-0000-0000-C000-000000000046
    //
    // {0002A500-0000-0000-C000-000000000046} - Wix Installer WinPrint UpgradeCode (product family code), Default Sheet ID 
    // {0002A501-0000-0000-C000-000000000046} - Wix Installer Product Code, Default 1SheetUp ID 
    //
    public static class Uuid {
        public static readonly Guid UpgradeCode = Guid.Parse("{0002A500-0000-0000-C000-000000000046}");
        public static readonly Guid ProductCode = Guid.Parse("{0002A501-0000-0000-C000-000000000046}");

        public static readonly Guid DefaultSheet = Guid.Parse("{0002A500-0000-0000-C000-000000000046}");
        public static readonly Guid DefaultSheet1Up = Guid.Parse("{0002A501-0000-0000-C000-000000000046}");
    }
}

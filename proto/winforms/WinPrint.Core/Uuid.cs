using System;
using System.Collections.Generic;
using System.Text;

namespace WinPrint.Core {

    // WinPrint 2.0 has been assigned the following GUIDs by Microsoft
    //
    // Available range: 0002A5xx-0000-0000-C000-000000000046
    //
    public static class Uuid {
        public static readonly Guid DefaultSheet = Guid.Parse("{0002A500-0000-0000-C000-000000000046}");

    }
}

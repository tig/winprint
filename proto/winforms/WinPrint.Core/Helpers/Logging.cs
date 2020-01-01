using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text;

namespace WinPrint.Core.Helpers {
    public class Logging {
        public static void TraceMessage(string message = "",
        [System.Runtime.CompilerServices.CallerMemberName] string memberName = "",
        [System.Runtime.CompilerServices.CallerFilePath] string sourceFilePath = "",
        [System.Runtime.CompilerServices.CallerLineNumber] int sourceLineNumber = 0) {
            Trace.WriteLine($"{Path.GetFileName(sourceFilePath)}:{sourceLineNumber} {memberName}: {message}");
        }
    }
}

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text;

namespace WinPrint.LiteHtml {
    public class Logging {
        public static void TraceMessage(string message = "",
        [System.Runtime.CompilerServices.CallerMemberName] string memberName = "",
        [System.Runtime.CompilerServices.CallerFilePath] string sourceFilePath = "",
        [System.Runtime.CompilerServices.CallerLineNumber] int sourceLineNumber = 0) {
            Trace.WriteLine($"{DateTime.Now:mm:ss.fff}:{Path.GetFileName(sourceFilePath)}:{sourceLineNumber} {memberName}: {message}");
        }
    }
}

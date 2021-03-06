﻿using System.IO;
using Serilog;

namespace WinPrint.LiteHtml {
    public class Logging {
        public static void TraceMessage(string message = "",
        [System.Runtime.CompilerServices.CallerMemberName] string memberName = "",
        [System.Runtime.CompilerServices.CallerFilePath] string sourceFilePath = "",
        [System.Runtime.CompilerServices.CallerLineNumber] int sourceLineNumber = 0) {
            Log.Debug($"{Path.GetFileName(sourceFilePath)}:{sourceLineNumber} {memberName}: {message}");
        }
    }
}

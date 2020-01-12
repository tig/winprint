using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using Microsoft.Extensions.Configuration;
using Serilog;
using Serilog.Core;
using Serilog.Events;

namespace WinPrint.Core.Services {
    public class LogService {
        public string LogPath { get; set; }
        public LoggingLevelSwitch MasterLevelSwitch { get; set; } = new LoggingLevelSwitch();
        public LoggingLevelSwitch FileLevelSwitch { get; set; } = new LoggingLevelSwitch();
        public LoggingLevelSwitch ConsoleLevelSwitch { get; set; } = new LoggingLevelSwitch();
        public LoggingLevelSwitch DebugLevelSwitch { get; set; } = new LoggingLevelSwitch();

        public void Start(string path) {
            LogPath = $"{path}logs\\{AppDomain.CurrentDomain.FriendlyName}.txt".Replace(@"file:\", "");

            MasterLevelSwitch.MinimumLevel = LogEventLevel.Verbose;
            DebugLevelSwitch.MinimumLevel = LogEventLevel.Debug;

#if DEBUG
            FileLevelSwitch.MinimumLevel = LogEventLevel.Debug;
            ConsoleLevelSwitch.MinimumLevel = LogEventLevel.Debug;
#else
            FileLevelSwitch.MinimumLevel = LogEventLevel.Debug;
            ConsoleLevelSwitch.MinimumLevel = LogEventLevel.Information;
#endif 

            Log.Logger = new LoggerConfiguration()
                .MinimumLevel.ControlledBy(MasterLevelSwitch)
                .WriteTo.Console(levelSwitch: ConsoleLevelSwitch)
                .WriteTo.Debug(levelSwitch: DebugLevelSwitch)
                .WriteTo.File(LogPath, shared: true, levelSwitch: FileLevelSwitch)
                .CreateLogger();

            string productVersion = FileVersionInfo.GetVersionInfo(Assembly.GetAssembly(typeof(LogService)).Location).FileVersion;
            Log.Debug("--------- {app} {v} ---------", AppDomain.CurrentDomain.FriendlyName, productVersion);
            Log.Debug("Logging to {path}", ServiceLocator.Current.LogService.LogPath);
        }

        public LogService() {

        }
        public static void TraceMessage(string msg = "",
        [System.Runtime.CompilerServices.CallerMemberName] string memberName = "",
        [System.Runtime.CompilerServices.CallerFilePath] string sourceFilePath = "",
        [System.Runtime.CompilerServices.CallerLineNumber] int sourceLineNumber = 0) {
            Log.Logger.Debug($"{Path.GetFileName(sourceFilePath)}:{sourceLineNumber} {memberName}: {{msg}}", msg);
        }

        public static string GetTraceMsg(
            [System.Runtime.CompilerServices.CallerMemberName] string memberName = "",
            [System.Runtime.CompilerServices.CallerFilePath] string sourceFilePath = "",
            [System.Runtime.CompilerServices.CallerLineNumber] int sourceLineNumber = 0) {
            return $"{Path.GetFileName(sourceFilePath)}:{sourceLineNumber} {memberName}: {{msg}}";
        }
    }
}

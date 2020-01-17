using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Microsoft.Extensions.Configuration;
using Serilog;
using Serilog.Core;
using Serilog.Events;
using WinPrint.Core.Helpers;

namespace WinPrint.Core.Services {
    public class LogService {
        public string LogPath { get; set; }
        public LoggingLevelSwitch MasterLevelSwitch { get; set; } = new LoggingLevelSwitch();
        public LoggingLevelSwitch FileLevelSwitch { get; set; } = new LoggingLevelSwitch();
        public LoggingLevelSwitch ConsoleLevelSwitch { get; set; } = new LoggingLevelSwitch();
        public LoggingLevelSwitch DebugLevelSwitch { get; set; } = new LoggingLevelSwitch();

        public void Start(string path) {
            MasterLevelSwitch.MinimumLevel = LogEventLevel.Verbose;
            DebugLevelSwitch.MinimumLevel = LogEventLevel.Debug;

#if DEBUG
            FileLevelSwitch.MinimumLevel = LogEventLevel.Debug;
            ConsoleLevelSwitch.MinimumLevel = LogEventLevel.Debug;
#else
            FileLevelSwitch.MinimumLevel = LogEventLevel.Debug;
            ConsoleLevelSwitch.MinimumLevel = LogEventLevel.Information;
#endif 

            LogPath = $"{path}logs{Path.DirectorySeparatorChar}{AppDomain.CurrentDomain.FriendlyName}.txt".Replace(@"file:\", "");
            Log.Logger = new LoggerConfiguration()
                .MinimumLevel.ControlledBy(MasterLevelSwitch)
                .WriteTo.Console(levelSwitch: ConsoleLevelSwitch)
                .WriteTo.Debug(levelSwitch: DebugLevelSwitch)
                .WriteTo.File(LogPath, shared: true, levelSwitch: FileLevelSwitch)
                .CreateLogger();

            string productVersion = FileVersionInfo.GetVersionInfo(Assembly.GetAssembly(typeof(LogService)).Location).FileVersion;
            Log.Debug("--------- {app} {v} ---------", AppDomain.CurrentDomain.FriendlyName, productVersion);
            Log.Debug("Logging to {path}", ServiceLocator.Current.LogService.LogPath);
            Log.Debug("OS Environment: {os} version: version, architecture: {arch}, .NET version: {dotnet}",
                Environment.OSVersion, Environment.Is64BitProcess ? "x64" : "x86", Environment.Version);
            if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows)) {
                Log.Debug("libgdiplus version: {v}", Diagnostics.GetLibgdiplusVersion());
            }

        }

        public LogService() {

        }
        public static void TraceMessage(string msg = "",
        [System.Runtime.CompilerServices.CallerMemberName] string memberName = "",
        [System.Runtime.CompilerServices.CallerFilePath] string sourceFilePath = "",
        [System.Runtime.CompilerServices.CallerLineNumber] int sourceLineNumber = 0) {
            Log.Logger.Debug($"{Path.GetFileName(sourceFilePath)}:{sourceLineNumber} {memberName}: {{msg}}", msg);
        }

        /// <summary>
        /// Generates a trace message. 
        /// e.g. `Log.Debug(LogService.GetTraceMsg());`
        ///      `Log.Debug(LogService.GetTraceMsg("{n} PageUnit: {pu}"), sheetNum, graphics.PageUnit);`
        /// </summary>
        /// <param name="msg">Optional string that will be appended. This can be a Serilog messageTemplate.</param>
        /// <param name="memberName"></param>
        /// <param name="sourceFilePath"></param>
        /// <param name="sourceLineNumber"></param>
        /// <returns></returns>
        public static string GetTraceMsg(string msg = "",
            [System.Runtime.CompilerServices.CallerMemberName] string memberName = "",
            [System.Runtime.CompilerServices.CallerFilePath] string sourceFilePath = "",
            [System.Runtime.CompilerServices.CallerLineNumber] int sourceLineNumber = 0) {
            return $"{Path.GetFileName(sourceFilePath)}:{sourceLineNumber} {memberName}: {msg}";
        }
    }
}

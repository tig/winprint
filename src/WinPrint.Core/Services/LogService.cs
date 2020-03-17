using System;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Runtime.InteropServices;
using Serilog;
using Serilog.Core;
using Serilog.Events;
using WinPrint.Core.Helpers;

namespace WinPrint.Core.Services {
    /// <summary>
    /// Configures Serilog for logging to the console and logfiles. Provides simple helper
    /// functions for tracing. 
    /// </summary>
    public class LogService {
        public string LogPath { get; set; }
        public LoggingLevelSwitch MasterLevelSwitch { get; set; } = new LoggingLevelSwitch();
        public LoggingLevelSwitch FileLevelSwitch { get; set; } = new LoggingLevelSwitch();
        public LoggingLevelSwitch ConsoleLevelSwitch { get; set; } = new LoggingLevelSwitch();
        public LoggingLevelSwitch DebugLevelSwitch { get; set; } = new LoggingLevelSwitch();

        public void Start(string appName) {
            MasterLevelSwitch.MinimumLevel = LogEventLevel.Verbose;
            DebugLevelSwitch.MinimumLevel = LogEventLevel.Debug;

#if DEBUG
            FileLevelSwitch.MinimumLevel = LogEventLevel.Debug;
            ConsoleLevelSwitch.MinimumLevel = LogEventLevel.Debug;
#else
            FileLevelSwitch.MinimumLevel = LogEventLevel.Debug;
            ConsoleLevelSwitch.MinimumLevel = LogEventLevel.Information;
#endif
            string productVersion = FileVersionInfo.GetVersionInfo(Assembly.GetAssembly(typeof(LogService)).Location).FileVersion;
            LogPath = $"{SettingsService.SettingsPath}logs{Path.DirectorySeparatorChar}{AppDomain.CurrentDomain.FriendlyName}.txt".Replace(@"file:\", "");

            // Setup logging
            Log.Logger = new LoggerConfiguration()
                .MinimumLevel.ControlledBy(MasterLevelSwitch)
                .WriteTo.Console(levelSwitch: ConsoleLevelSwitch)
                .WriteTo.Debug(levelSwitch: DebugLevelSwitch)
                .WriteTo.File(LogPath, shared: true, levelSwitch: FileLevelSwitch)
                ////.WriteTo.ApplicationInsights(config, new CustomConverter(), restrictedToMinimumLevel: LogEventLevel.Information)
                .CreateLogger();

            Log.Debug("--------- {app} {v} ---------", appName, productVersion);
            if (ServiceLocator.Current.TelemetryService.TelemetryEnabled) {
                string msg = string.IsNullOrEmpty(TelemetryService.Key) ? "However, telemetry key is missing so no telemetry will be tracked." : "";
                Log.Debug($"Telemetry is enabled. {msg}");
            }
            Log.Debug("Logging to {path}", ServiceLocator.Current.LogService.LogPath);
            Log.Debug("OS Environment: {os}, architecture: {arch}, .NET version: {dotnet}",
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
            Log.Debug($"{Path.GetFileName(sourceFilePath)}:{sourceLineNumber} {memberName}: {{msg}}", msg);
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

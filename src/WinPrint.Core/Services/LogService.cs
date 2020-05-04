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

        /// <summary>
        /// Starts Serilog-based logging. 
        /// </summary>
        /// <param name="appName">The name used to identify the log entries; emitted in the first log entry of each run.</param>
        /// <param name="consoleSink">Provides the ILogEventSink for the console.</param>
        /// <param name="debug">If true, the console log will emit LogEventLevel.Debug entries</param>
        /// <param name="verbose">If true, the console log will emit LogEventLevel.Information entries.</param>
        public void Start(string appName, ILogEventSink consoleSink = null, bool debug = false, bool verbose = false) {
            MasterLevelSwitch.MinimumLevel = LogEventLevel.Verbose;
            DebugLevelSwitch.MinimumLevel = LogEventLevel.Debug;

            ConsoleLevelSwitch.MinimumLevel = (verbose ? LogEventLevel.Information : LogEventLevel.Warning);
            ConsoleLevelSwitch.MinimumLevel = (debug ? LogEventLevel.Debug : ConsoleLevelSwitch.MinimumLevel);

#if DEBUG
            FileLevelSwitch.MinimumLevel = LogEventLevel.Debug;
#else
            // TODO: Keep this at Debug until after Beta, then change it to Information
            FileLevelSwitch.MinimumLevel = LogEventLevel.Debug;
#endif
            var productVersion = FileVersionInfo.GetVersionInfo(Assembly.GetAssembly(typeof(LogService)).Location).FileVersion;
            LogPath = $"{SettingsService.SettingsPath}{Path.DirectorySeparatorChar}logs{Path.DirectorySeparatorChar}{appName}.log".Replace(@"file:\", "");

            if (consoleSink == null) {
                ConsoleLevelSwitch.MinimumLevel = LogEventLevel.Fatal;
            }

            // Setup logging
            if (consoleSink == null) {
                // GUI
                Log.Logger = new LoggerConfiguration()
                    .MinimumLevel.ControlledBy(MasterLevelSwitch)
                    .WriteTo.Debug(levelSwitch: DebugLevelSwitch)
                    .WriteTo.File(LogPath, shared: true, levelSwitch: FileLevelSwitch)
                    .CreateLogger();
            }
            else {
                // Console / Powershell
                Log.Logger = new LoggerConfiguration()
                    .MinimumLevel.ControlledBy(MasterLevelSwitch)
                    .WriteTo.Sink(consoleSink, levelSwitch: ConsoleLevelSwitch)
                    .WriteTo.Debug(levelSwitch: DebugLevelSwitch)
                    .WriteTo.File(LogPath, shared: true, levelSwitch: FileLevelSwitch)
                    .CreateLogger();
            }

            Log.Debug("--------- {app} {v} ---------", appName, productVersion);
            if (ServiceLocator.Current.TelemetryService.TelemetryEnabled) {
#if CI_BUILD
                var msg = "Telemetry key is missing so no telemetry will be tracked." : "";
#else
                var msg = string.IsNullOrEmpty(TelemetryService.Key) ? "However, telemetry key is missing so no telemetry will be tracked." : "";
#endif
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

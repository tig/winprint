using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.ApplicationInsights;
using Microsoft.ApplicationInsights.Channel;
using Microsoft.ApplicationInsights.DataContracts;
using Microsoft.ApplicationInsights.Extensibility;
using Microsoft.ApplicationInsights.Extensibility.Implementation;
using Microsoft.Extensions.Configuration;
using Serilog;
using Serilog.Core;
using Serilog.Events;
using Serilog.Sinks.ApplicationInsights.Sinks.ApplicationInsights.TelemetryConverters;
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

        public bool TelemetryEnabled { get; set; } = true;

        public TelemetryClient GetTelemetryClient() => telemetry;
        private TelemetryClient telemetry;

        private Stopwatch runtime;

        public void Start(string path) {
            runtime = System.Diagnostics.Stopwatch.StartNew();

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
            LogPath = $"{path}logs{Path.DirectorySeparatorChar}{AppDomain.CurrentDomain.FriendlyName}.txt".Replace(@"file:\", "");

            // Setup telemetry via Azure Application Insights.
            var config = TelemetryConfiguration.CreateDefault();

            // Get key from UserSecrets in a way that never puts the key in source
            config.InstrumentationKey = TelemetryService.Key;

            // Turn off Debug spew
            TelemetryDebugWriter.IsTracingDisabled = true;
#if DEBUG
            config.TelemetryChannel.DeveloperMode = true;
#else
            config.TelemetryChannel.DeveloperMode = Debugger.IsAttached;
#endif

            telemetry = new TelemetryClient(config);
            telemetry.Context.Component.Version = productVersion;
            telemetry.Context.Session.Id = Guid.NewGuid().ToString();
            telemetry.Context.Device.OperatingSystem = Environment.OSVersion.ToString();
            // Anonymyize user ID
            using var h = SHA256.Create();
            h.Initialize();
            h.ComputeHash(Encoding.UTF8.GetBytes($"{Environment.UserName}/{Environment.MachineName}"));
            telemetry.Context.User.Id = Convert.ToBase64String(h.Hash);

            var properties = new Dictionary<string, string> {
                {"app", AppDomain.CurrentDomain.FriendlyName },
                {"version", productVersion},
                {"os", Environment.OSVersion.ToString()},
                {"arch", Environment.Is64BitProcess ? "x64" : "x86" },
                {"dotNetVersion", Environment.Version.ToString()}
            };
            TrackEvent("Application Started", properties);

            // Setup logging
            Log.Logger = new LoggerConfiguration()
                .MinimumLevel.ControlledBy(MasterLevelSwitch)
                .WriteTo.Console(levelSwitch: ConsoleLevelSwitch)
                .WriteTo.Debug(levelSwitch: DebugLevelSwitch)
                .WriteTo.File(LogPath, shared: true, levelSwitch: FileLevelSwitch)
                ////.WriteTo.ApplicationInsights(config, new CustomConverter(), restrictedToMinimumLevel: LogEventLevel.Information)
                .CreateLogger();

            Log.Debug("--------- {app} {v} ---------", AppDomain.CurrentDomain.FriendlyName, productVersion);
            if (TelemetryEnabled) {
                string msg = config.InstrumentationKey == "" ? "However, telemetry key is missing so no telemetry will be tracked." : "";
                Log.Debug($"Telemetry is enabled. {msg}");
            }
            Log.Debug("Logging to {path}", ServiceLocator.Current.LogService.LogPath);
            Log.Debug("OS Environment: {os}, architecture: {arch}, .NET version: {dotnet}",
                Environment.OSVersion, Environment.Is64BitProcess ? "x64" : "x86", Environment.Version);
            if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows)) {
                Log.Debug("libgdiplus version: {v}", Diagnostics.GetLibgdiplusVersion());
            }
        }

        public void Stop() {
            TrackEvent("Application Stopped", metrics: new Dictionary<string, double>
                {{"runTime", runtime.Elapsed.TotalMilliseconds}});
            // before exit, flush the remaining data
            Flush();

            // flush is not blocking so wait a bit
            Task.Delay(1000).Wait();
        }

        public void SetUser(string user) {
            telemetry.Context.User.AuthenticatedUserId = user;
        }

        public void TrackEvent(string key, IDictionary<string, string> properties = null, IDictionary<string, double> metrics = null) {
            if (TelemetryEnabled) {
                telemetry.TrackEvent(key, properties, metrics);
            }
        }

        public void TrackException(Exception ex, bool log = false) {
            if (ex != null && log is true)
                Log.Error(ex, "{msg}", ex.Message);

            if (ex != null && TelemetryEnabled) {
                var telex = new Microsoft.ApplicationInsights.DataContracts.ExceptionTelemetry(ex);
                telemetry.TrackException(telex);
                Flush();
            }
        }
        internal void Flush() {
            telemetry.Flush();
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

        //private class CustomConverter : TraceTelemetryConverter {
        //    public override IEnumerable<ITelemetry> Convert(LogEvent logEvent, IFormatProvider formatProvider) {
        //        // first create a default TraceTelemetry using the sink's default logic
        //        // .. but without the log level, and (rendered) message (template) included in the Properties
        //        foreach (ITelemetry telemetry in base.Convert(logEvent, formatProvider)) {
        //            // then go ahead and post-process the telemetry's context to contain the user id as desired
        //            if (logEvent.Properties.ContainsKey("UserId")) {
        //                telemetry.Context.User.Id = logEvent.Properties["UserId"].ToString();
        //            }
        //            // post-process the telemetry's context to contain the operation id
        //            if (logEvent.Properties.ContainsKey("operation_Id")) {
        //                telemetry.Context.Operation.Id = logEvent.Properties["operation_Id"].ToString();
        //            }
        //            // post-process the telemetry's context to contain the operation parent id
        //            if (logEvent.Properties.ContainsKey("operation_parentId")) {
        //                telemetry.Context.Operation.ParentId = logEvent.Properties["operation_parentId"].ToString();
        //            }
        //            // typecast to ISupportProperties so you can manipulate the properties as desired
        //            ISupportProperties propTelematry = (ISupportProperties)telemetry;

        //            // find redundent properties
        //            var removeProps = new[] { "UserId", "operation_parentId", "operation_Id" };
        //            removeProps = removeProps.Where(prop => propTelematry.Properties.ContainsKey(prop)).ToArray();

        //            foreach (var prop in removeProps) {
        //                // remove redundent properties
        //                propTelematry.Properties.Remove(prop);
        //            }

        //            yield return telemetry;
        //        }
        //    }
        //}
    }
}

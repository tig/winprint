using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Management.Automation;
using Serilog.Core;
using Serilog.Events;
using Serilog.Formatting;
//using TTRider.PowerShellAsync;

namespace WinPrint.Console {

    public class PowerShellSink : ILogEventSink {
        public static PowerShellSink Instance => _instance.Value;
        readonly static Lazy<PowerShellSink> _instance = new Lazy<PowerShellSink>(() => new PowerShellSink());


        readonly object _syncRoot = new object();

        // TODO: This is not thread-safe
        private Dictionary<int, AsyncCmdlet> _cmdlets = new Dictionary<int, AsyncCmdlet>();

        public ITextFormatter TextFormatter { get; set; }

        public PowerShellSink() {
            TextFormatter = new Serilog.Formatting.Display.MessageTemplateTextFormatter("{Message:lj}");
        }

        public void Register(AsyncCmdlet cmdlet) {
            if (cmdlet == null) {
                throw new ArgumentNullException(nameof(cmdlet));
            }
            _cmdlets[cmdlet.GetHashCode()] = cmdlet;
            //System.Console.WriteLine($"Register: {cmdlet.GetHashCode()}");
        }

        public void UnRegister(int cmdLetHash) {
            _cmdlets.Remove(cmdLetHash);
            //System.Console.WriteLine($"UnRegister: {cmdLetHash}");
        }

        public void Emit(LogEvent logEvent) {
            if (logEvent == null) {
                throw new ArgumentNullException(nameof(logEvent));
            }

            lock (_syncRoot) {
                //if (_cmdlet.ProcessingCount == 0) {
                //    Debug.WriteLine("PowerShellSink is disabled because a cmdlet is not processing.");
                //    return;
                //}
                foreach (var cmdlet in _cmdlets.Values)
                    EmitToCmdLet(cmdlet, logEvent);
            }
        }

        private void EmitToCmdLet(AsyncCmdlet cmdlet, LogEvent logEvent) {
            using var strWriter = new StringWriter();
            TextFormatter.Format(logEvent, strWriter);
            try {
                string msg = $"{strWriter}";
                switch (logEvent.Level) {
                    // -Verbose
                    case LogEventLevel.Verbose:
                    case LogEventLevel.Information:
                        cmdlet.WriteVerbose(msg);
                        break;

                    // -Debug
                    case LogEventLevel.Debug:
                        cmdlet.WriteDebug(msg);
                        break;

                    // The Write-Warning cmdlet writes a warning message to the PowerShell host
                    // The response to the warning depends on the value of the user's $WarningPreference variable and the use of the WarningAction common parameter.
                    case LogEventLevel.Warning:
                        cmdlet.WriteWarning(msg);
                        break;

                    // The Write-Error cmdlet declares a non-terminating error.
                    case LogEventLevel.Error:
                        //_cmdlet.WriteDebug("error: " + strWriter.ToString());
                        var ex = logEvent.Exception;
                        if (logEvent.Exception == null) {
                            ex = new Exception();
                        }

                        var er = new ErrorRecord(ex, errorId: msg, errorCategory: ErrorCategory.NotSpecified, targetObject: null);
                        cmdlet.WriteError(er);
                        break;

                    case LogEventLevel.Fatal:
                        //_cmdlet.WriteDebug("fatal: " + strWriter.ToString());
                        var fatal = new ErrorRecord(logEvent.Exception, errorId: msg, errorCategory: ErrorCategory.InvalidOperation, targetObject: null);
                        cmdlet.ThrowTerminatingError(fatal);
                        break;

                    default:
                        cmdlet.WriteDebug(msg);
                        break;
                }
            }
            catch (PipelineStoppedException pse) {
                Debug.WriteLine(pse.Message);
            }
            catch (PSInvalidOperationException psiop) {
                Debug.WriteLine(psiop.Message);
            }
        }
    }
}

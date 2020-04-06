using System;
using System.Diagnostics;
using System.IO;
using System.Management.Automation;
using Serilog.Core;
using Serilog.Events;
using Serilog.Formatting;
//using TTRider.PowerShellAsync;

namespace WinPrint.Console {

    public class PowerShellSink : ILogEventSink {
        readonly object _syncRoot = new object();
        private AsyncCmdlet _cmdlet;

        public ITextFormatter TextFormatter { get; set; }

        public PowerShellSink(AsyncCmdlet cmdlet) {
            if (cmdlet == null) {
                throw new ArgumentNullException(nameof(cmdlet));
            }

            _cmdlet = cmdlet;
            TextFormatter = new Serilog.Formatting.Display.MessageTemplateTextFormatter("{Message:lj}");
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

                using var strWriter = new StringWriter();
                TextFormatter.Format(logEvent, strWriter);
                try {
                    switch (logEvent.Level) {
                        // -Verbose
                        case LogEventLevel.Verbose:
                        case LogEventLevel.Information:
                            _cmdlet.WriteVerbose(strWriter.ToString());
                            break;

                        // -Debug
                        case LogEventLevel.Debug:
                            _cmdlet.WriteDebug(strWriter.ToString());
                            break;

                        // The Write-Warning cmdlet writes a warning message to the PowerShell host
                        // The response to the warning depends on the value of the user's $WarningPreference variable and the use of the WarningAction common parameter.
                        case LogEventLevel.Warning:
                            _cmdlet.WriteWarning(strWriter.ToString());
                            break;

                        // The Write-Error cmdlet declares a non-terminating error.
                        case LogEventLevel.Error:
                            //_cmdlet.WriteDebug("error: " + strWriter.ToString());
                            var ex = logEvent.Exception;
                            if (logEvent.Exception == null) {
                                ex = new Exception();
                            }

                            var er = new ErrorRecord(ex, errorId: strWriter.ToString(), errorCategory: ErrorCategory.NotSpecified, targetObject: null);
                            _cmdlet.WriteError(er);
                            break;

                        case LogEventLevel.Fatal:
                            //_cmdlet.WriteDebug("fatal: " + strWriter.ToString());
                            var fatal = new ErrorRecord(logEvent.Exception, errorId: strWriter.ToString(), errorCategory: ErrorCategory.InvalidOperation, targetObject: null);
                            _cmdlet.ThrowTerminatingError(fatal);
                            break;

                        default:
                            _cmdlet.WriteDebug(strWriter.ToString());
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
}

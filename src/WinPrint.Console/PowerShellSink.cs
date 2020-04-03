using System;
using System.IO;
using System.Management.Automation;
using System.Threading.Tasks;
using Serilog.Core;
using Serilog.Events;
using Serilog.Formatting;
using TTRider.PowerShellAsync;

namespace WinPrint.Core.Services {

    public class PowerShellSink : ILogEventSink {
        readonly object _syncRoot = new object();
        private AsyncCmdlet _cmdlet;

        public ITextFormatter TextFormatter { get; set; }

        public PowerShellSink(AsyncCmdlet cmdlet) {
            if (cmdlet == null) throw new ArgumentNullException(nameof(cmdlet));
            _cmdlet = cmdlet;
            TextFormatter = new Serilog.Formatting.Display.MessageTemplateTextFormatter("{Message:lj}");
        }

        public void Emit(LogEvent logEvent) {
            if (logEvent == null) throw new ArgumentNullException(nameof(logEvent));
            StringWriter strWriter = new StringWriter();
            TextFormatter.Format(logEvent, strWriter);

            lock (_syncRoot) {
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
                        ErrorRecord er = new ErrorRecord(logEvent.Exception, errorId: strWriter.ToString(), errorCategory: ErrorCategory.InvalidOperation, targetObject: null);
                        _cmdlet.WriteError(er);
                        break;

                    case LogEventLevel.Fatal:
                        //_cmdlet.WriteDebug("fatal: " + strWriter.ToString());
                        ErrorRecord fatal = new ErrorRecord(logEvent.Exception, errorId: strWriter.ToString(), errorCategory: ErrorCategory.InvalidOperation, targetObject: null);
                        _cmdlet.ThrowTerminatingError(fatal);
                        break;

                    default:
                        _cmdlet.WriteDebug(strWriter.ToString());
                        break;
                }
            }
        }
    }
}

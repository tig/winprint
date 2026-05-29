using System;
using System.Diagnostics;
using System.IO;
using System.Windows.Forms;
using Serilog.Core;
using Serilog.Events;
using Serilog.Formatting;
using Serilog.Formatting.Display;

//using TTRider.PowerShellAsync;

namespace WinPrint.WinForms;

public class GuiLogSink : ILogEventSink
{
    private static readonly Lazy<GuiLogSink> _instance = new(() => new GuiLogSink());

    // Set by Program before log events are emitted.
    public Control OutputWindow = null!;

    public GuiLogSink()
    {
        TextFormatter = new MessageTemplateTextFormatter("{Message:lj}");
    }

    public static GuiLogSink Instance => _instance.Value;

    public ITextFormatter TextFormatter { get; set; }

    public void Emit(LogEvent logEvent)
    {
        if (logEvent == null)
        {
            throw new ArgumentNullException(nameof(logEvent));
        }

        if (OutputWindow == null)
        {
            throw new ArgumentNullException("GuiLogSink: Output not set");
        }

        using var strWriter = new StringWriter();
        TextFormatter.Format(logEvent, strWriter);
        try
        {
            string msg = $"{strWriter}";
            switch (logEvent.Level)
            {
                // -Verbose
                case LogEventLevel.Verbose:
                case LogEventLevel.Information:
                    OutputWindow.Text = msg;
                    break;

                // -Debug
                case LogEventLevel.Debug:
                    OutputWindow.Text = msg;
                    break;

                case LogEventLevel.Warning:
                    OutputWindow.Text = msg;
                    break;

                // The Write-Error cmdlet declares a non-terminating error.
                case LogEventLevel.Error:
                    //cmdlet.WriteDebug("error: " + strWriter.ToString());
                    Exception? ex = logEvent.Exception;
                    if (logEvent.Exception == null)
                    {
                        ex = new Exception();
                    }

                    OutputWindow.Text = msg;
                    break;

                case LogEventLevel.Fatal:
                    //_cmdlet.WriteDebug("fatal: " + strWriter.ToString());
                    //var fatal = new ErrorRecord(logEvent.Exception, errorId: msg, errorCategory: ErrorCategory.InvalidOperation, targetObject: null);
                    //cmdlet.ThrowTerminatingError(fatal);
                    OutputWindow.Text = msg;
                    break;
            }
        }
        catch (Exception e)
        {
            Debug.WriteLine(e.Message);
        }
    }
}

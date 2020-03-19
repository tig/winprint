using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Management.Automation;
using System.Management.Automation.Internal;
using System.Management.Automation.Runspaces;
using System.Reflection;
using System.Threading.Tasks;
using Serilog;
using Serilog.Events;
using WinPrint.Core;
using WinPrint.Core.Models;
using WinPrint.Core.Services;
using TTRider.PowerShellAsync;

namespace WinPrint.Console {
    [Cmdlet(VerbsData.Out, nounName: "WinPrint", HelpUri = "https://tig.github.io./winprint")]
    [Alias("wp")]
    public class OutWinPrintCmdlet : AsyncCmdlet {

        private const string DataNotQualifiedForWinprint = "DataNotQualifiedForWinPrint";

        private List<PSObject> _psObjects = new List<PSObject>();

        public OutWinPrintCmdlet() {
            //this.implementation = new OutputManagerInner();
        }

        #region Command Line Switches

        /// <summary>
        /// Optional name of the printer to print to.
        /// The alias allows "lp -P printer".
        /// </summary>
        [Parameter(Position = 0, HelpMessage = "Printer name.")]
        [Alias("PrinterName")]
        public string Name {
            get { return _printerName; }

            set { _printerName = value; }
        }

        private string _printerName;

        /// <summary>
        /// Optional name of the WinPrint sheet definition to use.
        /// </summary>
        [Parameter(HelpMessage = "Name of the WinPrint sheet definition to use (e.g. \"Default 2-Up\")")]
        [Alias("Sheet")]
        public string SheetDefintion {
            get { return _sheetDefintion; }

            set { _sheetDefintion = value; }
        }

        private string _sheetDefintion;

        /// <summary>
        /// Optional name of the WinPrint Content Type Engine to use.
        /// </summary>
        [Parameter(HelpMessage = "Name of the WinPrint Content Type Engine to use (default is \"text/plain\")")]
        [Alias("Engine")]
        public string ContentTypeEngine {
            get { return _cteName; }

            set { _cteName = value; }
        }
        private string _cteName;

        private bool _verbose = false;

        [Parameter(ValueFromPipeline = true)]
        public PSObject InputObject { set; get; } = AutomationNull.Value;

        [Parameter(HelpMessage = "Exit code is set to number of sheets that would be printed. Use -Verbose to display the count.")]
        public SwitchParameter CountSheets { get; set; }

        #endregion

        #region Overrides
        /// <summary>
        /// Read command line parameters. 
        /// This method gets called once for each cmdlet in the pipeline when the pipeline starts executing
        /// </summary>
        protected override async Task BeginProcessingAsync() {
            AppDomain.CurrentDomain.UnhandledException += CurrentDomain_UnhandledException;
            TaskScheduler.UnobservedTaskException += TaskScheduler_UnobservedTaskException;

            ServiceLocator.Current.TelemetryService.Start("out-winprint");
            ServiceLocator.Current.LogService.Start("out-winprint");

            if(MyInvocation.BoundParameters.TryGetValue("Verbose", out object verbose)) {
                _verbose = true;
            }

            if (MyInvocation.BoundParameters.TryGetValue("Debug", out object debug)) {
                ServiceLocator.Current.LogService.ConsoleLevelSwitch.MinimumLevel = LogEventLevel.Debug;
                ServiceLocator.Current.LogService.MasterLevelSwitch.MinimumLevel = LogEventLevel.Debug;
            }
            else {
                ServiceLocator.Current.LogService.ConsoleLevelSwitch.MinimumLevel = LogEventLevel.Information;
            }

            await base.BeginProcessingAsync();
        }

        // This method will be called for each input received from the pipeline to this cmdlet; if no input is received, this method is not called
        protected override async Task ProcessRecordAsync() {
            await base.ProcessRecordAsync();
            if (InputObject == null || InputObject == AutomationNull.Value) {
                return;
            }

            IDictionary dictionary = InputObject.BaseObject as IDictionary;
            if (dictionary != null) {
                // Dictionaries should be enumerated through because the pipeline does not enumerate through them.
                foreach (DictionaryEntry entry in dictionary) {
                    ProcessObject(PSObject.AsPSObject(entry));
                }
            }
            else {
                ProcessObject(InputObject);
            }
        }

        private void ProcessObject(PSObject input) {

            object baseObject = input.BaseObject;

            // Throw a terminating error for types that are not supported.
            if (baseObject is ScriptBlock ||
                baseObject is SwitchParameter ||
                baseObject is PSReference ||
                baseObject is PSObject) {
                ErrorRecord error = new ErrorRecord(
                    new FormatException("Invalid data type for Out-WinPrint"),
                    DataNotQualifiedForWinprint,
                    ErrorCategory.InvalidType,
                    null);

                this.ThrowTerminatingError(error);
            }

            _psObjects.Add(input);
        }

        // This method will be called once at the end of pipeline execution; if no input is received, this method is not called
        protected override async Task EndProcessingAsync() {
            await base.EndProcessingAsync();

            //Return if no objects
            if (_psObjects.Count == 0) {
                return;
            }

            // See: https://stackoverflow.com/questions/60712580/invoking-cmdlet-from-a-c-based-pscmdlet-providing-input-and-capturing-output
            var result = this.SessionState.InvokeCommand.InvokeScript(@"$input| Out-String", true, PipelineResultTypes.None, _psObjects, null);
            string text = result[0].ToString();

            //this.WriteObject(text, false);

            ServiceLocator.Current.UpdateService.GotLatestVersion += LogUpdateResults();
            await Task.Run(() => ServiceLocator.Current.UpdateService.GetLatestStableVersionAsync());

            var print = new Print();

            if (!string.IsNullOrEmpty(_printerName))
                print.SetPrinter(_printerName);

            //print.PrintingSheet += (s, sheetNum) => this.WriteProgress(new ProgressRecord(0, "Printing", $"Printing sheet {sheetNum}"));
            //print.SheetViewModel.PropertyChanged += PropertyChangedEventHandler;
            //print.SheetViewModel.SettingsChanged += SettingsChangedEventHandler;
            //print.SheetViewModel.ReflowProgress += (s, msg) => this.WriteInformation(new InformationRecord($"Reflow Progress {msg}", "script"));

            print.PrintingSheet += (s, sheetNum) => {
                //if (_verbose)
                Log.Information("Printing sheet {sheetNum}", sheetNum);
            };


            string sheetID;
            SheetSettings sheet = print.SheetViewModel.FindSheet(_sheetDefintion, out sheetID);

            if (_verbose) {
                Log.Information("    Printer:          {printer}", print.PrintDocument.PrinterSettings.PrinterName);
                Log.Information("    Paper Size:       {size}", print.PrintDocument.DefaultPageSettings.PaperSize.PaperName);
                Log.Information("    Orientation:      {s}", print.PrintDocument.DefaultPageSettings.Landscape ? $"Landscape" : $"Portrait");
                Log.Information("    Sheet Definition: {name} ({id})", sheet.Name, sheetID);
            }

            print.PrintDocument.DefaultPageSettings.Landscape = sheet.Landscape;
            print.SheetViewModel.SetSheet(sheet);
            if (string.IsNullOrEmpty(_cteName))
                _cteName = "text/plain";
            await print.SheetViewModel.LoadStringAsync(text, _cteName).ConfigureAwait(false);

            var sheetsCounted = await print.DoPrint().ConfigureAwait(false);

            if (_verbose) {
                if (ModelLocator.Current.Options.CountPages)
                    Log.Information("Would have printed a total of {pagesCounted} sheets.", sheetsCounted);
                else
                    Log.Information("Printed a total of {pagesCounted} sheets.", sheetsCounted);
            }

            //this.WriteProgress(new ProgressRecord(0, "Printing", $"Printed {sheetsCounted} sheets"));

            // Don't write anything out to the pipeline if PassThru wasn't specified.
            //if (!PassThru.IsPresent) {
            //    return;
            //}

            //var selectedIndexes = _consoleGui.SelectedIndexes;

            //foreach (int idx in selectedIndexes) {
            //    var selectedObject = _psObjects[idx];
            //    if (selectedObject == null) {
            //        continue;
            //    }
            //    this.WriteObject(selectedObject, false);
            //}

            //this.WriteObject(sheetsCounted, false);

            ServiceLocator.Current.UpdateService.GotLatestVersion -= LogUpdateResults();
            AppDomain.CurrentDomain.UnhandledException -= CurrentDomain_UnhandledException;
            TaskScheduler.UnobservedTaskException -= TaskScheduler_UnobservedTaskException;
        }

        private static EventHandler<Version> LogUpdateResults() {
            return (s, v) => {
                var cur = new Version(FileVersionInfo.GetVersionInfo(Assembly.GetAssembly(typeof(LogService)).Location).FileVersion);
                Log.Debug("Got new version info. Current: {cur}, Available: {version}", cur, v);
                if (v != null && v.CompareTo(cur) > 0) {
                    Log.Information("A newer version of winprint ({v}) is available at {l}.", v, ServiceLocator.Current.UpdateService.DownloadUri);
                }
                else {
                    Log.Information("This is the most up-to-date version of winprint");
                }
            };
        }

        public async Task<string> GetNodeDirectory() {
            LogService.TraceMessage();

            string path = "";
            Process proc = null;
            try {
                ProcessStartInfo psi = new ProcessStartInfo();
                psi.UseShellExecute = false;   // This is important
                psi.CreateNoWindow = true;     // This is what hides the command window.
                psi.FileName = @"where.exe";
                psi.Arguments = "node";
                psi.RedirectStandardInput = true;
                psi.RedirectStandardOutput = true;

                proc = Process.Start(psi);
                //StreamWriter sw = node.StandardInput;
                //sw.WriteLine("");
                //sw.Close();
                path = await proc.StandardOutput.ReadLineAsync();
            }
            catch (Exception e) {
                // TODO: Better error message (output of stderr?)
                ServiceLocator.Current.TelemetryService.TrackException(e, false);
            }
            finally {
                proc?.Dispose();
            }
            return Path.GetDirectoryName(path);
        }
        public override string GetResourceString(string baseName, string resourceId) {
            return base.GetResourceString(baseName, resourceId);
        }

        protected override async Task StopProcessingAsync() {
            await base.StopProcessingAsync();
        }

        /// <summary>
        /// Callback for the implementation to obtain a reference to the Cmdlet object.
        /// </summary>
        /// <returns>Cmdlet reference.</returns>
        protected virtual Cmdlet OuterCmdletCall() {
            return this;
        }

        /// <summary>
        /// Callback for the implementation to get the current pipeline object.
        /// </summary>
        /// <returns>Current object from the pipeline.</returns>
        protected virtual Object InputObjectCall() {
            // just bind to the input object parameter
            return this.InputObject;
        }

        /// <summary>
        /// Callback for the implementation to write objects.
        /// </summary>
        /// <param name="value">Object to be written.</param>
        protected virtual void WriteObjectCall(object value) {
            // just call Monad API
            this.WriteObject(value);
        }
        #endregion

        #region IDisposable Implementation

        /// <summary>
        /// Default implementation just delegates to internal helper.
        /// </summary>
        /// <remarks>This method calls GC.SuppressFinalize</remarks>
        public void Dispose() {
            Dispose(true);

            GC.SuppressFinalize(this);
        }

        /// <summary>
        /// Dispose pattern implementation.
        /// </summary>
        /// <param name="disposing"></param>
        protected virtual void Dispose(bool disposing) {
            if (disposing) {
                //InternalDispose();
            }
        }

        /// <summary>
        /// Do-nothing implementation: derived classes will override as see fit.
        /// </summary>
        //protected virtual void InternalDispose() {
        //    if (this.implementation == null)
        //        return;

        //    this.implementation.Dispose();
        //    this.implementation = null;
        #endregion

        /// <summary>
        /// One-time initialization: acquire a screen host interface by creating one on top of a memory buffer.
        /// </summary>
        private WinPrint.Core.Print InstantiateWinPrint() {
            WinPrint.Core.Print print = new WinPrint.Core.Print();
            return (WinPrint.Core.Print)print;
        }

        private static void TaskScheduler_UnobservedTaskException(object sender, UnobservedTaskExceptionEventArgs e) {
            ServiceLocator.Current.TelemetryService.TrackException(e.Exception);
        }

        private static void CurrentDomain_UnhandledException(object sender, UnhandledExceptionEventArgs e) {
            var ex = e.ExceptionObject as Exception;
            ServiceLocator.Current.TelemetryService.TrackException(ex);
        }
    }
}

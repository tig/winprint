using System;
using System.Collections;
using System.Collections.Generic;
using System.Management.Automation;
using System.Management.Automation.Internal;
using System.Text;
using System.Text.Json.Serialization;
using Microsoft.PowerShell.Commands.Internal.Format;
using Microsoft.PowerShell.Commands;
using WinPrint.Core;
using System.Linq;
using System.Management.Automation.Runspaces;
using System.Globalization;
using WinPrint.Core.Models;
using WinPrint.Core.Services;
using System.Threading.Tasks;
using Serilog.Events;

namespace WinPrint.PowerShell {
    [Cmdlet(VerbsData.Out, nounName: "WinPrint", HelpUri = "https://tig.github.io./winprint")]
    [Alias("wp")]
    public class OutWinPrintCmdlet : PSCmdlet {

        private const string DataNotQualifiedForWinprint = "DataNotQualifiedForWinPrint";

        private List<PSObject> _psObjects = new List<PSObject>();

        public OutWinPrintCmdlet() {
            //this.implementation = new OutputManagerInner();
        }

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
        [Parameter(HelpMessage = "Name of the WinPrint sheet definition to use (e.g. \"Default 2-Up\"")]
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
        [Alias("cte")]
        public string ContentTypeEngine {
            get { return _cteName; }

            set { _cteName = value; }
        }

        private string _cteName;

        #region Command Line Switches

        [Parameter(ValueFromPipeline = true)]
        public PSObject InputObject { set; get; } = AutomationNull.Value;

        #endregion

        #region Overrides
        /// <summary>
        /// Read command line parameters. 
        /// This method gets called once for each cmdlet in the pipeline when the pipeline starts executing
        /// </summary>
        protected override void BeginProcessing() {
            // set up the Scree Host interface
            //OutputManagerInner outInner = (OutputManagerInner)this.implementation;

            ////outInner.LineOutput = InstantiateLineOutputInterface();
            //this.implementation.OuterCmdletCall = new ImplementationCommandBase.OuterCmdletCallback(this.OuterCmdletCall);
            //this.implementation.InputObjectCall = new ImplementationCommandBase.InputObjectCallback(this.InputObjectCall);
            //this.implementation.WriteObjectCall = new ImplementationCommandBase.WriteObjectCallback(this.WriteObjectCall);

            //this.implementation.CreateTerminatingErrorContext();

            //implementation.BeginProcessing();
            // finally call the base class for general hookup

            AppDomain.CurrentDomain.UnhandledException += CurrentDomain_UnhandledException;
            TaskScheduler.UnobservedTaskException += TaskScheduler_UnobservedTaskException;

            if (MyInvocation.BoundParameters.TryGetValue("Debug", out object debug)) {
                ServiceLocator.Current.LogService.ConsoleLevelSwitch.MinimumLevel = LogEventLevel.Debug;
                ServiceLocator.Current.LogService.MasterLevelSwitch.MinimumLevel = LogEventLevel.Debug;
            }
            else {
                ServiceLocator.Current.LogService.ConsoleLevelSwitch.MinimumLevel = LogEventLevel.Information;
            }

            ServiceLocator.Current.TelemetryService.Start("out-winprint");
            ServiceLocator.Current.LogService.Start("out-winprint");

            base.BeginProcessing();
        }

        // This method will be called for each input received from the pipeline to this cmdlet; if no input is received, this method is not called
        protected override void ProcessRecord() {
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
        protected override async void EndProcessing() {
            base.EndProcessing();

            //Return if no objects
            if (_psObjects.Count == 0) {
                return;
            }

            //var text = this.SessionState.InvokeCommand.InvokeScript(@"Out-String", true, PipelineResultTypes.None, _psObjects, null);
            //this.WriteObject(text, false);

            var commandInfo = new CmdletInfo("Out-String", typeof(Microsoft.PowerShell.Commands.OutStringCommand));
            using var ps = System.Management.Automation.PowerShell.Create(RunspaceMode.CurrentRunspace);
            ps.AddCommand(commandInfo);
            ps.AddParameter("InputObject", _psObjects);
            var text = ps.Invoke<string>()[0];

            this.WriteObject(text, false);

            var print = new Print();

            if (!string.IsNullOrEmpty(_printerName))
                print.SetPrinter(_printerName);

            //print.PrintingSheet += (s, sheetNum) => this.WriteProgress(new ProgressRecord(0, "Printing", $"Printing sheet {sheetNum}"));
            //print.SheetViewModel.PropertyChanged += PropertyChangedEventHandler;
            //print.SheetViewModel.SettingsChanged += SettingsChangedEventHandler;
            //print.SheetViewModel.ReflowProgress += (s, msg) => this.WriteInformation(new InformationRecord($"Reflow Progress {msg}", "script"));

            string sheetID;
            SheetSettings sheet = print.SheetViewModel.FindSheet(_sheetDefintion, out sheetID);
            print.PrintDocument.DefaultPageSettings.Landscape = sheet.Landscape;
            print.SheetViewModel.SetSheet(sheet);
            if (string.IsNullOrEmpty(_cteName))
                _cteName = "text/plain";
            await print.SheetViewModel.SetDocumentAsync(text, _cteName).ConfigureAwait(false) ;

            var sheetsCounted = await print.DoPrint().ConfigureAwait(false); 

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

            AppDomain.CurrentDomain.UnhandledException -= CurrentDomain_UnhandledException;
            TaskScheduler.UnobservedTaskException -= TaskScheduler_UnobservedTaskException;

        }

        public override string GetResourceString(string baseName, string resourceId) {
            return base.GetResourceString(baseName, resourceId);
        }

        protected override void StopProcessing() {
            base.StopProcessing();
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

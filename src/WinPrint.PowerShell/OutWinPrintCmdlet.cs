using System;
using System.Management.Automation;
using System.Management.Automation.Internal;
using Microsoft.PowerShell.Commands.Internal.Format;

namespace WinPrint.PowerShell {


   // internal sealed class PrinterLineOutput : LineOutput {
    //}

        [Cmdlet(VerbsData.Out, "WinPrint")]
    public class OutWinPrintCmdlet : Cmdlet {
        public OutWinPrintCmdlet() {
            //this.implementation = new OutputManagerInner();
        }

        /// <summary>
        /// Optional name of the printer to print to.
        /// The alias allows "lp -P printer".
        /// </summary>
        [Parameter(Position = 0)]
        [Alias("PrinterName")]
        public string Name {
            get { return _printerName; }

            set { _printerName = value; }
        }

        private string _printerName;

        #region Command Line Switches

        [Parameter(ValueFromPipeline = true)]
        public PSObject InputObject { set; get; } = AutomationNull.Value;

        #endregion

        /// <summary>
        /// Read command line parameters.
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
            base.BeginProcessing();
        }

        protected override void ProcessRecord() {

        }

        protected override void EndProcessing() {
            base.EndProcessing();
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

        // <summary>
        /// Reference to the implementation command that this class
        /// is wrapping.
        /// </summary>
        //internal ImplementationCommandBase implementation = null;

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
    }
    #endregion

    /// <summary>
    /// One-time initialization: acquire a screen host interface by creating one on top of a memory buffer.
    /// </summary>
    //private LineOutput InstantiateLineOutputInterface() {
    //    //PrinterLineOutput printOutput = new PrinterLineOutput(_printerName);
    //    return (LineOutput)printOutput;
    //}
}

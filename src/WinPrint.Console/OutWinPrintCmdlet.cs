using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Drawing.Printing;
using System.Globalization;
using System.Management.Automation;
using System.Management.Automation.Internal;
using System.Management.Automation.Runspaces;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Serilog;
//using TTRider.PowerShellAsync;
using WinPrint.Core;
using WinPrint.Core.ContentTypeEngines;
using WinPrint.Core.Models;
using WinPrint.Core.Services;

namespace WinPrint.Console {
    [Cmdlet(VerbsData.Out,
        nounName: "WinPrint",
        HelpUri = "https://tig.github.io./winprint",
        DefaultParameterSetName = "print")]
    [Alias("wp")]
    public class OutWinPrintCmdlet : AsyncCmdlet {
        private const string DataNotQualifiedForWinprint = "DataNotQualifiedForWinPrint";

        // Private fields
        private List<PSObject> _psObjects = new List<PSObject>();
        private Print _print = new WinPrint.Core.Print();

        public OutWinPrintCmdlet() {
            //this.implementation = new OutputManagerInner();
            //AppDomain.CurrentDomain.UnhandledException += CurrentDomain_UnhandledException;
            //TaskScheduler.UnobservedTaskException += TaskScheduler_UnobservedTaskException;
            //BoundedCapacity = 500;
        }

        #region Command Line Switches

        /// <summary>
        /// Optional name of the printer to print to.
        /// The alias allows "lp -P printer".
        /// Name alias: becuase that's what out-printer uses.
        /// </summary>
        [Parameter(Position = 0, HelpMessage = "The name of the printer to print to. If not specified the default printer will be used.",
            ParameterSetName = "Print")]
        [Alias("Name")]
        public string PrinterName { get; set; }

        /// <summary>
        /// Optional name of the WinPrint sheet definition to use.
        /// </summary>
        [Parameter(HelpMessage = "Name of the WinPrint sheet definition to use (e.g. \"Default 2-Up\")",
            ParameterSetName = "Print")]
        [Alias("Sheet")]
        public string SheetDefintion { get; set; }

        /// <summary>
        /// Optional name of the WinPrint Content Type Engine to use.
        /// </summary>
        [Parameter(HelpMessage = "Name of the WinPrint Content Type Engine to use (default is \"text/plain\").",
            ParameterSetName = "Print")]
        [Alias("Engine")]
        public string ContentTypeEngine { get; set; }

        /// <summary>
        /// Optional FileName - will be displayed in header/footer and as title of print job.
        /// </summary>
        [Parameter(HelpMessage = "FileName to be displayed in header/footer with the {FileName} (or {Title}) macros. " +
            "If ContentType is not specified, the Filename will be used to try to determine the content type engine to use.",
            ParameterSetName = "Print")]
        [Alias("File")]
        public string FileName { get; set; }

        /// <summary>
        /// Optional FileName - will be displayed in header/footer and as title of print job.
        /// </summary>
        [Parameter(HelpMessage = "Title to be displayed in header/footer with the {Title} or {FileName} macros.",
            ParameterSetName = "Print")]
        public string Title { get; set; }

        /// <summary>
        /// For the -Verbose switch
        /// </summary>
        private bool _verbose { get => MyInvocation.BoundParameters.TryGetValue("Verbose", out var o); }

        /// <summary>
        /// For the -Debug switch
        /// </summary>
#if DEBUG
        private bool _debug = true;
#else
        private bool _debug { get => MyInvocation.BoundParameters.TryGetValue("Debug", out object o); }
#endif

        /// <summary>
        /// Input stream.
        /// </summary>
        [Parameter(ValueFromPipeline = true,
            ParameterSetName = "Print")]
        public PSObject InputObject { set; get; } = AutomationNull.Value;

        /// <summary>
        /// -WhatIf switch
        /// </summary>
        [Parameter(HelpMessage = "Output is the number of sheets that would be printed. Use -Verbose to print the count of pages.",
            ParameterSetName = "Print")]
        public SwitchParameter WhatIf { get; set; }
        private bool _whatIf { get => MyInvocation.BoundParameters.TryGetValue("WhatIf", out var o); }

        /// <summary>
        /// -InstallUpdate switch
        /// </summary>
        [Parameter(HelpMessage = "If an updated version of winprint is available online, download and install it.",
            ParameterSetName = "Updates")]
        public SwitchParameter InstallUpdate { get; set; }
        private bool _installUpdate { get => MyInvocation.BoundParameters.TryGetValue("InstallUpdate", out var o); }

        /// <summary>
        /// -Force switch
        /// </summary>
        [Parameter(HelpMessage = "Allows winprint to kill the host Powershell process when updating.",
            ParameterSetName = "Updates")]
        public SwitchParameter Force { get; set; }
        private bool _force { get => MyInvocation.BoundParameters.TryGetValue("Force", out var o); }
        #endregion

        #region Update Service Related Code

        CancellationTokenSource _cancellationToken;

        // Update stuff
        private void Setup() {
            _cancellationToken = new CancellationTokenSource();
            ServiceLocator.Current.UpdateService.GotLatestVersion += UpdateService_GotLatestVersion;
            ServiceLocator.Current.UpdateService.DownloadProgressChanged += UpdateService_DownloadProgressChanged;
        }
        private void CleanUp() {
            ServiceLocator.Current.UpdateService.GotLatestVersion -= UpdateService_GotLatestVersion;
            ServiceLocator.Current.UpdateService.DownloadProgressChanged -= UpdateService_DownloadProgressChanged;
            _cancellationToken?.Cancel();
        }

        private void UpdateService_DownloadProgressChanged(object sender, System.Net.DownloadProgressChangedEventArgs e) {
            //Debug.WriteLine("UpdateService_DownloadProgressChanged");
            var rec = new ProgressRecord(0, "Downloading", "Downloading");
            rec.CurrentOperation = $"Downloading";
            rec.PercentComplete = e.ProgressPercentage;
            WriteProgress(rec);
        }
        private async Task<bool> DoUpdateAsync() {

            Debug.WriteLine("Kicking off update check thread...");
            var version = await ServiceLocator.Current.UpdateService.GetLatestStableVersionAsync(_cancellationToken.Token).ConfigureAwait(true);
            Debug.WriteLine($"Starting update...");
            var path = await ServiceLocator.Current.UpdateService.StartUpgradeAsync().ConfigureAwait(true);

#if DEBUG
            var log = "-lv winprint.msiexec.log";
#else
            string log = "";
#endif
            using var p = new Process {
                StartInfo = {
                        FileName = $"msiexec.exe",
                        Arguments = $"{log} -i {path}",
                        UseShellExecute = true
                    },
            };

            Log.Information($"Download Complete. Running installer ({p.StartInfo.FileName} {p.StartInfo.Arguments})...");
            var rec = new ProgressRecord(0, "Installing", $"Download Complete");
            rec.CurrentOperation = $"Installing";
            rec.PercentComplete = -1;
            //await Task.Run(() => WriteProgress(rec));
            WriteProgress(rec);
            Debug.WriteLine($"wrote progress");

            try {
                p.Start();
            }
            catch (Win32Exception we) {
                Log.Information($"{this.GetType().Name}: '{p.StartInfo.FileName} {p.StartInfo.Arguments}' failed to run with error: {we.Message}");
                return false;
            }

            if (_force ||
                //await Task.Run(() => ShouldContinue("The winprint installer requires any Powershell instances that have used out-winprint be closed.",
                //"Exit this Powershell instance?"))) {
                ShouldContinue("The winprint installer requires any Powershell instances that have used out-winprint be closed.",
                "Exit this Powershell instance?")) {
                // Kill process?
                System.Environment.Exit(0);
            }
            return true;
        }

        private string _updateMsg;

        private void UpdateService_GotLatestVersion(object sender, Version version) {
            Debug.WriteLine("UpdateService_GotLatestVersion");
            if (version == null && !String.IsNullOrWhiteSpace(ServiceLocator.Current.UpdateService.ErrorMessage)) {
                _updateMsg = $"Could not access github.com/tig/winprint to see if a newer version is available" +
                    $" {ServiceLocator.Current.UpdateService.ErrorMessage}";
                return;
            }

            if (ServiceLocator.Current.UpdateService.CompareVersions() < 0) {
                _updateMsg = $"An update to winprint is available at {ServiceLocator.Current.UpdateService.ReleasePageUri}. " +
                    $"Run '{MyInvocation.InvocationName} -InstallUpdate' to upgrade";
            }
            else if (ServiceLocator.Current.UpdateService.CompareVersions() > 0) {
                _updateMsg = $"This is a MORE recent version than can be found at github.com/tig/winprint ({version})";
            }
            else {
                _updateMsg = "This is lastest version of winprint";
            }
        }
        #endregion

        #region PowerShell AsyncCmdlet Overrides
        /// <summary>
        /// Read command line parameters. 
        /// This method gets called once for each cmdlet in the pipeline when the pipeline starts executing
        /// </summary>
        protected override async Task BeginProcessingAsync() {
            //Log.Debug("BeginProcessingAsync");
            //ServiceLocator.Reset();
            //ModelLocator.Reset();

            ServiceLocator.Current.TelemetryService.Start(this.MyInvocation.MyCommand.Name,
                startProperties: new Dictionary<string, string> {
                    ["PowerShellVersion"] = this.Host.Version.ToString(),
                    ["InvocationName"] = this.MyInvocation.InvocationName,
                    ["Debug"] = _debug.ToString(CultureInfo.CurrentCulture),
                    ["Verbose"] = _verbose.ToString(CultureInfo.CurrentCulture)
                }); ;

            ServiceLocator.Current.LogService.Start(this.MyInvocation.MyCommand.Name, new PowerShellSink(this), _debug, _verbose);

            var ver = FileVersionInfo.GetVersionInfo(Assembly.GetAssembly(typeof(UpdateService)).Location);
            Log.Information("{appname} {version} - {copyright} - {link}", this.MyInvocation.MyCommand.Name, ver.ProductVersion, ver.LegalCopyright, @"https://tig.github.io/winprint");

            await base.BeginProcessingAsync().ConfigureAwait(true);
        }

        // This method will be called for each input received from the pipeline to this cmdlet; if no input is received, this method is not called
        protected override async Task ProcessRecordAsync() {
            //Log.Debug("ProcessRecordAsync");
            await base.ProcessRecordAsync().ConfigureAwait(true);

            if (InputObject == null || InputObject == AutomationNull.Value) {
                return;
            }

            var dictionary = InputObject.BaseObject as IDictionary;
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

            var baseObject = input.BaseObject;

            // Throw a terminating error for types that are not supported.
            if (baseObject is ScriptBlock ||
                baseObject is SwitchParameter ||
                baseObject is PSReference ||
                baseObject is PSObject) {
                var error = new ErrorRecord(
                    new FormatException($"Invalid data type for {MyInvocation.InvocationName}"),
                    DataNotQualifiedForWinprint,
                    ErrorCategory.InvalidType,
                    null);

                this.ThrowTerminatingError(error);
            }

            _psObjects.Add(input);
        }

        // This method will be called once at the end of pipeline execution; if no input is received, this method is not called
        protected override async Task EndProcessingAsync() {
            await base.EndProcessingAsync().ConfigureAwait(true);

            //Log.Debug("EndProcessingAsync");

            Setup();

            // Check for new version
            if (_installUpdate) {
                await DoUpdateAsync().ConfigureAwait(true);
                CleanUp();
                return;
            }

            // Whenever we run, check for an update. We use the cancellation token to kill the thread that's doing this
            // if we exit before getting a version info result back. Checking for updates should never shlow cmd line down.
            Log.Debug("Kicking off update check thread...");
            await Task.Run(() => ServiceLocator.Current.UpdateService.GetLatestStableVersionAsync(_cancellationToken.Token).ConfigureAwait(true),
                _cancellationToken.Token).ConfigureAwait(true);

            //Return if no objects
            if (_psObjects.Count == 0) {
                Log.Debug("No objects...");
                CleanUp();
                return;
            }

            //var settings = ServiceLocator.Current.SettingsService.ReadSettings();

            var rec = new ProgressRecord(1, "Printing", "Printing...");
            rec.PercentComplete = 0;
            rec.StatusDescription = "Initializing winprint";
            WriteProgress(rec);

            // See: https://stackoverflow.com/questions/60712580/invoking-cmdlet-from-a-c-based-pscmdlet-providing-input-and-capturing-output
            var result = this.SessionState.InvokeCommand.InvokeScript(@"$input | Out-String", true, PipelineResultTypes.None, _psObjects, null);
            var text = result[0].ToString();

            Debug.Assert(_print != null);
            if (!string.IsNullOrEmpty(PrinterName)) {
                try {
                    rec.PercentComplete = 10;
                    rec.StatusDescription = $"Setting printer name to {PrinterName}";
                    WriteProgress(rec);
                    _print.SetPrinter(PrinterName);
                }
                catch (InvalidPrinterException e) {
                    //Log.Error<InvalidPrinterException>(e, "", e);
                    Log.Information("Installed printers:");
                    foreach (string printer in PrinterSettings.InstalledPrinters) {
                        Log.Information("   {printer}", printer);
                    }

                    Log.Fatal(e, "");
                }
            }

            if (string.IsNullOrEmpty(Title)) {
                Title = this.MyInvocation.MyCommand.Name;
            }

            _print.SheetViewModel.File = Title;

            //_print.PrintingSheet += (s, sheetNum) => this.WriteProgress(new ProgressRecord(0, "Printing", $"Printing sheet {sheetNum}"));
            //_print.SheetViewModel.PropertyChanged += PropertyChangedEventHandler;
            //_print.SheetViewModel.SettingsChanged += SettingsChangedEventHandler;
            //_print.SheetViewModel.ReflowProgress += (s, msg) => this.WriteInformation(new InformationRecord($"Reflow Progress {msg}", "script"));

            _print.PrintingSheet += (s, sheetNum) => {
                rec.PercentComplete = 40 + (sheetNum);
                rec.StatusDescription = $"Printing sheet {sheetNum}";
                WriteProgress(rec);
                Log.Information("Printing sheet {sheetNum}", sheetNum);
            };


            try {
                var sheet = _print.SheetViewModel.FindSheet(SheetDefintion, out var sheetID);

                if (_verbose) {
                    Log.Information("    Printer:          {printer}", _print.PrintDocument.PrinterSettings.PrinterName);
                    Log.Information("    Paper Size:       {size}", _print.PrintDocument.DefaultPageSettings.PaperSize.PaperName);
                    Log.Information("    Orientation:      {s}", _print.PrintDocument.DefaultPageSettings.Landscape ? $"Landscape" : $"Portrait");
                    Log.Information("    Sheet Definition: {name} ({id})", sheet.Name, sheetID);
                }

                rec.PercentComplete = 20;
                rec.StatusDescription = $"Setting Sheet Settings for {sheet.Name}";
                WriteProgress(rec);

                _print.PrintDocument.DefaultPageSettings.Landscape = sheet.Landscape;
                _print.SheetViewModel.SetSheet(sheet);
            }
            catch (InvalidOperationException e) {
                Log.Error(e, "Could not find sheet settings");
                CleanUp();
                return;
            }

            if (string.IsNullOrEmpty(ContentTypeEngine) && !string.IsNullOrEmpty(FileName)) {
                ContentTypeEngine = ContentTypeEngineBase.GetContentType(FileName);
            }

            rec.PercentComplete = 30;
            rec.StatusDescription = $"Loading content";
            WriteProgress(rec);
            await _print.SheetViewModel.LoadStringAsync(text, ContentTypeEngine).ConfigureAwait(true);

            rec.PercentComplete = 40;
            rec.StatusDescription = _whatIf ? "Counting" : $"Printing";
            WriteProgress(rec);

            var sheetsCounted = 0;
            try {
                if (_whatIf) {
                    sheetsCounted = await _print.CountSheets().ConfigureAwait(true);
                }
                else {
                    sheetsCounted = await _print.DoPrint().ConfigureAwait(true);
                }
            }
            catch (System.ComponentModel.Win32Exception w32e) {
                // This can happen when PDF driver can't access PDF file.
                Log.Error(w32e, "Print failed.");
                CleanUp();
                return;
            }

            if (_verbose) {
                if (ModelLocator.Current.Options.CountPages) {
                    Log.Information("Would have printed a total of {pagesCounted} sheets.", sheetsCounted);
                }
                else {
                    Log.Information("Printed a total of {pagesCounted} sheets.", sheetsCounted);
                }
            }

            // Don't write anything out to the pipeline if PassThru wasn't specified.
            //if (!PassThru.IsPresent) {
            //    return;
            //}

            //this.WriteObject(sheetsCounted, false);

            rec.PercentComplete = -1;
            rec.StatusDescription = $"Complete";
            WriteProgress(rec);


            // End by sharing update info, if any
            if (!string.IsNullOrEmpty(_updateMsg)) {
                Log.Information(_updateMsg);
            }

            CleanUp();
        }

        private void PropertyChangedEventHandler(object o, PropertyChangedEventArgs e) {
            Log.Debug("SheetViewModel.PropertyChanged: {s}", e.PropertyName);
            switch (e.PropertyName) {
                case "Landscape":
                    Log.Information("    Paper Orientation: {s}", _print.SheetViewModel.Landscape ? "Landscape" : "Portrait");
                    break;

                case "Header":
                    Log.Information("    Header Text:      {s}", _print.SheetViewModel.Header.Text);
                    break;

                case "Footer":
                    Log.Information("    Footer Text:      {s}", _print.SheetViewModel.Footer.Text);
                    break;

                case "Margins":
                    Log.Information("    Margins:          {v}", _print.SheetViewModel.Margins);
                    break;

                case "PageSeparator":
                    Log.Information("    PageSeparator     {s}", _print.SheetViewModel.PageSeparator);
                    break;

                case "Rows":
                    Log.Information("    Rows:             {s}", _print.SheetViewModel.Rows);
                    break;

                case "Columns":
                    Log.Information("    Columns:          {s}", _print.SheetViewModel.Columns);
                    break;

                // TODO: Add INF logging of other sheet properties
                case "Padding":
                    Log.Information("    Padding:          {s}", _print.SheetViewModel.Padding / 100M);
                    break;

                case "ContentSettings":
                    Log.Information("    ContentSettings:  {s}", _print.SheetViewModel.ContentSettings);
                    break;

                case "Loading":
                    //WriteProgress(new ProgressRecord(0, "Reading", "reading..."));
                    break;

                case "Reflowing":
                    //WriteProgress(new ProgressRecord(0, "Formatting", "formatting..."));
                    break;
            }
        }


        public override string GetResourceString(string baseName, string resourceId) {
            return base.GetResourceString(baseName, resourceId);
        }

        //protected override async Task StopProcessingAsync() {
        //    await base.StopProcessingAsync();
        //}

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
        public new void Dispose() {
            Dispose(true);

            //GC.SuppressFinalize(this);
        }

        /// <summary>
        /// Dispose pattern implementation.
        /// </summary>
        /// <param name="disposing"></param>
        protected new void Dispose(bool disposing) {
            base.Dispose(disposing);
            if (disposing) {
                AppDomain.CurrentDomain.UnhandledException -= CurrentDomain_UnhandledException;
                TaskScheduler.UnobservedTaskException -= TaskScheduler_UnobservedTaskException;

                _print?.Dispose();
                _cancellationToken?.Dispose();
            }
        }

        #endregion

        private static void TaskScheduler_UnobservedTaskException(object sender, UnobservedTaskExceptionEventArgs e) {
            ServiceLocator.Current.TelemetryService.TrackException(e.Exception);
        }

        private static void CurrentDomain_UnhandledException(object sender, UnhandledExceptionEventArgs e) {
            var ex = e.ExceptionObject as Exception;
            ServiceLocator.Current.TelemetryService.TrackException(ex);
        }
    }
}

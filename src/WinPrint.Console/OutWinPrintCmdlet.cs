using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Drawing.Printing;
using System.IO;
using System.Management.Automation;
using System.Management.Automation.Internal;
using System.Management.Automation.Runspaces;
using System.Reflection;
using System.Security.Policy;
using System.Threading;
using System.Threading.Tasks;
using Serilog;
using TTRider.PowerShellAsync;
using WinPrint.Core;
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
            AppDomain.CurrentDomain.UnhandledException += CurrentDomain_UnhandledException;
            TaskScheduler.UnobservedTaskException += TaskScheduler_UnobservedTaskException;
            BoundedCapacity = 500;
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
        [Parameter(HelpMessage = "Name of the WinPrint Content Type Engine to use (default is \"text/plain\")",
            ParameterSetName = "Print")]
        [Alias("Engine")]
        public string ContentTypeEngine { get; set; }

        /// <summary>
        /// Optional FileName - will be displayed in header/footer and as title of print job.
        /// </summary>
        [Parameter(HelpMessage = "Filename to be displayed in header/footer and as title of print job.",
            ParameterSetName = "Print")]
        [Alias("File")]
        public string FileName { get; set; }

        /// <summary>
        /// For the -Verbose switch
        /// </summary>
        private bool _verbose { get => MyInvocation.BoundParameters.TryGetValue("Verbose", out object o); }

        /// <summary>
        /// For the -Debug switch
        /// </summary>
#if DEBUG
        private bool _debug = true;
#else
        private bool _debug = { get => MyInvocation.BoundParameters.TryGetValue("Debug", out object o); }
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
        private bool _whatIf { get => MyInvocation.BoundParameters.TryGetValue("WhatIf", out object o); }

        /// <summary>
        /// -InstallUpdate switch
        /// </summary>
        [Parameter(HelpMessage = "If an updated version of winprint is available online, download and install it.",
            ParameterSetName = "Updates")]
        public SwitchParameter InstallUpdate { get; set; }
        private bool _installUpdate { get => MyInvocation.BoundParameters.TryGetValue("InstallUpdate", out object o); }

        /// <summary>
        /// -Force switch
        /// </summary>
        [Parameter(HelpMessage = "Allows winprint to kill the host Powershell process when updating.",
            ParameterSetName = "Updates")]
        public SwitchParameter Force { get; set; }
        private bool _force { get => MyInvocation.BoundParameters.TryGetValue("Force", out object o); }
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
            ProgressRecord rec = new ProgressRecord(0, "Downloading", "Downloading");
            rec.CurrentOperation = $"Downloading";
            rec.PercentComplete = e.ProgressPercentage;
            WriteProgress(rec);
        }
        private async Task<bool> DoUpdateAsync() {

            Debug.WriteLine("Kicking off update check thread...");
            var version = await ServiceLocator.Current.UpdateService.GetLatestStableVersionAsync(_cancellationToken.Token);
            Debug.WriteLine($"Starting update...");
            var path = await ServiceLocator.Current.UpdateService.StartUpgradeAsync();

#if DEBUG
            string log = "-lv winprint.msiexec.log";
#else
                string log = ";
#endif
            var p = new Process {
                StartInfo = {
                        FileName = $"msiexec.exe",
                        Arguments = $"{log} -i {path}",
                        UseShellExecute = true
                    },
            };

            Log.Information($"Download Complete. Running installer ({p.StartInfo.FileName} {p.StartInfo.Arguments})...");
            ProgressRecord rec = new ProgressRecord(0, "Installing", $"Download Complete");
            rec.CurrentOperation = $"Installing";
            rec.PercentComplete = -1;
            await Task.Run(() => WriteProgress(rec));
            Debug.WriteLine($"wrote progress");

            try {
                p.Start();
            }
            catch (Win32Exception we) {
                Log.Information($"{this.GetType().Name}: '{p.StartInfo.FileName} {p.StartInfo.Arguments}' failed to run with error: {we.Message}");
                return false;
            }

            if (_force ||
                await Task.Run(() => ShouldContinue("The winprint installer requires any Powershell instances that have used out-winprint be closed.",
                "Exit this Powershell instance?"))) {
                // Kill process?
                System.Environment.Exit(0);
            }
            return true;
        }
//            Debug.WriteLine("Kicking off update check thread...");
//            var version = await ServiceLocator.Current.UpdateService.GetLatestStableVersionAsync(_cancellationToken.Token);

//            Debug.WriteLine($"Starting update...");
//            var path = await ServiceLocator.Current.UpdateService.StartUpgradeAsync();

//#if DEBUG
//            string log = "-lv winprint.msiexec.log";
//#else
//            string log = ";
//#endif
//            var p = new Process {
//                StartInfo = {
//                        FileName = $"msiexec.exe",
//                        Arguments = $"{log} -i {path}",
//                        UseShellExecute = true
//                    },
//            };

//            Debug.WriteLine($"Download Complete. Running installer ({p.StartInfo.FileName} {p.StartInfo.Arguments})...");
//            Log.Information($"Download Complete. Running installer ({p.StartInfo.FileName} {p.StartInfo.Arguments})...");
//            ProgressRecord rec = new ProgressRecord(1, "Installing", $"Download Complete. Running installer ({ p.StartInfo.FileName } { p.StartInfo.Arguments})");
//            rec.CurrentOperation = $"Installing";
//            rec.PercentComplete = 100;
//            WriteProgress(rec);
//            Debug.WriteLine($"wrote progress");

//            try {
//                p.Start();
//            }
//            catch (Win32Exception we) {
//                Log.Information($"{this.GetType().Name}: '{p.StartInfo.FileName} {p.StartInfo.Arguments}' failed to run with error: {we.Message}");
//            }

//            Debug.WriteLine($"about to exit");
//            if (_force || ShouldContinue("The winprint installer requires any Powershell instances that have used out-winprint be closed.", "Exit this Powershell instance?")) {
//                // Kill process?
//                System.Environment.Exit(0);
//            }
//            return;
//        }

        private void UpdateService_GotLatestVersion(object sender, Version version) {
            Debug.WriteLine("UpdateService_GotLatestVersion");
            if (version == null && !String.IsNullOrWhiteSpace(ServiceLocator.Current.UpdateService.ErrorMessage)) {
                Log.Information($"Could not access tig.github.io/winprint to see if a newer version is available" +
                    $" {ServiceLocator.Current.UpdateService.ErrorMessage}");
                return;
            }

            if (ServiceLocator.Current.UpdateService.CompareVersions() < 0) {
                Log.Information("A newer version of winprint ({version}) is available at {url}", version, ServiceLocator.Current.UpdateService.ReleasePageUri);
                Log.Information($"Run '{MyInvocation.InvocationName} -InstallUpdate' to upgrade");
            }
            else if (ServiceLocator.Current.UpdateService.CompareVersions() > 0) {
                Log.Information($"You are are running a MORE recent version than can be found at tig.github.io/winprint ({version})");
            }
            else {
                Log.Information("You are running the most recent version of winprint");
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

            ServiceLocator.Current.TelemetryService.Start(this.MyInvocation.MyCommand.Name,
                startProperties: new Dictionary<string, string> {
                    ["PowerShellVersion"] = this.Host.Version.ToString(),
                    ["InvocationName"] = this.MyInvocation.InvocationName,
                    ["Debug"] = _debug.ToString(),
                    ["Verbose"] = _verbose.ToString()
                }); ;

            ServiceLocator.Current.LogService.Start(this.MyInvocation.MyCommand.Name, new PowerShellSink(this), _debug, _verbose);

            var ver = FileVersionInfo.GetVersionInfo(Assembly.GetAssembly(typeof(UpdateService)).Location);
            Log.Information("{appname} {version} - {copyright} - {link}", this.MyInvocation.MyCommand.Name, ver.ProductVersion, ver.LegalCopyright, @"https://tig.github.io/winprint");

            await base.BeginProcessingAsync();
        }

        // This method will be called for each input received from the pipeline to this cmdlet; if no input is received, this method is not called
        protected override async Task ProcessRecordAsync() {
            //Log.Debug("ProcessRecordAsync");
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
                    new FormatException($"Invalid data type for {MyInvocation.InvocationName}"),
                    DataNotQualifiedForWinprint,
                    ErrorCategory.InvalidType,
                    null);

                this.ThrowTerminatingError(error);
            }

            _psObjects.Add(input);
        }

        //protected override async Task EndProcessingAsync() {
        //    _cancellationToken = new CancellationTokenSource();
        //    var version = await ServiceLocator.Current.UpdateService.GetLatestStableVersionAsync(_cancellationToken.Token);
        //    Debug.WriteLine($"Version: {version}");
        //    var path = await ServiceLocator.Current.UpdateService.StartUpgradeAsync();
        //    Debug.WriteLine($"path: {path}");
        //    if (await Task.Run(() => ShouldContinue("my message.", "Exit this Powershell instance?")))
        //        Log.Information($"Yes.");
        //    else
        //        Log.Information($"No.");
        //    await base.EndProcessingAsync();
        //}

        // This method will be called once at the end of pipeline execution; if no input is received, this method is not called
        protected override async Task EndProcessingAsync() {
            await base.EndProcessingAsync();

            //Log.Debug("EndProcessingAsync");

            Setup();

            // Check for new version
            if (_installUpdate) {
                await DoUpdateAsync();
                CleanUp();
                return;
            }

            // Whenever we run, check for an update. We use the cancellation token to kill the thread that's doing this
            // if we exit before getting a version info result back. Checking for updates should never shlow cmd line down.
            Log.Debug("Kicking off update check thread...");
            await Task.Run(() => ServiceLocator.Current.UpdateService.GetLatestStableVersionAsync(_cancellationToken.Token).ConfigureAwait(false),
                _cancellationToken.Token);

            //Return if no objects
            if (_psObjects.Count == 0) {
                Log.Debug("No objects...");
                CleanUp();
                return;
            }

            ProgressRecord rec = new ProgressRecord(1, "Printing", "Printing...");
            rec.PercentComplete = 0;
            rec.StatusDescription = "Initializing winprint";
            WriteProgress(rec);

            // See: https://stackoverflow.com/questions/60712580/invoking-cmdlet-from-a-c-based-pscmdlet-providing-input-and-capturing-output
            var result = this.SessionState.InvokeCommand.InvokeScript(@"$input | Out-String", true, PipelineResultTypes.None, _psObjects, null);
            string text = result[0].ToString();

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
                    foreach (string printer in PrinterSettings.InstalledPrinters)
                        Log.Information("   {printer}", printer);
                    Log.Fatal(e, "");
                }
            }

            if (!string.IsNullOrEmpty(FileName)) {
                FileName = this.MyInvocation.MyCommand.Name;
            }

            _print.SheetViewModel.File = FileName;

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


            string sheetID;
            SheetSettings sheet = _print.SheetViewModel.FindSheet(SheetDefintion, out sheetID);

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
            if (string.IsNullOrEmpty(ContentTypeEngine))
                ContentTypeEngine = "text/plain";

            rec.PercentComplete = 30;
            rec.StatusDescription = $"Loading content";
            WriteProgress(rec);
            await _print.SheetViewModel.LoadStringAsync(text, ContentTypeEngine).ConfigureAwait(false);

            rec.PercentComplete = 40;
            rec.StatusDescription = _whatIf ? "Counting" : $"Printing";
            WriteProgress(rec);
            var sheetsCounted = 0;
            if (_whatIf) {
                sheetsCounted = await _print.CountSheets().ConfigureAwait(false);
            }
            else {
                sheetsCounted = await _print.DoPrint().ConfigureAwait(false);
            }

            if (_verbose) {
                if (ModelLocator.Current.Options.CountPages)
                    Log.Information("Would have printed a total of {pagesCounted} sheets.", sheetsCounted);
                else
                    Log.Information("Printed a total of {pagesCounted} sheets.", sheetsCounted);
            }

            // Don't write anything out to the pipeline if PassThru wasn't specified.
            //if (!PassThru.IsPresent) {
            //    return;
            //}

            //this.WriteObject(sheetsCounted, false);

            rec.PercentComplete = -1;
            rec.StatusDescription = $"Complete";
            WriteProgress(rec);

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
        protected void Dispose(bool disposing) {
            if (disposing) {
                AppDomain.CurrentDomain.UnhandledException -= CurrentDomain_UnhandledException;
                TaskScheduler.UnobservedTaskException -= TaskScheduler_UnobservedTaskException;
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

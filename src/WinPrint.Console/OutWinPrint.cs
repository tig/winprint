using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.Drawing.Printing;
using System.IO;
using System.Linq;
using System.Linq.Dynamic.Core;
using System.Management.Automation;
using System.Management.Automation.Internal;
using System.Management.Automation.Runspaces;
using System.Reflection;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Serilog;
using Serilog.Events;
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
    [Alias("winprint", "wp")]
    public partial class OutWinPrint : AsyncCmdlet, IDynamicParameters {
        // Private fields
        private List<PSObject> _psObjects = new List<PSObject>();
        private Print _print = new WinPrint.Core.Print();

        #region Command Line Switches
        /// <summary>
        /// Optional name of the printer to print to.
        /// The alias allows "lp -P printer".
        /// Name alias: because that's what out-printer uses.
        /// </summary>
        [Parameter(HelpMessage = "The name of the printer to print to. If not specified the default printer will be used.",
            ParameterSetName = "Print"), ArgumentCompleter(typeof(PrinterNameCompleter))]
        [Alias("Name")]
        public string PrinterName { get; set; }

        /// <summary>
        /// Optional The paper size name.
        /// </summary>
        /// PaperSize - Implemented via IDynamicParameters.GetDynamicParameters

        /// <summary>
        /// Optional name of the WinPrint sheet definition to use.
        /// </summary>
        /// SheetDefinition - Implemented via IDynamicParameters.GetDynamicParameters

        public enum PortraitLandscape {
            Portrait = 0,
            Landscape = 1
        }
        /// <summary>
        /// If specified, overrides the landscape setting in the sheet definition.
        /// </summary>
        [Parameter(HelpMessage = "If specified (Yes or No) overrides the landscape setting in the sheet definition.",
            ParameterSetName = "Print")]
        public PortraitLandscape? Orientation { get; set; }

        public enum YesNo {
            No = 0,
            Yes = 1
        }
        /// <summary>
        /// If specified, overrides the line numbers setting in the sheet definition.
        /// </summary>
        [Parameter(HelpMessage = " If specfied, overrides the line numbers setting in the sheet definition (Yes, No).",
            ParameterSetName = "Print")]
        public YesNo? LineNumbers { get; set; }

        /// <summary>
        /// Optional name of the WinPrint Content Type Engine to use.
        /// </summary>
        // ContentTypeEngine - Implemented via IDynamicParameters.GetDynamicParameters

        //// -Language

        /// <summary>
        /// Optional language or content type to use for syntax highlighting. 
        /// </summary>
        [Parameter(HelpMessage = "Optional language or content type to use for syntax highlighting. If specified, automatic detection will be overridden. E.g. \"C#\" or \"text/x-julia\"",
            ParameterSetName = "Print"), ArgumentCompleter(typeof(LanguageCompleter))]
        [Alias("Lang")]
        public string Language { get; set; }

        /// <summary>
        /// Optional FileName - will be displayed in header/footer and as title of print job (if -Title is not provided).
        /// If $input is not available, FileName will be used as the path to the file to print.
        /// </summary>
        [Parameter(Position = 0, HelpMessage = "FileName to be displayed in header/footer with the {FileName} (and {Title} if -Title is not provided) macros. " +
            "If ContentType is not specified, the Filename will be used to try to determine the content type engine to use. " +
            "If $input is not available, FileName will be used as the path to the file to print.",
            ParameterSetName = "Print")]
        [Alias("File")]
        public string FileName { get; set; }

        /// <summary>
        /// Optional FileName - will be displayed in header/footer and as title of print job.
        /// </summary>
        [Parameter(HelpMessage = "Title to be displayed in header/footer with the {Title} macro.",
            ParameterSetName = "Print")]
        public string Title { get; set; }

        /// <summary>
        ///  Number of first sheet to print(may be used with `-ToSheet`)
        /// </summary>
        [Parameter(HelpMessage = "Number of first sheet to print (may be used with `-ToSheet`).",
        ParameterSetName = "Print")]
        public int FromSheet { get; set; } = 0;

        /// <summary>
        ///  Number of last sheet to print(may be used with `-Fromsheet`)
        /// </summary>
        [Parameter(HelpMessage = "Number of last sheet to print(may be used with `--Fromsheet`).",
        ParameterSetName = "Print")]
        public int ToSheet { get; set; } = 0;

        /// <summary>
        /// Show *winprint* GUI (to preview or change sheet settings).
        /// </summary>
        [Parameter(HelpMessage = "Show *winprint* GUI (to preview or change sheet settings).", ParameterSetName = "Print")]
        public SwitchParameter Gui { get; set; }
        /// <summary>
        /// For the -Verbose switch
        /// </summary>
        private bool _verbose => MyInvocation.BoundParameters.TryGetValue("Verbose", out var o);

        /// <summary>
        /// For the -Debug switch
        /// </summary>
        private bool _debug => MyInvocation.BoundParameters.TryGetValue("Debug", out var o);

        /// <summary>
        /// Input stream.
        /// </summary>
        [Parameter(ValueFromPipeline = true,
            ParameterSetName = "Print")]
        public PSObject InputObject { set; get; } = AutomationNull.Value;

        /// <summary>
        /// -WhatIf switch
        /// </summary>
        [Parameter(HelpMessage = "Output is the number of sheets that would be printed. Use -Verbose to print the count of .",
            ParameterSetName = "Print")]
        public SwitchParameter WhatIf { get; set; }
        //private bool _whatIf { get => MyInvocation.BoundParameters.TryGetValue("WhatIf", out var o); }

        /// <summary>
        /// -InstallUpdate switch
        /// </summary>
        [Parameter(HelpMessage = "If an updated version of winprint is available online, download and install it.",
            ParameterSetName = "Updates")]
        public SwitchParameter InstallUpdate { get; set; }
        private bool _installUpdate => MyInvocation.BoundParameters.TryGetValue("InstallUpdate", out var o);

        /// <summary>
        /// -Force switch
        /// </summary>
        [Parameter(HelpMessage = "Allows winprint to kill the host Powershell process when updating.",
            ParameterSetName = "Updates")]
        public SwitchParameter Force { get; set; }
        private bool _force => MyInvocation.BoundParameters.TryGetValue("Force", out var o);

        /// <summary>
        /// -InstallUpdate switch
        /// </summary>
        [Parameter(HelpMessage = "Edit the winprint config file in the editor configured for .json files.",
            ParameterSetName = "Config")]
        public SwitchParameter Config { get; set; }

        #endregion

        #region Update Service Related Code

        private CancellationTokenSource _getVersionCancellationToken;

        // Update stuff
        private void SetupUpdateHandler() {
            Log.Debug("Update Handler Setup");
            _getVersionCancellationToken = new CancellationTokenSource();
            ServiceLocator.Current.UpdateService.GotLatestVersion += UpdateService_GotLatestVersion;
            ServiceLocator.Current.UpdateService.DownloadProgressChanged += UpdateService_DownloadProgressChanged;
        }
        private void CleanUpUpdateHandler() {
            Log.Debug("Update Handler Cleanup");
            ServiceLocator.Current.UpdateService.GotLatestVersion -= UpdateService_GotLatestVersion;
            ServiceLocator.Current.UpdateService.DownloadProgressChanged -= UpdateService_DownloadProgressChanged;
            _getVersionCancellationToken?.Cancel();
        }

        private void UpdateService_DownloadProgressChanged(object sender, System.Net.DownloadProgressChangedEventArgs e) {
            //Debug.WriteLine("UpdateService_DownloadProgressChanged");
            var rec = new ProgressRecord(0, "Downloading", "Downloading") {
                CurrentOperation = $"Downloading",
                PercentComplete = e.ProgressPercentage
            };
            WriteProgress(rec);
        }
        private async Task<bool> DoUpdateAsync() {
            var version = await ServiceLocator.Current.UpdateService.GetLatestVersionAsync(_getVersionCancellationToken.Token).ConfigureAwait(true);
            var path = await ServiceLocator.Current.UpdateService.StartUpgradeAsync().ConfigureAwait(true);
            Log.Debug("Latest version found: {ver}", version);
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
            var rec = new ProgressRecord(0, "Installing", $"Download Complete") {
                CurrentOperation = $"Installing",
                PercentComplete = -1
            };
            WriteProgress(rec);

            try {
                p.Start();
            }
            catch (Win32Exception we) {
                Log.Information($"{GetType().Name}: '{p.StartInfo.FileName} {p.StartInfo.Arguments}' failed to run with error: {we.Message}");
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
            Log.Debug("UpdateService_GotLatestVersion");
            if (_getVersionCancellationToken.IsCancellationRequested) {
                return;
            }

            if (version == null && !string.IsNullOrWhiteSpace(ServiceLocator.Current.UpdateService.ErrorMessage)) {
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
            Log.Debug("UpdateService_GotLatestVersion" + _updateMsg);

        }
        #endregion

        #region PowerShell AsyncCmdlet Overrides
        /// <summary>
        /// Read command line parameters.
        /// This method gets called once for each cmdlet in the pipeline when the pipeline starts executing
        /// </summary>
        protected override async Task BeginProcessingAsync() {
            Log.Debug("BeginProcessingAsync");

            // If this is the first invoke since loading start telemetry and logging
            if (ServiceLocator.Current.TelemetryService.GetTelemetryClient() == null) {
                ServiceLocator.Current.TelemetryService.Start("out-winprint");

                // AsyncCmdlet base adds each cmdlet instance to PowerShellSink.Instance; this call configures
                // the Debug and File LogEventLevel's only
                ServiceLocator.Current.LogService.Start("out-winprint", PowerShellSink.Instance, debug: _debug, verbose: _verbose);
            }
            else {
                // Change Console logging as specified by paramters (e.g. -verbose and/or -debug)
                // ConsoleLevelSwitch is for the PowerShellSink logger only
                ServiceLocator.Current.LogService.ConsoleLevelSwitch.MinimumLevel = (_verbose ? LogEventLevel.Information : LogEventLevel.Warning);
                ServiceLocator.Current.LogService.ConsoleLevelSwitch.MinimumLevel = (_debug ? LogEventLevel.Debug : ServiceLocator.Current.LogService.ConsoleLevelSwitch.MinimumLevel);
            }

            var ver = FileVersionInfo.GetVersionInfo(Assembly.GetAssembly(typeof(UpdateService)).Location);
            Log.Information("out-winprint v{version} - {copyright} - {link}", ver.ProductVersion, ver.LegalCopyright, @"https://tig.github.io/winprint");
            Log.Debug("PowerShell Invoked: command: {appname}, module: {modulename}", MyInvocation.MyCommand.Name, MyInvocation.MyCommand.ModuleName);

            var dict = MyInvocation.BoundParameters.ToDictionary(item => item.Key, item => $"{item.Value}");
            Log.Debug("Bound Parameters: {params}", JsonSerializer.Serialize(dict, new JsonSerializerOptions { WriteIndented = false }));
            ServiceLocator.Current.TelemetryService.TrackEvent($"{MyInvocation.MyCommand.Name} BeginProcessing", properties: dict);

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
                    "DataNotQualifiedForWinprint",
                    ErrorCategory.InvalidType,
                    null);

                ThrowTerminatingError(error);
            }
            _psObjects.Add(input);
        }

        // This method will be called once at the end of pipeline execution; if no input is received, this method is not called
        protected override async Task EndProcessingAsync() {
            await base.EndProcessingAsync().ConfigureAwait(true);
            Log.Debug("EndProcessingAsync");

            if (Config) {
                Process proc = null;
                try {
                    var psi = new ProcessStartInfo {
                        UseShellExecute = true,   // This is important
                        FileName = ServiceLocator.Current.SettingsService.SettingsFileName
                    };
                    proc = Process.Start(psi);
                }
                catch (Win32Exception e) {
                    // TODO: Better error message (output of stderr?)
                    ServiceLocator.Current.TelemetryService.TrackException(e, false);

                    Log.Error(e, $"Couldn't open settings file {ServiceLocator.Current.SettingsService.SettingsFileName}.");
                }
                finally {
                    proc?.Dispose();
                }
                return;
            }

            if (WinPrint.Core.Models.ModelLocator.Current.Settings == null) {
                Log.Fatal(new Exception($"Settings are invalid. See {ServiceLocator.Current.LogService.LogPath} for more information."), "");
                return;
            }

            SetupUpdateHandler();

            // Check for new version
            if (_installUpdate) {
                await DoUpdateAsync().ConfigureAwait(true);
                CleanUpUpdateHandler();
                return;
            }

            if (_psObjects.Count == 0) {
                Log.Debug("No objects...");

                if (string.IsNullOrEmpty(FileName)) {
                    //Return if no objects or file specified
                    CleanUpUpdateHandler();
                    return;
                }
            }

            // Whenever we run, check for an update. We use the cancellation token to kill the thread that's doing this
            // if we exit before getting a version info result back. Checking for updates should never shlow cmd line down.
            Log.Debug("Kicking off update check thread...");
            await Task.Run(() => ServiceLocator.Current.UpdateService.GetLatestVersionAsync(_getVersionCancellationToken.Token).ConfigureAwait(true),
                _getVersionCancellationToken.Token).ConfigureAwait(true);

            var rec = new ProgressRecord(1, "Printing", "Printing...") {
                PercentComplete = 0,
                StatusDescription = "Initializing winprint"
            };
            WriteProgress(rec);

            Debug.Assert(_print != null);
            if (!string.IsNullOrEmpty(PrinterName)) {
                try {
                    rec.PercentComplete = 10;
                    rec.StatusDescription = $"Setting printer name to {PrinterName}";
                    WriteProgress(rec);
                    _print.SetPrinter(PrinterName);
                }
                catch (InvalidPrinterException) {
                    Log.Information("Installed printers:");
                    foreach (string printer in PrinterSettings.InstalledPrinters) {
                        Log.Information("   {printer}", printer);
                    }
                    Log.Fatal(new Exception($"{PrinterName} is not a valid printer name. Valid printer names include " +
                        $"{string.Join(", ", PrinterSettings.InstalledPrinters.ToDynamicList().ToArray())}."), "");
                    CleanUpUpdateHandler();
                    return;
                }
            }

            if (string.IsNullOrEmpty(Title)) {
                if (string.IsNullOrEmpty(FileName)) {
                    Title = MyInvocation.MyCommand.Name;
                }
                else {
                    Title = FileName;
                }
            }

            // Core requires a fully qualified path. If FileName was provided, ensure it's fully qualified.
            // Note, Title stays as was provided via -FileName or -Title
            if (!string.IsNullOrEmpty(FileName) && !Path.IsPathFullyQualified(FileName)) {
                FileName = Path.GetFullPath(FileName, SessionState.Path.CurrentFileSystemLocation.Path);
            }

            SheetSettings sheet = null;
            string sheetID = null;
            try {
                MyInvocation.BoundParameters.TryGetValue("SheetDefinition", out var sheetDefinition);
                sheet = _print.SheetViewModel.FindSheet((string)sheetDefinition, out sheetID);

                rec.PercentComplete = 20;
                rec.StatusDescription = $"Setting Sheet Settings for {sheet.Name}";
                WriteProgress(rec);

                if (Orientation.HasValue) {
                    sheet.Landscape = Orientation == PortraitLandscape.Landscape;
                }

                if (LineNumbers.HasValue) {
                    sheet.ContentSettings.LineNumbers = LineNumbers == YesNo.Yes;
                }

                // Must set landscape after printer/paper selection
                _print.PrintDocument.DefaultPageSettings.Landscape = sheet.Landscape;
                _print.SheetViewModel.SetSheet(sheet);
            }
            catch (InvalidOperationException e) {
                Log.Fatal(new Exception($"Could not find sheet settings. {e.Message}. See {ServiceLocator.Current.LogService.LogPath} for more information."), "");
                CleanUpUpdateHandler();
                return;
            }

            // If Language is provided, use it instead of CTE.
            if (!MyInvocation.BoundParameters.TryGetValue("Language", out var contentType)) {
                if (!MyInvocation.BoundParameters.TryGetValue("ContentTypeEngine", out contentType)) {
                    // If neither were specified, smartly pick CTE
                    contentType = ContentTypeEngineBase.GetContentType(FileName);
                }
            }

            if (MyInvocation.BoundParameters.TryGetValue("PaperSize", out var paperSize)) {
                _print.SetPaperSize((string)paperSize);
            }

            rec.PercentComplete = 30;
            rec.StatusDescription = $"Loading content";
            WriteProgress(rec);

            _print.SheetViewModel.File = FileName;
            _print.SheetViewModel.Title = Title;

            _print.PrintingSheet += (s, sheetNum) => {
                if (sheetNum > 60) {
                    rec.PercentComplete = 95;
                }
                else {
                    rec.PercentComplete = 40 + sheetNum;
                }

                rec.StatusDescription = $"Printing sheet {sheetNum}";
                WriteProgress(rec);
                Log.Information("Printing sheet {sheetNum}", sheetNum);
            };

            try {
                if (_psObjects.Count == 0 && !string.IsNullOrEmpty(FileName)) {
                    if (!Path.IsPathFullyQualified(FileName)) {
                        FileName = Path.GetFullPath(FileName, SessionState.Path.CurrentFileSystemLocation.Path);
                    }

                    await _print.SheetViewModel.LoadFileAsync(FileName, (string)contentType).ConfigureAwait(true);
                }
                else {
                    // Get $input into a string we can use
                    // See: https://stackoverflow.com/questions/60712580/invoking-cmdlet-from-a-c-based-pscmdlet-providing-input-and-capturing-output
                    var textToPrint = SessionState.InvokeCommand.InvokeScript(@"$input | Out-String", true, PipelineResultTypes.None, _psObjects, null)[0].ToString();

                    _print.SheetViewModel.Encoding = Encoding.UTF8;
                    await _print.SheetViewModel.LoadStringAsync(textToPrint, (string)contentType).ConfigureAwait(true);
                }
                if (_verbose) {
                    Log.Information("FileName:            {FileName}", FileName ?? "");
                    Log.Information("Title:               {title}", Title ?? "");
                    Log.Information("Content Type:        {contentType}", _print.SheetViewModel.ContentType);
                    Log.Information("Language:            {Language}", _print.SheetViewModel.Language);
                    Log.Information("Content Type Engine: {cte}", _print.SheetViewModel.ContentEngine.GetType().Name);
                    Log.Information("Printer:             {printer}", _print.PrintDocument.PrinterSettings.PrinterName);
                    Log.Information("Paper Size:          {size}", _print.PrintDocument.DefaultPageSettings.PaperSize.PaperName);
                    Log.Information("Orientation:         {s}", _print.PrintDocument.DefaultPageSettings.Landscape ? $"Landscape" : $"Portrait");
                    Log.Information("Sheet Definition:    {name} ({id})", sheet.Name, sheetID);
                }
            }
            catch (System.IO.DirectoryNotFoundException dnfe) {
                Log.Error(dnfe, "Print failed.");
                CleanUpUpdateHandler();
                return;
            }
            catch (System.IO.FileNotFoundException fnfe) {
                Log.Error(fnfe, "Print failed.");
                CleanUpUpdateHandler();
                return;
            }
            catch (InvalidOperationException ioe) {
                // TODO: Use our own execptions
                Log.Error(ioe, "Print failed.");
                CleanUpUpdateHandler();
                return;
            }

            rec.PercentComplete = 40;
            rec.StatusDescription = WhatIf ? "Counting" : $"Printing";
            WriteProgress(rec);

            var sheetsCounted = 0;
            try {
                var sheetRangeSet = false;
                if (FromSheet != 0) {
                    _print.PrintDocument.PrinterSettings.FromPage = FromSheet;
                    sheetRangeSet = true;
                }
                else {
                    _print.PrintDocument.PrinterSettings.FromPage = 0;
                }

                if (ToSheet != 0) {
                    _print.PrintDocument.PrinterSettings.ToPage = ToSheet;
                    sheetRangeSet = true;
                }
                else {
                    _print.PrintDocument.PrinterSettings.ToPage = 0;
                }

                if (sheetRangeSet) {
                    Log.Information("Printing from sheet {from} to sheet {to}.", _print.PrintDocument.PrinterSettings.FromPage, _print.PrintDocument.PrinterSettings.ToPage);
                }

                if (WhatIf) {
                    sheetsCounted = await _print.CountSheets().ConfigureAwait(true);
                }
                else {
                    sheetsCounted = await _print.DoPrint().ConfigureAwait(true);
                }
            }
            catch (System.ComponentModel.Win32Exception w32e) {
                // This can happen when PDF driver can't access PDF file.
                Log.Error(w32e, "Print failed.");
                CleanUpUpdateHandler();
                return;
            }

            // Finalize Progress
            rec.PercentComplete = -1;
            rec.StatusDescription = $"Complete";
            WriteProgress(rec);

            // Output via verbose how much printing got done
            if (_verbose) {
                Log.Information($"{(WhatIf ? "Would have printed" : "Printed")} {{sheetsCounted}} sheets.", sheetsCounted);
            }

            // End by sharing update info, if any
            if (!string.IsNullOrEmpty(_updateMsg)) {
                Log.Information(_updateMsg);
            }

            CleanUpUpdateHandler();

            // Very last thing we do is write to the output if WhatIf was specified
            if (WhatIf) {
                WriteObject(sheetsCounted, false);
            }
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

                default:
                    throw new InvalidOperationException($"Property change not handled: {e.PropertyName}");
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
        protected virtual object InputObjectCall() {
            // just bind to the input object parameter
            return InputObject;
        }

        /// <summary>
        /// Callback for the implementation to write objects.
        /// </summary>
        /// <param name="value">Object to be written.</param>
        protected virtual void WriteObjectCall(object value) {
            // just call Monad API
            WriteObject(value);
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
            //base.Dispose(disposing);
            if (disposing) {
                AppDomain.CurrentDomain.UnhandledException -= CurrentDomain_UnhandledException;
                TaskScheduler.UnobservedTaskException -= TaskScheduler_UnobservedTaskException;

                _print?.Dispose();
                _getVersionCancellationToken?.Dispose();
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

        /// <summary>
        /// See https://stackoverflow.com/questions/25823910/pscmdlet-dynamic-auto-complete-a-parameter-like-get-process
        /// </summary>
        /// <returns></returns>
        public object GetDynamicParameters() {

            // We can't report errors here
            if (WinPrint.Core.Models.ModelLocator.Current.Settings == null) {
                //System.Console.WriteLine($"Settings are not valid. Check {ServiceLocator.Current.SettingsService.SettingsFileName}.");
                return null;
            }

            var runtimeDict = new RuntimeDefinedParameterDictionary();

            // -PrinterName
            var printerNames = new List<string>();
            using var pd = new PrintDocument();

            if (!string.IsNullOrEmpty(PrinterName)) {
                PrinterName = PrinterName.Trim('\"').Trim('\'');
                pd.PrinterSettings.PrinterName = PrinterName;

                foreach (PaperSize size in pd.PrinterSettings.PaperSizes) {
                    printerNames.Add(size.PaperName);
                }
            }

            runtimeDict.Add("PaperSize", new RuntimeDefinedParameter("PaperSize", typeof(string), new Collection<Attribute>() {
                    new ParameterAttribute() {
                        HelpMessage = "The paper size name. E.g. \"Letter\"",
                        ParameterSetName = "Print"
                    },
                    printerNames.Count > 0 ? new ValidateSetAttribute(printerNames.ToArray()) : null
            }));

            // -SheetDefinition
            //  [Parameter(HelpMessage = "Name of the WinPrint sheet definition to use (e.g. \"Default 2-Up\")",
            //    ParameterSetName = "Print")]
            runtimeDict.Add("SheetDefinition", new RuntimeDefinedParameter("SheetDefinition", typeof(string), new Collection<Attribute>() {
                    new ParameterAttribute() {
                        HelpMessage = "Name of the WinPrint sheet definition to use (e.g. \"Default 2-Up\").",
                        ParameterSetName = "Print"
                    },
                    ModelLocator.Current.Settings.Sheets.Count > 0 ? new ValidateSetAttribute(ModelLocator.Current.Settings.Sheets.Values.Select(s => s.Name).ToArray()) : null
            }));

            // -ContentTypeEngine
            runtimeDict.Add("ContentTypeEngine", new RuntimeDefinedParameter("ContentTypeEngine", typeof(string), new Collection<Attribute>() {
                    new ParameterAttribute() {
                        HelpMessage = "Optional name of the WinPrint Content Type Engine to use. If specified, automatic selection will be overridden. E.g. \"TextCte\".",
                        ParameterSetName = "Print"
                    },
                    new ValidateSetAttribute(ContentTypeEngineBase.GetDerivedClassesCollection().Select(cte => cte.GetType().Name).ToArray())
            }));
            return runtimeDict;
        }
    }
}

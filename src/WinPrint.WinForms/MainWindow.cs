// Copyright Kindel, LLC - http://www.kindel.com
// Published under the MIT License at https://github.com/tig/winprint

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Printing;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using LiteHtmlSharp;
using Serilog;
using WinPrint.Core;
using WinPrint.Core.Models;
using WinPrint.Core.Services;
using WinPrint.WinForms;

namespace WinPrint.Winforms {
    public partial class MainWindow : Form {

        // The Windows printer document
        private PrintDocument printDoc = new PrintDocument();

        // The active file
        private string activeFile;
        private OpenFileDialog openFileDialog = new OpenFileDialog();

        private WinPrint.WinForms.PrintPreview printPreview;

        public MainWindow() {
            InitializeComponent();

            Icon = Resources.printer_and_fax_w;

            printPreview = new PrintPreview {
                Dock = dummyButton.Dock,
                Anchor = dummyButton.Anchor,
                BackColor = dummyButton.BackColor,
                Location = dummyButton.Location,
                Margin = dummyButton.Margin,
                Padding = dummyButton.Padding,
                Name = "printPreview",
                Size = dummyButton.Size,
                MinimumSize = new Size(0, 0),
                TabIndex = 1,
                TabStop = true
            };
            printPreview.Click += new System.EventHandler(printPreview_Click);

            Color();

            // This gets the version # from winprint.core.dll
            versionLabel.Text = $"v{FileVersionInfo.GetVersionInfo(Assembly.GetAssembly(typeof(LogService)).Location).FileVersion}";

            if (LicenseManager.UsageMode != LicenseUsageMode.Designtime) {
                panelRight.Controls.Remove(dummyButton);
                panelRight.Controls.Add(printPreview);
                printersCB.Enabled = false;
                paperSizesCB.Enabled = false;
            }

#if DEBUG
            openFileDialog.InitialDirectory = $@"..\..\..\..\..\testfiles\";
#else
            openFileDialog.InitialDirectory = $"{Environment.GetFolderPath(Environment.SpecialFolder.UserProfile)}";
#endif

            // Set common fontDialog props
            fontDialog.ShowEffects = false;
            fontDialog.ShowColor = true;
            fontDialog.AllowScriptChange = false;
            fontDialog.AllowSimulations = false;  // GDI+ does not support OFT fonts
            fontDialog.AllowVectorFonts = false;
            // fontDialog.AllowVerticalFonts = false;
            fontDialog.ShowHelp = false;
        }

        private void Color() {
            var back = System.Drawing.Color.FromName("white");
            var text = System.Drawing.SystemColors.ControlText;
            //printPreview.BackColor = back;
            printersCB.BackColor = back;

            settingsButton.BackColor = back;
            paperSizesCB.BackColor = back;
            landscapeCheckbox.BackColor = back;
            printButton.BackColor = back;
            pageUp.BackColor = back;
            pageDown.BackColor = back;
            headerTextBox.BackColor = back;
            footerTextBox.BackColor = back;
            panelLeft.BackColor = back;
            enableHeader.BackColor = back;
            enableFooter.BackColor = back;
            comboBoxSheet.BackColor = back;
            labelPaper.BackColor = back;
            labelTop.BackColor = back;
            topMargin.BackColor = back;
            labelLeft.BackColor = back;
            leftMargin.BackColor = back;
            labelRight.BackColor = back;
            rightMargin.BackColor = back;
            labelBottom.BackColor = back;
            bottomMargin.BackColor = back;
            labelRows.BackColor = back;
            rows.BackColor = back;
            labelColumns.BackColor = back;
            columns.BackColor = back;
            groupMargins.BackColor = back;
            groupPages.BackColor = back;
            pageSeparator.BackColor = back;
            labelPadding.BackColor = back;
            padding.BackColor = back;
            headerPanel.BackColor = back;
            footerPanel.BackColor = back;
            fileButton.BackColor = back;
            toText.BackColor = back;
            label1.BackColor = back;
            fromText.BackColor = back;
            fromLabel.BackColor = back;
            pagesLabel.BackColor = back;

            printPreview.ForeColor = text;
            printersCB.ForeColor = text;
            paperSizesCB.ForeColor = text;
            landscapeCheckbox.ForeColor = text;
            printButton.ForeColor = text;
            pageUp.ForeColor = text;
            pageDown.ForeColor = text;
            headerTextBox.ForeColor = text;
            footerTextBox.ForeColor = text;
            panelLeft.ForeColor = text;
            enableHeader.ForeColor = text;
            enableFooter.ForeColor = text;
            comboBoxSheet.ForeColor = text;
            labelPaper.ForeColor = text;
            labelTop.ForeColor = text;
            topMargin.ForeColor = text;
            labelLeft.ForeColor = text;
            leftMargin.ForeColor = text;
            labelRight.ForeColor = text;
            rightMargin.ForeColor = text;
            labelBottom.ForeColor = text;
            bottomMargin.ForeColor = text;
            labelRows.ForeColor = text;
            rows.ForeColor = text;
            labelColumns.ForeColor = text;
            columns.ForeColor = text;
            groupMargins.ForeColor = text;
            groupPages.ForeColor = text;
            pageSeparator.ForeColor = text;
            labelPadding.ForeColor = text;
            padding.ForeColor = text;
            headerPanel.ForeColor = text;
            footerPanel.ForeColor = text;
            fileButton.ForeColor = text;
            toText.ForeColor = text;
            label1.ForeColor = text;
            fromText.ForeColor = text;
            fromLabel.ForeColor = text;
            pagesLabel.ForeColor = text;
        }

        // Flag: Has Dispose already been called?
        private bool disposed = false;
        // Protected implementation of Dispose pattern.
        /// <summary>
        ///  Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose(bool disposing) {
            if (disposed) {
                return;
            }

            if (disposing && (components != null)) {
                components.Dispose();
                //if (streamToPrint != null) streamToPrint.Dispose();
                if (printDoc != null) {
                    printDoc.Dispose();
                }

                if (printPreview != null) {
                    printPreview.Dispose();
                }
            }
            disposed = true;
            base.Dispose(disposing);
        }

        /// <summary>
        /// Wire up property change notifications from the View Model.
        /// This should only be called once
        /// </summary>
        // TODO: Refactor PropertyChanged lambdas to be functions so they can be -=
        private void SetupSheetViewModelNotifications() {
            //LogService.TraceMessage();
            if (printPreview.SheetViewModel != null) {
                LogService.TraceMessage("SetupSheetViewModelNotifications was already called");
                return;
            }

            printPreview.SheetViewModel = new SheetViewModel();
            printPreview.SheetViewModel.PropertyChanged += PropertyChangedEventHandler;
            printPreview.SheetViewModel.SettingsChanged += SettingsChangedEventHandler;
            printPreview.SheetViewModel.PageSettingsSet += PageSettingsSetEventHandler;
            printPreview.SheetViewModel.Loaded += FileLoadedEventHandler;
            printPreview.SheetViewModel.ReadyChanged += ReadyChangedEventHandler;
        }

        private void FileLoadedEventHandler(object sender, bool loading) {
            if (InvokeRequired) {
                BeginInvoke((Action)(() => FileLoadedEventHandler(sender, loading)));
            }
            else {
                LogService.TraceMessage($"{loading}");
                if (loading) {
                    return;
                }
            }
        }

        private void PageSettingsSetEventHandler(object sender, EventArgs e) {
            if (InvokeRequired) {
                BeginInvoke((Action)(() => PageSettingsSetEventHandler(sender, e)));
            }
            else {
                LogService.TraceMessage();
            }
        }

        private void ReadyChangedEventHandler(object sender, bool ready) {
            if (InvokeRequired) {
                BeginInvoke((Action)(() => ReadyChangedEventHandler(sender, ready)));
            }
            else {
                LogService.TraceMessage($"{ready}");
                printButton.Enabled = ready;

                if (!ready) {
                    return;
                }

                if (string.IsNullOrEmpty(activeFile)) {
                    printPreview.Invalidate();
                    printPreview.Text = Resources.HelloMsg;
                }
                else {
                    // Document is loaded and flowed. We're ready to preview and/or pring.
                    printPreview.CurrentSheet = 1;
                    printPreview.Invalidate();
                    printPreview.Text = "";
                }
            }
        }

        private void PropertyChangedEventHandler(object sender, PropertyChangedEventArgs e) {
            if (InvokeRequired) {
                BeginInvoke((Action)(() => PropertyChangedEventHandler(sender, e)));
            }
            else {
                //LogService.TraceMessage($"SheetViewModel.PropertyChanged: {e.PropertyName}");
                switch (e.PropertyName) {
                    case "Landscape":
                        LogService.TraceMessage($"  Checking checkbox: {ModelLocator.Current.Settings.Sheets.GetValueOrDefault(ModelLocator.Current.Settings.DefaultSheet.ToString()).Landscape}");
                        landscapeCheckbox.Checked = printPreview.SheetViewModel.Landscape;
                        break;

                    case "Header":
                        headerTextBox.Text = printPreview.SheetViewModel.Header.Text;
                        enableHeader.Checked = printPreview.SheetViewModel.Header.Enabled;
                        headerFooterFontLink.Text = printPreview.SheetViewModel.Header.Font.ToString();
                        break;

                    case "Footer":
                        footerTextBox.Text = printPreview.SheetViewModel.Footer.Text;
                        enableFooter.Checked = printPreview.SheetViewModel.Footer.Enabled;
                        break;

                    case "Margins":
                        topMargin.Value = printPreview.SheetViewModel.Margins.Top / 100M;
                        leftMargin.Value = printPreview.SheetViewModel.Margins.Left / 100M;
                        rightMargin.Value = printPreview.SheetViewModel.Margins.Right / 100M;
                        bottomMargin.Value = printPreview.SheetViewModel.Margins.Bottom / 100M;

                        // Keep PrintDocument updated for WinForms.PrintPreview
                        printDoc.PrinterSettings.DefaultPageSettings.Margins = (Margins)printPreview.SheetViewModel.Margins.Clone();
                        break;

                    case "PageSeparator":
                        pageSeparator.Checked = printPreview.SheetViewModel.PageSeparator;
                        break;

                    case "Rows":
                        rows.Value = printPreview.SheetViewModel.Rows;
                        break;

                    case "Columns":
                        columns.Value = printPreview.SheetViewModel.Columns;
                        break;

                    case "Padding":
                        padding.Value = printPreview.SheetViewModel.Padding / 100M;
                        break;

                    case "File":
                        Text = $"winprint - {printPreview.SheetViewModel.File}";
                        printPreview.CurrentSheet = 1;
                        break;

                    case "Title":
                        break;

                    // When ContentEngine changes we know the document has been loaded.
                    case "ContentEngine":
                        //printPreview.CurrentSheet = 1;
                        //printButton.Enabled = printPreview.SheetViewModel.NumSheets > 0;
                        break;

                    case "ContentType":
                        break;

                    case "Language":
                        break;

                    case "ContentSettings":
                        contentFontLink.Text = printPreview.SheetViewModel.ContentSettings.Font.ToString();
                        lineNumbers.Checked = printPreview.SheetViewModel.ContentSettings.LineNumbers;
                        //lineNumberSeparator.Checked = printPreview.SheetViewModel.ContentSettings.lineNumberSeparator;
                        break;

                    case "DiagnosticRulesFont":
                        break;

                    case "Encoding":
                        break;

                    case "Loading":
                        break;

                    case "Ready":
                        break;


                    default:
                        throw new InvalidOperationException($"Property Change Not Handled: {e.PropertyName}");
                }
            }
        }

        private void SettingsChangedEventHandler(object o, SheetViewModel.SheetViewModelSettingsChangedEvent e) {
            if (InvokeRequired) {
                BeginInvoke((Action)(() => SettingsChangedEventHandler(o, e)));
            }
            else {
                LogService.TraceMessage($"{e.Reflow}");
                if (e.Reflow) {
                    LoadFile();
                }
                else {
                    //if (e.PropertyName.Equals("Header")) {
                    //    printPreview.Invalidate(Rectangle.Round(printPreview.SheetViewModel.Header.Bounds));
                    //}
                    //else if (e.PropertyName.Equals("Footer")) {
                    //    printPreview.Invalidate(Rectangle.Round(printPreview.SheetViewModel.Footer.Bounds));
                    //}
                    //else {
                    //    printPreview.Invalidate();
                    //}
                    printPreview.Invalidate();
                }
            }
        }

        private CancellationTokenSource _cancellationToken = new CancellationTokenSource();

        private void MainWindow_Load(object sender, EventArgs e) {
            // Check for updates
            LogService.TraceMessage("First reference to UpdateService");
            if (ServiceLocator.Current.UpdateService == null) {
                _ = MessageBox.Show(Resources.UpdateServiceFailure);
                return;
            }

            ServiceLocator.Current.UpdateService.GotLatestVersion += UpdateService_GotLatestVersion;
            ServiceLocator.Current.UpdateService.DownloadComplete += UpdateService_DownloadComplete;
            ServiceLocator.Current.UpdateService.GetLatestVersionAsync(_cancellationToken.Token).ConfigureAwait(false);

            // Load settings by referencing ModelLocator.Current
            LogService.TraceMessage("First reference to ModelLocator.Current.Settings");
            if (ModelLocator.Current.Settings == null) {
                MessageBox.Show(Resources.SettingsLoadMsg);
                return;
            }
            if (ModelLocator.Current.Settings.Size != null) {
                Size = new Size(ModelLocator.Current.Settings.Size.Width, ModelLocator.Current.Settings.Size.Height);
            }

            if (ModelLocator.Current.Settings.Location != null) {
                Location = new Point(ModelLocator.Current.Settings.Location.X, ModelLocator.Current.Settings.Location.Y);
            }

            WindowState = (System.Windows.Forms.FormWindowState)ModelLocator.Current.Settings.WindowState;

            printPreview.KeyUp += (s, e) => {
                if (e.KeyCode == Keys.F5) {
                    //printPreview.Invalidate(true);
                    Log.Debug("-------- F5 ---------");

                    ServiceLocator.Current.TelemetryService.TrackEvent("Refresh");

                    // TODO: Refactor threading
                    Task.Run(() => Start());
                }
            };

            //this.Cursor = Cursors.WaitCursor;
            var sheets = ModelLocator.Current.Settings.Sheets;

            // Load Language/ContentType mapping
            LogService.TraceMessage($"{ModelLocator.Current.FileTypeMapping.ContentTypes.Count} languages, {ModelLocator.Current.FileTypeMapping.FilesAssociations.Count} file assocations");

            ModelLocator.Current.Settings.PropertyChanged += (s, e) => BeginInvoke((Action)(() => {
                LogService.TraceMessage($"Settings.PropertyChanged: {e.PropertyName}");
                switch (e.PropertyName) {
                    case "DefaultSheet":
                        SheetChanged();
                        break;
                }
            }));

            comboBoxSheet.DisplayMember = "Value";
            comboBoxSheet.ValueMember = "Key";
            foreach (var s in sheets) {
                comboBoxSheet.Items.Add(new KeyValuePair<string, string>(s.Key, s.Value.Name));
            }

            // Select default printer and paper size
            foreach (string printer in System.Drawing.Printing.PrinterSettings.InstalledPrinters) {
                printersCB.Items.Add(printer);
                if (printDoc.PrinterSettings.IsDefaultPrinter && printer == printDoc.PrinterSettings.PrinterName) {
                    printersCB.Text = printDoc.PrinterSettings.PrinterName;
                }
            }

            // --p
            if (!string.IsNullOrEmpty(ModelLocator.Current.Options.Printer)) {
                printDoc.PrinterSettings.PrinterName = printersCB.Text = ModelLocator.Current.Options.Printer;
            }

            foreach (PaperSize ps in printDoc.PrinterSettings.PaperSizes) {
                paperSizesCB.Items.Add(ps.PaperName);
            }

            // --z
            if (!string.IsNullOrEmpty(ModelLocator.Current.Options.PaperSize)) {
                paperSizesCB.Text = ModelLocator.Current.Options.PaperSize;
            }
            else {
                paperSizesCB.Text = printDoc.DefaultPageSettings.PaperSize.PaperName;
            }

            // We kept these disabled during load
            printersCB.Enabled = true;
            paperSizesCB.Enabled = true;

            // Create sheet view model & wire up notifications
            SetupSheetViewModelNotifications();

            if (ModelLocator.Current.Options.FromPage != 0) {
                //printDoc.PrinterSettings.FromPage = ModelLocator.Current.Options.FromPage;
                fromText.Text = $"{ModelLocator.Current.Options.FromPage}";
            }

            if (ModelLocator.Current.Options.ToPage != 0) {
                //printDoc.PrinterSettings.ToPage = ModelLocator.Current.Options.ToPage;
                toText.Text = $"{ModelLocator.Current.Options.ToPage}";
            }

            // --s
            // Select default Sheet 
            var newSheet = ModelLocator.Current.Settings.Sheets.GetValueOrDefault(ModelLocator.Current.Settings.DefaultSheet.ToString());
            if (!string.IsNullOrEmpty(ModelLocator.Current.Options.Sheet)) {
                newSheet = printPreview.SheetViewModel.FindSheet(ModelLocator.Current.Options.Sheet, out var sheetID);
            }
            comboBoxSheet.Text = newSheet.Name;
            // This will cause a flurry of property change notifications, setting all UI elements
            printPreview.SheetViewModel.SetSheet(newSheet);

            // Must set landscape after printer/paper selection
            // --l and --o
            if (ModelLocator.Current.Options.Landscape) {
                printPreview.SheetViewModel.Landscape = true;
            }

            if (ModelLocator.Current.Options.Portrait) {
                printPreview.SheetViewModel.Landscape = false;
            }

            // Override content-type
            // --t
            if (!string.IsNullOrEmpty(ModelLocator.Current.Options.ContentType)) {
                // TODO: (nothing?)
            }

            printPreview.Select();
            printPreview.Focus();
            //this.Cursor = Cursors.Default;

            if (ModelLocator.Current.Options.Files != null && ModelLocator.Current.Options.Files.ToList().Count > 0) {
                activeFile = ModelLocator.Current.Options.Files.ToList()[0];
            }

            // Verify Pygments is installed
            (bool installed, string message) = ServiceLocator.Current.PygmentsConverterService.CheckInstall();
            if (!installed) {
                MessageBox.Show(message, "Warning");
            }

            // By running this on a different thread, we enable the main window to show
            // as quickly as possible; making startup seem faster.
            //Task.Run(() => Start());
            Start();
        }

        private void UpdateService_DownloadComplete(object sender, string path) {
            //Process.Start(ServiceLocator.Current.UpdateService.ReleasePageUri.AbsoluteUri);
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

            try {
                p.Start();
            }
            catch (Win32Exception we) {
                Log.Information($"{GetType().Name}: '{p.StartInfo.FileName} {p.StartInfo.Arguments}' failed to run with error: {we.Message}");
            }

            BeginInvoke((Action)(() => Close()));
        }

        private void UpdateService_GotLatestVersion(object sender, Version version) {
            if (InvokeRequired) {
                BeginInvoke((Action)(() => UpdateService_GotLatestVersion(sender, version)));
            }
            else {

                if (version == null && !string.IsNullOrWhiteSpace(ServiceLocator.Current.UpdateService.ErrorMessage)) {
                    Log.Information($"Could not access tig.github.io/winprint to see if a newer version is available. {ServiceLocator.Current.UpdateService.ErrorMessage}");
                }
                else if (ServiceLocator.Current.UpdateService.CompareVersions() < 0) {
                    Log.Information("------------------------------------------------");
                    Log.Information($"A newer version of winprint ({version}) is available at");
                    Log.Information($"   {ServiceLocator.Current.UpdateService.ReleasePageUri}");
                    Log.Information("------------------------------------------------");

                    using var dlg = new UpdateDialog();
                    dlg.ShowDialog(this);
                }
                else if (ServiceLocator.Current.UpdateService.CompareVersions() > 0) {
                    Log.Information($"You are are running a MORE recent version than can be found at tig.github.io/winprint ({version})");
                }
                else {
                    Log.Information("You are running the most recent version of winprint");
                }
            }
        }

        private void Start() {
            LogService.TraceMessage();

            if (string.IsNullOrEmpty(activeFile)) {
                // If a file's not been set, juice the print preview and show the file open dialog box
                LoadFile();
                //ShowFilesDialog();
            }
            else {
                LoadFile();
            }
        }

        private void LoadFile() {
            if (InvokeRequired) {
                BeginInvoke((Action)(() => LoadFile()));
            }
            else {
                // Reset View Model
                printPreview.SheetViewModel.Reset();
                printPreview.Invalidate();
                printPreview.Text = Resources.LoadingMsg;

                // On another thread 
                //    - load file
                //    - set printer page settings
                //    - reflow
                Task.Run(async () => {
                    var stage = "Loading";
                    var fileToPrint = activeFile;
                    try {
                        BeginInvoke((Action)(() => {
                            printPreview.Text = $"{stage}...";
                        }));
                        // This is an IO bound operation. 
                        // TODO: This does not need to run on another thread if we are using async/await correctly
                        // Core requires a fully qualified path. If FileName was provided, ensure it's fully qualified.
                        // Note, Title stays as was provided via -FileName or -Title
                        if (!string.IsNullOrEmpty(fileToPrint) && !Path.IsPathFullyQualified(fileToPrint)) {
                            fileToPrint = Path.GetFullPath(fileToPrint, Directory.GetCurrentDirectory());
                        }
                        await printPreview.SheetViewModel.LoadFileAsync(fileToPrint, ModelLocator.Current.Options.ContentType).ConfigureAwait(false);

                        // Set landscape. This causes other DefaultPageSettings to change
                        // These are CPU bound operations. 
                        // TODO: Do not use async/await for CPU bound operations https://docs.microsoft.com/en-us/dotnet/standard/async-in-depth
                        stage = "Getting Printer Page Settings";
                        BeginInvoke((Action)(() => {
                            printPreview.Text = $"{stage}...";
                        }));
                        printDoc.DefaultPageSettings.Landscape = printPreview.SheetViewModel.Landscape;
                        printPreview.SheetViewModel.SetPrinterPageSettings(printDoc.DefaultPageSettings);


                        stage = "Rendering";
                        BeginInvoke((Action)(() => {
                            printPreview.Text = $"{stage}...";
                        }));
                        await printPreview.SheetViewModel.ReflowAsync().ConfigureAwait(false);

                    }
                    catch (DirectoryNotFoundException dnfe) {
                        Log.Error(dnfe, "File Not Found");
                        ShowMessage($"{stage}: {dnfe.Message}");
                        return;
                    }
                    catch (FileNotFoundException fnfe) {
                        Log.Error(fnfe, "File Not Found");
                        ShowMessage($"{stage}: {fnfe.Message}");
                        //fileButton_Click(null, null);
                    }
                    catch (InvalidOperationException ioe) {
                        ServiceLocator.Current.TelemetryService.TrackException(ioe, false);
                        Log.Error(ioe, "Error Operation {file}", fileToPrint);
                        ShowMessage($"{stage}: {ioe.Message}");
                        //                fileButton_Click(null, null);
                    }
#pragma warning disable CA1031 // Do not catch general exception types
                    catch (Exception e) {
#pragma warning restore CA1031 // Do not catch general exception types
                        ServiceLocator.Current.TelemetryService.TrackException(e, false);
                        Log.Error(e, "Exception {file}", fileToPrint);
                        ShowMessage($"{stage}: Exception: {e.Message}{Environment.NewLine}({fileToPrint})");
                    }
                    finally {
                        // Set Loading to false in case of an error
                        //printPreview.SheetViewModel.Loading = false;
                        //printPreview.SheetViewModel.Ready = false;
                        printPreview.Select();
                        printPreview.Focus();

                    }
                });
            }
        }

        private void ShowMessage(string message) {
            if (InvokeRequired) {
                BeginInvoke((Action)(() => ShowMessage(message)));
            }
            else {
                printPreview.Text = message;
            }
        }

        private void ShowFilesDialog() {
            if (InvokeRequired) {
                BeginInvoke((Action)(() => ShowFilesDialog()));
            }
            else {
                LogService.TraceMessage();
                ServiceLocator.Current.TelemetryService.TrackEvent("Show Files Dialog");
                openFileDialog.Filter = Resources.FileOpenTemplate;
                openFileDialog.FilterIndex = 3;
                //openFileDialog.RestoreDirectory = true;
                if (openFileDialog.ShowDialog(this) == DialogResult.OK) {
                    activeFile = openFileDialog.FileNames.ToList()[0];
                    LoadFile();
                }
            }
        }

        private void SheetChanged() {
            LogService.TraceMessage();
            var newSheet = ModelLocator.Current.Settings.Sheets.GetValueOrDefault(ModelLocator.Current.Settings.DefaultSheet.ToString());
            comboBoxSheet.Text = newSheet.Name;
            printPreview.SheetViewModel.SetSheet(newSheet);
            //SheetSettingsChanged();
            LoadFile();
        }

        /// <summary>
        /// If Sheet settings change (either a new sheet or something that causes a reflow)
        /// Update printer settings and Reflow.
        /// Because getting printer settings can take 4-5 seconds we do that and reflow on another thread
        ///// </summary>
        //internal void SheetSettingsChanged() {
        //    LogService.TraceMessage();

        //    Task.Run(async () => {

        //    });
        //}

        private void MainWindow_FormClosing(object sender, FormClosingEventArgs e) {
            if (ModelLocator.Current.Settings is null) {
                return;
            }

            ServiceLocator.Current.UpdateService.GotLatestVersion -= UpdateService_GotLatestVersion;
            ServiceLocator.Current.UpdateService.DownloadComplete -= UpdateService_DownloadComplete; ;

            // Save Window state
            if (WindowState == System.Windows.Forms.FormWindowState.Normal) {
                ModelLocator.Current.Settings.Size = new Core.Models.WindowSize(Size.Width, Size.Height);
                ModelLocator.Current.Settings.Location = new Core.Models.WindowLocation(Location.X, Location.Y);
            }
            else {
                ModelLocator.Current.Settings.Size = new Core.Models.WindowSize(RestoreBounds.Width, RestoreBounds.Height);
                ModelLocator.Current.Settings.Location = new Core.Models.WindowLocation(RestoreBounds.X, RestoreBounds.Y);
            }
            ModelLocator.Current.Settings.WindowState = (Core.Models.FormWindowState)WindowState;

            ServiceLocator.Current.TelemetryService.TrackEvent("Form Closing",
                 properties: new Dictionary<string, string> {
                    {"windowState", ModelLocator.Current.Settings.WindowState.ToString() },
                    {"size", $"{ModelLocator.Current.Settings.Size.Width}x{ModelLocator.Current.Settings.Size.Height}" },
                    {"location", $"{ModelLocator.Current.Settings.Location.X}x{ModelLocator.Current.Settings.Location.Y}"},
                 });

            ServiceLocator.Current.SettingsService.SaveSettings(ModelLocator.Current.Settings);
        }

        private void MainWindow_Layout(object sender, LayoutEventArgs e) {
            // This event is raised once at startup with the AffectedControl
            // and AffectedProperty properties on the LayoutEventArgs as null. 
            // The event provides size preferences for that case.
            if ((e.AffectedControl != null) && (e.AffectedProperty != null)) {
                // Ensure that the affected property is the Bounds property
                // of the form.
                //if (e.AffectedProperty.ToString().Equals("Bounds", StringComparison.InvariantCultureIgnoreCase)) {
                //Core.Helpers.Logging.TraceMessage("MainWindow_Layout bounds changed");
                //}
            }
        }

        private void landscapeCheckbox_CheckedChanged(object sender, EventArgs e) {
            LogService.TraceMessage($"{landscapeCheckbox.Checked}");
            ModelLocator.Current.Settings.Sheets.GetValueOrDefault(
            ModelLocator.Current.Settings.DefaultSheet.ToString()).Landscape =
                printDoc.DefaultPageSettings.Landscape =
                landscapeCheckbox.Checked;

            ServiceLocator.Current.TelemetryService.TrackEvent("landscapeCheckbox_CheckedChanged",
                 properties: new Dictionary<string, string> {
                                        {"landscape", landscapeCheckbox.Checked.ToString() }
                 });
        }

        private void headerTextBox_TextChanged(object sender, EventArgs e) {
            ModelLocator.Current.Settings.Sheets.GetValueOrDefault(
                ModelLocator.Current.Settings.DefaultSheet.ToString()).Header.Text = headerTextBox.Text;

            ServiceLocator.Current.TelemetryService.TrackEvent("headerTextBox_TextChanged");
        }

        private void footerTextBox_TextChanged(object sender, EventArgs e) {
            ModelLocator.Current.Settings.Sheets.GetValueOrDefault(
                ModelLocator.Current.Settings.DefaultSheet.ToString()).Footer.Text = footerTextBox.Text;

            ServiceLocator.Current.TelemetryService.TrackEvent("footerTextBox_TextChanged");
        }

        private void printersCB_SelectedIndexChanged(object sender, EventArgs e) {
            if (printersCB.Enabled) {
                LogService.TraceMessage("printersCB_SelectedIndexChanged");
                printDoc.PrinterSettings.PrinterName = (string)printersCB.SelectedItem;

                ServiceLocator.Current.TelemetryService.TrackEvent("printersCB_SelectedIndexChanged",
                     properties: new Dictionary<string, string> {
                            {"printerName", printDoc.PrinterSettings.PrinterName }
                     });

                paperSizesCB.Items.Clear();
                foreach (PaperSize ps in printDoc.PrinterSettings.PaperSizes) {
                    paperSizesCB.Items.Add(ps.PaperName);
                }
                ServiceLocator.Current.TelemetryService.TrackEvent("printersCB_SelectedIndexChanged",
                     properties: new Dictionary<string, string> {
                            {"printerName", printDoc.PrinterSettings.PrinterName }
                     });

                paperSizesCB.Text = printDoc.DefaultPageSettings.PaperSize.PaperName;
            }
        }

        private void paperSizesCB_SelectedIndexChanged(object sender, EventArgs e) {
            if (printersCB.Enabled) {
                LogService.TraceMessage("paperSizesCB_SelectedIndexChanged");
                // Set the paper size based upon the selection in the combo box.
                if (paperSizesCB.SelectedIndex != -1) {
                    printDoc.DefaultPageSettings.PaperSize = printDoc.PrinterSettings.PaperSizes[paperSizesCB.SelectedIndex];
                    ServiceLocator.Current.TelemetryService.TrackEvent("paperSizesCB_SelectedIndexChanged",
                         properties: new Dictionary<string, string> {
                            {"paperName", printDoc.DefaultPageSettings.PaperSize.PaperName }
                         });

                }
                LoadFile();
            }
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Design", "CA1031:Do not catch general exception types", Justification = "<Pending>")]
        private async void printButton_Click(object sender, EventArgs args) {
            using var print = new Core.Print();
            // TODO: It's hokey that Landscape is the only printer setting that's treated specially
            // 
            print.PrintDocument.DefaultPageSettings.Landscape = landscapeCheckbox.Checked;
            print.SheetViewModel.SetSheet(ModelLocator.Current.Settings.Sheets.GetValueOrDefault(ModelLocator.Current.Settings.DefaultSheet.ToString()));

            try {
                ShowMessage("Preparing to print..");
                await print.SheetViewModel.LoadFileAsync(printPreview.SheetViewModel.File, ModelLocator.Current.Options.ContentType).ConfigureAwait(false);

                print.SetPrinter(printDoc.PrinterSettings.PrinterName);
                print.SetPaperSize(printDoc.DefaultPageSettings.PaperSize.PaperName);

            }
            catch (FileNotFoundException fnfe) {
                Log.Error(fnfe, "File Not Found");
                ShowMessage($"{fnfe.Message}");
                return;
            }
            catch (InvalidOperationException ioe) {
                ServiceLocator.Current.TelemetryService.TrackException(ioe, false);
                Log.Error(ioe, "Error Operation {file}", activeFile);
                ShowMessage($"{ioe.Message}");
                return;
            }
#pragma warning disable CA1031 // Do not catch general exception types
            catch (Exception e) {
#pragma warning restore CA1031 // Do not catch general exception types
                ServiceLocator.Current.TelemetryService.TrackException(e, false);
                Log.Error(e, "Exception {file}", activeFile);
                ShowMessage($"Exception: {e.Message}{Environment.NewLine}({activeFile})");
                return;
            }
            finally {
            }

            if (!int.TryParse(fromText.Text, out var from)) {
                from = 0;
            }
            // Ideally we'd get NumSheets from print.SheetSVM but that would cause a
            // un-needed Reflow. So use the printPreview VM.
            if (!int.TryParse(toText.Text, out var to)) {
                to = 0;// printPreview.SheetViewModel.NumSheets;
            }

            if (from > 0 && to > 0) {
                print.PrintDocument.PrinterSettings.PrintRange = PrintRange.SomePages;
                print.PrintDocument.PrinterSettings.FromPage = from;
                print.PrintDocument.PrinterSettings.ToPage = to;
            }

            if (ModelLocator.Current.Settings.ShowPrintDialog) {
                BeginInvoke((Action)(async () => {
                    using var printDialog = new PrintDialog {
                        AllowSomePages = true,
                        ShowHelp = true,
                        // printDialog.AllowSelection = true;

                        Document = print.PrintDocument
                    };
                    printDialog.PrinterSettings.PrintRange = print.PrintDocument.PrinterSettings.PrintRange;
                    printDialog.PrinterSettings.FromPage = print.PrintDocument.PrinterSettings.FromPage;
                    printDialog.PrinterSettings.ToPage = print.PrintDocument.PrinterSettings.ToPage;

                    //If the result is OK then print the document.
                    if (printDialog.ShowDialog() == DialogResult.OK) {
                        if (printDialog.PrinterSettings.PrintRange == PrintRange.SomePages) {
                            print.PrintDocument.PrinterSettings.PrintRange = printDialog.PrinterSettings.PrintRange;
                            print.PrintDocument.PrinterSettings.FromPage = printDialog.PrinterSettings.FromPage;
                            print.PrintDocument.PrinterSettings.ToPage = printDialog.PrinterSettings.ToPage;
                        }
                        ShowMessage($"Printing to {printDoc.PrinterSettings.PrinterName}");
                        // BUGBUG: having the printpreview invalidate while we're printing causes an exception
                        // in litehtml. Do not un comment this until figured out
                        //print.PrintingSheet += Print_PrintingSheet;
                        await print.DoPrint().ConfigureAwait(false);
                        ShowMessage($"");
                    }
                }));
            }
            else {
                ShowMessage($"Printing to {printDoc.PrinterSettings.PrinterName}");

                // BUGBUG: having the printpreview invalidate while we're printing causes an exception
                // in litehtml. Do not un comment this until figured out
                //print.PrintingSheet += Print_PrintingSheet;
                await print.DoPrint().ConfigureAwait(false);
                ShowMessage("");
            }
        }

        private void Print_PrintingSheet(object sender, int sheetNum) {
            ShowMessage($"Printing sheet {sheetNum}");
        }

        private void panelRight_Resize(object sender, EventArgs e) {
            //SizePreview();
        }

        private void enableHeader_CheckedChanged(object sender, EventArgs e) {
            LogService.TraceMessage($"enableHeader_CheckedChanged: {enableHeader.Checked}");
            if (printersCB.Enabled) {
                // TODO: This should find the Preview SheetViewModel instance and set the property on this, not
                // the model
                ModelLocator.Current.Settings.Sheets.GetValueOrDefault(ModelLocator.Current.Settings.DefaultSheet.ToString()).Header.Enabled =
                    enableHeader.Checked;
            }
        }

        private void enableFooter_CheckedChanged(object sender, EventArgs e) {
            LogService.TraceMessage($"enableFooter_CheckedChanged: {enableFooter.Checked}");
            if (printersCB.Enabled) {
                // TODO: This should find the Preview SheetViewModel instance and set the property on this, not
                // the model
                ModelLocator.Current.Settings.Sheets.GetValueOrDefault(ModelLocator.Current.Settings.DefaultSheet.ToString()).Footer.Enabled =
                    enableFooter.Checked;
            }
        }

        private void comboBoxSheet_SelectedIndexChanged(object sender, EventArgs e) {
            var si = (KeyValuePair<string, string>)comboBoxSheet.SelectedItem;
            LogService.TraceMessage($"comboBoxSheet_SelectedIndexChanged: {si.Key}, {si.Value}");
            if (printersCB.Enabled) {
                ModelLocator.Current.Settings.DefaultSheet = Guid.Parse(si.Key);
                ServiceLocator.Current.TelemetryService.TrackEvent("Change Selected Sheet Settings",
                    properties: new Dictionary<string, string> {
                                    {"sheetSettingsName", si.Value },
                                    {"sheetSettingsId", si.Key },
                    });
                //ChangeSheet(ModelLocator.Current.Settings.Sheets[si.Key]);
            }
        }

        private void topMargin_ValueChanged(object sender, EventArgs e) {
            var margins = (Margins)ModelLocator.Current.Settings.Sheets.GetValueOrDefault(ModelLocator.Current.Settings.DefaultSheet.ToString()).Margins.Clone();
            margins.Top = (int)(topMargin.Value * 100M);
            ModelLocator.Current.Settings.Sheets.GetValueOrDefault(ModelLocator.Current.Settings.DefaultSheet.ToString()).Margins = margins;
        }

        private void leftMargin_ValueChanged(object sender, EventArgs e) {
            var margins = (Margins)ModelLocator.Current.Settings.Sheets.GetValueOrDefault(ModelLocator.Current.Settings.DefaultSheet.ToString()).Margins.Clone();
            margins.Left = (int)(leftMargin.Value * 100M);
            ModelLocator.Current.Settings.Sheets.GetValueOrDefault(ModelLocator.Current.Settings.DefaultSheet.ToString()).Margins = margins;
        }

        private void rightMargin_ValueChanged(object sender, EventArgs e) {
            var margins = (Margins)ModelLocator.Current.Settings.Sheets.GetValueOrDefault(ModelLocator.Current.Settings.DefaultSheet.ToString()).Margins.Clone();
            margins.Right = (int)(rightMargin.Value * 100M);
            ModelLocator.Current.Settings.Sheets.GetValueOrDefault(ModelLocator.Current.Settings.DefaultSheet.ToString()).Margins = margins;
        }

        private void bottomMargin_ValueChanged(object sender, EventArgs e) {
            var margins = (Margins)ModelLocator.Current.Settings.Sheets.GetValueOrDefault(ModelLocator.Current.Settings.DefaultSheet.ToString()).Margins.Clone();
            margins.Bottom = (int)(bottomMargin.Value * 100M);
            ModelLocator.Current.Settings.Sheets.GetValueOrDefault(ModelLocator.Current.Settings.DefaultSheet.ToString()).Margins = margins;
        }

        private void pageSeparator_CheckedChanged(object sender, EventArgs e) {
            ModelLocator.Current.Settings.Sheets.GetValueOrDefault(ModelLocator.Current.Settings.DefaultSheet.ToString()).PageSeparator = pageSeparator.Checked;
        }

        private void rows_ValueChanged(object sender, EventArgs e) {
            ModelLocator.Current.Settings.Sheets.GetValueOrDefault(ModelLocator.Current.Settings.DefaultSheet.ToString()).Rows = (int)rows.Value;
        }

        private void columns_ValueChanged(object sender, EventArgs e) {
            ModelLocator.Current.Settings.Sheets.GetValueOrDefault(ModelLocator.Current.Settings.DefaultSheet.ToString()).Columns = (int)columns.Value;

        }

        private void padding_ValueChanged(object sender, EventArgs e) {
            ModelLocator.Current.Settings.Sheets.GetValueOrDefault(ModelLocator.Current.Settings.DefaultSheet.ToString()).Padding = (int)(padding.Value * 100M);
        }

        private void fileButton_Click(object sender, EventArgs e) {
            ShowFilesDialog();
        }

        private void lineNumbers_CheckedChanged(object sender, EventArgs e) {
            LogService.TraceMessage($"lineNumbers_CheckedChanged: {lineNumbers.Checked}");
            ModelLocator.Current.Settings.Sheets.GetValueOrDefault(ModelLocator.Current.Settings.DefaultSheet.ToString()).ContentSettings.LineNumbers =
                    lineNumbers.Checked;
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Design",
            "CA1031:Do not catch general exception types", Justification = "<Pending>")]
        private void settingsButton_Click(object sender, EventArgs args) {
            Log.Debug($"Opening settings file: {ServiceLocator.Current.SettingsService.SettingsFileName}");

            ServiceLocator.Current.TelemetryService.TrackEvent("Settings Button Click");

            Process proc = null;
            try {
                var psi = new ProcessStartInfo {
                    UseShellExecute = true,   // This is important
                    FileName = ServiceLocator.Current.SettingsService.SettingsFileName
                };
                proc = Process.Start(psi);
            }
            catch (Exception e) {
                // TODO: Better error message (output of stderr?)
                ServiceLocator.Current.TelemetryService.TrackException(e, false);

                Log.Error(e, $"Couldn't open settings file {ServiceLocator.Current.SettingsService.SettingsFileName}.");
            }
            finally {
                proc?.Dispose();
            }
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Design",
            "CA1031:Do not catch general exception types", Justification = "<Pending>")]
        private void helpaboutLink_LinkClicked(object sender, LinkLabelLinkClickedEventArgs args) {
            var url = "https://tig.github.io/winprint";
            Log.Debug($"Browsing to home page: {url}");

            ServiceLocator.Current.TelemetryService.TrackEvent("Help/About Link Click");

            Process proc = null;
            try {
                var psi = new ProcessStartInfo {
                    UseShellExecute = true,   // This is important
                    FileName = url
                };
                proc = Process.Start(psi);
            }
            catch (Exception e) {
                // TODO: Better error message (output of stderr?)
                ServiceLocator.Current.TelemetryService.TrackException(e, false);

                Log.Error(e, $"Couldn't browse to {url}.");
            }
            finally {
                proc?.Dispose();
            }
        }

        private void contentFontLink_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e) {
            var contentSettings = ModelLocator.Current.Settings.Sheets.GetValueOrDefault(ModelLocator.Current.Settings.DefaultSheet.ToString()).ContentSettings;

            fontDialog.Font = new System.Drawing.Font(contentSettings.Font.Family, contentSettings.Font.Size, contentSettings.Font.Style, GraphicsUnit.Point);
            if (DialogResult.OK == fontDialog.ShowDialog(this)) {
                contentSettings.Font = new Core.Models.Font() { Family = fontDialog.Font.FontFamily.Name, Size = (float)Math.Round(fontDialog.Font.SizeInPoints), Style = fontDialog.Font.Style };
                contentFontLink.Text = contentSettings.Font.ToString();
            }
        }

        private void headerFooterFontLink_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e) {
            var header = ModelLocator.Current.Settings.Sheets.GetValueOrDefault(ModelLocator.Current.Settings.DefaultSheet.ToString()).Header;
            var footer = ModelLocator.Current.Settings.Sheets.GetValueOrDefault(ModelLocator.Current.Settings.DefaultSheet.ToString()).Footer;

            fontDialog.Font = new System.Drawing.Font(header.Font.Family, header.Font.Size, GraphicsUnit.Point);

            if (DialogResult.OK == fontDialog.ShowDialog(this)) {
                // Note we set BOTH the header & footer to the same thing. But for GUI we use the Header as source of truth.
                header.Font = footer.Font = new Core.Models.Font() { Family = fontDialog.Font.FontFamily.Name, Size = (float)Math.Round(fontDialog.Font.SizeInPoints), Style = fontDialog.Font.Style };
                headerFooterFontLink.Text = header.Font.ToString();
            }
        }

        private void printPreview_Click(object sender, EventArgs e) {
            if (string.IsNullOrEmpty(activeFile)) {
                ShowFilesDialog();
            }
        }
    }
}

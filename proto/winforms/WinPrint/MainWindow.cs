﻿using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Printing;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;
using WinPrint.Core;
using WinPrint.Core.Models;
using WinPrint.Core.Services;
using WinPrint.WinForms;

namespace WinPrint {
    public partial class MainWindow : Form {

        // The Windows printer document
        private PrintDocument printDoc = new PrintDocument();

        // Winprint Print Preview control
        private PrintPreview printPreview;

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Globalization", "CA1303:Do not pass literals as localized parameters", Justification = "<Pending>")]
        public MainWindow() {
            InitializeComponent();

            Icon = Resources.printer_and_fax_w;

            printPreview = new PrintPreview();
            printPreview.Dock = this.dummyButton.Dock;
            printPreview.Anchor = this.dummyButton.Anchor;
            printPreview.BackColor = this.dummyButton.BackColor;
            printPreview.Location = this.dummyButton.Location;
            printPreview.Margin = this.dummyButton.Margin;
            printPreview.Name = "printPreview";
            printPreview.Size = this.dummyButton.Size;
            printPreview.MinimumSize = new Size(0, 0);
            printPreview.TabIndex = 1;
            printPreview.TabStop = true;

            printPreview.KeyUp += (s, e) => {
                switch (e.KeyCode) {
                    case Keys.F5:
                        printPreview.Invalidate(true);
                        Core.Helpers.Logging.TraceMessage("-------- F5 ---------");
                        Task.Run(() =>
                            printPreview.SheetViewModel.LoadAsync(printPreview.SheetViewModel.File).ConfigureAwait(false));
                        break;
                }
            };


            if (LicenseManager.UsageMode != LicenseUsageMode.Designtime) {
                this.panelRight.Controls.Remove(this.dummyButton);
                this.panelRight.Controls.Add(this.printPreview);
                printersCB.Enabled = false;
                paperSizesCB.Enabled = false;
            }
        }

        // Flag: Has Dispose already been called?
        bool disposed = false;
        // Protected implementation of Dispose pattern.
        /// <summary>
        ///  Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose(bool disposing) {
            if (disposed)
                return;

            if (disposing && (components != null)) {
                components.Dispose();
                //if (streamToPrint != null) streamToPrint.Dispose();
                if (printDoc != null) printDoc.Dispose();
                if (printPreview != null) printPreview.Dispose();
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
            Core.Helpers.Logging.TraceMessage();
            if (printPreview.SheetViewModel != null) {
                Core.Helpers.Logging.TraceMessage("  SetupSheetViewModelNotifications was alreadya called");
                return;
            }

            printPreview.SheetViewModel = new SheetViewModel();
            printPreview.SheetViewModel.PropertyChanged += PropertyChangedEventHandler;
            printPreview.SheetViewModel.SettingsChanged += SettingsChangedEventHandler;
        }

        private void PropertyChangedEventHandler(object o, PropertyChangedEventArgs e) {
            if (InvokeRequired)
                BeginInvoke((Action)(() => PropertyChangedEventHandler(o, e)));
            else {
                Core.Helpers.Logging.TraceMessage($"SheetViewModel.PropertyChanged: {e.PropertyName}");
                switch (e.PropertyName) {
                    case "Landscape":
                        Core.Helpers.Logging.TraceMessage($"  Checking checkbox: {ModelLocator.Current.Settings.Sheets.GetValueOrDefault(ModelLocator.Current.Settings.DefaultSheet.ToString()).Landscape}");
                        landscapeCheckbox.Checked = printPreview.SheetViewModel.Landscape;
                        break;

                    case "Header":
                        headerTextBox.Text = printPreview.SheetViewModel.Header.Text;
                        break;

                    case "Footer":
                        footerTextBox.Text = printPreview.SheetViewModel.Footer.Text;
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
                        this.Text = $"WinPrint - {printPreview.SheetViewModel.File}";
                        printPreview.CurrentSheet = 1;
                        break;

                    case "Content":
                        printPreview.CurrentSheet = 1;
                        SheetSettingsChanged();
                        break;

                    case "Loading":
                        printPreview.Text = printPreview.SheetViewModel.Loading ? "Loading..." : "";
                        printPreview.Invalidate(false);
                        printPreview.Select();
                        printPreview.Focus();
                        break;

                    case "Reflowing":
                        printPreview.Text = printPreview.SheetViewModel.Reflowing ? "Rendering..." : "";
                        printPreview.Invalidate(false);
                        break;
                }
            }
        }

        private void SettingsChangedEventHandler(object o, bool reflow) {
            if (InvokeRequired)
                BeginInvoke((Action)(() => SettingsChangedEventHandler(o, reflow)));
            else { 
                Core.Helpers.Logging.TraceMessage($"SheetViewModel.SettingsChanged: {reflow}");
                if (reflow)
                    SheetSettingsChanged();
                else
                    printPreview.Invalidate(false);
            }
        }

        private async void MainWindow_Load(object sender, EventArgs e) {
            Core.Helpers.Logging.TraceMessage();

            this.Cursor = Cursors.WaitCursor;
            // Load settings
            Core.Helpers.Logging.TraceMessage("First reference to ModelLocator.Current.Settings");
            var sheets = ModelLocator.Current.Settings.Sheets;

            // Load file assocations
            var languages = ModelLocator.Current.Associations;
            Core.Helpers.Logging.TraceMessage($"{languages.Languages.Count} languages, {languages.FilesAssociations.Count} file assocations");

            ModelLocator.Current.Settings.PropertyChanged += (s, e) => BeginInvoke((Action)(() => {
                Core.Helpers.Logging.TraceMessage($"Settings.PropertyChanged: {e.PropertyName}");
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
                //printersCB.Text = "OneNote";
                if (printDoc.PrinterSettings.IsDefaultPrinter)
                    printersCB.Text = printDoc.PrinterSettings.PrinterName;
            }
            printDoc.PrinterSettings.PrinterName = (string)printersCB.SelectedItem;
            foreach (PaperSize ps in printDoc.PrinterSettings.PaperSizes) {
                paperSizesCB.Items.Add(ps);
            }
            paperSizesCB.Text = printDoc.DefaultPageSettings.PaperSize.ToString();

            // We kept these disabled during load
            printersCB.Enabled = true;
            paperSizesCB.Enabled = true;


            // Create sheet view model & wire up notifications
            SetupSheetViewModelNotifications();

            // Select default Sheet 
            var newSheet = ModelLocator.Current.Settings.Sheets.GetValueOrDefault(ModelLocator.Current.Settings.DefaultSheet.ToString());
            comboBoxSheet.Text = newSheet.Name;
            // This will cause a flurry of property change notifications, setting all UI elements
            printPreview.SheetViewModel.SetSheet(newSheet);

            // Batch print
            if (ModelLocator.Current.Options.Files != null)
            //&&
            //  ModelLocator.Current.Options.Files.Any() &&
            //  !string.IsNullOrEmpty(ModelLocator.Current.Options.Files.ToList<string>()[0])) 
            {
                await printPreview.SheetViewModel.LoadAsync(ModelLocator.Current.Options.Files.ToList()[0]).ConfigureAwait(false); ;
            }

            // Even if a file's not been set, SheetSettingsChanged to Reflow in order ot juice the print preview
            SheetSettingsChanged();

            this.Size = new Size(ModelLocator.Current.Settings.Size.Width, ModelLocator.Current.Settings.Size.Height);
            this.Location = new Point(ModelLocator.Current.Settings.Location.X, ModelLocator.Current.Settings.Location.Y);
            this.WindowState = (System.Windows.Forms.FormWindowState)ModelLocator.Current.Settings.WindowState;

            printPreview.Select();
            printPreview.Focus();

            this.Cursor = Cursors.Default;
            if (string.IsNullOrEmpty(printPreview.SheetViewModel.File))
                ShowFilesDialog();

            // Go!
        }

        private async void ShowFilesDialog() {
            Core.Helpers.Logging.TraceMessage();
            using (OpenFileDialog openFileDialog = new OpenFileDialog()) {
                openFileDialog.InitialDirectory = $"{Environment.GetFolderPath(Environment.SpecialFolder.UserProfile)}\\source\\winprint\\tests";
                openFileDialog.Filter = $"code files (*.c*)|*.c*|txt files (*.txt)|*.txt|All files (*.*)|*.*";
                openFileDialog.FilterIndex = 3;
                openFileDialog.RestoreDirectory = true;
                if (openFileDialog.ShowDialog() == DialogResult.OK) {
                    await Task.Run(() =>
                    printPreview.SheetViewModel.LoadAsync(openFileDialog.FileNames.ToList()[0])).ConfigureAwait(false);
                }
            }
            Core.Helpers.Logging.TraceMessage("exting ShowFilesDialog()");
        }

        private void SheetChanged() {
            Core.Helpers.Logging.TraceMessage();
            var newSheet = ModelLocator.Current.Settings.Sheets.GetValueOrDefault(ModelLocator.Current.Settings.DefaultSheet.ToString());
            comboBoxSheet.Text = newSheet.Name;
            printPreview.SheetViewModel.SetSheet(newSheet);
            SheetSettingsChanged();
        }

        private void MainWindow_FormClosing(object sender, FormClosingEventArgs e) {
            // Save Window state
            if (this.WindowState == System.Windows.Forms.FormWindowState.Normal) {
                ModelLocator.Current.Settings.Size = new Core.Models.WindowSize(this.Size.Width, this.Size.Height);
                ModelLocator.Current.Settings.Location = new Core.Models.WindowLocation(this.Location.X, this.Location.Y);
            }
            else {
                ModelLocator.Current.Settings.Size = new Core.Models.WindowSize(this.RestoreBounds.Width, this.RestoreBounds.Height);
                ModelLocator.Current.Settings.Location = new Core.Models.WindowLocation(this.RestoreBounds.X, this.RestoreBounds.Y);
            }
            ModelLocator.Current.Settings.WindowState = (Core.Models.FormWindowState)this.WindowState;
            ServiceLocator.Current.SettingsService.SaveSettings(ModelLocator.Current.Settings);
        }

        internal async void SheetSettingsChanged() {
            Core.Helpers.Logging.TraceMessage();

            // Set landscape. This causes other DefaultPageSettings to change
            printDoc.DefaultPageSettings.Landscape = printPreview.SheetViewModel.Landscape;
            BeginInvoke((Action)(() => printPreview.Invalidate(true)));
            await Task.Run(() => printPreview.SheetViewModel.ReflowAsync(printDoc.DefaultPageSettings)).ConfigureAwait(false);
            BeginInvoke((Action)(() => printPreview.Invalidate(true)));
        }

        private void MainWindow_Layout(object sender, LayoutEventArgs e) {
            // This event is raised once at startup with the AffectedControl
            // and AffectedProperty properties on the LayoutEventArgs as null. 
            // The event provides size preferences for that case.
            if ((e.AffectedControl != null) && (e.AffectedProperty != null)) {
                // Ensure that the affected property is the Bounds property
                // of the form.
                if (e.AffectedProperty.ToString().Equals("Bounds", StringComparison.InvariantCultureIgnoreCase)) {
                    //Core.Helpers.Logging.TraceMessage("MainWindow_Layout bounds changed");
                }
            }
        }

        private void landscapeCheckbox_CheckedChanged(object sender, EventArgs e) {
            Core.Helpers.Logging.TraceMessage($"{landscapeCheckbox.Checked}");
            ModelLocator.Current.Settings.Sheets.GetValueOrDefault(
            ModelLocator.Current.Settings.DefaultSheet.ToString()).Landscape =
                printDoc.DefaultPageSettings.Landscape =
                landscapeCheckbox.Checked;
        }

        private void headerTextBox_TextChanged(object sender, EventArgs e) {
            ModelLocator.Current.Settings.Sheets.GetValueOrDefault(
                ModelLocator.Current.Settings.DefaultSheet.ToString()).Header.Text = headerTextBox.Text;
        }

        private void footerTextBox_TextChanged(object sender, EventArgs e) {
            ModelLocator.Current.Settings.Sheets.GetValueOrDefault(
                ModelLocator.Current.Settings.DefaultSheet.ToString()).Footer.Text = footerTextBox.Text;
        }

        private void printersCB_SelectedIndexChanged(object sender, EventArgs e) {
            if (printersCB.Enabled) {
                Core.Helpers.Logging.TraceMessage("printersCB_SelectedIndexChanged");
                printDoc.PrinterSettings.PrinterName = (string)printersCB.SelectedItem;
                paperSizesCB.Items.Clear();
                foreach (PaperSize ps in printDoc.PrinterSettings.PaperSizes) {
                    paperSizesCB.Items.Add(ps);
                }
                paperSizesCB.Text = printDoc.DefaultPageSettings.PaperSize.ToString();
            }
        }

        private void paperSizesCB_SelectedIndexChanged(object sender, EventArgs e) {
            if (printersCB.Enabled) {
                Core.Helpers.Logging.TraceMessage("paperSizesCB_SelectedIndexChanged");
                // Set the paper size based upon the selection in the combo box.
                if (paperSizesCB.SelectedIndex != -1) {
                    printDoc.DefaultPageSettings.PaperSize = printDoc.PrinterSettings.PaperSizes[paperSizesCB.SelectedIndex];
                }
                SheetSettingsChanged();
            }
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Design", "CA1031:Do not catch general exception types", Justification = "<Pending>")]
        private async void printButton_Click(object sender, EventArgs e) {
            using var print = new Core.Print();
            // TODO: It's hokey that Landscape is the only printer setting that's treated specially
            // 
            print.PrintDocument.DefaultPageSettings.Landscape = landscapeCheckbox.Checked;
            print.SheetVM.SetSheet(ModelLocator.Current.Settings.Sheets.GetValueOrDefault(ModelLocator.Current.Settings.DefaultSheet.ToString()));

            await print.SheetVM.LoadAsync(printPreview.SheetViewModel.File).ConfigureAwait(false);

            print.SetPrinter(printDoc.PrinterSettings.PrinterName);
            print.SetPaperSize(printDoc.DefaultPageSettings.PaperSize.PaperName);

            // TODO: Decide how to make showing the print dialog a setting (or if needed at all)
            // the only reason I can think of now is from/to page support.
            bool showPrintDialog = true;
            if (showPrintDialog) {
                using PrintDialog printDialog = new PrintDialog();
                printDialog.AllowSomePages = true;
                printDialog.ShowHelp = true;
                // printDialog.AllowSelection = true;

                printDialog.Document = print.PrintDocument;
                printDialog.PrinterSettings.FromPage = 1;
                // Ideally we'd get NumSheets from print.SheetSVM but that would cause a
                // un-needed Reflow. So use the printPreview VM.
                printDialog.PrinterSettings.ToPage = printPreview.SheetViewModel.NumSheets;
                //If the result is OK then print the document.
                if (printDialog.ShowDialog() == DialogResult.OK && printDialog.PrinterSettings.PrintRange == PrintRange.SomePages) {
                    print.PrintDocument.PrinterSettings.PrintRange = printDialog.PrinterSettings.PrintRange;
                    print.PrintDocument.PrinterSettings.FromPage = printDialog.PrinterSettings.FromPage;
                    print.PrintDocument.PrinterSettings.ToPage = printDialog.PrinterSettings.ToPage;
                }
            }
            print.DoPrint();
        }

        private void panelRight_Resize(object sender, EventArgs e) {
            //SizePreview();
        }

        private void enableHeader_CheckedChanged(object sender, EventArgs e) {
            Core.Helpers.Logging.TraceMessage($"enableHeader_CheckedChanged: {enableHeader.Checked}");
            if (printersCB.Enabled) {
                // TODO: This should find the Preview SheetViewModel instance and set the property on this, not
                // the model
                ModelLocator.Current.Settings.Sheets.GetValueOrDefault(ModelLocator.Current.Settings.DefaultSheet.ToString()).Header.Enabled =
                    enableHeader.Checked;
            }
        }

        private void enableFooter_CheckedChanged(object sender, EventArgs e) {
            Core.Helpers.Logging.TraceMessage($"enableFooter_CheckedChanged: {enableFooter.Checked}");
            if (printersCB.Enabled) {
                // TODO: This should find the Preview SheetViewModel instance and set the property on this, not
                // the model
                ModelLocator.Current.Settings.Sheets.GetValueOrDefault(ModelLocator.Current.Settings.DefaultSheet.ToString()).Footer.Enabled =
                    enableFooter.Checked;
            }
        }

        private void comboBoxSheet_SelectedIndexChanged(object sender, EventArgs e) {
            KeyValuePair<string, string> si = (KeyValuePair<string, string>)comboBoxSheet.SelectedItem;
            Core.Helpers.Logging.TraceMessage($"comboBoxSheet_SelectedIndexChanged: {si.Key}, {si.Value}");
            if (printersCB.Enabled) {
                ModelLocator.Current.Settings.DefaultSheet = Guid.Parse(si.Key);
                //ChangeSheet(ModelLocator.Current.Settings.Sheets[si.Key]);
            }
        }

        private void topMargin_ValueChanged(object sender, EventArgs e) {
            Margins margins = (Margins)ModelLocator.Current.Settings.Sheets.GetValueOrDefault(ModelLocator.Current.Settings.DefaultSheet.ToString()).Margins.Clone();
            margins.Top = (int)(topMargin.Value * 100M);
            ModelLocator.Current.Settings.Sheets.GetValueOrDefault(ModelLocator.Current.Settings.DefaultSheet.ToString()).Margins = margins;
        }

        private void leftMargin_ValueChanged(object sender, EventArgs e) {
            Margins margins = (Margins)ModelLocator.Current.Settings.Sheets.GetValueOrDefault(ModelLocator.Current.Settings.DefaultSheet.ToString()).Margins.Clone();
            margins.Left = (int)(leftMargin.Value * 100M);
            ModelLocator.Current.Settings.Sheets.GetValueOrDefault(ModelLocator.Current.Settings.DefaultSheet.ToString()).Margins = margins;
        }

        private void rightMargin_ValueChanged(object sender, EventArgs e) {
            Margins margins = (Margins)ModelLocator.Current.Settings.Sheets.GetValueOrDefault(ModelLocator.Current.Settings.DefaultSheet.ToString()).Margins.Clone();
            margins.Right = (int)(rightMargin.Value * 100M);
            ModelLocator.Current.Settings.Sheets.GetValueOrDefault(ModelLocator.Current.Settings.DefaultSheet.ToString()).Margins = margins;
        }

        private void bottomMargin_ValueChanged(object sender, EventArgs e) {
            Margins margins = (Margins)ModelLocator.Current.Settings.Sheets.GetValueOrDefault(ModelLocator.Current.Settings.DefaultSheet.ToString()).Margins.Clone();
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
    }
}

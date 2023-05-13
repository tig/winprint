﻿using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using WinPrint.Core.Services;

// This is a very long comment to test wrapping of comments in source code. It goes on and on...
using test; // This is a long comment that extends way to the right in order to test wrapping

// private string file = "..\\..\\..\\..\\..\\..\\tests\\formfeeds.txt";
// private string file = "..\\..\\..\\..\\..\\..\\tests\\TEST.TXT";
    // HAS TAB BEFORE private string file = "..\\..\\..\\..\\..\\..\\tests\\long html doc as text.TXT";

namespace WinPrint {
    public partial class MainWindow : Form {

        // The Windows printer document
        private PrintDocument printDoc = new PrintDocument();

        // Winprint Print Preview control
        private PrintPreview printPreview;

        //private string file = "..\\..\\..\\..\\..\\..\\tests\\formfeeds.txt";
        //private string file = "..\\..\\..\\..\\..\\..\\tests\\TEST.TXT";
        //private string file = "..\\..\\..\\..\\..\\..\\tests\\long html doc as text.TXT";
        // This is a very long comment to test wrapping of comments in source code. It goes on and on...
        private string file = ""; // this is a long comment that extends way to the right in order to test wrapping

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Globalization", "CA1303:Do not pass literals as localized parameters", Justification = "<Pending>")]
        public MainWindow() {
            InitializeComponent();

            Icon = Resources.printer_and_fax_w;

            printPreview = new PrintPreview();
            printPreview.Anchor = this.dummyButton.Anchor;
            printPreview.BackColor = this.dummyButton.BackColor;
            printPreview.Location = this.dummyButton.Location;
            printPreview.Margin = this.dummyButton.Margin;
            printPreview.Name = "printPreview";
            printPreview.Size = this.dummyButton.Size;
            printPreview.TabIndex = 1;// this.dummyButton.TabIndex;
            printPreview.TabStop = true;


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

        private SheetViewModel CreatePreviewSheetViewModel() {
            Debug.WriteLine("CreateSheetViewModel");
            SheetViewModel svm = new SheetViewModel();

            svm.SetSettings(ModelLocator.Current.Settings.Sheets.GetValueOrDefault(ModelLocator.Current.Settings.DefaultSheet.ToString()));

            landscapeCheckbox.Checked = svm.Landscape;
            enableHeader.Checked = svm.Header.Enabled;
            headerTextBox.Text = svm.Header.Text;
            enableFooter.Checked = svm.Footer.Enabled;
            footerTextBox.Text = svm.Footer.Text;

            topMargin.Value = svm.Margins.Top / 100M;
            leftMargin.Value = svm.Margins.Left / 100M;
            rightMargin.Value = svm.Margins.Right / 100M;
            bottomMargin.Value = svm.Margins.Bottom / 100M;

            pageSeparator.Checked = svm.PageSepartor;
            rows.Value = svm.Rows;
            columns.Value = svm.Columns;
            padding.Value = svm.Padding / 100M;

            svm.PropertyChanged += (s, e) => BeginInvoke((Action)(() => {
                Debug.WriteLine($"SheetViewModel.PropertyChanged: {e.PropertyName}");
                switch (e.PropertyName) {
                    case "Landscape":
                        Debug.WriteLine($"  Checking checkbox: {ModelLocator.Current.Settings.Sheets.GetValueOrDefault(ModelLocator.Current.Settings.DefaultSheet.ToString()).Landscape}");
                        landscapeCheckbox.Checked = svm.Landscape;
                        break;

                    case "Header":
                        headerTextBox.Text = svm.Header.Text;
                        break;

                    case "Footer":
                        footerTextBox.Text = svm.Footer.Text;
                        break;

                    case "Margins":
                        topMargin.Value = svm.Margins.Top / 100M;
                        leftMargin.Value = svm.Margins.Left / 100M;
                        rightMargin.Value = svm.Margins.Right / 100M;
                        bottomMargin.Value = svm.Margins.Bottom / 100M;

                        // Keep PrintDocument updated for WinForms.PrintPreview
                        printDoc.PrinterSettings.DefaultPageSettings.Margins = (Margins)svm.Margins.Clone();
                        break;

                    case "PageSeparator":
                        pageSeparator.Checked = svm.PageSepartor;
                        break;

                    case "Rows":
                        rows.Value = svm.Rows;
                        break;

                    case "Columns":
                        columns.Value = svm.Columns;
                        break;

                    case "Padding":
                        padding.Value = svm.Padding / 100M;
                        break;
                }
            }));

            svm.SettingsChanged += (s, reflow) => BeginInvoke((Action)(() => {
                Debug.WriteLine($"SheetViewModel.SettingsChanged: {reflow}");
                if (reflow)
                    SheetSettingsChanged();
                else
                    printPreview.Invalidate(false);
            }));

            printPreview.SheetViewModel = svm;

            // TODO: Batch Print
            if (ModelLocator.Current.Options.Files != null &&
                ModelLocator.Current.Options.Files.Any() &&
                !string.IsNullOrEmpty(ModelLocator.Current.Options.Files.ToList()[0])) {
                List<string> list = ModelLocator.Current.Options.Files.ToList();
                file = list[0];
            }

            svm.File = file;
            this.Text = $"WinPrint - {file}";
            return svm;
        }

        private void landscapeCheckbox_CheckedChanged(object sender, EventArgs e) {
            Debug.WriteLine($"landscapeCheckbox_CheckedChanged: {landscapeCheckbox.Checked}");
            if (printersCB.Enabled) {
                // TODO: This should find the Preview SheetViewModel instance and set the property on this, not
                // the model
                ModelLocator.Current.Settings.Sheets.GetValueOrDefault(ModelLocator.Current.Settings.DefaultSheet.ToString()).Landscape =
                    printDoc.DefaultPageSettings.Landscape =
                    landscapeCheckbox.Checked;

                // We do NOT force settings reflow here; as it will come through with a SettingsChanged from viewmodel
                //SheetSettingsChanged();
            }
        }

        private void MainWindow_Load(object sender, EventArgs e) {
            this.Cursor = Cursors.WaitCursor;
            // Load settings
            Debug.WriteLine("First reference to ModelLocator.Current.Settings");
            var sheets = ModelLocator.Current.Settings.Sheets;

            ModelLocator.Current.Settings.PropertyChanged += (s, e) => BeginInvoke((Action)(() => {
                Debug.WriteLine($"Settings.PropertyChanged: {e.PropertyName}");
                switch (e.PropertyName) {
                    case "DefaultSheet":
                        comboBoxSheet.Text = ModelLocator.Current.Settings.Sheets.GetValueOrDefault(ModelLocator.Current.Settings.DefaultSheet.ToString()).Name;
                        ChangeSheet();
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

            // Select default Sheet 
            comboBoxSheet.Text = ModelLocator.Current.Settings.Sheets.GetValueOrDefault(ModelLocator.Current.Settings.DefaultSheet.ToString()).Name;

            if (ModelLocator.Current.Options.Files != null)
                //&&
                //  ModelLocator.Current.Options.Files.Any() &&
                //  !string.IsNullOrEmpty(ModelLocator.Current.Options.Files.ToList<string>()[0])) 
            {
                file = ModelLocator.Current.Options.Files.ToList()[0];
            }

            printPreview.Select();
            this.Size = new Size(ModelLocator.Current.Settings.Size.Width, ModelLocator.Current.Settings.Size.Height);
            this.Location = new Point(ModelLocator.Current.Settings.Location.X, ModelLocator.Current.Settings.Location.Y);
            this.WindowState = (System.Windows.Forms.FormWindowState)ModelLocator.Current.Settings.WindowState;

            this.Cursor = Cursors.Default;

            if (string.IsNullOrEmpty(file)) {
                using (OpenFileDialog openFileDialog = new OpenFileDialog()) {
                    
                    openFileDialog.InitialDirectory = $"{Environment.GetFolderPath(Environment.SpecialFolder.UserProfile)}\\source\\winprint\\tests";
                    openFileDialog.Filter = "code files (*.c*)|*.c*|txt files (*.txt)|*.txt|All files (*.*)|*.*";
                    openFileDialog.FilterIndex = 3;
                    openFileDialog.RestoreDirectory = true;
                    if (openFileDialog.ShowDialog() == DialogResult.OK) {
                        file = openFileDialog.FileNames.ToList<string>()[0];
                    }
                }
            }
            // Go!
            ChangeSheet();

        }

        private void ChangeSheet() {
            this.Cursor = Cursors.WaitCursor;
            printPreview.SheetViewModel = CreatePreviewSheetViewModel();
            this.Cursor = Cursors.Default;
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

        internal void SheetSettingsChanged() {
            Debug.WriteLine("SheetSettingsChanged()");

            this.Cursor = Cursors.WaitCursor;
            // Set landscape. This causes other DefaultPageSettings to change
            printDoc.DefaultPageSettings.Landscape = landscapeCheckbox.Checked;
            printPreview.SheetViewModel.Reflow(printDoc.DefaultPageSettings);
            printPreview.Invalidate(true);
            SizePreview();

            this.Cursor = Cursors.Default;
        }

        internal void SizePreview() {
            Debug.WriteLine("SizePreview()");
            if (printPreview == null || printPreview.SheetViewModel == null) return;
            Size size = panelRight.Size;
            size.Height -= headerTextBox.Height * 3;
            size.Width -= headerTextBox.Height;

            double w = printPreview.SheetViewModel.Bounds.Width;
            double h = printPreview.SheetViewModel.Bounds.Height;

            var scalingX = (double)size.Width / (double)w;
            var scalingY = (double)size.Height / (double)h;

            // Now, we have two scaling ratios, which one produces the smaller image? The one that has the smallest scaling factor.
            var scale = Math.Min(scalingY, scalingX);

            printPreview.Size = new Size((int)(w * scale), (int)(h * scale));

            // Now center
            printPreview.Location = new Point((panelRight.Width / 2) - (printPreview.Width / 2),
                (panelRight.Height / 2) - (printPreview.Height / 2));
        }

        private void MainWindow_Layout(object sender, LayoutEventArgs e) {
            // This event is raised once at startup with the AffectedControl
            // and AffectedProperty properties on the LayoutEventArgs as null. 
            // The event provides size preferences for that case.
            if ((e.AffectedControl != null) && (e.AffectedProperty != null)) {
                // Ensure that the affected property is the Bounds property
                // of the form.
                if (e.AffectedProperty.ToString().Equals("Bounds", StringComparison.InvariantCultureIgnoreCase)) {
                    Debug.WriteLine("MainWindow_Layout bounds changed");
                }
            }
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
                Debug.WriteLine("printersCB_SelectedIndexChanged");
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
                Debug.WriteLine("paperSizesCB_SelectedIndexChanged");
                // Set the paper size based upon the selection in the combo box.
                if (paperSizesCB.SelectedIndex != -1) {
                    printDoc.DefaultPageSettings.PaperSize = printDoc.PrinterSettings.PaperSizes[paperSizesCB.SelectedIndex];
                }
                SheetSettingsChanged();
            }
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Design", "CA1031:Do not catch general exception types", Justification = "<Pending>")]
        private void printButton_Click(object sender, EventArgs e) {
            using var print = new Core.Print();
            // TODO: It's hokey that Landscape is the only printer setting that's treated specially
            // 
            print.PrintDocument.DefaultPageSettings.Landscape = landscapeCheckbox.Checked;
            print.SheetVM.File = file;
            print.SheetVM.SetSettings(ModelLocator.Current.Settings.Sheets.GetValueOrDefault(ModelLocator.Current.Settings.DefaultSheet.ToString()));
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
            SizePreview();
        }

        private void enableHeader_CheckedChanged(object sender, EventArgs e) {
            Debug.WriteLine($"enableHeader_CheckedChanged: {enableHeader.Checked}");
            if (printersCB.Enabled) {
                // TODO: This should find the Preview SheetViewModel instance and set the property on this, not
                // the model
                ModelLocator.Current.Settings.Sheets.GetValueOrDefault(ModelLocator.Current.Settings.DefaultSheet.ToString()).Header.Enabled =
                    enableHeader.Checked;
            }
        }

        private void enableFooter_CheckedChanged(object sender, EventArgs e) {
            Debug.WriteLine($"enableFooter_CheckedChanged: {enableFooter.Checked}");
            if (printersCB.Enabled) {
                // TODO: This should find the Preview SheetViewModel instance and set the property on this, not
                // the model
                ModelLocator.Current.Settings.Sheets.GetValueOrDefault(ModelLocator.Current.Settings.DefaultSheet.ToString()).Footer.Enabled =
                    enableFooter.Checked;
            }
        }

        private void comboBoxSheet_SelectedIndexChanged(object sender, EventArgs e) {
            KeyValuePair<string, string> si = (KeyValuePair<string, string>)comboBoxSheet.SelectedItem;
            Debug.WriteLine($"comboBoxSheet_SelectedIndexChanged: {si.Key}, {si.Value}");
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
    }
}
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Printing;
using System.IO;
using System.Linq;
using System.Windows.Forms;
using WinPrint.Core;
using WinPrint.Core.Models;
using WinPrint.Core.Services;

namespace WinPrint {
    public partial class MainWindow : Form {

        // The Windows printer document
        private PrintDocument printDoc = new PrintDocument();

        // Winprint Print Preview control
        private PrintPreview printPreview;

        //private string file = "..\\..\\..\\..\\..\\..\\tests\\formfeeds.txt";
        //private string file = "..\\..\\..\\..\\..\\..\\tests\\TEST.TXT";
        //private string file = "..\\..\\..\\..\\..\\..\\tests\\long html doc as text.TXT";
        //private string file = @"C:\Users\ckindel\source\winprint\tests\test.html";
        private string file = ""; //@"..\..\..\..\..\..\proto\winforms\WinPrint\ViewModels\SheetViewModel.cs";

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Globalization", "CA1303:Do not pass literals as localized parameters", Justification = "<Pending>")]
        public MainWindow() {
            InitializeComponent();

            Icon = Resources.printer_and_fax_w;

            printPreview = new PrintPreview();
            printPreview.Anchor = this.dummyButton.Anchor;
            printPreview.BackColor = this.dummyButton.BackColor;
            printPreview.Location = this.dummyButton.Location;
            printPreview.Margin = this.dummyButton.Margin;
            printPreview.Name = "printPreview";
            printPreview.Size = this.dummyButton.Size;
            printPreview.TabIndex = 1;// this.dummyButton.TabIndex;
            printPreview.TabStop = true;


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

        private SheetViewModel CreatePreviewSheetViewModel() {
            Debug.WriteLine("CreateSheetViewModel");
            SheetViewModel svm = new SheetViewModel();

            svm.SetSettings(ModelLocator.Current.Settings.Sheets.GetValueOrDefault(ModelLocator.Current.Settings.DefaultSheet.ToString()));

            landscapeCheckbox.Checked = svm.Landscape;
            enableHeader.Checked = svm.Header.Enabled;
            headerTextBox.Text = svm.Header.Text;
            enableFooter.Checked = svm.Footer.Enabled;
            footerTextBox.Text = svm.Footer.Text;

            topMargin.Value = svm.Margins.Top / 100M;
            leftMargin.Value = svm.Margins.Left / 100M;
            rightMargin.Value = svm.Margins.Right / 100M;
            bottomMargin.Value = svm.Margins.Bottom / 100M;

            pageSeparator.Checked = svm.PageSepartor;
            rows.Value = svm.Rows;
            columns.Value = svm.Columns;
            padding.Value = svm.Padding / 100M;

            svm.PropertyChanged += (s, e) => BeginInvoke((Action)(() => {
                Debug.WriteLine($"SheetViewModel.PropertyChanged: {e.PropertyName}");
                switch (e.PropertyName) {
                    case "Landscape":
                        Debug.WriteLine($"  Checking checkbox: {ModelLocator.Current.Settings.Sheets.GetValueOrDefault(ModelLocator.Current.Settings.DefaultSheet.ToString()).Landscape}");
                        landscapeCheckbox.Checked = svm.Landscape;
                        break;

                    case "Header":
                        headerTextBox.Text = svm.Header.Text;
                        break;

                    case "Footer":
                        footerTextBox.Text = svm.Footer.Text;
                        break;

                    case "Margins":
                        topMargin.Value = svm.Margins.Top / 100M;
                        leftMargin.Value = svm.Margins.Left / 100M;
                        rightMargin.Value = svm.Margins.Right / 100M;
                        bottomMargin.Value = svm.Margins.Bottom / 100M;

                        // Keep PrintDocument updated for WinForms.PrintPreview
                        printDoc.PrinterSettings.DefaultPageSettings.Margins = (Margins)svm.Margins.Clone();
                        break;

                    case "PageSeparator":
                        pageSeparator.Checked = svm.PageSepartor;
                        break;

                    case "Rows":
                        rows.Value = svm.Rows;
                        break;

                    case "Columns":
                        columns.Value = svm.Columns;
                        break;

                    case "Padding":
                        padding.Value = svm.Padding / 100M;
                        break;
                }
            }));

            svm.SettingsChanged += (s, reflow) => BeginInvoke((Action)(() => {
                Debug.WriteLine($"SheetViewModel.SettingsChanged: {reflow}");
                if (reflow)
                    SheetSettingsChanged();
                else
                    printPreview.Invalidate(false);
            }));

            printPreview.SheetViewModel = svm;

            // TODO: Batch Print
            if (ModelLocator.Current.Options.Files != null &&
                ModelLocator.Current.Options.Files.Any() &&
                !string.IsNullOrEmpty(ModelLocator.Current.Options.Files.ToList()[0])) {
                List<string> list = ModelLocator.Current.Options.Files.ToList();
                file = list[0];
            }

            svm.File = file;
            this.Text = $"WinPrint - {file}";
            return svm;
        }

        private void landscapeCheckbox_CheckedChanged(object sender, EventArgs e) {
            Debug.WriteLine($"landscapeCheckbox_CheckedChanged: {landscapeCheckbox.Checked}");
            if (printersCB.Enabled) {
                // TODO: This should find the Preview SheetViewModel instance and set the property on this, not
                // the model
                ModelLocator.Current.Settings.Sheets.GetValueOrDefault(ModelLocator.Current.Settings.DefaultSheet.ToString()).Landscape =
                    printDoc.DefaultPageSettings.Landscape =
                    landscapeCheckbox.Checked;

                // We do NOT force settings reflow here; as it will come through with a SettingsChanged from viewmodel
                //SheetSettingsChanged();
            }
        }

        private void MainWindow_Load(object sender, EventArgs e) {
            this.Cursor = Cursors.WaitCursor;
            // Load settings
            Debug.WriteLine("First reference to ModelLocator.Current.Settings");
            var sheets = ModelLocator.Current.Settings.Sheets;

            ModelLocator.Current.Settings.PropertyChanged += (s, e) => BeginInvoke((Action)(() => {
                Debug.WriteLine($"Settings.PropertyChanged: {e.PropertyName}");
                switch (e.PropertyName) {
                    case "DefaultSheet":
                        comboBoxSheet.Text = ModelLocator.Current.Settings.Sheets.GetValueOrDefault(ModelLocator.Current.Settings.DefaultSheet.ToString()).Name;
                        ChangeSheet();
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

            // Select default Sheet 
            comboBoxSheet.Text = ModelLocator.Current.Settings.Sheets.GetValueOrDefault(ModelLocator.Current.Settings.DefaultSheet.ToString()).Name;

            if (ModelLocator.Current.Options.Files != null)
                //&&
                //  ModelLocator.Current.Options.Files.Any() &&
                //  !string.IsNullOrEmpty(ModelLocator.Current.Options.Files.ToList<string>()[0])) 
            {
                file = ModelLocator.Current.Options.Files.ToList()[0];
            }

            printPreview.Select();
            this.Size = new Size(ModelLocator.Current.Settings.Size.Width, ModelLocator.Current.Settings.Size.Height);
            this.Location = new Point(ModelLocator.Current.Settings.Location.X, ModelLocator.Current.Settings.Location.Y);
            this.WindowState = (System.Windows.Forms.FormWindowState)ModelLocator.Current.Settings.WindowState;

            this.Cursor = Cursors.Default;

            if (string.IsNullOrEmpty(file)) {
                using (OpenFileDialog openFileDialog = new OpenFileDialog()) {
                    
                    openFileDialog.InitialDirectory = $"{Environment.GetFolderPath(Environment.SpecialFolder.UserProfile)}\\source\\winprint\\tests";
                    openFileDialog.Filter = "code files (*.c*)|*.c*|txt files (*.txt)|*.txt|All files (*.*)|*.*";
                    openFileDialog.FilterIndex = 3;
                    openFileDialog.RestoreDirectory = true;
                    if (openFileDialog.ShowDialog() == DialogResult.OK) {
                        file = openFileDialog.FileNames.ToList<string>()[0];
                    }
                }
            }
            // Go!
            ChangeSheet();

        }

        private void ChangeSheet() {
            this.Cursor = Cursors.WaitCursor;
            printPreview.SheetViewModel = CreatePreviewSheetViewModel();
            this.Cursor = Cursors.Default;
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

        internal void SheetSettingsChanged() {
            Debug.WriteLine("SheetSettingsChanged()");

            this.Cursor = Cursors.WaitCursor;
            // Set landscape. This causes other DefaultPageSettings to change
            printDoc.DefaultPageSettings.Landscape = landscapeCheckbox.Checked;
            printPreview.SheetViewModel.Reflow(printDoc.DefaultPageSettings);
            printPreview.Invalidate(true);
            SizePreview();

            this.Cursor = Cursors.Default;
        }

        internal void SizePreview() {
            Debug.WriteLine("SizePreview()");
            if (printPreview == null || printPreview.SheetViewModel == null) return;
            Size size = panelRight.Size;
            size.Height -= headerTextBox.Height * 3;
            size.Width -= headerTextBox.Height;

            double w = printPreview.SheetViewModel.Bounds.Width;
            double h = printPreview.SheetViewModel.Bounds.Height;

            var scalingX = (double)size.Width / (double)w;
            var scalingY = (double)size.Height / (double)h;

            // Now, we have two scaling ratios, which one produces the smaller image? The one that has the smallest scaling factor.
            var scale = Math.Min(scalingY, scalingX);

            printPreview.Size = new Size((int)(w * scale), (int)(h * scale));

            // Now center
            printPreview.Location = new Point((panelRight.Width / 2) - (printPreview.Width / 2),
                (panelRight.Height / 2) - (printPreview.Height / 2));
        }

        private void MainWindow_Layout(object sender, LayoutEventArgs e) {
            // This event is raised once at startup with the AffectedControl
            // and AffectedProperty properties on the LayoutEventArgs as null. 
            // The event provides size preferences for that case.
            if ((e.AffectedControl != null) && (e.AffectedProperty != null)) {
                // Ensure that the affected property is the Bounds property
                // of the form.
                if (e.AffectedProperty.ToString().Equals("Bounds", StringComparison.InvariantCultureIgnoreCase)) {
                    Debug.WriteLine("MainWindow_Layout bounds changed");
                }
            }
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
                Debug.WriteLine("printersCB_SelectedIndexChanged");
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
                Debug.WriteLine("paperSizesCB_SelectedIndexChanged");
                // Set the paper size based upon the selection in the combo box.
                if (paperSizesCB.SelectedIndex != -1) {
                    printDoc.DefaultPageSettings.PaperSize = printDoc.PrinterSettings.PaperSizes[paperSizesCB.SelectedIndex];
                }
                SheetSettingsChanged();
            }
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Design", "CA1031:Do not catch general exception types", Justification = "<Pending>")]
        private void printButton_Click(object sender, EventArgs e) {
            using var print = new Core.Print();
            // TODO: It's hokey that Landscape is the only printer setting that's treated specially
            // 
            print.PrintDocument.DefaultPageSettings.Landscape = landscapeCheckbox.Checked;
            print.SheetVM.File = file;
            print.SheetVM.SetSettings(ModelLocator.Current.Settings.Sheets.GetValueOrDefault(ModelLocator.Current.Settings.DefaultSheet.ToString()));
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
            SizePreview();
        }

        private void enableHeader_CheckedChanged(object sender, EventArgs e) {
            Debug.WriteLine($"enableHeader_CheckedChanged: {enableHeader.Checked}");
            if (printersCB.Enabled) {
                // TODO: This should find the Preview SheetViewModel instance and set the property on this, not
                // the model
                ModelLocator.Current.Settings.Sheets.GetValueOrDefault(ModelLocator.Current.Settings.DefaultSheet.ToString()).Header.Enabled =
                    enableHeader.Checked;
            }
        }

        private void enableFooter_CheckedChanged(object sender, EventArgs e) {
            Debug.WriteLine($"enableFooter_CheckedChanged: {enableFooter.Checked}");
            if (printersCB.Enabled) {
                // TODO: This should find the Preview SheetViewModel instance and set the property on this, not
                // the model
                ModelLocator.Current.Settings.Sheets.GetValueOrDefault(ModelLocator.Current.Settings.DefaultSheet.ToString()).Footer.Enabled =
                    enableFooter.Checked;
            }
        }

        private void comboBoxSheet_SelectedIndexChanged(object sender, EventArgs e) {
            KeyValuePair<string, string> si = (KeyValuePair<string, string>)comboBoxSheet.SelectedItem;
            Debug.WriteLine($"comboBoxSheet_SelectedIndexChanged: {si.Key}, {si.Value}");
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
    }
}
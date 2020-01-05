using System;
using System.Windows.Forms;

namespace WinPrint.Winforms {
    partial class MainWindow
    {
        /// <summary>
        ///  Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;


        #region Windows Form Designer generated code

        /// <summary>
        ///  Required method for Designer support - do not modify
        ///  the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            this.dummyButton = new System.Windows.Forms.Button();
            this.printersCB = new System.Windows.Forms.ComboBox();
            this.paperSizesCB = new System.Windows.Forms.ComboBox();
            this.landscapeCheckbox = new System.Windows.Forms.CheckBox();
            this.printButton = new System.Windows.Forms.Button();
            this.pageUp = new System.Windows.Forms.Button();
            this.pageDown = new System.Windows.Forms.Button();
            this.headerTextBox = new System.Windows.Forms.TextBox();
            this.enableHeader = new System.Windows.Forms.CheckBox();
            this.footerTextBox = new System.Windows.Forms.TextBox();
            this.enableFooter = new System.Windows.Forms.CheckBox();
            this.panelLeft = new System.Windows.Forms.Panel();
            this.groupPages = new System.Windows.Forms.GroupBox();
            this.pageSeparator = new System.Windows.Forms.CheckBox();
            this.labelPadding = new System.Windows.Forms.Label();
            this.padding = new System.Windows.Forms.NumericUpDown();
            this.labelRows = new System.Windows.Forms.Label();
            this.labelColumns = new System.Windows.Forms.Label();
            this.columns = new System.Windows.Forms.NumericUpDown();
            this.rows = new System.Windows.Forms.NumericUpDown();
            this.groupMargins = new System.Windows.Forms.GroupBox();
            this.labelBottom = new System.Windows.Forms.Label();
            this.bottomMargin = new System.Windows.Forms.NumericUpDown();
            this.leftMargin = new System.Windows.Forms.NumericUpDown();
            this.labelLeft = new System.Windows.Forms.Label();
            this.rightMargin = new System.Windows.Forms.NumericUpDown();
            this.labelTop = new System.Windows.Forms.Label();
            this.labelRight = new System.Windows.Forms.Label();
            this.topMargin = new System.Windows.Forms.NumericUpDown();
            this.labelSheet = new System.Windows.Forms.Label();
            this.comboBoxSheet = new System.Windows.Forms.ComboBox();
            this.labelPrinter = new System.Windows.Forms.Label();
            this.labelPaper = new System.Windows.Forms.Label();
            this.panelRight = new System.Windows.Forms.Panel();
            this.footerPanel = new System.Windows.Forms.Panel();
            this.headerPanel = new System.Windows.Forms.Panel();
            this.fileButton = new System.Windows.Forms.Button();
            this.headerTextBox.SuspendLayout();
            this.footerTextBox.SuspendLayout();
            this.panelLeft.SuspendLayout();
            this.groupPages.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.padding)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.columns)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.rows)).BeginInit();
            this.groupMargins.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.bottomMargin)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.leftMargin)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.rightMargin)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.topMargin)).BeginInit();
            this.panelRight.SuspendLayout();
            this.footerPanel.SuspendLayout();
            this.headerPanel.SuspendLayout();
            this.SuspendLayout();
            // 
            // dummyButton
            // 
            this.dummyButton.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.dummyButton.BackColor = System.Drawing.SystemColors.AppWorkspace;
            this.dummyButton.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.dummyButton.Location = new System.Drawing.Point(11, 40);
            this.dummyButton.Margin = new System.Windows.Forms.Padding(0);
            this.dummyButton.Name = "dummyButton";
            this.dummyButton.Size = new System.Drawing.Size(497, 691);
            this.dummyButton.TabIndex = 0;
            this.dummyButton.Text = "dummyButton";
            this.dummyButton.UseVisualStyleBackColor = false;
            // 
            // printersCB
            // 
            this.printersCB.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
            this.printersCB.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.printersCB.FormattingEnabled = true;
            this.printersCB.Location = new System.Drawing.Point(15, 617);
            this.printersCB.Name = "printersCB";
            this.printersCB.Size = new System.Drawing.Size(259, 28);
            this.printersCB.TabIndex = 20;
            this.printersCB.SelectedIndexChanged += new System.EventHandler(this.printersCB_SelectedIndexChanged);
            // 
            // paperSizesCB
            // 
            this.paperSizesCB.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
            this.paperSizesCB.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.paperSizesCB.FormattingEnabled = true;
            this.paperSizesCB.Location = new System.Drawing.Point(14, 673);
            this.paperSizesCB.Name = "paperSizesCB";
            this.paperSizesCB.Size = new System.Drawing.Size(260, 28);
            this.paperSizesCB.TabIndex = 21;
            this.paperSizesCB.SelectedIndexChanged += new System.EventHandler(this.paperSizesCB_SelectedIndexChanged);
            // 
            // landscapeCheckbox
            // 
            this.landscapeCheckbox.AutoSize = true;
            this.landscapeCheckbox.Location = new System.Drawing.Point(14, 105);
            this.landscapeCheckbox.Name = "landscapeCheckbox";
            this.landscapeCheckbox.Size = new System.Drawing.Size(101, 24);
            this.landscapeCheckbox.TabIndex = 2;
            this.landscapeCheckbox.Text = "&Landscape";
            this.landscapeCheckbox.UseVisualStyleBackColor = true;
            this.landscapeCheckbox.CheckedChanged += new System.EventHandler(this.landscapeCheckbox_CheckedChanged);
            // 
            // printButton
            // 
            this.printButton.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
            this.printButton.Location = new System.Drawing.Point(15, 724);
            this.printButton.Name = "printButton";
            this.printButton.Size = new System.Drawing.Size(125, 37);
            this.printButton.TabIndex = 22;
            this.printButton.Text = "&Print...";
            this.printButton.UseVisualStyleBackColor = true;
            this.printButton.Click += new System.EventHandler(this.printButton_Click);
            // 
            // pageUp
            // 
            this.pageUp.Location = new System.Drawing.Point(0, 0);
            this.pageUp.Name = "pageUp";
            this.pageUp.Size = new System.Drawing.Size(75, 23);
            this.pageUp.TabIndex = 0;
            // 
            // pageDown
            // 
            this.pageDown.Location = new System.Drawing.Point(0, 0);
            this.pageDown.Name = "pageDown";
            this.pageDown.Size = new System.Drawing.Size(75, 23);
            this.pageDown.TabIndex = 0;
            // 
            // headerTextBox
            // 
            this.headerTextBox.BorderStyle = System.Windows.Forms.BorderStyle.None;
            this.headerTextBox.Controls.Add(this.enableHeader);
            this.headerTextBox.Dock = System.Windows.Forms.DockStyle.Fill;
            this.headerTextBox.Location = new System.Drawing.Point(0, 0);
            this.headerTextBox.Margin = new System.Windows.Forms.Padding(11, 13, 11, 13);
            this.headerTextBox.Name = "headerTextBox";
            this.headerTextBox.Size = new System.Drawing.Size(519, 20);
            this.headerTextBox.TabIndex = 7;
            this.headerTextBox.Text = "Header Text";
            this.headerTextBox.TextAlign = System.Windows.Forms.HorizontalAlignment.Center;
            this.headerTextBox.TextChanged += new System.EventHandler(this.headerTextBox_TextChanged);
            // 
            // enableHeader
            // 
            this.enableHeader.AutoSize = true;
            this.enableHeader.Location = new System.Drawing.Point(10, 0);
            this.enableHeader.Margin = new System.Windows.Forms.Padding(3, 4, 3, 4);
            this.enableHeader.Name = "enableHeader";
            this.enableHeader.Size = new System.Drawing.Size(80, 24);
            this.enableHeader.TabIndex = 9;
            this.enableHeader.Text = "&Header";
            this.enableHeader.UseVisualStyleBackColor = true;
            this.enableHeader.CheckedChanged += new System.EventHandler(this.enableHeader_CheckedChanged);
            // 
            // footerTextBox
            // 
            this.footerTextBox.BorderStyle = System.Windows.Forms.BorderStyle.None;
            this.footerTextBox.Controls.Add(this.enableFooter);
            this.footerTextBox.Dock = System.Windows.Forms.DockStyle.Fill;
            this.footerTextBox.Location = new System.Drawing.Point(0, 0);
            this.footerTextBox.Margin = new System.Windows.Forms.Padding(11, 13, 11, 13);
            this.footerTextBox.Name = "footerTextBox";
            this.footerTextBox.Size = new System.Drawing.Size(519, 20);
            this.footerTextBox.TabIndex = 8;
            this.footerTextBox.TextAlign = System.Windows.Forms.HorizontalAlignment.Center;
            this.footerTextBox.TextChanged += new System.EventHandler(this.footerTextBox_TextChanged);
            // 
            // enableFooter
            // 
            this.enableFooter.AutoSize = true;
            this.enableFooter.Location = new System.Drawing.Point(10, 0);
            this.enableFooter.Margin = new System.Windows.Forms.Padding(3, 4, 3, 4);
            this.enableFooter.Name = "enableFooter";
            this.enableFooter.Size = new System.Drawing.Size(74, 24);
            this.enableFooter.TabIndex = 9;
            this.enableFooter.Text = "&Footer";
            this.enableFooter.UseVisualStyleBackColor = true;
            this.enableFooter.CheckedChanged += new System.EventHandler(this.enableFooter_CheckedChanged);
            // 
            // panelLeft
            // 
            this.panelLeft.BackColor = System.Drawing.SystemColors.Window;
            this.panelLeft.Controls.Add(this.fileButton);
            this.panelLeft.Controls.Add(this.groupPages);
            this.panelLeft.Controls.Add(this.groupMargins);
            this.panelLeft.Controls.Add(this.landscapeCheckbox);
            this.panelLeft.Controls.Add(this.paperSizesCB);
            this.panelLeft.Controls.Add(this.printersCB);
            this.panelLeft.Controls.Add(this.labelSheet);
            this.panelLeft.Controls.Add(this.comboBoxSheet);
            this.panelLeft.Controls.Add(this.labelPrinter);
            this.panelLeft.Controls.Add(this.labelPaper);
            this.panelLeft.Controls.Add(this.printButton);
            this.panelLeft.Dock = System.Windows.Forms.DockStyle.Left;
            this.panelLeft.ForeColor = System.Drawing.Color.Black;
            this.panelLeft.Location = new System.Drawing.Point(0, 0);
            this.panelLeft.Margin = new System.Windows.Forms.Padding(3, 4, 3, 4);
            this.panelLeft.Name = "panelLeft";
            this.panelLeft.Size = new System.Drawing.Size(288, 776);
            this.panelLeft.TabIndex = 0;
            // 
            // groupPages
            // 
            this.groupPages.Controls.Add(this.pageSeparator);
            this.groupPages.Controls.Add(this.labelPadding);
            this.groupPages.Controls.Add(this.padding);
            this.groupPages.Controls.Add(this.labelRows);
            this.groupPages.Controls.Add(this.labelColumns);
            this.groupPages.Controls.Add(this.columns);
            this.groupPages.Controls.Add(this.rows);
            this.groupPages.Location = new System.Drawing.Point(14, 289);
            this.groupPages.Margin = new System.Windows.Forms.Padding(3, 4, 3, 4);
            this.groupPages.Name = "groupPages";
            this.groupPages.Padding = new System.Windows.Forms.Padding(3, 4, 3, 4);
            this.groupPages.Size = new System.Drawing.Size(262, 180);
            this.groupPages.TabIndex = 15;
            this.groupPages.TabStop = false;
            this.groupPages.Text = "&Multiple Pages Up";
            // 
            // pageSeparator
            // 
            this.pageSeparator.AutoSize = true;
            this.pageSeparator.Location = new System.Drawing.Point(23, 140);
            this.pageSeparator.Margin = new System.Windows.Forms.Padding(3, 4, 3, 4);
            this.pageSeparator.Name = "pageSeparator";
            this.pageSeparator.Size = new System.Drawing.Size(132, 24);
            this.pageSeparator.TabIndex = 16;
            this.pageSeparator.Text = "Page &Separator";
            this.pageSeparator.UseVisualStyleBackColor = true;
            this.pageSeparator.CheckedChanged += new System.EventHandler(this.pageSeparator_CheckedChanged);
            // 
            // labelPadding
            // 
            this.labelPadding.AutoSize = true;
            this.labelPadding.Location = new System.Drawing.Point(17, 104);
            this.labelPadding.Name = "labelPadding";
            this.labelPadding.Size = new System.Drawing.Size(66, 20);
            this.labelPadding.TabIndex = 14;
            this.labelPadding.Text = "&Padding:";
            // 
            // padding
            // 
            this.padding.DecimalPlaces = 2;
            this.padding.Increment = new decimal(new int[] {
            1,
            0,
            0,
            131072});
            this.padding.Location = new System.Drawing.Point(86, 101);
            this.padding.Margin = new System.Windows.Forms.Padding(3, 4, 3, 4);
            this.padding.Name = "padding";
            this.padding.Size = new System.Drawing.Size(62, 27);
            this.padding.TabIndex = 15;
            this.padding.ValueChanged += new System.EventHandler(this.padding_ValueChanged);
            // 
            // labelRows
            // 
            this.labelRows.AutoSize = true;
            this.labelRows.Location = new System.Drawing.Point(35, 25);
            this.labelRows.Name = "labelRows";
            this.labelRows.Size = new System.Drawing.Size(47, 20);
            this.labelRows.TabIndex = 12;
            this.labelRows.Text = "&Rows:";
            // 
            // labelColumns
            // 
            this.labelColumns.AutoSize = true;
            this.labelColumns.Location = new System.Drawing.Point(13, 64);
            this.labelColumns.Name = "labelColumns";
            this.labelColumns.Size = new System.Drawing.Size(69, 20);
            this.labelColumns.TabIndex = 12;
            this.labelColumns.Text = "&Columns:";
            // 
            // columns
            // 
            this.columns.Location = new System.Drawing.Point(86, 61);
            this.columns.Margin = new System.Windows.Forms.Padding(3, 4, 3, 4);
            this.columns.Minimum = new decimal(new int[] {
            1,
            0,
            0,
            0});
            this.columns.Name = "columns";
            this.columns.Size = new System.Drawing.Size(47, 27);
            this.columns.TabIndex = 13;
            this.columns.Value = new decimal(new int[] {
            1,
            0,
            0,
            0});
            this.columns.ValueChanged += new System.EventHandler(this.columns_ValueChanged);
            // 
            // rows
            // 
            this.rows.Location = new System.Drawing.Point(86, 23);
            this.rows.Margin = new System.Windows.Forms.Padding(3, 4, 3, 4);
            this.rows.Minimum = new decimal(new int[] {
            1,
            0,
            0,
            0});
            this.rows.Name = "rows";
            this.rows.Size = new System.Drawing.Size(47, 27);
            this.rows.TabIndex = 13;
            this.rows.Value = new decimal(new int[] {
            1,
            0,
            0,
            0});
            this.rows.ValueChanged += new System.EventHandler(this.rows_ValueChanged);
            // 
            // groupMargins
            // 
            this.groupMargins.Controls.Add(this.labelBottom);
            this.groupMargins.Controls.Add(this.bottomMargin);
            this.groupMargins.Controls.Add(this.leftMargin);
            this.groupMargins.Controls.Add(this.labelLeft);
            this.groupMargins.Controls.Add(this.rightMargin);
            this.groupMargins.Controls.Add(this.labelTop);
            this.groupMargins.Controls.Add(this.labelRight);
            this.groupMargins.Controls.Add(this.topMargin);
            this.groupMargins.Location = new System.Drawing.Point(11, 137);
            this.groupMargins.Margin = new System.Windows.Forms.Padding(3, 4, 3, 4);
            this.groupMargins.Name = "groupMargins";
            this.groupMargins.Padding = new System.Windows.Forms.Padding(3, 4, 3, 4);
            this.groupMargins.Size = new System.Drawing.Size(262, 144);
            this.groupMargins.TabIndex = 0;
            this.groupMargins.TabStop = false;
            this.groupMargins.Text = "&Margins";
            // 
            // labelBottom
            // 
            this.labelBottom.AutoSize = true;
            this.labelBottom.Location = new System.Drawing.Point(43, 107);
            this.labelBottom.Name = "labelBottom";
            this.labelBottom.Size = new System.Drawing.Size(62, 20);
            this.labelBottom.TabIndex = 0;
            this.labelBottom.Text = "&Bottom:";
            // 
            // bottomMargin
            // 
            this.bottomMargin.DecimalPlaces = 2;
            this.bottomMargin.Increment = new decimal(new int[] {
            1,
            0,
            0,
            131072});
            this.bottomMargin.Location = new System.Drawing.Point(106, 104);
            this.bottomMargin.Margin = new System.Windows.Forms.Padding(3, 4, 3, 4);
            this.bottomMargin.Name = "bottomMargin";
            this.bottomMargin.Size = new System.Drawing.Size(61, 27);
            this.bottomMargin.TabIndex = 4;
            this.bottomMargin.Value = new decimal(new int[] {
            25,
            0,
            0,
            131072});
            this.bottomMargin.ValueChanged += new System.EventHandler(this.bottomMargin_ValueChanged);
            // 
            // leftMargin
            // 
            this.leftMargin.DecimalPlaces = 2;
            this.leftMargin.Increment = new decimal(new int[] {
            1,
            0,
            0,
            131072});
            this.leftMargin.Location = new System.Drawing.Point(56, 64);
            this.leftMargin.Margin = new System.Windows.Forms.Padding(3, 4, 3, 4);
            this.leftMargin.Name = "leftMargin";
            this.leftMargin.Size = new System.Drawing.Size(56, 27);
            this.leftMargin.TabIndex = 4;
            this.leftMargin.Value = new decimal(new int[] {
            25,
            0,
            0,
            131072});
            this.leftMargin.ValueChanged += new System.EventHandler(this.leftMargin_ValueChanged);
            // 
            // labelLeft
            // 
            this.labelLeft.AutoSize = true;
            this.labelLeft.Location = new System.Drawing.Point(15, 67);
            this.labelLeft.Name = "labelLeft";
            this.labelLeft.Size = new System.Drawing.Size(37, 20);
            this.labelLeft.TabIndex = 2;
            this.labelLeft.Text = "&Left:";
            // 
            // rightMargin
            // 
            this.rightMargin.DecimalPlaces = 2;
            this.rightMargin.Increment = new decimal(new int[] {
            1,
            0,
            0,
            131072});
            this.rightMargin.Location = new System.Drawing.Point(191, 64);
            this.rightMargin.Margin = new System.Windows.Forms.Padding(3, 4, 3, 4);
            this.rightMargin.Name = "rightMargin";
            this.rightMargin.Size = new System.Drawing.Size(61, 27);
            this.rightMargin.TabIndex = 3;
            this.rightMargin.Value = new decimal(new int[] {
            25,
            0,
            0,
            131072});
            this.rightMargin.ValueChanged += new System.EventHandler(this.rightMargin_ValueChanged);
            // 
            // labelTop
            // 
            this.labelTop.AutoSize = true;
            this.labelTop.Location = new System.Drawing.Point(67, 25);
            this.labelTop.Name = "labelTop";
            this.labelTop.Size = new System.Drawing.Size(37, 20);
            this.labelTop.TabIndex = 0;
            this.labelTop.Text = "&Top:";
            // 
            // labelRight
            // 
            this.labelRight.AutoSize = true;
            this.labelRight.Location = new System.Drawing.Point(141, 67);
            this.labelRight.Name = "labelRight";
            this.labelRight.Size = new System.Drawing.Size(47, 20);
            this.labelRight.TabIndex = 0;
            this.labelRight.Text = "&Right:";
            // 
            // topMargin
            // 
            this.topMargin.DecimalPlaces = 2;
            this.topMargin.Increment = new decimal(new int[] {
            1,
            0,
            0,
            131072});
            this.topMargin.Location = new System.Drawing.Point(106, 23);
            this.topMargin.Margin = new System.Windows.Forms.Padding(3, 4, 3, 4);
            this.topMargin.Name = "topMargin";
            this.topMargin.Size = new System.Drawing.Size(61, 27);
            this.topMargin.TabIndex = 1;
            this.topMargin.Value = new decimal(new int[] {
            25,
            0,
            0,
            131072});
            this.topMargin.ValueChanged += new System.EventHandler(this.topMargin_ValueChanged);
            // 
            // labelSheet
            // 
            this.labelSheet.AutoSize = true;
            this.labelSheet.Location = new System.Drawing.Point(14, 53);
            this.labelSheet.Name = "labelSheet";
            this.labelSheet.Size = new System.Drawing.Size(99, 20);
            this.labelSheet.TabIndex = 9;
            this.labelSheet.Text = "&Sheet Design:";
            // 
            // comboBoxSheet
            // 
            this.comboBoxSheet.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.comboBoxSheet.FormattingEnabled = true;
            this.comboBoxSheet.Location = new System.Drawing.Point(14, 77);
            this.comboBoxSheet.Margin = new System.Windows.Forms.Padding(3, 4, 3, 4);
            this.comboBoxSheet.Name = "comboBoxSheet";
            this.comboBoxSheet.Size = new System.Drawing.Size(259, 28);
            this.comboBoxSheet.TabIndex = 8;
            this.comboBoxSheet.SelectedIndexChanged += new System.EventHandler(this.comboBoxSheet_SelectedIndexChanged);
            // 
            // labelPrinter
            // 
            this.labelPrinter.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
            this.labelPrinter.AutoSize = true;
            this.labelPrinter.Location = new System.Drawing.Point(15, 595);
            this.labelPrinter.Name = "labelPrinter";
            this.labelPrinter.Size = new System.Drawing.Size(55, 20);
            this.labelPrinter.TabIndex = 0;
            this.labelPrinter.Text = "&Printer:";
            // 
            // labelPaper
            // 
            this.labelPaper.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
            this.labelPaper.AutoSize = true;
            this.labelPaper.Location = new System.Drawing.Point(15, 651);
            this.labelPaper.Name = "labelPaper";
            this.labelPaper.Size = new System.Drawing.Size(49, 20);
            this.labelPaper.TabIndex = 0;
            this.labelPaper.Text = "P&aper:";
            // 
            // panelRight
            // 
            this.panelRight.BackColor = System.Drawing.SystemColors.AppWorkspace;
            this.panelRight.Controls.Add(this.dummyButton);
            this.panelRight.Controls.Add(this.footerPanel);
            this.panelRight.Controls.Add(this.headerPanel);
            this.panelRight.Dock = System.Windows.Forms.DockStyle.Fill;
            this.panelRight.Location = new System.Drawing.Point(288, 0);
            this.panelRight.Margin = new System.Windows.Forms.Padding(0);
            this.panelRight.Name = "panelRight";
            this.panelRight.Size = new System.Drawing.Size(519, 776);
            this.panelRight.TabIndex = 4;
            this.panelRight.TabStop = true;
            // 
            // footerPanel
            // 
            this.footerPanel.BackColor = System.Drawing.SystemColors.Window;
            this.footerPanel.Controls.Add(this.footerTextBox);
            this.footerPanel.Dock = System.Windows.Forms.DockStyle.Bottom;
            this.footerPanel.Location = new System.Drawing.Point(0, 744);
            this.footerPanel.Margin = new System.Windows.Forms.Padding(3, 4, 3, 4);
            this.footerPanel.Name = "footerPanel";
            this.footerPanel.Size = new System.Drawing.Size(519, 32);
            this.footerPanel.TabIndex = 3;
            // 
            // headerPanel
            // 
            this.headerPanel.BackColor = System.Drawing.SystemColors.Window;
            this.headerPanel.Controls.Add(this.headerTextBox);
            this.headerPanel.Dock = System.Windows.Forms.DockStyle.Top;
            this.headerPanel.Location = new System.Drawing.Point(0, 0);
            this.headerPanel.Margin = new System.Windows.Forms.Padding(3, 4, 3, 4);
            this.headerPanel.Name = "headerPanel";
            this.headerPanel.Size = new System.Drawing.Size(519, 27);
            this.headerPanel.TabIndex = 1;
            // 
            // fileButton
            // 
            this.fileButton.Location = new System.Drawing.Point(15, 13);
            this.fileButton.Name = "fileButton";
            this.fileButton.Size = new System.Drawing.Size(147, 29);
            this.fileButton.TabIndex = 0;
            this.fileButton.Text = "&File...";
            this.fileButton.UseVisualStyleBackColor = true;
            this.fileButton.Click += new System.EventHandler(this.fileButton_Click);
            // 
            // MainWindow
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(8F, 20F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.BackColor = System.Drawing.SystemColors.ControlLight;
            this.ClientSize = new System.Drawing.Size(807, 776);
            this.Controls.Add(this.panelRight);
            this.Controls.Add(this.panelLeft);
            this.HelpButton = true;
            this.Margin = new System.Windows.Forms.Padding(30, 29, 30, 29);
            this.Name = "MainWindow";
            this.Text = "WinPrint";
            this.FormClosing += new System.Windows.Forms.FormClosingEventHandler(this.MainWindow_FormClosing);
            this.Load += new System.EventHandler(this.MainWindow_Load);
            this.Layout += new System.Windows.Forms.LayoutEventHandler(this.MainWindow_Layout);
            this.headerTextBox.ResumeLayout(false);
            this.headerTextBox.PerformLayout();
            this.footerTextBox.ResumeLayout(false);
            this.footerTextBox.PerformLayout();
            this.panelLeft.ResumeLayout(false);
            this.panelLeft.PerformLayout();
            this.groupPages.ResumeLayout(false);
            this.groupPages.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)(this.padding)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.columns)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.rows)).EndInit();
            this.groupMargins.ResumeLayout(false);
            this.groupMargins.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)(this.bottomMargin)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.leftMargin)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.rightMargin)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.topMargin)).EndInit();
            this.panelRight.ResumeLayout(false);
            this.footerPanel.ResumeLayout(false);
            this.footerPanel.PerformLayout();
            this.headerPanel.ResumeLayout(false);
            this.headerPanel.PerformLayout();
            this.ResumeLayout(false);

        }

        #endregion

        private Button dummyButton;
        private ComboBox printersCB;
        private ComboBox paperSizesCB;
        private CheckBox landscapeCheckbox;
        private Button printButton;
        private Button pageUp;
        private Button pageDown;
        private TextBox headerTextBox;
        private TextBox footerTextBox;
        private Panel panelLeft;
        private Panel panelRight;
        private CheckBox enableHeader;
        private CheckBox enableFooter;
        private Label labelSheet;
        private ComboBox comboBoxSheet;
        private Label labelPrinter;
        private Label labelPaper;
        private Label labelTop;
        private NumericUpDown topMargin;
        private Label labelLeft;
        private NumericUpDown leftMargin;
        private Label labelRight;
        private NumericUpDown rightMargin;
        private Label labelBottom;
        private NumericUpDown bottomMargin;
        private Label labelRows;
        private NumericUpDown rows;
        private Label labelColumns;
        private NumericUpDown columns;
        private GroupBox groupMargins;
        private GroupBox groupPages;
        private CheckBox pageSeparator;
        private Label labelPadding;
        private NumericUpDown padding;
        private Panel headerPanel;
        private Panel footerPanel;
        private Button fileButton;
    }
}


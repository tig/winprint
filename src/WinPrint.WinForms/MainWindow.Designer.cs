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
            this.fileButton = new System.Windows.Forms.Button();
            this.groupBox1 = new System.Windows.Forms.GroupBox();
            this.groupPages = new System.Windows.Forms.GroupBox();
            this.pageSeparator = new System.Windows.Forms.CheckBox();
            this.labelPadding = new System.Windows.Forms.Label();
            this.padding = new System.Windows.Forms.NumericUpDown();
            this.labelRows = new System.Windows.Forms.Label();
            this.labelColumns = new System.Windows.Forms.Label();
            this.columns = new System.Windows.Forms.NumericUpDown();
            this.rows = new System.Windows.Forms.NumericUpDown();
            this.groupMargins = new System.Windows.Forms.GroupBox();
            this.labelLeft = new System.Windows.Forms.Label();
            this.leftMargin = new System.Windows.Forms.NumericUpDown();
            this.labelRight = new System.Windows.Forms.Label();
            this.rightMargin = new System.Windows.Forms.NumericUpDown();
            this.labelBottom = new System.Windows.Forms.Label();
            this.bottomMargin = new System.Windows.Forms.NumericUpDown();
            this.labelTop = new System.Windows.Forms.Label();
            this.topMargin = new System.Windows.Forms.NumericUpDown();
            this.comboBoxSheet = new System.Windows.Forms.ComboBox();
            this.panelAbout = new System.Windows.Forms.Panel();
            this.versionLabel = new System.Windows.Forms.Label();
            this.wikiLink = new System.Windows.Forms.LinkLabel();
            this.settingsButton = new System.Windows.Forms.Button();
            this.printerGroup = new System.Windows.Forms.GroupBox();
            this.pagesLabel = new System.Windows.Forms.Label();
            this.labelPaper = new System.Windows.Forms.Label();
            this.label1 = new System.Windows.Forms.Label();
            this.toText = new System.Windows.Forms.TextBox();
            this.fromText = new System.Windows.Forms.TextBox();
            this.fromLabel = new System.Windows.Forms.Label();
            this.panelRight = new System.Windows.Forms.Panel();
            this.footerPanel = new System.Windows.Forms.Panel();
            this.headerPanel = new System.Windows.Forms.Panel();
            this.panelLeft.SuspendLayout();
            this.groupBox1.SuspendLayout();
            this.groupPages.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.padding)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.columns)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.rows)).BeginInit();
            this.groupMargins.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.leftMargin)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.rightMargin)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.bottomMargin)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.topMargin)).BeginInit();
            this.panelAbout.SuspendLayout();
            this.printerGroup.SuspendLayout();
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
            this.dummyButton.Location = new System.Drawing.Point(10, 50);
            this.dummyButton.Margin = new System.Windows.Forms.Padding(0);
            this.dummyButton.Name = "dummyButton";
            this.dummyButton.Size = new System.Drawing.Size(645, 485);
            this.dummyButton.TabIndex = 0;
            this.dummyButton.Text = "dummyButton";
            this.dummyButton.UseVisualStyleBackColor = false;
            // 
            // printersCB
            // 
            this.printersCB.DropDownHeight = 300;
            this.printersCB.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.printersCB.DropDownWidth = 450;
            this.printersCB.FormattingEnabled = true;
            this.printersCB.IntegralHeight = false;
            this.printersCB.Location = new System.Drawing.Point(14, 19);
            this.printersCB.Margin = new System.Windows.Forms.Padding(3, 2, 3, 2);
            this.printersCB.Name = "printersCB";
            this.printersCB.Size = new System.Drawing.Size(208, 23);
            this.printersCB.TabIndex = 20;
            this.printersCB.SelectedIndexChanged += new System.EventHandler(this.printersCB_SelectedIndexChanged);
            // 
            // paperSizesCB
            // 
            this.paperSizesCB.DropDownHeight = 300;
            this.paperSizesCB.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.paperSizesCB.DropDownWidth = 450;
            this.paperSizesCB.FormattingEnabled = true;
            this.paperSizesCB.IntegralHeight = false;
            this.paperSizesCB.Location = new System.Drawing.Point(14, 67);
            this.paperSizesCB.Margin = new System.Windows.Forms.Padding(3, 2, 3, 2);
            this.paperSizesCB.Name = "paperSizesCB";
            this.paperSizesCB.Size = new System.Drawing.Size(208, 23);
            this.paperSizesCB.TabIndex = 21;
            this.paperSizesCB.SelectedIndexChanged += new System.EventHandler(this.paperSizesCB_SelectedIndexChanged);
            // 
            // landscapeCheckbox
            // 
            this.landscapeCheckbox.AutoSize = true;
            this.landscapeCheckbox.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.landscapeCheckbox.Location = new System.Drawing.Point(14, 47);
            this.landscapeCheckbox.Margin = new System.Windows.Forms.Padding(3, 2, 3, 2);
            this.landscapeCheckbox.Name = "landscapeCheckbox";
            this.landscapeCheckbox.Size = new System.Drawing.Size(79, 19);
            this.landscapeCheckbox.TabIndex = 2;
            this.landscapeCheckbox.Text = "&Landscape";
            this.landscapeCheckbox.UseVisualStyleBackColor = true;
            this.landscapeCheckbox.CheckedChanged += new System.EventHandler(this.landscapeCheckbox_CheckedChanged);
            // 
            // printButton
            // 
            this.printButton.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.printButton.Font = new System.Drawing.Font("Segoe UI", 10F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point);
            this.printButton.Location = new System.Drawing.Point(109, 5);
            this.printButton.Margin = new System.Windows.Forms.Padding(3, 2, 3, 2);
            this.printButton.Name = "printButton";
            this.printButton.Size = new System.Drawing.Size(85, 27);
            this.printButton.TabIndex = 22;
            this.printButton.Text = "🖶 &Print...";
            this.printButton.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            this.printButton.UseVisualStyleBackColor = false;
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
            this.headerTextBox.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.headerTextBox.BackColor = System.Drawing.SystemColors.Control;
            this.headerTextBox.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.headerTextBox.Location = new System.Drawing.Point(27, 7);
            this.headerTextBox.Margin = new System.Windows.Forms.Padding(10, 10, 10, 10);
            this.headerTextBox.Name = "headerTextBox";
            this.headerTextBox.Size = new System.Drawing.Size(628, 23);
            this.headerTextBox.TabIndex = 7;
            this.headerTextBox.Text = "Header Text";
            this.headerTextBox.TextAlign = System.Windows.Forms.HorizontalAlignment.Center;
            this.headerTextBox.TextChanged += new System.EventHandler(this.headerTextBox_TextChanged);
            // 
            // enableHeader
            // 
            this.enableHeader.AutoSize = true;
            this.enableHeader.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.enableHeader.Location = new System.Drawing.Point(10, 10);
            this.enableHeader.Name = "enableHeader";
            this.enableHeader.Size = new System.Drawing.Size(12, 11);
            this.enableHeader.TabIndex = 9;
            this.enableHeader.UseVisualStyleBackColor = true;
            this.enableHeader.CheckedChanged += new System.EventHandler(this.enableHeader_CheckedChanged);
            // 
            // footerTextBox
            // 
            this.footerTextBox.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.footerTextBox.BackColor = System.Drawing.SystemColors.Control;
            this.footerTextBox.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.footerTextBox.Location = new System.Drawing.Point(27, 5);
            this.footerTextBox.Margin = new System.Windows.Forms.Padding(10, 10, 10, 10);
            this.footerTextBox.Name = "footerTextBox";
            this.footerTextBox.Size = new System.Drawing.Size(628, 23);
            this.footerTextBox.TabIndex = 8;
            this.footerTextBox.Text = "Footer Text";
            this.footerTextBox.TextAlign = System.Windows.Forms.HorizontalAlignment.Center;
            this.footerTextBox.TextChanged += new System.EventHandler(this.footerTextBox_TextChanged);
            // 
            // enableFooter
            // 
            this.enableFooter.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
            this.enableFooter.AutoSize = true;
            this.enableFooter.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.enableFooter.Location = new System.Drawing.Point(10, 9);
            this.enableFooter.Name = "enableFooter";
            this.enableFooter.Size = new System.Drawing.Size(12, 11);
            this.enableFooter.TabIndex = 9;
            this.enableFooter.UseVisualStyleBackColor = true;
            this.enableFooter.CheckedChanged += new System.EventHandler(this.enableFooter_CheckedChanged);
            // 
            // panelLeft
            // 
            this.panelLeft.BackColor = System.Drawing.SystemColors.Control;
            this.panelLeft.Controls.Add(this.fileButton);
            this.panelLeft.Controls.Add(this.groupBox1);
            this.panelLeft.Controls.Add(this.panelAbout);
            this.panelLeft.Controls.Add(this.settingsButton);
            this.panelLeft.Controls.Add(this.printButton);
            this.panelLeft.Controls.Add(this.printerGroup);
            this.panelLeft.Dock = System.Windows.Forms.DockStyle.Left;
            this.panelLeft.ForeColor = System.Drawing.Color.Black;
            this.panelLeft.Location = new System.Drawing.Point(0, 0);
            this.panelLeft.Name = "panelLeft";
            this.panelLeft.Size = new System.Drawing.Size(249, 586);
            this.panelLeft.TabIndex = 0;
            // 
            // fileButton
            // 
            this.fileButton.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.fileButton.Font = new System.Drawing.Font("Segoe UI", 10F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point);
            this.fileButton.Location = new System.Drawing.Point(8, 5);
            this.fileButton.Margin = new System.Windows.Forms.Padding(3, 2, 3, 2);
            this.fileButton.Name = "fileButton";
            this.fileButton.Size = new System.Drawing.Size(94, 27);
            this.fileButton.TabIndex = 0;
            this.fileButton.Text = "📂 &File...";
            this.fileButton.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            this.fileButton.UseVisualStyleBackColor = false;
            this.fileButton.Click += new System.EventHandler(this.fileButton_Click);
            // 
            // groupBox1
            // 
            this.groupBox1.Controls.Add(this.landscapeCheckbox);
            this.groupBox1.Controls.Add(this.groupPages);
            this.groupBox1.Controls.Add(this.groupMargins);
            this.groupBox1.Controls.Add(this.comboBoxSheet);
            this.groupBox1.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.groupBox1.Location = new System.Drawing.Point(8, 43);
            this.groupBox1.Margin = new System.Windows.Forms.Padding(3, 2, 3, 2);
            this.groupBox1.Name = "groupBox1";
            this.groupBox1.Padding = new System.Windows.Forms.Padding(3, 2, 3, 2);
            this.groupBox1.Size = new System.Drawing.Size(232, 345);
            this.groupBox1.TabIndex = 27;
            this.groupBox1.TabStop = false;
            this.groupBox1.Text = "Sheet Definition";
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
            this.groupPages.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.groupPages.Location = new System.Drawing.Point(13, 198);
            this.groupPages.Name = "groupPages";
            this.groupPages.Size = new System.Drawing.Size(209, 138);
            this.groupPages.TabIndex = 15;
            this.groupPages.TabStop = false;
            this.groupPages.Text = "&Multiple Pages Up";
            // 
            // pageSeparator
            // 
            this.pageSeparator.AutoSize = true;
            this.pageSeparator.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.pageSeparator.Location = new System.Drawing.Point(38, 111);
            this.pageSeparator.Name = "pageSeparator";
            this.pageSeparator.Size = new System.Drawing.Size(102, 19);
            this.pageSeparator.TabIndex = 16;
            this.pageSeparator.Text = "Page Separat&or";
            this.pageSeparator.UseVisualStyleBackColor = true;
            this.pageSeparator.CheckedChanged += new System.EventHandler(this.pageSeparator_CheckedChanged);
            // 
            // labelPadding
            // 
            this.labelPadding.AutoSize = true;
            this.labelPadding.Location = new System.Drawing.Point(32, 83);
            this.labelPadding.Name = "labelPadding";
            this.labelPadding.Size = new System.Drawing.Size(54, 15);
            this.labelPadding.TabIndex = 14;
            this.labelPadding.Text = "Pa&dding:";
            // 
            // padding
            // 
            this.padding.DecimalPlaces = 2;
            this.padding.Increment = new decimal(new int[] {
            1,
            0,
            0,
            131072});
            this.padding.Location = new System.Drawing.Point(92, 82);
            this.padding.Name = "padding";
            this.padding.Size = new System.Drawing.Size(57, 23);
            this.padding.TabIndex = 15;
            this.padding.ValueChanged += new System.EventHandler(this.padding_ValueChanged);
            // 
            // labelRows
            // 
            this.labelRows.AutoSize = true;
            this.labelRows.Location = new System.Drawing.Point(48, 25);
            this.labelRows.Name = "labelRows";
            this.labelRows.Size = new System.Drawing.Size(38, 15);
            this.labelRows.TabIndex = 12;
            this.labelRows.Text = "&Rows:";
            // 
            // labelColumns
            // 
            this.labelColumns.AutoSize = true;
            this.labelColumns.Location = new System.Drawing.Point(29, 53);
            this.labelColumns.Name = "labelColumns";
            this.labelColumns.Size = new System.Drawing.Size(58, 15);
            this.labelColumns.TabIndex = 12;
            this.labelColumns.Text = "&Columns:";
            // 
            // columns
            // 
            this.columns.Location = new System.Drawing.Point(92, 53);
            this.columns.Minimum = new decimal(new int[] {
            1,
            0,
            0,
            0});
            this.columns.Name = "columns";
            this.columns.Size = new System.Drawing.Size(57, 23);
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
            this.rows.Location = new System.Drawing.Point(92, 23);
            this.rows.Minimum = new decimal(new int[] {
            1,
            0,
            0,
            0});
            this.rows.Name = "rows";
            this.rows.Size = new System.Drawing.Size(57, 23);
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
            this.groupMargins.Controls.Add(this.labelLeft);
            this.groupMargins.Controls.Add(this.leftMargin);
            this.groupMargins.Controls.Add(this.labelRight);
            this.groupMargins.Controls.Add(this.rightMargin);
            this.groupMargins.Controls.Add(this.labelBottom);
            this.groupMargins.Controls.Add(this.bottomMargin);
            this.groupMargins.Controls.Add(this.labelTop);
            this.groupMargins.Controls.Add(this.topMargin);
            this.groupMargins.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.groupMargins.Location = new System.Drawing.Point(13, 70);
            this.groupMargins.Name = "groupMargins";
            this.groupMargins.Size = new System.Drawing.Size(209, 122);
            this.groupMargins.TabIndex = 0;
            this.groupMargins.TabStop = false;
            this.groupMargins.Text = "&Margins";
            // 
            // labelLeft
            // 
            this.labelLeft.AutoSize = true;
            this.labelLeft.Location = new System.Drawing.Point(4, 52);
            this.labelLeft.Name = "labelLeft";
            this.labelLeft.Size = new System.Drawing.Size(30, 15);
            this.labelLeft.TabIndex = 2;
            this.labelLeft.Text = "&Left:";
            // 
            // leftMargin
            // 
            this.leftMargin.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.leftMargin.DecimalPlaces = 2;
            this.leftMargin.Increment = new decimal(new int[] {
            1,
            0,
            0,
            131072});
            this.leftMargin.Location = new System.Drawing.Point(38, 50);
            this.leftMargin.Name = "leftMargin";
            this.leftMargin.Size = new System.Drawing.Size(57, 23);
            this.leftMargin.TabIndex = 4;
            this.leftMargin.Value = new decimal(new int[] {
            25,
            0,
            0,
            131072});
            this.leftMargin.ValueChanged += new System.EventHandler(this.leftMargin_ValueChanged);
            // 
            // labelRight
            // 
            this.labelRight.AutoSize = true;
            this.labelRight.Location = new System.Drawing.Point(99, 52);
            this.labelRight.Name = "labelRight";
            this.labelRight.Size = new System.Drawing.Size(38, 15);
            this.labelRight.TabIndex = 0;
            this.labelRight.Text = "&Right:";
            // 
            // rightMargin
            // 
            this.rightMargin.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.rightMargin.DecimalPlaces = 2;
            this.rightMargin.Increment = new decimal(new int[] {
            1,
            0,
            0,
            131072});
            this.rightMargin.Location = new System.Drawing.Point(144, 50);
            this.rightMargin.Name = "rightMargin";
            this.rightMargin.Size = new System.Drawing.Size(57, 23);
            this.rightMargin.TabIndex = 3;
            this.rightMargin.Value = new decimal(new int[] {
            25,
            0,
            0,
            131072});
            this.rightMargin.ValueChanged += new System.EventHandler(this.rightMargin_ValueChanged);
            // 
            // labelBottom
            // 
            this.labelBottom.AutoSize = true;
            this.labelBottom.Location = new System.Drawing.Point(38, 82);
            this.labelBottom.Name = "labelBottom";
            this.labelBottom.Size = new System.Drawing.Size(50, 15);
            this.labelBottom.TabIndex = 0;
            this.labelBottom.Text = "&Bottom:";
            // 
            // bottomMargin
            // 
            this.bottomMargin.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.bottomMargin.DecimalPlaces = 2;
            this.bottomMargin.Increment = new decimal(new int[] {
            1,
            0,
            0,
            131072});
            this.bottomMargin.Location = new System.Drawing.Point(92, 80);
            this.bottomMargin.Name = "bottomMargin";
            this.bottomMargin.Size = new System.Drawing.Size(57, 23);
            this.bottomMargin.TabIndex = 4;
            this.bottomMargin.Value = new decimal(new int[] {
            25,
            0,
            0,
            131072});
            this.bottomMargin.ValueChanged += new System.EventHandler(this.bottomMargin_ValueChanged);
            // 
            // labelTop
            // 
            this.labelTop.AutoSize = true;
            this.labelTop.Location = new System.Drawing.Point(59, 20);
            this.labelTop.Name = "labelTop";
            this.labelTop.Size = new System.Drawing.Size(29, 15);
            this.labelTop.TabIndex = 0;
            this.labelTop.Text = "&Top:";
            // 
            // topMargin
            // 
            this.topMargin.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.topMargin.DecimalPlaces = 2;
            this.topMargin.Increment = new decimal(new int[] {
            1,
            0,
            0,
            131072});
            this.topMargin.Location = new System.Drawing.Point(92, 19);
            this.topMargin.Name = "topMargin";
            this.topMargin.Size = new System.Drawing.Size(57, 23);
            this.topMargin.TabIndex = 1;
            this.topMargin.Value = new decimal(new int[] {
            25,
            0,
            0,
            131072});
            this.topMargin.ValueChanged += new System.EventHandler(this.topMargin_ValueChanged);
            // 
            // comboBoxSheet
            // 
            this.comboBoxSheet.DropDownHeight = 200;
            this.comboBoxSheet.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.comboBoxSheet.DropDownWidth = 450;
            this.comboBoxSheet.FormattingEnabled = true;
            this.comboBoxSheet.IntegralHeight = false;
            this.comboBoxSheet.Location = new System.Drawing.Point(13, 20);
            this.comboBoxSheet.Name = "comboBoxSheet";
            this.comboBoxSheet.Size = new System.Drawing.Size(208, 23);
            this.comboBoxSheet.TabIndex = 8;
            this.comboBoxSheet.SelectedIndexChanged += new System.EventHandler(this.comboBoxSheet_SelectedIndexChanged);
            // 
            // panelAbout
            // 
            this.panelAbout.Controls.Add(this.versionLabel);
            this.panelAbout.Controls.Add(this.wikiLink);
            this.panelAbout.Dock = System.Windows.Forms.DockStyle.Bottom;
            this.panelAbout.Location = new System.Drawing.Point(0, 555);
            this.panelAbout.Margin = new System.Windows.Forms.Padding(2, 2, 2, 2);
            this.panelAbout.Name = "panelAbout";
            this.panelAbout.Size = new System.Drawing.Size(249, 31);
            this.panelAbout.TabIndex = 28;
            // 
            // versionLabel
            // 
            this.versionLabel.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
            this.versionLabel.AutoSize = true;
            this.versionLabel.Location = new System.Drawing.Point(162, 8);
            this.versionLabel.Margin = new System.Windows.Forms.Padding(2, 0, 2, 0);
            this.versionLabel.Name = "versionLabel";
            this.versionLabel.Size = new System.Drawing.Size(64, 15);
            this.versionLabel.TabIndex = 0;
            this.versionLabel.Text = "v0.0.0.0000";
            this.versionLabel.TextAlign = System.Drawing.ContentAlignment.MiddleRight;
            // 
            // wikiLink
            // 
            this.wikiLink.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
            this.wikiLink.AutoSize = true;
            this.wikiLink.Location = new System.Drawing.Point(10, 8);
            this.wikiLink.Margin = new System.Windows.Forms.Padding(2, 0, 2, 0);
            this.wikiLink.Name = "wikiLink";
            this.wikiLink.Size = new System.Drawing.Size(88, 15);
            this.wikiLink.TabIndex = 1;
            this.wikiLink.TabStop = true;
            this.wikiLink.Text = "Help && about...";
            this.wikiLink.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            this.wikiLink.LinkClicked += new System.Windows.Forms.LinkLabelLinkClickedEventHandler(this.helpaboutLink_LinkClicked);
            // 
            // settingsButton
            // 
            this.settingsButton.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.settingsButton.Font = new System.Drawing.Font("Segoe UI", 10F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point);
            this.settingsButton.Location = new System.Drawing.Point(200, 5);
            this.settingsButton.Margin = new System.Windows.Forms.Padding(3, 2, 3, 2);
            this.settingsButton.Name = "settingsButton";
            this.settingsButton.Size = new System.Drawing.Size(41, 27);
            this.settingsButton.TabIndex = 25;
            this.settingsButton.Text = "⚙";
            this.settingsButton.UseVisualStyleBackColor = false;
            this.settingsButton.Click += new System.EventHandler(this.settingsButton_Click);
            // 
            // printerGroup
            // 
            this.printerGroup.Controls.Add(this.pagesLabel);
            this.printerGroup.Controls.Add(this.paperSizesCB);
            this.printerGroup.Controls.Add(this.labelPaper);
            this.printerGroup.Controls.Add(this.printersCB);
            this.printerGroup.Controls.Add(this.label1);
            this.printerGroup.Controls.Add(this.toText);
            this.printerGroup.Controls.Add(this.fromText);
            this.printerGroup.Controls.Add(this.fromLabel);
            this.printerGroup.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.printerGroup.Location = new System.Drawing.Point(8, 395);
            this.printerGroup.Margin = new System.Windows.Forms.Padding(3, 2, 3, 2);
            this.printerGroup.Name = "printerGroup";
            this.printerGroup.Padding = new System.Windows.Forms.Padding(3, 2, 3, 2);
            this.printerGroup.Size = new System.Drawing.Size(232, 154);
            this.printerGroup.TabIndex = 26;
            this.printerGroup.TabStop = false;
            this.printerGroup.Text = "Printer";
            // 
            // pagesLabel
            // 
            this.pagesLabel.AutoSize = true;
            this.pagesLabel.Location = new System.Drawing.Point(13, 101);
            this.pagesLabel.Margin = new System.Windows.Forms.Padding(1, 0, 1, 0);
            this.pagesLabel.Name = "pagesLabel";
            this.pagesLabel.Size = new System.Drawing.Size(41, 15);
            this.pagesLabel.TabIndex = 23;
            this.pagesLabel.Text = "Pages:";
            // 
            // labelPaper
            // 
            this.labelPaper.AutoSize = true;
            this.labelPaper.Location = new System.Drawing.Point(8, 50);
            this.labelPaper.Name = "labelPaper";
            this.labelPaper.Size = new System.Drawing.Size(40, 15);
            this.labelPaper.TabIndex = 0;
            this.labelPaper.Text = "P&aper:";
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.label1.Location = new System.Drawing.Point(115, 128);
            this.label1.Margin = new System.Windows.Forms.Padding(1, 0, 1, 0);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(22, 15);
            this.label1.TabIndex = 23;
            this.label1.Text = "To:";
            // 
            // toText
            // 
            this.toText.Location = new System.Drawing.Point(143, 126);
            this.toText.Margin = new System.Windows.Forms.Padding(1, 1, 1, 1);
            this.toText.Name = "toText";
            this.toText.Size = new System.Drawing.Size(47, 23);
            this.toText.TabIndex = 24;
            // 
            // fromText
            // 
            this.fromText.Location = new System.Drawing.Point(60, 126);
            this.fromText.Margin = new System.Windows.Forms.Padding(1, 1, 1, 1);
            this.fromText.Name = "fromText";
            this.fromText.Size = new System.Drawing.Size(40, 23);
            this.fromText.TabIndex = 24;
            // 
            // fromLabel
            // 
            this.fromLabel.AutoSize = true;
            this.fromLabel.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.fromLabel.Location = new System.Drawing.Point(14, 128);
            this.fromLabel.Margin = new System.Windows.Forms.Padding(1, 0, 1, 0);
            this.fromLabel.Name = "fromLabel";
            this.fromLabel.Size = new System.Drawing.Size(38, 15);
            this.fromLabel.TabIndex = 23;
            this.fromLabel.Text = "&From:";
            // 
            // panelRight
            // 
            this.panelRight.BackColor = System.Drawing.SystemColors.AppWorkspace;
            this.panelRight.Controls.Add(this.dummyButton);
            this.panelRight.Controls.Add(this.footerPanel);
            this.panelRight.Controls.Add(this.headerPanel);
            this.panelRight.Dock = System.Windows.Forms.DockStyle.Fill;
            this.panelRight.Location = new System.Drawing.Point(249, 0);
            this.panelRight.Margin = new System.Windows.Forms.Padding(0);
            this.panelRight.Name = "panelRight";
            this.panelRight.Size = new System.Drawing.Size(665, 586);
            this.panelRight.TabIndex = 4;
            this.panelRight.TabStop = true;
            // 
            // footerPanel
            // 
            this.footerPanel.BackColor = System.Drawing.SystemColors.Control;
            this.footerPanel.Controls.Add(this.enableFooter);
            this.footerPanel.Controls.Add(this.footerTextBox);
            this.footerPanel.Dock = System.Windows.Forms.DockStyle.Bottom;
            this.footerPanel.Location = new System.Drawing.Point(0, 555);
            this.footerPanel.Name = "footerPanel";
            this.footerPanel.Size = new System.Drawing.Size(665, 31);
            this.footerPanel.TabIndex = 3;
            // 
            // headerPanel
            // 
            this.headerPanel.BackColor = System.Drawing.SystemColors.Control;
            this.headerPanel.Controls.Add(this.enableHeader);
            this.headerPanel.Controls.Add(this.headerTextBox);
            this.headerPanel.Dock = System.Windows.Forms.DockStyle.Top;
            this.headerPanel.Location = new System.Drawing.Point(0, 0);
            this.headerPanel.Name = "headerPanel";
            this.headerPanel.Size = new System.Drawing.Size(665, 32);
            this.headerPanel.TabIndex = 1;
            // 
            // MainWindow
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(7F, 15F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.BackColor = System.Drawing.SystemColors.Window;
            this.ClientSize = new System.Drawing.Size(914, 586);
            this.Controls.Add(this.panelRight);
            this.Controls.Add(this.panelLeft);
            this.HelpButton = true;
            this.Margin = new System.Windows.Forms.Padding(27, 22, 27, 22);
            this.Name = "MainWindow";
            this.Text = "winprint";
            this.FormClosing += new System.Windows.Forms.FormClosingEventHandler(this.MainWindow_FormClosing);
            this.Load += new System.EventHandler(this.MainWindow_Load);
            this.Layout += new System.Windows.Forms.LayoutEventHandler(this.MainWindow_Layout);
            this.panelLeft.ResumeLayout(false);
            this.groupBox1.ResumeLayout(false);
            this.groupBox1.PerformLayout();
            this.groupPages.ResumeLayout(false);
            this.groupPages.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)(this.padding)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.columns)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.rows)).EndInit();
            this.groupMargins.ResumeLayout(false);
            this.groupMargins.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)(this.leftMargin)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.rightMargin)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.bottomMargin)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.topMargin)).EndInit();
            this.panelAbout.ResumeLayout(false);
            this.panelAbout.PerformLayout();
            this.printerGroup.ResumeLayout(false);
            this.printerGroup.PerformLayout();
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
        private ComboBox comboBoxSheet;
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
        private TextBox toText;
        private Label label1;
        private TextBox fromText;
        private Label fromLabel;
        private Label pagesLabel;
        private Button settingsButton;
        private GroupBox printerGroup;
        private GroupBox groupBox1;
        private Panel panelAbout;
        private Label versionLabel;
        private LinkLabel wikiLink;
    }
}


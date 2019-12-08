﻿using System;
using System.Windows.Forms;

namespace WinPrint
{
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
            this.previewButton = new System.Windows.Forms.Button();
            this.pageUp = new System.Windows.Forms.Button();
            this.pageDown = new System.Windows.Forms.Button();
            this.headerTextBox = new System.Windows.Forms.TextBox();
            this.enableHeader = new System.Windows.Forms.CheckBox();
            this.footerTextBox = new System.Windows.Forms.TextBox();
            this.enableFooter = new System.Windows.Forms.CheckBox();
            this.panelLeft = new System.Windows.Forms.Panel();
            this.panelRight = new System.Windows.Forms.Panel();
            this.labelPaper = new System.Windows.Forms.Label();
            this.labelPrinter = new System.Windows.Forms.Label();
            this.comboBoxSheet = new System.Windows.Forms.ComboBox();
            this.labelSheet = new System.Windows.Forms.Label();
            this.headerTextBox.SuspendLayout();
            this.footerTextBox.SuspendLayout();
            this.panelLeft.SuspendLayout();
            this.panelRight.SuspendLayout();
            this.SuspendLayout();
            // 
            // dummyButton
            // 
            this.dummyButton.BackColor = System.Drawing.SystemColors.Window;
            this.dummyButton.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.dummyButton.Location = new System.Drawing.Point(149, 257);
            this.dummyButton.Margin = new System.Windows.Forms.Padding(0);
            this.dummyButton.Name = "dummyButton";
            this.dummyButton.Size = new System.Drawing.Size(345, 263);
            this.dummyButton.TabIndex = 0;
            this.dummyButton.Text = "dummyButton";
            this.dummyButton.UseVisualStyleBackColor = false;
            // 
            // printersCB
            // 
            this.printersCB.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
            this.printersCB.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.printersCB.FormattingEnabled = true;
            this.printersCB.Location = new System.Drawing.Point(13, 630);
            this.printersCB.Margin = new System.Windows.Forms.Padding(3, 2, 3, 2);
            this.printersCB.Name = "printersCB";
            this.printersCB.Size = new System.Drawing.Size(227, 23);
            this.printersCB.TabIndex = 2;
            this.printersCB.SelectedIndexChanged += new System.EventHandler(this.printersCB_SelectedIndexChanged);
            // 
            // paperSizesCB
            // 
            this.paperSizesCB.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
            this.paperSizesCB.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.paperSizesCB.FormattingEnabled = true;
            this.paperSizesCB.Location = new System.Drawing.Point(12, 672);
            this.paperSizesCB.Margin = new System.Windows.Forms.Padding(3, 2, 3, 2);
            this.paperSizesCB.Name = "paperSizesCB";
            this.paperSizesCB.Size = new System.Drawing.Size(228, 23);
            this.paperSizesCB.TabIndex = 3;
            this.paperSizesCB.SelectedIndexChanged += new System.EventHandler(this.paperSizesCB_SelectedIndexChanged);
            // 
            // landscapeCheckbox
            // 
            this.landscapeCheckbox.AutoSize = true;
            this.landscapeCheckbox.Location = new System.Drawing.Point(23, 63);
            this.landscapeCheckbox.Margin = new System.Windows.Forms.Padding(3, 2, 3, 2);
            this.landscapeCheckbox.Name = "landscapeCheckbox";
            this.landscapeCheckbox.Size = new System.Drawing.Size(82, 19);
            this.landscapeCheckbox.TabIndex = 4;
            this.landscapeCheckbox.Text = "&Landscape";
            this.landscapeCheckbox.UseVisualStyleBackColor = true;
            this.landscapeCheckbox.CheckedChanged += new System.EventHandler(this.landscapeCheckbox_CheckedChanged);
            // 
            // printButton
            // 
            this.printButton.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
            this.printButton.Location = new System.Drawing.Point(13, 710);
            this.printButton.Margin = new System.Windows.Forms.Padding(3, 2, 3, 2);
            this.printButton.Name = "printButton";
            this.printButton.Size = new System.Drawing.Size(109, 28);
            this.printButton.TabIndex = 5;
            this.printButton.Text = "&Print...";
            this.printButton.UseVisualStyleBackColor = true;
            this.printButton.Click += new System.EventHandler(this.printButton_Click);
            // 
            // previewButton
            // 
            this.previewButton.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
            this.previewButton.Location = new System.Drawing.Point(140, 710);
            this.previewButton.Margin = new System.Windows.Forms.Padding(3, 2, 3, 2);
            this.previewButton.Name = "previewButton";
            this.previewButton.Size = new System.Drawing.Size(99, 28);
            this.previewButton.TabIndex = 6;
            this.previewButton.Text = "P&review...";
            this.previewButton.UseVisualStyleBackColor = true;
            this.previewButton.Click += new System.EventHandler(this.previewButton_Click);
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
            this.headerTextBox.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.headerTextBox.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.headerTextBox.Controls.Add(this.enableHeader);
            this.headerTextBox.Location = new System.Drawing.Point(6, 9);
            this.headerTextBox.Margin = new System.Windows.Forms.Padding(3, 2, 3, 2);
            this.headerTextBox.Name = "headerTextBox";
            this.headerTextBox.Size = new System.Drawing.Size(606, 23);
            this.headerTextBox.TabIndex = 7;
            this.headerTextBox.TextAlign = System.Windows.Forms.HorizontalAlignment.Center;
            this.headerTextBox.TextChanged += new System.EventHandler(this.headerTextBox_TextChanged);
            // 
            // enableHeader
            // 
            this.enableHeader.AutoSize = true;
            this.enableHeader.Location = new System.Drawing.Point(5, 3);
            this.enableHeader.Name = "enableHeader";
            this.enableHeader.Size = new System.Drawing.Size(64, 19);
            this.enableHeader.TabIndex = 9;
            this.enableHeader.Text = "&Header";
            this.enableHeader.UseVisualStyleBackColor = true;
            this.enableHeader.CheckedChanged += new System.EventHandler(this.enableHeader_CheckedChanged);
            // 
            // footerTextBox
            // 
            this.footerTextBox.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.footerTextBox.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.footerTextBox.Controls.Add(this.enableFooter);
            this.footerTextBox.Location = new System.Drawing.Point(6, 716);
            this.footerTextBox.Margin = new System.Windows.Forms.Padding(10);
            this.footerTextBox.Name = "footerTextBox";
            this.footerTextBox.Size = new System.Drawing.Size(606, 23);
            this.footerTextBox.TabIndex = 8;
            this.footerTextBox.TextAlign = System.Windows.Forms.HorizontalAlignment.Center;
            this.footerTextBox.TextChanged += new System.EventHandler(this.footerTextBox_TextChanged);
            // 
            // enableFooter
            // 
            this.enableFooter.AutoSize = true;
            this.enableFooter.Location = new System.Drawing.Point(5, 3);
            this.enableFooter.Name = "enableFooter";
            this.enableFooter.Size = new System.Drawing.Size(60, 19);
            this.enableFooter.TabIndex = 9;
            this.enableFooter.Text = "&Footer";
            this.enableFooter.UseVisualStyleBackColor = true;
            this.enableFooter.CheckedChanged += new System.EventHandler(this.enableFooter_CheckedChanged);
            // 
            // panelLeft
            // 
            this.panelLeft.BackColor = System.Drawing.SystemColors.Control;
            this.panelLeft.Controls.Add(this.previewButton);
            this.panelLeft.Controls.Add(this.paperSizesCB);
            this.panelLeft.Controls.Add(this.printersCB);
            this.panelLeft.Controls.Add(this.labelSheet);
            this.panelLeft.Controls.Add(this.landscapeCheckbox);
            this.panelLeft.Controls.Add(this.comboBoxSheet);
            this.panelLeft.Controls.Add(this.labelPrinter);
            this.panelLeft.Controls.Add(this.labelPaper);
            this.panelLeft.Controls.Add(this.printButton);
            this.panelLeft.Dock = System.Windows.Forms.DockStyle.Left;
            this.panelLeft.ForeColor = System.Drawing.Color.Black;
            this.panelLeft.Location = new System.Drawing.Point(0, 0);
            this.panelLeft.Name = "panelLeft";
            this.panelLeft.Size = new System.Drawing.Size(252, 749);
            this.panelLeft.TabIndex = 9;
            // 
            // panelRight
            // 
            this.panelRight.Controls.Add(this.footerTextBox);
            this.panelRight.Controls.Add(this.headerTextBox);
            this.panelRight.Controls.Add(this.dummyButton);
            this.panelRight.Dock = System.Windows.Forms.DockStyle.Fill;
            this.panelRight.Location = new System.Drawing.Point(252, 0);
            this.panelRight.Name = "panelRight";
            this.panelRight.Size = new System.Drawing.Size(618, 749);
            this.panelRight.TabIndex = 10;
            this.panelRight.Resize += new System.EventHandler(this.panelRight_Resize);
            // 
            // labelPaper
            // 
            this.labelPaper.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
            this.labelPaper.AutoSize = true;
            this.labelPaper.Location = new System.Drawing.Point(13, 655);
            this.labelPaper.Name = "labelPaper";
            this.labelPaper.Size = new System.Drawing.Size(40, 15);
            this.labelPaper.TabIndex = 7;
            this.labelPaper.Text = "P&aper:";
            // 
            // labelPrinter
            // 
            this.labelPrinter.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
            this.labelPrinter.AutoSize = true;
            this.labelPrinter.Location = new System.Drawing.Point(13, 613);
            this.labelPrinter.Name = "labelPrinter";
            this.labelPrinter.Size = new System.Drawing.Size(45, 15);
            this.labelPrinter.TabIndex = 7;
            this.labelPrinter.Text = "&Printer:";
            // 
            // comboBoxSheet
            // 
            this.comboBoxSheet.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.comboBoxSheet.FormattingEnabled = true;
            this.comboBoxSheet.Location = new System.Drawing.Point(12, 35);
            this.comboBoxSheet.Name = "comboBoxSheet";
            this.comboBoxSheet.Size = new System.Drawing.Size(227, 23);
            this.comboBoxSheet.TabIndex = 8;
            this.comboBoxSheet.SelectedIndexChanged += new System.EventHandler(this.comboBoxSheet_SelectedIndexChanged);
            // 
            // labelSheet
            // 
            this.labelSheet.AutoSize = true;
            this.labelSheet.Location = new System.Drawing.Point(12, 17);
            this.labelSheet.Name = "labelSheet";
            this.labelSheet.Size = new System.Drawing.Size(78, 15);
            this.labelSheet.TabIndex = 9;
            this.labelSheet.Text = "&Sheet Design:";
            // 
            // MainWindow
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(7F, 15F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.BackColor = System.Drawing.SystemColors.ControlDark;
            this.ClientSize = new System.Drawing.Size(870, 749);
            this.Controls.Add(this.panelRight);
            this.Controls.Add(this.panelLeft);
            this.Margin = new System.Windows.Forms.Padding(26, 22, 26, 22);
            this.Name = "MainWindow";
            this.Text = "Winprint";
            this.FormClosing += new System.Windows.Forms.FormClosingEventHandler(this.MainWindow_FormClosing);
            this.Load += new System.EventHandler(this.MainWindow_Load);
            this.Layout += new System.Windows.Forms.LayoutEventHandler(this.MainWindow_Layout);
            this.headerTextBox.ResumeLayout(false);
            this.headerTextBox.PerformLayout();
            this.footerTextBox.ResumeLayout(false);
            this.footerTextBox.PerformLayout();
            this.panelLeft.ResumeLayout(false);
            this.panelLeft.PerformLayout();
            this.panelRight.ResumeLayout(false);
            this.panelRight.PerformLayout();
            this.ResumeLayout(false);

        }

        #endregion

        private Button dummyButton;
        private ComboBox printersCB;
        private ComboBox paperSizesCB;
        private CheckBox landscapeCheckbox;
        private Button printButton;
        private Button previewButton;
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
    }
}


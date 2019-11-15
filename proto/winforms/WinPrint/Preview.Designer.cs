using System;
using System.Windows.Forms;

namespace WinPrint
{
    partial class Preview
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
            this.footerTextBox = new System.Windows.Forms.TextBox();
            // 
            // dummyButton
            // 
            this.dummyButton.Anchor = System.Windows.Forms.AnchorStyles.None;
            this.dummyButton.BackColor = System.Drawing.SystemColors.Window;
            this.dummyButton.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.dummyButton.Location = new System.Drawing.Point(247, 275);
            this.dummyButton.Margin = new System.Windows.Forms.Padding(0);
            this.dummyButton.Name = "dummyButton";
            this.dummyButton.Size = new System.Drawing.Size(354, 215);
            this.dummyButton.TabIndex = 0;
            this.dummyButton.Text = "dummyButton";
            this.dummyButton.UseVisualStyleBackColor = false;
            // 
            // printersCB
            // 
            this.printersCB.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.printersCB.FormattingEnabled = true;
            this.printersCB.Location = new System.Drawing.Point(10, 72);
            this.printersCB.Name = "printersCB";
            this.printersCB.Size = new System.Drawing.Size(450, 23);
            this.printersCB.TabIndex = 1;
            this.printersCB.SelectedIndexChanged += new System.EventHandler(this.printersCB_SelectedIndexChanged);
            // 
            // paperSizesCB
            // 
            this.paperSizesCB.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.paperSizesCB.FormattingEnabled = true;
            this.paperSizesCB.Location = new System.Drawing.Point(10, 101);
            this.paperSizesCB.Name = "paperSizesCB";
            this.paperSizesCB.Size = new System.Drawing.Size(450, 23);
            this.paperSizesCB.TabIndex = 2;
            this.paperSizesCB.SelectedIndexChanged += new System.EventHandler(this.paperSizesCB_SelectedIndexChanged);
            // 
            // landscapeCheckbox
            // 
            this.landscapeCheckbox.AutoSize = true;
            this.landscapeCheckbox.Location = new System.Drawing.Point(10, 135);
            this.landscapeCheckbox.Name = "landscapeCheckbox";
            this.landscapeCheckbox.Size = new System.Drawing.Size(82, 19);
            this.landscapeCheckbox.TabIndex = 3;
            this.landscapeCheckbox.Text = "&Landscape";
            this.landscapeCheckbox.UseVisualStyleBackColor = true;
            this.landscapeCheckbox.CheckedChanged += new System.EventHandler(this.landscapeCheckbox_CheckedChanged);
            // 
            // printButton
            // 
            this.printButton.Location = new System.Drawing.Point(10, 165);
            this.printButton.Name = "printButton";
            this.printButton.Size = new System.Drawing.Size(75, 26);
            this.printButton.TabIndex = 4;
            this.printButton.Text = "&Print...";
            this.printButton.UseVisualStyleBackColor = true;
            this.printButton.Click += new System.EventHandler(this.printButton_Click);
            // 
            // previewButton
            // 
            this.previewButton.Location = new System.Drawing.Point(91, 164);
            this.previewButton.Name = "previewButton";
            this.previewButton.Size = new System.Drawing.Size(75, 28);
            this.previewButton.TabIndex = 4;
            this.previewButton.Text = "P&review...";
            this.previewButton.UseVisualStyleBackColor = true;
            this.previewButton.Click += new System.EventHandler(this.previewButton_Click);
            // 
            // pageUp
            // 
            this.pageUp.Anchor = System.Windows.Forms.AnchorStyles.Bottom;
            this.pageUp.Location = new System.Drawing.Point(349, 970);
            this.pageUp.Name = "pageUp";
            this.pageUp.Size = new System.Drawing.Size(94, 29);
            this.pageUp.TabIndex = 5;
            this.pageUp.Text = "Page &up";
            this.pageUp.UseVisualStyleBackColor = true;
            this.pageUp.Click += new System.EventHandler(this.pageUp_Click);
            // 
            // pageDown
            // 
            this.pageDown.Anchor = System.Windows.Forms.AnchorStyles.Bottom;
            this.pageDown.Location = new System.Drawing.Point(444, 970);
            this.pageDown.Name = "pageDown";
            this.pageDown.Size = new System.Drawing.Size(94, 29);
            this.pageDown.TabIndex = 5;
            this.pageDown.Text = "Page &down";
            this.pageDown.UseVisualStyleBackColor = true;
            this.pageDown.Click += new System.EventHandler(this.pageDown_Click);
            // 
            // headerTextBox
            // 
            this.headerTextBox.Dock = System.Windows.Forms.DockStyle.Top;
            this.headerTextBox.Location = new System.Drawing.Point(0, 0);
            this.headerTextBox.Name = "headerTextBox";
            this.headerTextBox.Size = new System.Drawing.Size(994, 23);
            this.headerTextBox.TabIndex = 6;
            this.headerTextBox.TextAlign = System.Windows.Forms.HorizontalAlignment.Center;
            this.headerTextBox.TextChanged += new System.EventHandler(this.headerTextBox_TextChanged);
            // 
            // footerTextBox
            // 
            this.footerTextBox.Dock = System.Windows.Forms.DockStyle.Bottom;
            this.footerTextBox.Location = new System.Drawing.Point(0, 976);
            this.footerTextBox.Name = "footerTextBox";
            this.footerTextBox.Size = new System.Drawing.Size(994, 23);
            this.footerTextBox.TabIndex = 7;
            this.footerTextBox.TextAlign = System.Windows.Forms.HorizontalAlignment.Center;
            // 
            // Preview
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(7F, 15F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.BackColor = System.Drawing.SystemColors.ControlDark;
            this.ClientSize = new System.Drawing.Size(994, 999);
            this.Controls.Add(this.footerTextBox);
            this.Controls.Add(this.headerTextBox);
            this.Controls.Add(this.pageDown);
            this.Controls.Add(this.pageUp);
            this.Controls.Add(this.previewButton);
            this.Controls.Add(this.printButton);
            this.Controls.Add(this.landscapeCheckbox);
            this.Controls.Add(this.paperSizesCB);
            this.Controls.Add(this.printersCB);
            this.Controls.Add(this.dummyButton);
            this.Margin = new System.Windows.Forms.Padding(30);
            this.Name = "Preview";
            this.Text = "Preview";
            this.Load += new System.EventHandler(this.Preview_Load);
            this.Layout += new System.Windows.Forms.LayoutEventHandler(this.Preview_Layout);

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
    }
}


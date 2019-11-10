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

        /// <summary>
        ///  Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

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
            // 
            // dummyButton
            // 
            this.dummyButton.Anchor = System.Windows.Forms.AnchorStyles.None;
            this.dummyButton.BackColor = System.Drawing.SystemColors.Window;
            this.dummyButton.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.dummyButton.Location = new System.Drawing.Point(262, 288);
            this.dummyButton.Margin = new System.Windows.Forms.Padding(30);
            this.dummyButton.Name = "dummyButton";
            this.dummyButton.Size = new System.Drawing.Size(354, 215);
            this.dummyButton.TabIndex = 0;
            this.dummyButton.Text = "dummyButton";
            this.dummyButton.UseVisualStyleBackColor = false;
            // 
            // printersCB
            // 
            this.printersCB.FormattingEnabled = true;
            this.printersCB.Location = new System.Drawing.Point(10, 10);
            this.printersCB.Name = "printersCB";
            this.printersCB.Size = new System.Drawing.Size(450, 23);
            this.printersCB.TabIndex = 1;
            this.printersCB.SelectedIndexChanged += new System.EventHandler(this.printersCB_SelectedIndexChanged);
            // 
            // paperSizesCB
            // 
            this.paperSizesCB.FormattingEnabled = true;
            this.paperSizesCB.Location = new System.Drawing.Point(10, 39);
            this.paperSizesCB.Name = "paperSizesCB";
            this.paperSizesCB.Size = new System.Drawing.Size(450, 23);
            this.paperSizesCB.TabIndex = 2;
            this.paperSizesCB.SelectedIndexChanged += new System.EventHandler(this.paperSizesCB_SelectedIndexChanged);
            // 
            // landscapeCheckbox
            // 
            this.landscapeCheckbox.AutoSize = true;
            this.landscapeCheckbox.Location = new System.Drawing.Point(10, 73);
            this.landscapeCheckbox.Name = "landscapeCheckbox";
            this.landscapeCheckbox.Size = new System.Drawing.Size(82, 19);
            this.landscapeCheckbox.TabIndex = 3;
            this.landscapeCheckbox.Text = "Landscape";
            this.landscapeCheckbox.UseVisualStyleBackColor = true;
            this.landscapeCheckbox.CheckedChanged += new System.EventHandler(this.landscapeCheckbox_CheckedChanged);
            // 
            // printButton
            // 
            this.printButton.Location = new System.Drawing.Point(15, 99);
            this.printButton.Name = "printButton";
            this.printButton.Size = new System.Drawing.Size(75, 23);
            this.printButton.TabIndex = 4;
            this.printButton.Text = "&Print...";
            this.printButton.UseVisualStyleBackColor = true;
            this.printButton.Click += new System.EventHandler(this.printButton_Click);
            // 
            // previewButton
            // 
            this.previewButton.Location = new System.Drawing.Point(15, 128);
            this.previewButton.Name = "previewButton";
            this.previewButton.Size = new System.Drawing.Size(75, 23);
            this.previewButton.TabIndex = 4;
            this.previewButton.Text = "P&review...";
            this.previewButton.UseVisualStyleBackColor = true;
            this.previewButton.Click += new System.EventHandler(this.previewButton_Click);
            // 
            // Preview
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(7F, 15F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.BackColor = System.Drawing.SystemColors.ControlDark;
            this.ClientSize = new System.Drawing.Size(1024, 1024);
            this.Controls.Add(this.previewButton);
            this.Controls.Add(this.printButton);
            this.Controls.Add(this.landscapeCheckbox);
            this.Controls.Add(this.paperSizesCB);
            this.Controls.Add(this.printersCB);
            this.Controls.Add(this.dummyButton);
            this.Name = "Preview";
            this.Text = "Preview";
            this.Layout += new System.Windows.Forms.LayoutEventHandler(this.Preview_Layout);

        }

        #endregion

        private Button dummyButton;
        private ComboBox printersCB;
        private ComboBox paperSizesCB;
        private CheckBox landscapeCheckbox;
        private Button printButton;
        private Button previewButton;
    }
}


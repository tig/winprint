using System;
using System.Collections.Generic;
using System.Text;

namespace WinPrint.Core.Models {
    /// <summary>
    /// Model for page content (properties that impact how content w/in a page is printed).
    /// Each Sheet prints sheet.Columns by sheet.Rows Pages. 
    /// </summary>
    public class ContentSettings : ModelBase {

        /// <summary>
        /// Font used for content. Will override any content font settings specified by a ContentType provider.
        /// </summary>
        public Core.Models.Font Font { get => font; set => SetField(ref font, value); }
        private Core.Models.Font font = new Font();

        /// <summary>
        /// if True, print content background, if present. Otherwise, all backgrounds will be paper color.
        /// </summary>
        public bool PrintBackground { get => printBackground; set => SetField(ref printBackground, value); }
        private bool printBackground = true;

        /// <summary>
        /// If True, all content will be printed in grayscale. Use Darkness property to change how
        /// dark the grey is.
        /// </summary>
        public bool Grayscale { get => grayscale; set => SetField(ref grayscale, value); }
        private bool grayscale = false;

        /// <summary>
        /// Darkness factor. 0 = RGB. 100 = black.
        /// </summary>
        public int Darkness { get => darkness; set => SetField(ref darkness, value); }
        private int darkness = 0;

        /// <summary>
        /// If true, content will be drawn with diagnostic info and/or rules.
        /// </summary>
        public bool Diagnostics { get => diagnostics; set => SetField(ref diagnostics, value); }
        private bool diagnostics = false;
    }
}

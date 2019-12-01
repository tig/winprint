﻿using System;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Printing;
using System.IO;

namespace WinPrint {
    /// <summary>
    /// Represents a single page to be printed. Each page in a doucument is different.
    /// In MLP all pages use the same paper and landscape mode. 
    /// Knows how to paint a page (TODO: Separate view/models).
    /// </summary>
    sealed public class Page {
        private SheetViewModel svm;
        public SheetViewModel SheetViewModel { get => svm; }
        public int PageNum { get; internal set; }
        /// <summary>
        /// An object holding the content for this page to be printed. 
        /// Typically a string for a Text based file
        /// </summary>

        public Page(SheetViewModel svm) {

            this.svm = svm;
        }

    } 
}


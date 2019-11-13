using System;
using System.Collections.Generic;
using System.Text;

namespace WinPrint {
    /// <summary>
    /// Represents a document to be printed. Holds document specific data.
    /// </summary>
    public class Document {
        private string file;
        private List<Page> pages;

        public Document() {
            pages = new List<Page>();
        }

        public string File { get => file; set => file = value; }
        public List<Page> Pages { get => pages; }
    }
}

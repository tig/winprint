using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Text;

namespace WinPrint.Core.ContentTypes {
    /// <summary>
    /// base class for Content/File types
    /// </summary>
    public abstract class ContentBase {

        public abstract string GetType();

        /// <summary>
        /// Calculated page size. Set by Sheet view model.
        /// </summary>
        public SizeF PageSize;

        /// <summary>
        /// Default content font for this content type
        /// </summary>
        public WinPrint.Core.Models.Font Font { get;set; }

        internal int numPages = 0;
        public int GetNumPages() { return numPages;  }
 
        /// <summary>
        /// Get total count of pages. Set any local page-size related values (e.g. linesPerPage).
        /// </summary>
        /// <param name="e"></param>
        /// <returns></returns>
        public abstract int CountPages(StreamReader streamToPrint);

        /// <summary>
        /// Paints a single page
        /// </summary>
        /// <param name="g">Graphics with 0,0 being the origin of the Page</param>
        /// <param name="pageNum">Page number to print</param>
        public abstract void PaintPage(Graphics g, int pageNum);

    }
}

using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Text;

namespace WinPrint {
    /// <summary>
    /// base class for Content/File types
    /// </summary>
    public abstract class ContentBase {

        /// <summary>
        /// Get total count of pages. Set any local page-size related values (e.g. linesPerPage)
        /// </summary>
        /// <param name="e"></param>
        /// <returns></returns>
        internal abstract List<Page> GetPages(StreamReader streamToPrint);

        /// <summary>
        /// Paints a single page
        /// </summary>
        /// <param name="g">Graphics with 0,0 being the origin of the Page</param>
        /// <param name="pageNum">Page number to print</param>
        internal abstract void PaintPage(Graphics g, int pageNum);

    }
}

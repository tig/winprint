using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using TheArtOfDev.HtmlRenderer.WinForms;

namespace WinPrint.Core.ContentTypes {
    /// <summary>
    /// Implements generic HTML file type support. 
    /// </summary>
    public class HtmlFileContent : ContentBase, IDisposable  {

        public static string Type = "text/html";

        //public HtmlFileContent() {
        //    type = "text/html";
        //}
        public void Dispose() {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        // Protected implementation of Dispose pattern.
        // Flag: Has Dispose already been called?
        bool disposed = false;
        void Dispose(bool disposing) {
            if (disposed)
                return;

            if (disposing) {
            }
            disposed = true;
        }

        private string html;


        /// <summary>
        /// Get total count of pages. Set any local page-size related values (e.g. linesPerPage)
        /// </summary>
        /// <param name="e"></param>
        /// <returns></returns>
        public override int CountPages(StreamReader streamToPrint) {
            html = streamToPrint.ReadToEnd();
            using Bitmap bitmap = new Bitmap(1, 1);
            Graphics g = Graphics.FromImage(bitmap);
            //g.PageUnit = GraphicsUnit.Document;

            SizeF size = HtmlRender.MeasureGdiPlus(g, html, PageSize.Width);
            
            int maxPages = (int)(size.Height / PageSize.Height);
            for (numPages = 0; numPages < maxPages; numPages++) { 

            }

            return numPages;
        }

        /// <summary>
        /// Gets next page from stream. Returns false if no more pages
        /// </summary>
        /// <param name="streamToPrint"></param>
        /// <param name="page"></param>
        /// <returns></returns>
        // TODO: Deal with PageFeeds
        // TODO: Support different forms of linewrap (truncate/clip, word, line)
        // TODO: Support custom line wrap symbol
        //private bool NextPage(StreamReader streamToPrint, out Page page) {
        //    page = new Page(containingSheet);

        //    // define context used for determining glyph metrics.        
        //    using Bitmap bitmap = new Bitmap(1, 1);
        //    Graphics g = Graphics.FromImage(bitmap);
        //    //g = Graphics.FromHwnd(PrintPreview.Instance.Handle);
        //    g.PageUnit = GraphicsUnit.Document;


        //    SizeF size = HtmlRender.MeasureGdiPlus(g, html, containingSheet.GetPageWidth());

        //    return false;
        //}

        /// <summary>
        /// Paints a single page
        /// </summary>
        /// <param name="g">Graphics with 0,0 being the origin of the Page</param>
        /// <param name="pageNum">Page number to print</param>
        public override void PaintPage(Graphics g, int pageNum) {
            float leftMargin = 0;
            float contentHeight = PageSize.Height;

            SizeF size = new SizeF(PageSize.Width, PageSize.Height);

            SizeF renderedSize = HtmlRender.RenderGdiPlus(g, html, new PointF(0, 0), size );
            
        }
    }
}

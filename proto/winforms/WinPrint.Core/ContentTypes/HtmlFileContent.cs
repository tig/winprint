using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using TheArtOfDev.HtmlRenderer.WinForms;

namespace WinPrint.Core.ContentTypes {
    /// <summary>
    /// Implements generic HTML file type support. 
    /// </summary>
    public class HtmlFileContent : ContentBase, IDisposable {

        public static string Type = "text/html";
        private Bitmap htmlBitmap;
        //private Image htmlImage;

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
                if (htmlBitmap != null)
                    htmlBitmap.Dispose();
            }
            disposed = true;
        }

        private string html;


        /// <summary>
        /// Get total count of pages. Set any local page-size related values (e.g. linesPerPage)
        /// </summary>
        /// <param name="e"></param>
        /// <returns></returns>
        public override int CountPages(StreamReader streamToPrint, System.Drawing.Printing.PrinterResolution printerResolution) {
            html = streamToPrint.ReadToEnd();

            //// Do this in pixel units
            htmlBitmap = new Bitmap(10000, 10000);
            htmlBitmap.SetResolution(printerResolution.X, printerResolution.Y);
            var g = Graphics.FromImage(htmlBitmap);
            Debug.WriteLine($"HtmlFileContent.CountPages - sizing htmlBitmap is {htmlBitmap.Width}x{htmlBitmap.Height}");

            g.PageUnit = GraphicsUnit.Pixel;
            float measureWidth = PageSize.Width / 100 * printerResolution.X;
            Debug.WriteLine($"HtmlFileContent.CountPages - measure width: {measureWidth}");
            SizeF size = HtmlRender.MeasureGdiPlus(g, html, measureWidth);
            Debug.WriteLine($"HtmlFileContent.CountPages - size is {size.Width}x{size.Height}.");
            //htmlBitmap = new Bitmap((int)size.Width, (int)size.Height);
            //htmlBitmap.SetResolution(printerResolution.X, printerResolution.Y);

            //g = Graphics.FromImage(htmlBitmap);
            //g.PageUnit = GraphicsUnit.Pixel;
            //htmlImage = HtmlRender.RenderToImageGdiPlus(html, maxWidth: (int)size.Width);
            SizeF renderedSize = HtmlRender.RenderGdiPlus(g, html, new PointF(0, 0), size);
            
            Debug.WriteLine($"HtmlFileContent.CountPages - htmlImage is {htmlBitmap.Width}x{htmlBitmap.Height} pixels.");

            // PageSize is in 100ths of inch. 
            int maxPages = (int)(htmlBitmap.Height / (PageSize.Height)) + 1;
            Debug.WriteLine($"HtmlFileContent.CountPages - {maxPages} pages.");
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
            SizeF pagesizeInPixels;
            if (g.PageUnit == GraphicsUnit.Display) {
                // Print
                pagesizeInPixels = new SizeF(PageSize.Width, PageSize.Height);
            }
            else {
                // Preview
                pagesizeInPixels = new SizeF(PageSize.Width / 100 * g.DpiX, PageSize.Height / 100 * g.DpiY);
            }


            int yPosInBitmap = (pageNum - 1) * (int)(htmlBitmap.Height / numPages);
            Debug.WriteLine($"HtmlFileContent.PaintPage {pageNum} - yPos is {yPosInBitmap}.");


            var state = g.Save();
            //g.PageUnit = GraphicsUnit.Pixel;

            g.DrawImage(htmlBitmap, new Rectangle(0, 0, (int)PageSize.Width, (int)PageSize.Height),
                0, yPosInBitmap,
                (int)(htmlBitmap.Width), (int)(htmlBitmap.Height / numPages),
                GraphicsUnit.Pixel);

            g.Restore(state);
            g.DrawRectangle(Pens.Red, new Rectangle(0, 0, (int)PageSize.Width, (int)PageSize.Height));

        }
    }
}

using System;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Net.Http;
using System.Threading.Tasks;
using LiteHtmlSharp;
using WinPrint.Core.Models;
using WinPrint.Core.Services;
using WinPrint.LiteHtml;

namespace WinPrint.Core.ContentTypes {

    /// <summary>
    /// Implements generic HTML file type support. 
    /// </summary>
    public class HtmlFileContent : ContentBase, IDisposable {
        public static new string ContentType = "text/html";

        public static HtmlFileContent Create() {
            var content = new HtmlFileContent();
            content.CopyPropertiesFrom(ModelLocator.Current.Settings.HtmlFileSettings);
            return content;
        }

        internal GDIPlusContainer litehtml;

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
                //if (litehtml != null)
                //    litehtml.Dispose();
            }
            disposed = true;
        }

        private Bitmap htmlBitmap;

        public Models.Font MonspacedFont { get; internal set; }

        public async override Task<bool> LoadAsync(string filePath) {
            litehtml = null;
            htmlBitmap = null;
            return await base.LoadAsync(filePath);
        }

        /// <summary>
        /// Get total count of pages. Set any local page-size related values (e.g. linesPerPage)
        /// </summary>
        /// <param name="e"></param>
        /// <returns></returns>
        public override async Task<int> RenderAsync(System.Drawing.Printing.PrinterResolution printerResolution, EventHandler<string> reflowProgress) {
            LogService.TraceMessage();
            reflowProgress?.Invoke(this, "HtmlFileContent.RenderAsync");
            await base.RenderAsync(printerResolution, reflowProgress);

            int width = (int)PageSize.Width;// (printerResolution.X * PageSize.Width / 100);
            int height = (int)PageSize.Height;// (printerResolution.Y * PageSize.Height / 100);
            LogService.TraceMessage($"HtmlFileContent.RenderAsync - Page size: {width}x{height} @ {printerResolution.X}x{printerResolution.Y} dpi");

            string css;
            try {
                // TODO: Make sure wiprint.css is in the same dir as .config file once setup is impl
                using StreamReader cssStream = new StreamReader("winprint.css");
                css = await cssStream.ReadToEndAsync();
                cssStream.Close();
                reflowProgress?.Invoke(this, "Read winprint.css");
            }
            catch {
                css = IncludedWinPrintCss.CssString;
            }

            var resources = new HtmlResources(filePath);
            litehtml = new GDIPlusContainer(css, resources.GetResourceString, resources.GetResourceBytes);
            litehtml.Size = new LiteHtmlSize(width, 0);
            litehtml.PageHeight = height;

            htmlBitmap = new Bitmap(width, height);
            //htmlBitmap.SetResolution(printerResolution.X, printerResolution.Y);
            var g = Graphics.FromImage(htmlBitmap);
            g.PageUnit = GraphicsUnit.Display;
            //g.FillRectangle(Brushes.LightYellow, new Rectangle(0, 0, width, height));
            LogService.TraceMessage($"HtmlFileContent.RenderAsync() Graphic is {htmlBitmap.Width}x{htmlBitmap.Height} @ {g.DpiX}x{g.DpiY} dpi. PageUnit = {g.PageUnit.ToString()}");
            litehtml.Graphics = g;
            LogService.TraceMessage($"PageUnit = {g.PageUnit.ToString()}");

            Logging.TraceMessage("litehtml.Document.CreateFromString(document)");
            reflowProgress?.Invoke(this, "litehtml.Document.CreateFromString(document)");
            litehtml.Document.CreateFromString(document);
            Logging.TraceMessage("back from litehtml.Document.CreateFromString(document)");
            reflowProgress?.Invoke(this, "back from litehtml.Document.CreateFromString(document)");
            //litehtml.Document.OnMediaChanged();
            //Logging.TraceMessage("back from litehtml.Document.OnMediaChanged");

            // TODO: Use return of Render() to get "best width"
            int bestWidth = litehtml.Document.Render((int)width);
            reflowProgress?.Invoke(this, "Done with Render");
            //litehtml.SetViewport(new LiteHtmlPoint(0, 0), new LiteHtmlSize(width, height));
            //litehtml.Draw();
            litehtml.Graphics = null;

            Logging.TraceMessage($"Litehtml_DocumentSizeKnown {litehtml.Document.Width()}x{litehtml.Document.Height()} bestWidth = {bestWidth}");

            var n = (int)(litehtml.Document.Height() / height) + 1;
            Logging.TraceMessage($"HtmlFileContent.RenderAsync - {n} pages.");

            return n;
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
            //if (pageNum > NumPages) {
            //    Logging.TraceMessage($"HtmlFileContent.PaintPage({pageNum}) when NumPages is {NumPages}");
            //    return;
            //}
            if (litehtml == null) {
                LogService.TraceMessage($"HtmlFileContent.PaintPage({pageNum}) when litehtml is null");
                return;
            }
            SizeF pagesizeInPixels;
            var state = g.Save();

            if (g.PageUnit == GraphicsUnit.Display) {
                // Print
                pagesizeInPixels = new SizeF(PageSize.Width, PageSize.Height);
            }
            else {
                // Preview
                pagesizeInPixels = new SizeF(PageSize.Width / 100 * g.DpiX, PageSize.Height / 100 * g.DpiY);
                g.PageUnit = GraphicsUnit.Display;
            }


            //if (litehtml.Graphics == null) {
            //    Logging.TraceMessage($"new print job. Rendering again");
            //    // This is a new print job. Re-render with new DPI
            //    litehtml.Graphics = g;
            //    litehtml.SetViewport(new LiteHtmlPoint(0, 0), new LiteHtmlSize(PageSize.Width, PageSize.Height));
            //    int bestWidth = litehtml.Document.Render((int)PageSize.Width);
            //}

            LogService.TraceMessage($"HtmlFileContent.PaintPage({pageNum} - {g.DpiX}x{g.DpiY} dpi. PageUnit = {g.PageUnit.ToString()})");

            litehtml.Graphics = g;

            int yPos = (pageNum - 1) * (int)Math.Round(PageSize.Height);
            g.SetClip(new Rectangle(0, 0, (int)Math.Round(PageSize.Width), (int)Math.Round(PageSize.Height)));

            LiteHtmlSize size = new LiteHtmlSize(Math.Round(PageSize.Width), Math.Round(PageSize.Height));
            litehtml.Document.Draw((int)-0, (int)-yPos, new position {
                x = 0,
                y = 0,
                width = (int)size.Width,
                height = (int)size.Height
            });
            //litehtml.SetViewport(new LiteHtmlPoint(0, yPos), new LiteHtmlSize(Math.Round(PageSize.Width), Math.Round(PageSize.Height)));
            //litehtml.ScrollOffset = new LiteHtmlPoint(0, yP
            // os);
            //litehtml.Draw();

            //g.DrawImage(htmlBitmap, 0, 0);
            //g.DrawRectangle(Pens.Red, new Rectangle(0, 0, (int)Math.Round(PageSize.Width), (int)Math.Round(PageSize.Height)));

            g.Restore(state);

        }
    }
}

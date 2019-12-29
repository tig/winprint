using System;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Net.Http;
using LiteHtmlSharp;
using WinPrint.LiteHtml;

namespace WinPrint.Core.ContentTypes {

    /// <summary>
    /// Implements generic HTML file type support. 
    /// </summary>
    public class HtmlFileContent : ContentBase, IDisposable {

        public static new string Type = "text/html";

        private GDIPlusContainer litehtml;

        HttpClient _httpClient;
        string _lastUrl;

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
                //litehtml.Dispose();
            }
            disposed = true;
        }

        private Bitmap htmlBitmap;

        public Models.Font MonspacedFont { get; internal set; }

        /// <summary>
        /// Get total count of pages. Set any local page-size related values (e.g. linesPerPage)
        /// </summary>
        /// <param name="e"></param>
        /// <returns></returns>
        public override int Render(string document, string title, System.Drawing.Printing.PrinterResolution printerResolution) {
            int width = (int)PageSize.Width;// (printerResolution.X * PageSize.Width / 100);
            int height = (int)PageSize.Height;// (printerResolution.Y * PageSize.Height / 100);
            Debug.WriteLine($"HtmlFileContent.CountPages - Page size: {width}x{height} @ {printerResolution.X}x{printerResolution.Y} dpi");

            string css;
            try {
                // TODO: Make sure wiprint.css is in the same dir as .config file once setup is impl
                using StreamReader cssStream = new StreamReader("winprint.css");
                css = cssStream.ReadToEnd();
                cssStream.Close();
            }
            catch {
                css = IncludedWinPrintCss.CssString;
            }

            litehtml = new GDIPlusContainer(css, GetResourceString, GetResourceBytes);
            litehtml.Size = new LiteHtmlSize(width, height);

            htmlBitmap = new Bitmap(width, height);
            //htmlBitmap.SetResolution(printerResolution.X, printerResolution.Y);
            var g = Graphics.FromImage(htmlBitmap);
            g.PageUnit = GraphicsUnit.Display;
            //g.FillRectangle(Brushes.LightYellow, new Rectangle(0, 0, width, height));
            Debug.WriteLine($"HtmlFileContent.CountPages() Graphic is {htmlBitmap.Width}x{htmlBitmap.Height} @ {g.DpiX}x{g.DpiY} dpi. PageUnit = {g.PageUnit.ToString()}");
            litehtml.Graphics = g;
            Debug.WriteLine($"PageUnit = {g.PageUnit.ToString()}");
            litehtml.Document.CreateFromString(document);
            litehtml.Document.OnMediaChanged();

            // TODO: Use return of Render() to get "best width"
            int bestWidth = litehtml.Document.Render((int)width);
            //litehtml.SetViewport(new LiteHtmlPoint(0, 0), new LiteHtmlSize(width, height));
            //litehtml.Draw();
            litehtml.Graphics = null;

            Debug.WriteLine($"Litehtml_DocumentSizeKnown {litehtml.Document.Width()}x{litehtml.Document.Height()} bestWidth = {bestWidth}");

            int maxPages = (int)(litehtml.Document.Height() / height) + 1;
            Debug.WriteLine($"HtmlFileContent.CountPages - {maxPages} pages.");
            for (numPages = 0; numPages < maxPages; numPages++) {

            }

            return numPages;
        }

        private byte[] GetResourceBytes(string resource) {
            if (string.IsNullOrWhiteSpace(resource)) {
                return new byte[0];
            }

            try {
                var url = GetUrlForRequest(resource);
                return _httpClient.GetByteArrayAsync(url).Result;
            }
            catch (Exception e) {
                Debug.WriteLine($"GetResourceString({resource} - {e.Message}");
                return null;
            }
        }

        private string GetResourceString(string resource) {
            string data = string.Empty;
            if (string.IsNullOrWhiteSpace(resource)) {
                return data;
            }
            try {
                if (resource.StartsWith("file:")) {
                    UriBuilder urlBuilder = new UriBuilder(resource);
                    using StreamReader reader = new StreamReader(urlBuilder.Path);
                    data = reader.ReadToEnd();
                }
                else {
                    var url = GetUrlForRequest(resource);
                    data = _httpClient.GetStringAsync(url).Result;
                }
                return data;
            }
            catch (Exception e) {
                Debug.WriteLine($"GetResourceString({resource} - {e.Message}");
                return data;
            }
        }

        private string GetUrlForRequest(string resource) {
            try {
                UriBuilder urlBuilder;

                if (resource.StartsWith("file:")) {
                    urlBuilder = new UriBuilder(resource);
                }
                else if (resource.StartsWith("//") || resource.StartsWith("http:") || resource.StartsWith("https:")) {
                    urlBuilder = new UriBuilder(resource.TrimStart(new char[] { '/' }));
                }
                else {
                    urlBuilder = new UriBuilder(_lastUrl);
                    urlBuilder.Path = resource;
                }
                var requestUrl = urlBuilder.ToString();
                return requestUrl;
            }
            catch {
                Debug.WriteLine($"GetUrlForReqeust({resource}) returning null.");
                return null;
            }
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
            //    Debug.WriteLine($"new print job. Rendering again");
            //    // This is a new print job. Re-render with new DPI
            //    litehtml.Graphics = g;
            //    litehtml.SetViewport(new LiteHtmlPoint(0, 0), new LiteHtmlSize(PageSize.Width, PageSize.Height));
            //    int bestWidth = litehtml.Document.Render((int)PageSize.Width);
            //}

            Debug.WriteLine($"HtmlFileContent.PaintPage({pageNum} - {g.DpiX}x{g.DpiY} dpi. PageUnit = {g.PageUnit.ToString()}");

            litehtml.Graphics = g;

            int yPos = (pageNum - 1) * (int)Math.Round(PageSize.Height);
            //g.SetClip(new Rectangle(0, 0, (int)Math.Round(PageSize.Width), (int)Math.Round(PageSize.Height)));

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

using System;
using System.Drawing;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using LiteHtmlSharp;
using Serilog;
using WinPrint.Core.Models;
using WinPrint.Core.Services;
using WinPrint.LiteHtml;

namespace WinPrint.Core.ContentTypeEngines {

    /// <summary>
    /// Implements generic HTML file type support. 
    /// </summary>
    public class HtmlCte : ContentTypeEngineBase, IDisposable {
        private static readonly string[] _supportedContentTypes = { "text/html" };
        public override string[] SupportedContentTypes => _supportedContentTypes;

        public static HtmlCte Create() {
            var content = new HtmlCte();
            content.CopyPropertiesFrom(ModelLocator.Current.Settings.HtmlContentTypeEngineSettings);
            return content;
        }

        internal GDIPlusContainer _litehtml;
        internal bool _ready = false; // Loaded Rendered 

        //public HtmlFileContent() {
        //    type = "text/html";
        //}
        public void Dispose() {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        // Protected implementation of Dispose pattern.
        // Flag: Has Dispose already been called?
        private bool _disposed = false;

        private void Dispose(bool disposing) {
            if (_disposed) {
                return;
            }

            if (disposing) {
                //if (litehtml != null)
                //    litehtml.Dispose();
            }
            _disposed = true;
        }

        private Bitmap _htmlBitmap;

        public override async Task<bool> SetDocumentAsync(string doc) {
            _ready = false;
            _litehtml = null;
            _htmlBitmap = null;
            Document = doc;
            return await Task.FromResult(true);
        }

        /// <summary>
        /// Get total count of pages. Set any local page-size related values (e.g. linesPerPage)
        /// </summary>
        /// <param name="e"></param>
        /// <returns></returns>
        public override async Task<int> RenderAsync(System.Drawing.Printing.PrinterResolution printerResolution, EventHandler<string> reflowProgress) {
            LogService.TraceMessage();

            _ready = false;

            reflowProgress?.Invoke(this, "HtmlFileContent.RenderAsync");
            await base.RenderAsync(printerResolution, reflowProgress).ConfigureAwait(false) ;

            var width = (int)PageSize.Width;// (printerResolution.X * PageSize.Width / 100);
            var height = (int)PageSize.Height;// (printerResolution.Y * PageSize.Height / 100);
            LogService.TraceMessage($"HtmlFileContent.RenderAsync - Page size: {width}x{height} @ {printerResolution.X}x{printerResolution.Y} dpi");

            string css;
            try {
                // TODO: Make sure wiprint.css is in the same dir as .config file once setup is impl
                using var cssStream = new StreamReader("winprint.css");
                css = await cssStream.ReadToEndAsync();
                cssStream.Close();
                reflowProgress?.Invoke(this, "Read winprint.css");
            }
            catch {
                css = IncludedWinPrintCss.CssString;
            }

            // BUGBUG: wihtout knowing the relative root path fo the html document we can't load any resources
            var resources = new HtmlResources("");
            _litehtml = new GDIPlusContainer(css, resources.GetResourceString, resources.GetResourceBytes) {
                Diagnostics = ContentSettings.Diagnostics,
                Size = new LiteHtmlSize(width, 0),
                PageHeight = height
            };
            
            _htmlBitmap = new Bitmap(width, height);
            //htmlBitmap.SetResolution(printerResolution.X, printerResolution.Y);
            var g = Graphics.FromImage(_htmlBitmap);
            g.PageUnit = GraphicsUnit.Display;
            g.TextRenderingHint = ContentTypeEngineBase.TextRenderingHint;

            //g.FillRectangle(Brushes.LightYellow, new Rectangle(0, 0, width, height));

            LogService.TraceMessage($"HtmlFileContent.RenderAsync() Graphic is {_htmlBitmap.Width} x {_htmlBitmap.Height} @ {g.DpiX} x {g.DpiY} dpi. PageUnit = {g.PageUnit.ToString()}");
            _litehtml.Graphics = g;
            _litehtml.StringFormat = ContentTypeEngineBase.StringFormat;
            _litehtml.Grayscale = ContentSettings.Grayscale;
            _litehtml.Darkness = ContentSettings.Darkness;
            _litehtml.PrintBackground = ContentSettings.PrintBackground;

            //Logging.TraceMessage("litehtml.Document.CreateFromString(document)");
            reflowProgress?.Invoke(this, "litehtml.Document.CreateFromString(document)");
            _litehtml.Document.CreateFromString(Document);
            //Logging.TraceMessage("back from litehtml.Document.CreateFromString(document)");
            reflowProgress?.Invoke(this, "back from litehtml.Document.CreateFromString(document)");

            _litehtml.Document.OnMediaChanged();

            // TODO: Use return of Render() to get "best width"
            var bestWidth = _litehtml.Document.Render(width);
            reflowProgress?.Invoke(this, "Done with Render");
            // Note, setting viewport does nothing
            //litehtml.SetViewport(new LiteHtmlPoint(0, 0), new LiteHtmlSize(width, height));
            _litehtml.Graphics = null;

            //Logging.TraceMessage($"Litehtml_DocumentSizeKnown {_litehtml.Document.Width()}x{_litehtml.Document.Height()} bestWidth = {bestWidth}");

            var n = _litehtml.Document.Height() / height + 1;
            Logging.TraceMessage($"HtmlFileContent.RenderAsync - {n} pages.");
            _ready = true;
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
            if (_litehtml == null || _ready == false) {
                //Log.Debug($"HtmlFileContent.PaintPage({pageNum}) when litehtml is not ready.");
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

            _litehtml.Graphics = g;
            g.TextRenderingHint = ContentTypeEngineBase.TextRenderingHint;

            var yPos = (pageNum - 1) * (int)Math.Round(PageSize.Height);

            if (!ContentSettings.Diagnostics) {
                g.SetClip(new Rectangle(0, 0, (int)Math.Round(PageSize.Width), (int)Math.Round(PageSize.Height)));
            }

            var size = new LiteHtmlSize(Math.Round(PageSize.Width), Math.Round(PageSize.Height));
            _litehtml.Document.Draw(-0, -yPos, new position {
                x = 0,
                y = 0,
                width = (int)size.Width,
                height = (int)size.Height
            });
            _litehtml.Graphics = null;

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

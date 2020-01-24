using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using LiteHtmlSharp;
using WinPrint.Core.Models;
using WinPrint.Core.Services;
using WinPrint.LiteHtml;

namespace WinPrint.Core.ContentTypes {

    internal struct HtmlLine {
        internal string html;
        internal GDIPlusContainer liteHtml;
        internal int lineNumber;
        internal int numLines;
    }
    /// <summary>
    /// Implements generic text file type support. 
    /// Base class for WinPrint content types. Each file type may have a Content type
    /// These classes know how to parse and paint the file type's content.
    /// </summary>
    // TOOD: Color code c# kewoards https://www.c-sharpcorner.com/UploadFile/kirtan007/syntax-highlighting-in-richtextbox-using-C-Sharp/
    public class CodeFileContent : ContentBase, IDisposable {
        public static CodeFileContent Create() {
            var content = new CodeFileContent();
            content.CopyPropertiesFrom(ModelLocator.Current.Settings.TextFileSettings);
            return content;
        }

        public static new string ContentType = "Source code";
        public CodeFileContent() {
            Font = new WinPrint.Core.Models.Font() { Family = "Lucida Sans Console", Size = 8F, Style = FontStyle.Regular };
        }

        // All of the lines of the text file, after reflow/line-wrap
        private List<HtmlLine> lines;
        private float lineHeight;
        private int linesPerPage;
        private float lineNumberWidth;
        private float minCharWidth;
        private System.Drawing.Font cachedFont;

        // Publics
        public bool LineNumbers { get => lineNumbers; set => SetField(ref lineNumbers, value); }
        private bool lineNumbers = true;

        public bool LineNumberSeparator { get => lineNumberSeparator; set => SetField(ref lineNumberSeparator, value); }
        private bool lineNumberSeparator = true;

        public int TabSpaces { get => tabSpaces; set => SetField(ref tabSpaces, value); }
        private int tabSpaces = 4;

        public bool NewPageOnFormFeed { get => newPageOnFormFeed; set => SetField(ref newPageOnFormFeed, value); }
        private bool newPageOnFormFeed = false;

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
                if (cachedFont != null) cachedFont.Dispose();
                lines = null;
            }
            disposed = true;
        }

        private bool convertedToHtml = false;

        public async override Task<bool> LoadAsync(string filePath) {
            if (await base.LoadAsync(filePath)) {
                if (!convertedToHtml)
                    lines = await DocumentToHtmlLines(filePath, Language);
                convertedToHtml = true;
                return true;
            }
            return false;
        }

        private async Task<List<HtmlLine>> DocumentToHtmlLines(string file, string language) {
            LogService.TraceMessage($"{language}");

           // const string cssTheme = "prism-coy.css";
            //const string cssPrism = "prism.css";
            //const string cssWinPrint = "prism-winprint-overrides.css";
            string appDir = Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().GetName().CodeBase);
            var cssUri = new UriBuilder();
            cssUri.Scheme = "file";
            cssUri.Host = @"";
            cssUri.Path = appDir;

            // Emit javascript to be run via node.js
            var sbNodeJS = new StringBuilder();
            sbNodeJS.AppendLine($"const Prism = require('prismjs');");
            sbNodeJS.AppendLine($"const loadLanguages = require('prismjs/components/');");
            sbNodeJS.AppendLine($"loadLanguages(['{language}']);");
            //document = "using System;";
            sbNodeJS.AppendLine($"const code = `{document}`;");
            sbNodeJS.AppendLine($"const html = Prism.highlight(code, Prism.languages.{language}, '{language}');");
            sbNodeJS.AppendLine($"console.log(html);");
            var nodeJS = sbNodeJS.ToString();

            List<HtmlLine> htmlLines = new List<HtmlLine>();
            try {
                ProcessStartInfo psi = new ProcessStartInfo();
                psi.UseShellExecute = false;   // This is important
                psi.CreateNoWindow = true;     // This is what hides the command window.
                psi.FileName = @"node";
                psi.RedirectStandardInput = true;
                psi.RedirectStandardOutput = true;
                using (var node = Process.Start(psi)) {
                    StreamWriter sw = node.StandardInput;
                    //Helpers.Logging.TraceMessage(sbNodeJS.ToString());
                    await sw.WriteLineAsync(sbNodeJS.ToString());
                    sw.Close();

                    int n = 1;
                    string css;
                    try {
                        // TODO: Make sure wiprint.css is in the same dir as .config file once setup is impl
                        using StreamReader cssStream = new StreamReader("winprint.css");
                        css = await cssStream.ReadToEndAsync();
                        cssStream.Close();
                    }
                    catch {
                        css = IncludedWinPrintCss.CssString;
                    }
                    var resources = new HtmlResources(filePath);
                    var prismThemes = GetPrismThemesPath();
                    cssUri.Path = prismThemes;

                    while (!node.StandardOutput.EndOfStream) {
                        // build a well-formed HTML file
                        var sbHtml = new StringBuilder();
                        sbHtml.AppendLine($"<!DOCTYPE html><html><head>");
                        sbHtml.AppendLine($"<meta charset=\"utf-8\"/>");

                        // TODO: detect node and prism installation
                        // Reference choosen theme style sheet
                        //sbHtml.AppendLine($"<link href=\"{cssUri.Uri + @"/" + cssTheme}\" rel=\"stylesheet\"/>");

                        cssUri.Path = appDir;
                        //Override styles with WinPrint settings for better printing

                        //If the app directory has the file, use it.Otherwise inline them.
                        //Strip "file:/" off of appDir for local

                        //string overridePath = appDir.Substring(6, appDir.Length - 6) + "\\" + cssWinPrint;
                        //if (File.Exists(overridePath))
                        //    sbHtml.AppendLine($"<link href=\"{cssUri.Path + @"/" + cssWinPrint}\" rel=\"stylesheet\"/>");
                        //else {
                        sbHtml.AppendLine($"<style>");
                        sbHtml.AppendLine(Properties.Resources.prism_winprint);
                        sbHtml.AppendLine(Properties.Resources.prism_winprint_overrides);
                        sbHtml.AppendLine($"</style>");
                        //}
                        sbHtml.AppendLine($"</head><body>");
                        sbHtml.AppendLine($"</body></html>");
                        sbHtml.AppendLine($"<pre class=\"language-{language}\"><code class=\"language-{language}\">");
                        sbHtml.AppendLine(await node.StandardOutput.ReadLineAsync());//.Replace(' ', (char)160));
                        sbHtml.AppendLine($"</code></pre>");

                        htmlLines.Add(new HtmlLine() {
                            html = sbHtml.ToString(),
                            lineNumber = n++,
                            numLines = -1,
                            liteHtml = new GDIPlusContainer(css, resources.GetResourceString, resources.GetResourceBytes)
                        });
                    }
                }
            }
            catch (Exception e) {
                LogService.TraceMessage(e.Message);
            }
            LogService.TraceMessage("DocumentToHtmlLines() - exiting");
            return htmlLines;
        }

        public string Language { get; internal set; }

        private string GetPrismThemesPath() {
            string path = @"C:\Users\ckindel\source\node_modules";
            //try {
            //    ProcessStartInfo psi = new ProcessStartInfo();
            //    psi.UseShellExecute = false;   // This is important
            //    psi.CreateNoWindow = true;     // This is what hides the command window.
            //    psi.FileName = @"npm";
            //    psi.RedirectStandardInput = true;
            //    psi.RedirectStandardOutput = true;
            //    using (var node = Process.Start(psi)) {
            //        StreamWriter sw = node.StandardInput;
            //        sw.WriteLine("npm root");
            //        sw.Close();
            //        path = node.StandardOutput.ReadLine();
            //    }

            //}
            //catch (Exception e) {
            //    Helpers.Logging.TraceMessage(e.Message);
            //}
            return path + @"\prismjs\themes";
        }

        /// <summary>
        /// Get total count of pages. Set any local page-size related values (e.g. linesPerPage)
        /// </summary>
        /// <param name="e"></param>
        /// <returns></returns>
        public new async Task<int> RenderAsync(System.Drawing.Printing.PrinterResolution printerResolution, EventHandler<string> reflowProgress) {
            //await base.RenderAsync(printerResolution, reflowProgress);

            if (document == null) throw new ArgumentNullException("document can't be null for Render");
            LogService.TraceMessage("CodeFileContent.Render");

            // Calculate the number of lines per page.
            cachedFont = new System.Drawing.Font(Font.Family,
                Font.Size / 72F * 96F, Font.Style, GraphicsUnit.Pixel); // World?
            lineHeight = cachedFont.GetHeight();
            linesPerPage = (int)Math.Floor(PageSize.Height / lineHeight);

            // 3 digits + 1 wide - Will support 999 lines before line numbers start to not fit
            // TODO: Make line number width dynamic
            // Note, Measure string is actually dependent on lineNumberWidth!
            lineNumberWidth = LineNumbers ? MeasureString(null, new string('0', 4)).Width : 0;

            var n = 0;

            // for now, assume 1 line per htmlLine

            // Note, MeasureLines may increment numPages due to form feeds
            lines = await MeasureLines(document).ConfigureAwait(false); // new List<string>();

            n += (lines.Count / linesPerPage) + 1;

            LogService.TraceMessage($"{lines.Count} lines across {n} pages.");
            return n;
        }

        // TODO: Profile for performance
        private async Task<List<HtmlLine>> MeasureLines(string document) {
            LogService.TraceMessage("CodeFileContent.MeasureLines");

            int width = (int)PageSize.Width;// (printerResolution.X * PageSize.Width / 100);
            int height = (int)PageSize.Height;// (printerResolution.Y * PageSize.Height / 100);

            foreach (var l in lines)
                await Task.Run(() => RenderLine(l));
            //await RenderLine(l);

            minCharWidth = MeasureString(null, "W").Width;
            int minLineLen = (int)((float)((PageSize.Width - lineNumberWidth) / minCharWidth));

            return lines;
        }

        private async Task RenderLine(HtmlLine line) {
            int width = (int)PageSize.Width;// (printerResolution.X * PageSize.Width / 100);
            int height = (int)PageSize.Height;// (printerResolution.Y * PageSize.Height / 100);
            var htmlBitmap = new Bitmap(width, height);
            var g = Graphics.FromImage(htmlBitmap);
            g.PageUnit = GraphicsUnit.Display;
            var lineSize = new LiteHtmlSize(width, height);
            line.liteHtml.Size = lineSize;
            line.liteHtml.Graphics = g;
            await Task.Run(() => line.liteHtml.Document.CreateFromString(line.html));
            //l.liteHtml.Document.OnMediaChanged();
            // TODO: Use return of Render() to get "best width"
            int bestWidth;
            await Task.Run(() => bestWidth = line.liteHtml.Document.Render((int)width));
            line.liteHtml.Graphics = null;
        }

        private SizeF MeasureString(Graphics g, string text) {
            int charsFitted, linesFilled;
            return MeasureString(g, text, out charsFitted, out linesFilled);
        }

        /// <summary>
        /// Measures how much width a string will take, given current page settings (including line numbers)
        /// </summary>
        /// <param name="g"></param>
        /// <param name="text"></param>
        /// <param name="charsFitted"></param>
        /// <param name="linesFilled"></param>
        /// <returns></returns>
        private SizeF MeasureString(Graphics g, string text, out int charsFitted, out int linesFilled) {
            if (g is null) {
                // define context used for determining glyph metrics.        
                using Bitmap bitmap = new Bitmap(1, 1);
                g = Graphics.FromImage(bitmap);
                //g = Graphics.FromHwnd(PrintPreview.Instance.Handle);
                g.PageUnit = GraphicsUnit.Document;
            }

            // determine width     
            float fontHeight = lineHeight;
            // Use page settings including lineNumberWidth
            SizeF proposedSize = new SizeF(PageSize.Width - lineNumberWidth, lineHeight * linesPerPage);
            SizeF size = g.MeasureString(text, cachedFont, proposedSize, StringFormat.GenericTypographic, out charsFitted, out linesFilled);
            return size;
        }

        /// <summary>
        /// Paints a single page
        /// </summary>
        /// <param name="g">Graphics with 0,0 being the origin of the Page</param>
        /// <param name="pageNum">Page number to print</param>
        public override void PaintPage(Graphics g, int pageNum) {
            //if (pageNum > NumPages) {
            //    Helpers.Logging.TraceMessage($"CodeFileContent.PaintPage({pageNum}) when NumPages is {NumPages}");
            //    return;
            //}

            float leftMargin = 0;// containingSheet.GetPageX(pageNum);

            PaintLineNumberSeparator(g);

            // Print each line of the file.
            int startLine = linesPerPage * (pageNum - 1);
            int endLine = startLine + linesPerPage;
            int lineOnPage;
            for (lineOnPage = 0; lineOnPage < linesPerPage; lineOnPage++) {
                int lineInDocument = lineOnPage + (linesPerPage * (pageNum - 1));
                if (lineInDocument < lines.Count && lineInDocument >= startLine && lineInDocument <= endLine) {
                    if (lines[lineInDocument].lineNumber > 0)
                        PaintLineNumber(g, pageNum, lineInDocument);
                    float xPos = leftMargin + lineNumberWidth;
                    float yPos = lineOnPage * lineHeight;
                    RenderCode(g, lineInDocument, cachedFont, xPos, yPos);
                }
            }
        }

        private void RenderCode(Graphics g, int lineInDocument, System.Drawing.Font cachedFont, float xPos, float yPos) {
            var line = lines[lineInDocument];

            line.liteHtml.Graphics = g;

            //            g.SetClip(new Rectangle(0, 0, (int)Math.Round(PageSize.Width), (int)Math.Round(PageSize.Height)));

            LiteHtmlSize size = new LiteHtmlSize(Math.Round(PageSize.Width), Math.Round(PageSize.Height));
            line.liteHtml.Document.Draw((int)xPos, (int)yPos-2, new position {
                x = 0,
                y = 0,
                width = (int)Math.Round(size.Width),
                height = (int)Math.Round(size.Height)
            });
            //            g.DrawString(lines[lineInDocument].text, cachedFont, Brushes.Black, xPos, yPos, StringFormat.GenericTypographic);
        }

        // TODO: Support setting color of line #s and separator
        // TODO: Only paint Line Number Separator if there's an actual line
        private void PaintLineNumberSeparator(Graphics g) {
            if (LineNumbers && LineNumberSeparator && lineNumberWidth != 0) {
                g.DrawLine(Pens.Gray, lineNumberWidth - 2, 0, lineNumberWidth - 2, PageSize.Height);
            }
        }

        // TODO: Allow a different (non-monospace) font for line numbers
        internal void PaintLineNumber(Graphics g, int pageNum, int lineNumber) {
            if (LineNumbers == true && lineNumberWidth != 0) {
                int lineOnPage = lineNumber % linesPerPage;
                // TOOD: Figure out how to make the spacig around separator more dynamic
                int x = LineNumberSeparator ? (int)(lineNumberWidth - 6 - MeasureString(g, $"{lines[lineNumber].lineNumber}").Width) : 0;
                g.DrawString($"{lines[lineNumber].lineNumber}", cachedFont, Brushes.Gray, x, lineOnPage * lineHeight, StringFormat.GenericDefault);
            }
        }
    }
}

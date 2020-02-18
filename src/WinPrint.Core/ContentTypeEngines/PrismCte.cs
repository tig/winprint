﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Printing;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using LiteHtmlSharp;
using Serilog;
using WinPrint.Core.Models;
using WinPrint.Core.Services;

namespace WinPrint.Core.ContentTypeEngines {
    public class PrismCte : HtmlCte {
        public static new string ContentType = "text/code";

        public static new PrismCte Create() {
            var content = new PrismCte();
            content.CopyPropertiesFrom(ModelLocator.Current.Settings.PrismContentTypeEngineSettings);
            return content;
        }

        private float lineHeight;
        private int linesPerPage;
        private double remainingPartialLineHeight;
        private float lineNumberWidth;
        //private float minCharWidth;
        private System.Drawing.Font cachedFont;

        // Publics
        public bool LineNumbers { get => lineNumbers; set => SetField(ref lineNumbers, value); }
        private bool lineNumbers = true;

        public bool LineNumberSeparator { get => lineNumberSeparator; set => SetField(ref lineNumberSeparator, value); }
        private bool lineNumberSeparator = true;

        private bool convertedToHtml = false;

        public async override Task<bool> LoadAsync(string filePath) {
            LogService.TraceMessage();

            if (!await ServiceLocator.Current.NodeService.IsPrismInstalled()) {
                Log.Warning("Prism.js is not installed. Installing...");

                var result = await ServiceLocator.Current.NodeService.RunNpmCommand("-g install prismjs");
                if (string.IsNullOrEmpty(result)) {
                    Log.Debug("Could not install PrismJS");
                    throw new InvalidOperationException("Could not install PrismJS.");
                }
            }

            if (!await base.LoadAsync(filePath)) return false;

            if (!convertedToHtml)
                document = await CodeToHtml(filePath, Language);
            convertedToHtml = true;

            //Helpers.Logging.TraceMessage(document);

#if DEBUG
            var w = new StreamWriter(filePath + "_.html");
            w.Write(document);
            w.Close();
#endif

#if USE_COLORCODE
            var formatter = new HtmlFormatter();
            var language = ColorCode.Languages.FindById(Type);
            document = formatter.GetHtmlString(document, language);
            StreamWriter w = new StreamWriter(title + "_.html");
            w.Write(document);
            w.Close();
#endif
            return !string.IsNullOrEmpty(document);
        }

        public override async Task<int> RenderAsync(PrinterResolution printerResolution, EventHandler<string> reflowProgress) {
            LogService.TraceMessage();

            // Calculate the number of lines per page etc..
            var numPages = await base.RenderAsync(printerResolution, reflowProgress);
            var fi = litehtml.GetCodeFontInfo();
            cachedFont = fi.Font;
            lineHeight = fi.LineHeight;
            linesPerPage = (int)Math.Floor(PageSize.Height / lineHeight);

            // If a line would split a page break, get the amount of space it uses
            remainingPartialLineHeight = PageSize.Height % lineHeight;
            linesInDocument = (int)Math.Floor(litehtml.Document.Height() / lineHeight);


            // 3 digits + 1 wide - Will support 999 lines before line numbers start to not fit
            // TODO: Make line number width dynamic
            // Note, Measure string is actually dependent on lineNumberWidth!
            lineNumberWidth = LineNumbers ? MeasureString(null, new string('0', 4)).Width : 0;

            return linesInDocument / linesPerPage + 1;
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

        private int linesInDocument;

        private async Task<string> CodeToHtml(string file, string language) {
            Log.Debug(LogService.GetTraceMsg(), $" {file} (type={language})", file, language);

            // TODO: Implement theme selection (or not; most don't make sense for print)
            const string cssUserProvidePrismThemeFile = "prism-winprint.css"; // "prism-coy.css";
            //const string cssUserProvidePrismThemeFile = "prism-dark.css";
            //const string cssUserProvidePrismThemeFile = "prism-funky.css";
            //const string cssUserProvidePrismThemeFile = "prism-okaidia.css";
            //const string cssUserProvidePrismThemeFile = "prism-solarizedlight.css";
            //const string cssUserProvidePrismThemeFile = "prism-tomorrow.css";
            //const string cssUserProvidePrismThemeFile = "prism-twilight.css";

            const string cssWinPrintOverrides = "prism-winprint-overrides.css";

            // Get the URI to our app dir so user-provided stylesheets can be placed there.
            // If (user provided sheet found in exeucting dir)
            //    Use user provided sheet
            // Else 
            //    Use sheet in prismjs\themes
            string appDir = Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().GetName().CodeBase);
            var cssUri = new UriBuilder();
            cssUri.Scheme = "file";
            cssUri.Host = @"";
            cssUri.Path = appDir;

            // TODO: detect node and prism installation
            // TODO: implement theme choice
            var nodeDir = await ServiceLocator.Current.NodeService.GetModulesDirectory();
            var prismThemes = nodeDir + @"\prismjs\themes";
            // Reference choosen theme style sheet
            cssUri.Path = prismThemes;

            // Emit javascript to be run via node.js
            var sbNodeJS = new StringBuilder();
            sbNodeJS.AppendLine($"const Prism = require('{nodeDir.Replace('\\', '/')}/prismjs');");
            sbNodeJS.AppendLine($"const loadLanguages = require('{nodeDir.Replace('\\', '/')}/prismjs/components/');");
            sbNodeJS.AppendLine($"loadLanguages(['{language}']);");
            // TODO: for very large files should we use TEMP file?
            sbNodeJS.AppendLine($"const code = `{document}`;");
            sbNodeJS.AppendLine($"const html = Prism.highlight(code, Prism.languages.{language}, '{language}');");
            sbNodeJS.AppendLine($"console.log(html);");
            var nodeJS = sbNodeJS.ToString();

            // build a well-formed HTML file
            var sbHtml = new StringBuilder();
            sbHtml.AppendLine($"<!DOCTYPE html><html><head><title>{file}</title>");

            // <meta/>
            sbHtml.AppendLine($"<meta charset=\"utf-8\"/>");

            sbHtml.AppendLine($"<style>");

            // Put all CSS inline - faster and enables self-contained saved files
            // Prism formatting is determined with this algo
            //    <heaad>
            //      <style>
            //         if (user provided prism-xxx.css) 
            //             User provided prism-xxx.css (Body -> ContentSettings.Font, Pre -> ContentSettings.Font) OR built-in prism.css
            //         else 
            //             Built-in provided prism-winprint.css (Body -> Font, Pre -> Monospace Font)
            //         if (PrismFileContent.Font != null)
            //             (Body -> PrismFileContent.Font, Pre -> PrismFileContent.Font Font)
            //         if (Sheet.Font != null)
            //             (Body -> Sheet.Font, Pre -> Sheet.Font Font)
            //         if (user provided prism-winprint-overrides.css) 
            //             User provided WinPrint Prism Overrides (makes Prism work with printing).
            //         else 
            //             Built-in WinPrint Prism Overrides (makes Prism work with printing).
            //      </style>
            string themePath = appDir.Substring(6, appDir.Length - 6) + "\\" + cssUserProvidePrismThemeFile;
            if (File.Exists(themePath)) {
                //string themePath = $"{prismThemes}/{cssUserProvidePrismThemeFile}";
                Log.Debug("Using user provided Prism theme: {prism}", themePath);
                // TODO: Test this. 
                // BUGBUG: Font specified in user provided CSS will not be detected due to lack of
                // "winprint" in style specifier (see GDIPlusContainer.cs)
                sbHtml.AppendLine(File.ReadAllText(cssUserProvidePrismThemeFile)); 
            }
            else
                sbHtml.AppendLine(Properties.Resources.prism_winprint);
 
            // If prismContentType settings specifies a font, override what's in CSS.
            if (ContentSettings.Font != null)
                sbHtml.AppendLine($"code[class*=\"language-\"], pre[class*=\"language-\"] {{" + Environment.NewLine +
                    $"font-family: '{ContentSettings.Font.Family}', winprint;" + Environment.NewLine +
                    $"font-size: {ContentSettings.Font.Size}pt;" +  Environment.NewLine +
                    // BUGBUG: This ain't right
                    $"font-weight: {ContentSettings.Font.Style};}}");

            // TODO: If Sheet settings specifies a font, override what's in CSS.
            //if (sheet.Font != null)
            //    sbHtml.AppendLine($"code[class*=\"language-\"], pre[class*=\"language-\"] {{" + Environment.NewLine +
            //        $"font-family: '{Font.Family}', winprint;" + Environment.NewLine +
            //        $"font-size: {Font.Size}pt;}}");

            // Finally, override styles with WinPrint settings for better printing
            // User can put a prism-winprint-overrides.css in the app dir to override built-in overrides
            cssUri.Path = appDir;
            // Strip "file:/" off of appDir for local
            string overridePath = appDir.Substring(6, appDir.Length - 6) + "\\" + cssWinPrintOverrides;
            if (File.Exists(overridePath)) {
                Log.Debug("Using user provided css overrides: {prism}", overridePath);

                sbHtml.AppendLine(File.ReadAllText(overridePath));
            }
            else
                sbHtml.AppendLine(Properties.Resources.prism_winprint_overrides);

            sbHtml.AppendLine($"</style>");

            sbHtml.Append($"</head><body>");

            // Run node
            Process node = null;
            ProcessStartInfo psi = new ProcessStartInfo();
            try {
                psi.UseShellExecute = false;   // This is important
                psi.CreateNoWindow = true;     // This is what hides the command window.
                psi.FileName = @"node";
                psi.RedirectStandardInput = true;
                psi.RedirectStandardOutput = true;
                psi.RedirectStandardError = true;

                Log.Debug("Starting Process: '{f} {a}'", psi.FileName, psi.Arguments);
                node = Process.Start(psi);
                StreamWriter sw = node.StandardInput;
                Log.Debug("Writing {n} chars to stdin", document.Length);
                await sw.WriteLineAsync(sbNodeJS.ToString());
                sw.Close();

                // TODO: Detect script failure and do right thing
                //while (!node.StandardError.EndOfStream) {
                //    var s = node.StandardError.ReadLine();
                //    Log.Debug(s);
                //}

                var ln = "";// LineNumbers ? "line-numbers" : "";
                sbHtml.AppendLine($"<pre class=\"language-{language} {ln}\"><code class=\"language-{language}\"><table>");
                // sbHtml.AppendLine(await node.StandardOutput.ReadToEndAsync());
                linesInDocument = 0;
                while (!node.StandardOutput.EndOfStream) {
                    sbHtml.AppendLine($"<tr><td class=\"line-number\">{++linesInDocument}</td><td>{await node.StandardOutput.ReadLineAsync()}</td></tr>");
                    //sbHtml.AppendLine($"<div class=\"ln\">{lineNumber++}</div>{await node.StandardOutput.ReadLineAsync()}");
                }
                Log.Debug("Read {n} lines from stdout", linesInDocument);
                sbHtml.AppendLine($"</table></code></pre>");
            }
            catch (Exception e) {
                // TODO: Decide what to do here. a) Provide error and fail?
                // b) render without syntax highlighting (call TextFileContent)? Tell the user why?
                Log.Error(e, "Failed to convert to html");
                sbHtml.AppendLine($"<p>Failed to convert to html. {e.Message}</p>");
            }
            finally {
                node?.Dispose();
            }
            sbHtml.AppendLine($"</body></html>");
            Log.Debug("CodeToHtml done: {n} chars", sbHtml.Length);
            return sbHtml.ToString();
        }

        internal string Language { get; set; }

        public override void PaintPage(Graphics g, int pageNum) {
            if (litehtml == null || ready == false) {
                Log.Debug($"PrismFileContent.PaintPage({pageNum}) when litehtml is not ready.");
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
            //Helpers.Logging.TraceMessage($"PaintPage({pageNum} - {g.DpiX}x{g.DpiY} dpi. PageUnit = {g.PageUnit.ToString()})");

            double extraLines = (pageNum - 1) * remainingPartialLineHeight;
            int yPos = (int)Math.Round((pageNum - 1) * PageSize.Height) - (int)Math.Round(extraLines);

            // Set the clip such that any extraLines are clipped off bottom
            if (!ContentSettings.Diagnostics)
                g.SetClip(new Rectangle(0, 0, (int)Math.Round(PageSize.Width), (int)Math.Round(PageSize.Height - remainingPartialLineHeight)));

            litehtml.Graphics = g;

            LiteHtmlSize size = new LiteHtmlSize(Math.Round(PageSize.Width), Math.Ceiling(PageSize.Height));

            // Note, setting viewport does nothing
            //litehtml.SetViewport(new LiteHtmlPoint((int)-0, (int)-yPos), new LiteHtmlSize((int)size.Width, (int)size.Height));

            litehtml.Document.Draw((int)-0, (int)-yPos, new position {
                x = 0,
                y = 0,
                width = (int)size.Width,
                height = (int)size.Height
            });

            float leftMargin = 0;// containingSheet.GetPageX(pageNum);

            //PaintLineNumberSeparator(g);

            // Diagnostics
            if (ContentSettings.Diagnostics) {
                g.ResetClip();
                int startLine = linesPerPage * (pageNum - 1);
                int endLine = startLine + linesPerPage;
                int lineOnPage;
                //int linesInDocument = (int)Math.Round(litehtml.Document.Height() / lineHeight);
                for (lineOnPage = 0; lineOnPage < linesPerPage; lineOnPage++) {
                    int lineInDocument = lineOnPage + (linesPerPage * (pageNum - 1));
                    if (lineInDocument < linesInDocument && lineInDocument >= startLine) {// && lineInDocument <= endLine) {
                                                                                          //if (lines[lineInDocument].lineNumber > 0)
                        PaintLineNumber(g, pageNum, lineInDocument);
                        int x = (int)leftMargin;
                        int y = lineOnPage * (int)lineHeight;
                        //RenderCode(g, lineInDocument, cachedFont, xPos, yPos);
                        g.DrawRectangle(Pens.Red, 0, y, (int)Math.Round(PageSize.Width), (int)lineHeight);
                    }
                }
            }
            g.Restore(state);
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
                lineNumber++;
                int x = LineNumberSeparator ? (int)(lineNumberWidth - 6 - MeasureString(g, $"{lineNumber}").Width) : 0;
                g.DrawString($"{lineNumber}", cachedFont, Brushes.Orange, x - lineNumberWidth, lineOnPage * lineHeight, StringFormat.GenericDefault);
            }
        }
    }
}
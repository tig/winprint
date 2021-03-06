﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Printing;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using LiteHtmlSharp;
using WinPrint.Core.Models;

namespace WinPrint.Core.ContentTypes {
    public class PrismFileContent : HtmlFileContent {
        public static new string ContentType = "Syntax Highlighted Code";

        public static PrismFileContent Create() {
            var content = new PrismFileContent();
            content.CopyPropertiesFrom(ModelLocator.Current.Settings.PrismFileSettings);
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

        public bool Diagnostics { get => diagnostics; set => SetField(ref diagnostics, value); }
        private bool diagnostics =false;

        public bool LineNumberSeparator { get => lineNumberSeparator; set => SetField(ref lineNumberSeparator, value); }
        private bool lineNumberSeparator = true;

        private bool convertedToHtml = false;

        public async override Task<string> LoadAsync(string filePath) {
            Helpers.Logging.TraceMessage("PrismFileContent.LoadAsync()");
            await base.LoadAsync(filePath);

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
            return document;
        }

        public override async Task<int> RenderAsync(PrinterResolution printerResolution) {
            Helpers.Logging.TraceMessage("PrismFileContent.RenderAsync()");

            // Calculate the number of lines per page etc..
            var numPages = await base.RenderAsync(printerResolution);
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
            Helpers.Logging.TraceMessage("PrismFileContent.CodeToHtml()");

            const string cssPrism = "prism.css";
            const string cssTheme = "prism-coy.css";
            //const string cssTheme = "prism-dark.css";
            //const string cssTheme = "prism-funky.css";
            //const string cssTheme = "prism-okaidia.css";
            //const string cssTheme = "prism-solarizedlight.css";
            //const string cssTheme = "prism-tomorrow.css";
            //const string cssTheme = "prism-twilight.css";

            const string cssWinPrint = "prism-winprint-overrides.css";

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

            // build a well-formed HTML file
            var sbHtml = new StringBuilder();
            sbHtml.AppendLine($"<!DOCTYPE html><html><head><title>{file}</title>");
            sbHtml.AppendLine($"<meta charset=\"utf-8\"/>");

            // TODO: detect node and prism installation
            var prismThemes = GetPrismThemesPath();
            // Reference choosen theme style sheet
            cssUri.Path = prismThemes;
            //sbHtml.AppendLine($"<link href=\"{cssUri.Uri + @"/" + cssPrism}\" rel=\"stylesheet\"/>");
            sbHtml.AppendLine($"<link href=\"{cssUri.Uri + @"/" + cssTheme}\" rel=\"stylesheet\"/>");

            cssUri.Path = appDir;
            // Override styles with WinPrint settings for better printing
            // If the app directory has the file, use it. Otherwise inline them.
            // Strip "file:/" off of appDir for local
            string overridePath = appDir.Substring(6, appDir.Length-6) + "\\" + cssWinPrint;
            if (File.Exists(overridePath))
                sbHtml.AppendLine($"<link href=\"{cssUri.Path + @"/" + cssWinPrint}\" rel=\"stylesheet\"/>");
            else {
                sbHtml.AppendLine($"<style>");
                sbHtml.AppendLine(Properties.Resources.prism_winprint_overrides);
                sbHtml.AppendLine($"code[class*=\"language-\"], pre[class*=\"language-\"] {{" + Environment.NewLine +
                    $"font-family: '{Font.Family}', winprint;" + Environment.NewLine +
                    $"font-size: {Font.Size}pt;}}");
                sbHtml.AppendLine($"</style>");
            }
            sbHtml.Append($"</head><body>");

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

                    var ln = "";// LineNumbers ? "line-numbers" : "";
                    sbHtml.AppendLine($"<pre class=\"language-{language} {ln}\"><code class=\"language-{language}\"><table>");
                    //                    sbHtml.AppendLine(await node.StandardOutput.ReadToEndAsync());
                    linesInDocument = 0;
                    while (!node.StandardOutput.EndOfStream) { 
                        sbHtml.AppendLine($"<tr><td class=\"line-number\">{++linesInDocument}</td><td>{await node.StandardOutput.ReadLineAsync()}</td></tr>");
                        //sbHtml.AppendLine($"<div class=\"ln\">{lineNumber++}</div>{await node.StandardOutput.ReadLineAsync()}");
                    }
                    sbHtml.AppendLine($"</table></code></pre>");
                    //node.WaitForExit(10000);
                }

            }
            catch (Exception e) {
                Helpers.Logging.TraceMessage(e.Message);
                sbHtml.AppendLine($"<p>Failed to convert to html. {e.Message}</p>");
            }
            sbHtml.AppendLine($"</body></html>");
            Helpers.Logging.TraceMessage("PrismFileContent.CodeToHtml() - exiting");
            return sbHtml.ToString();
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

        public override void PaintPage(Graphics g, int pageNum) {
            //base.PaintPage(g, pageNum);

            //if (pageNum > NumPages) {
            //    Helpers.Logging.TraceMessage($"PaintPage({pageNum}) when NumPages is {NumPages}");
            //    return;
            //}
            if (litehtml == null) {
                Helpers.Logging.TraceMessage($"PaintPage({pageNum}) when litehtml is null");
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
            g.SetClip(new Rectangle(0, 0, (int)Math.Round(PageSize.Width), (int)Math.Round(PageSize.Height - remainingPartialLineHeight)));
            litehtml.Graphics = g;
            LiteHtmlSize size = new LiteHtmlSize(Math.Round(PageSize.Width), Math.Ceiling(PageSize.Height));
            litehtml.Document.Draw((int)-0, (int)-yPos, new position {
                x = 0,
                y = 0,
                width = (int)size.Width,
                height = (int)size.Height
            });

            float leftMargin = 0;// containingSheet.GetPageX(pageNum);

            //PaintLineNumberSeparator(g);

            // Diagnostics
            if (diagnostics) {
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
                        g.DrawRectangle(Pens.LightSlateGray, 0, y, (int)Math.Round(PageSize.Width), (int)lineHeight);
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
                g.DrawString($"{lineNumber}", cachedFont, Brushes.Orange, x-lineNumberWidth, lineOnPage * lineHeight, StringFormat.GenericDefault);
            }
        }
    }
}

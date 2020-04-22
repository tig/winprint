using System;
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
        private static readonly string _contentType = "text/prism";
        /// <summary>
        /// ContentType identifier (shorthand for class name). 
        /// </summary>
        public override string ContentTypeEngineName {
            get {
                if (string.IsNullOrEmpty(Language)) {
                    return _contentType;
                }
                else {
                    return Language;
                }
            }
        }

        public static new PrismCte Create() {
            var content = new PrismCte();
            content.CopyPropertiesFrom(ModelLocator.Current.Settings.PrismContentTypeEngineSettings);
            return content;
        }

        private float _lineHeight;
        private int _linesPerPage;
        private double _remainingPartialLineHeight;
        private float _lineNumberWidth;
        //private float minCharWidth;
        private System.Drawing.Font cachedFont;
        private bool _convertedToHtml = false;

        // Publics

        public override async Task<bool> SetDocumentAsync(string doc) {
            LogService.TraceMessage();

            if (!await ServiceLocator.Current.NodeService.IsPrismInstalled()) {
                Log.Warning("Prism.js is not installed. Installing...");

                var result = await ServiceLocator.Current.NodeService.RunNpmCommand("-g install prismjs");
                if (string.IsNullOrEmpty(result)) {
                    Log.Debug("Could not install PrismJS");
                    throw new InvalidOperationException("Could not install PrismJS.");
                }
            }

            Document = doc;
            if (!_convertedToHtml) {
                Document = await CodeToHtml("", Language);
            }

            _convertedToHtml = true;

#if DEBUG
            var w = new StreamWriter("PrismCte.html");
            w.Write(document);
            w.Close();
#endif

            return true;
        }

        public override async Task<int> RenderAsync(PrinterResolution printerResolution, EventHandler<string> reflowProgress) {
            LogService.TraceMessage();

            // Calculate the number of lines per page etc..
            var numPages = await base.RenderAsync(printerResolution, reflowProgress).ConfigureAwait(false);
            var fi = _litehtml.GetCodeFontInfo();
            cachedFont = fi.Font;
            _lineHeight = fi.LineHeight;
            _linesPerPage = (int)Math.Floor(PageSize.Height / _lineHeight);

            // If a line would split a page break, get the amount of space it uses
            _remainingPartialLineHeight = PageSize.Height % _lineHeight;
            linesInDocument = (int)Math.Floor(_litehtml.Document.Height() / _lineHeight);

            // 3 digits + 1 wide - Will support 999 lines before line numbers start to not fit
            // TODO: Make line number width dynamic
            // Note, Measure string is actually dependent on lineNumberWidth!
            _lineNumberWidth = ContentSettings.LineNumbers ? MeasureString(null, new string('0', 4)).Width : 0;

            return linesInDocument / _linesPerPage + 1;
        }

        private SizeF MeasureString(Graphics g, string text) {
            return MeasureString(g, text, out var charsFitted, out var linesFilled);
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
                using var bitmap = new Bitmap(1, 1);
                g = Graphics.FromImage(bitmap);
                //g = Graphics.FromHwnd(PrintPreview.Instance.Handle);
                g.PageUnit = GraphicsUnit.Document;
            }

            // determine width     
            var fontHeight = _lineHeight;
            // Use page settings including lineNumberWidth
            var proposedSize = new SizeF(PageSize.Width - _lineNumberWidth, _lineHeight * _linesPerPage);
            var size = g.MeasureString(text, cachedFont, proposedSize, StringFormat.GenericTypographic, out charsFitted, out linesFilled);
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
            var appDir = Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().GetName().CodeBase);
            var cssUri = new UriBuilder {
                Scheme = "file",
                Host = @"",
                Path = appDir
            };

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
            var themePath = appDir.Substring(6, appDir.Length - 6) + "\\" + cssUserProvidePrismThemeFile;
            if (File.Exists(themePath)) {
                //string themePath = $"{prismThemes}/{cssUserProvidePrismThemeFile}";
                //Log.Debug("Using user provided Prism theme: {prism}", themePath);
                // TODO: Test this. 
                // BUGBUG: Font specified in user provided CSS will not be detected due to lack of
                // "winprint" in style specifier (see GDIPlusContainer.cs)
                sbHtml.AppendLine(File.ReadAllText(cssUserProvidePrismThemeFile));
            }
            else {
                sbHtml.AppendLine(Properties.Resources.prism_winprint);
            }

            // If prismContentType settings specifies a font, override what's in CSS.
            if (ContentSettings.Font != null) {
                sbHtml.AppendLine($"code[class*=\"language-\"], pre[class*=\"language-\"] {{" + Environment.NewLine +
                    $"font-family: '{ContentSettings.Font.Family}', winprint;" + Environment.NewLine +
                    $"font-size: {ContentSettings.Font.Size}pt;" + Environment.NewLine +
                    // BUGBUG: This ain't right
                    $"font-weight: {ContentSettings.Font.Style};}}");
            }

            // TODO: If Sheet settings specifies a font, override what's in CSS.
            //if (sheet.Font != null)
            //    sbHtml.AppendLine($"code[class*=\"language-\"], pre[class*=\"language-\"] {{" + Environment.NewLine +
            //        $"font-family: '{Font.Family}', winprint;" + Environment.NewLine +
            //        $"font-size: {Font.Size}pt;}}");

            // Finally, override styles with WinPrint settings for better printing
            // User can put a prism-winprint-overrides.css in the app dir to override built-in overrides
            cssUri.Path = appDir;
            // Strip "file:/" off of appDir for local
            var overridePath = appDir.Substring(6, appDir.Length - 6) + "\\" + cssWinPrintOverrides;
            if (File.Exists(overridePath)) {
                //Log.Debug("Using user provided css overrides: {prism}", overridePath);

                sbHtml.AppendLine(File.ReadAllText(overridePath));
            }
            else {
                sbHtml.AppendLine(Properties.Resources.prism_winprint_overrides);
            }

            sbHtml.AppendLine($"</style>");

            sbHtml.Append($"</head><body>");

            // Run node
            Process node = null;
            var psi = new ProcessStartInfo();
            try {
                psi.UseShellExecute = false;   // This is important
                psi.CreateNoWindow = true;     // This is what hides the command window.
                psi.FileName = @"node";
                psi.RedirectStandardInput = true;
                psi.RedirectStandardOutput = true;
                psi.RedirectStandardError = true;

                //Log.Debug("Starting Process: '{f} {a}'", psi.FileName, psi.Arguments);
                node = Process.Start(psi);
                var sw = node.StandardInput;
                //Log.Debug("Writing {n} chars to stdin", document.Length);
                await sw.WriteLineAsync(sbNodeJS.ToString()).ConfigureAwait(false);
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
                    if (ContentSettings.LineNumbers) {
                        sbHtml.AppendLine($"<tr><td class=\"line-number\">{++linesInDocument}</td><td>{await node.StandardOutput.ReadLineAsync().ConfigureAwait(false)}</td></tr>");
                    }
                    else {
                        sbHtml.AppendLine($"<tr><td>{await node.StandardOutput.ReadLineAsync().ConfigureAwait(false)}</td></tr>");
                    }
                    //sbHtml.AppendLine($"<div class=\"ln\">{lineNumber++}</div>{await node.StandardOutput.ReadLineAsync().ConfigureAwait(false)}");
                }
                Log.Debug("Read {n} lines from stdout", linesInDocument);

                if (linesInDocument == 0) {
                    //Log.Debug($"Reading stdErr...");
                    var stdErr = new StringBuilder();
                    if (!node.StandardError.EndOfStream) {
                        sbHtml.AppendLine($"<tr><td>There was an error syntax highlighting the file. Error details:</ td ></ tr > ");
                    }
                    while (!node.StandardError.EndOfStream) {
                        var outputLine = await node.StandardError.ReadLineAsync().ConfigureAwait(false);
                        Log.Debug("stdErr: {stdErr}", outputLine);
                        sbHtml.AppendLine($"<tr><td>{outputLine}</td></tr>");
                    }
                }

                sbHtml.AppendLine($"</table></code></pre>");
            }
            catch (Exception e) {
                ServiceLocator.Current.TelemetryService.TrackException(e, false);
                Log.Error(e, "Failed to convert to html - {msg}", e.Message);

                // TODO: Decide what to do here. a) Provide error and fail?
                // b) render without syntax highlighting (call TextFileContent)? Tell the user why?
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
            if (_litehtml == null || _ready == false) {
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

            var extraLines = (pageNum - 1) * _remainingPartialLineHeight;
            var yPos = (int)Math.Round((pageNum - 1) * PageSize.Height) - (int)Math.Round(extraLines);

            // Set the clip such that any extraLines are clipped off bottom
            if (!ContentSettings.Diagnostics) {
                g.SetClip(new Rectangle(0, 0, (int)Math.Round(PageSize.Width), (int)Math.Round(PageSize.Height - _remainingPartialLineHeight)));
            }

            var size = new LiteHtmlSize(Math.Round(PageSize.Width), Math.Ceiling(PageSize.Height));

            // Note, setting viewport does nothing
            //litehtml.SetViewport(new LiteHtmlPoint((int)-0, (int)-yPos), new LiteHtmlSize((int)size.Width, (int)size.Height));

            _litehtml.Graphics = g;
            _litehtml.Document.Draw(-0, -yPos, new position {
                x = 0,
                y = 0,
                width = (int)size.Width,
                height = (int)size.Height
            });
            _litehtml.Graphics = null;

            float leftMargin = 0;// containingSheet.GetPageX(pageNum);

            //PaintLineNumberSeparator(g);

            // Diagnostics
            if (ContentSettings.Diagnostics) {
                g.ResetClip();
                var startLine = _linesPerPage * (pageNum - 1);
                var endLine = startLine + _linesPerPage;
                int lineOnPage;
                //int linesInDocument = (int)Math.Round(litehtml.Document.Height() / lineHeight);
                for (lineOnPage = 0; lineOnPage < _linesPerPage; lineOnPage++) {
                    var lineInDocument = lineOnPage + (_linesPerPage * (pageNum - 1));
                    if (lineInDocument < linesInDocument && lineInDocument >= startLine) {// && lineInDocument <= endLine) {
                                                                                          //if (lines[lineInDocument].lineNumber > 0)
                        PaintDiagnosticLineNumber(g, pageNum, lineInDocument);
                        var x = (int)leftMargin;
                        var y = lineOnPage * (int)_lineHeight;
                        //RenderCode(g, lineInDocument, cachedFont, xPos, yPos);
                        g.DrawRectangle(Pens.Red, 0, y, (int)Math.Round(PageSize.Width), (int)_lineHeight);
                    }
                }
            }
            g.Restore(state);
        }

        private void PaintDiagnosticLineNumberSeparator(Graphics g) {
            if (ContentSettings.LineNumbers && ContentSettings.LineNumberSeparator && _lineNumberWidth != 0) {
                g.DrawLine(Pens.Gray, _lineNumberWidth - 2, 0, _lineNumberWidth - 2, PageSize.Height);
            }
        }

        internal void PaintDiagnosticLineNumber(Graphics g, int pageNum, int lineNumber) {
            if (ContentSettings.LineNumbers == true && _lineNumberWidth != 0) {
                var lineOnPage = lineNumber % _linesPerPage;
                // TOOD: Figure out how to make the spacig around separator more dynamic
                lineNumber++;
                var x = (int)(_lineNumberWidth - 6 - MeasureString(g, $"{lineNumber}").Width);
                g.DrawString($"{lineNumber}", cachedFont, Brushes.Orange, x - _lineNumberWidth, lineOnPage * _lineHeight, StringFormat.GenericDefault);
            }
        }
    }
}

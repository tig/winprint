using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Printing;
using System.IO;
using System.Text;

namespace WinPrint.Core.ContentTypes {
    public class PrismFileContent : HtmlFileContent {
        public static new string Type = "Prism";
        public bool LineNumbers { get; set; }

        public override int Render(string document, string title, System.Drawing.Printing.PrinterResolution printerResolution) {
            //var csharpstring = "public void Method()\n{\n}";

            document = CodeToHtml(document, title, Language);
            Debug.WriteLine(document);

#if DEBUG
            var w = new StreamWriter(title + "_.html");
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

            return base.Render(document, title, printerResolution);
        }

        private string CodeToHtml(string document, string file, string language) {
            const string cssTheme = "prism-coy.css";
            //const string cssPrism = "prism.css";
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
                sbHtml.AppendLine($"</style>");
            }
            sbHtml.AppendLine($"</head><body>");

            try {
                ProcessStartInfo psi = new ProcessStartInfo();
                psi.UseShellExecute = false;   // This is important
                psi.CreateNoWindow = true;     // This is what hides the command window.
                psi.FileName = @"node";
                psi.RedirectStandardInput = true;
                psi.RedirectStandardOutput = true;
                using (var node = Process.Start(psi)) {
                    StreamWriter sw = node.StandardInput;
                    //Debug.WriteLine(sbNodeJS.ToString());
                    sw.WriteLine(sbNodeJS.ToString());
                    sw.Close();

                    var ln = LineNumbers ? "line-numbers" : "";
                    sbHtml.AppendLine($"<pre class=\"language-{language} {ln} \"><code class=\"language-csharp\">");
                    while (!node.StandardOutput.EndOfStream) {
                        sbHtml.AppendLine(node.StandardOutput.ReadLine());//.Replace(' ', (char)160));
                    }
                    sbHtml.AppendLine($"</code></pre>");
                    //node.WaitForExit(10000);
                }

            }
            catch (Exception e) {
                Debug.WriteLine(e.Message);
                sbHtml.AppendLine($"<p>Failed to convert to html. {e.Message}</p>");
            }
            sbHtml.AppendLine($"</body></html>");
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
            //    Debug.WriteLine(e.Message);
            //}
            return path + @"\prismjs\themes";
        }

        //public override void PaintPage(Graphics g, int pageNum) {
        //    base.PaintPage(g, pageNum);
        //}
    }
}

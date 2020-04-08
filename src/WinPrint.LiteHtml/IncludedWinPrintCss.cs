using System;
using System.IO;
using System.Reflection;
using System.Text;

namespace LiteHtmlSharp {
    public static class IncludedWinPrintCss {
        public static string CssString => _includedCss.Value;

        private static Lazy<string> _includedCss = new Lazy<string>(GetMasterCssResource);

        private static string GetMasterCssResource() {
            var assembly = typeof(IncludedWinPrintCss).GetTypeInfo().Assembly;
            var masterCssResourceName = assembly.GetName().Name + ".winprint.css";
            using (var reader = new StreamReader(assembly.GetManifestResourceStream(masterCssResourceName), Encoding.UTF8)) {
                return reader.ReadToEnd();
            }
        }
    }
}

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace LiteHtmlSharp
{
    public static class IncludedWinPrintCss
    {
        public static string CssString => _includedCss.Value;

        static Lazy<string> _includedCss = new Lazy<string>(GetMasterCssResource);

        static string GetMasterCssResource()
        {
            var assembly = typeof(IncludedWinPrintCss).GetTypeInfo().Assembly;
            var masterCssResourceName = assembly.GetName().Name + ".winprint.css";
            using (var reader = new StreamReader(assembly.GetManifestResourceStream(masterCssResourceName), Encoding.UTF8))
            {
                return reader.ReadToEnd();
            }
        }
    }
}

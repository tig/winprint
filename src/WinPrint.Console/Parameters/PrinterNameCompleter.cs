using System.Collections;
using System.Collections.Generic;
using System.Drawing.Printing;
using System.Linq;
using System.Linq.Dynamic.Core;
using System.Management.Automation;
using System.Management.Automation.Language;
//using TTRider.PowerShellAsync;

namespace WinPrint.Console {
    public sealed class PrinterNameCompleter : IArgumentCompleter {
        public PrinterNameCompleter() {
        }

        IEnumerable<CompletionResult> IArgumentCompleter.CompleteArgument(string commandName,
                                                                              string parameterName,
                                                                              string wordToComplete,
                                                                              CommandAst commandAst,
                                                                              IDictionary fakeBoundParameters) {
            return GetPrinterNames();
        }

        private static IEnumerable<CompletionResult> GetPrinterNames() {
            // Must provide the quotes
            // https://stackoverflow.com/questions/30633098/powershell-param-validateset-values-with-spaces-and-tab-completion
            return PrinterSettings.InstalledPrinters
                .Cast<string>()
                .Select(printerName => printerName.Contains(" ", System.StringComparison.OrdinalIgnoreCase) ? new CompletionResult($"'{printerName}'") : new CompletionResult(printerName))
                .ToArray<CompletionResult>();
        }
    }
}

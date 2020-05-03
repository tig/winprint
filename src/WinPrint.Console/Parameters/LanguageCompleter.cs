using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Management.Automation;
using System.Management.Automation.Language;
using WinPrint.Core.Models;

namespace WinPrint.Console {
    public sealed class LanguageCompleter : IArgumentCompleter {
        public LanguageCompleter() {
        }

        IEnumerable<CompletionResult> IArgumentCompleter.CompleteArgument(string commandName,
                                                                              string parameterName,
                                                                              string wordToComplete,
                                                                              CommandAst commandAst,
                                                                              IDictionary fakeBoundParameters) {
            var ids = ModelLocator.Current.FileTypeMapping.ContentTypes.Select(l => new CompletionResult(l.Id));
            var aliases = ModelLocator.Current.FileTypeMapping.ContentTypes.SelectMany(l => l.Aliases.Select(a => new CompletionResult(a)));
            return ids.Concat(aliases).OrderBy(l => l.ListItemText);
        }
    }
}

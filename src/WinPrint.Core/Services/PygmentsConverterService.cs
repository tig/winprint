using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using IronPython.Hosting;

namespace WinPrint.Core.Services {
    /// <summary>
    /// Converts (syntax highlights) a document using the Pygments 
    /// Syntax highlighter (https://pygments.org/)
    /// </summary>
    public class PygmentsConverterService {
        internal PygmentsConverterService Create() {
            return new PygmentsConverterService();
        }

        public PygmentsConverterService() {

        }

        public static string Convert(string document) {

            var engine = Python.CreateEngine();

            dynamic builtin = engine.GetBuiltinModule();
            // you can store variables if you want
            dynamic list = builtin.list;
            dynamic itertools = engine.ImportModule("itertools");
            var numbers = new[] { 1, 1, 2, 3, 6, 2, 2 };
            //Debug.WriteLine(builtin.str(list(itertools.chain(numbers, "foobar"))));
            // prints `[1, 1, 2, 3, 6, 2, 2, 'f', 'o', 'o', 'b', 'a', 'r']`

            // to add to the search paths
            //var searchPaths = engine.GetSearchPaths();
            //searchPaths.Add(@"modules");
            //engine.SetSearchPaths(searchPaths);

            //// import the module
            //dynamic myModule = engine.ImportModule("mymodule");

            return string.Join(",", engine.GetModuleFilenames());  
        }

    }
}

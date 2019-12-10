using System;
using System.Collections.Generic;
using System.Text;
using CommandLine;
using CommandLine.Text;

namespace WinPrint.Core.Models {
    public class Options {

        // Files
        [Value(1, Required = true, MetaName = "Files", HelpText = "Files to be printed.")]
        public IEnumerable<string> Files { get; set; }

        // Print options
        [Option('s', "sheet", Required = false, Default = "default", HelpText = "Sheet defintion to use for formatting. Use sheet ID or friendly name.")]
        public string Sheet { get; set; }

        [Option('l', "landscape", Required = false, Default = false, HelpText = "Force printing in landscape mode.")]
        public bool Landscape { get; set; }

        [Option('r', "portrait", Required = false, Default = false, HelpText = "Force printing in portrait mode.")]
        public bool Portrait { get; set; }

        [Option('p', "printer",  HelpText = "Printer name.")]
        public string Printer { get; set; }

        [Option('z', "paper-size", HelpText = "Paper size name.")]
        public string PaperSize { get; set; }

        [Option('f', "from-page", Default = 1, HelpText = "Number of first page to print (may be used with --to-page).")]
        public int FromPage { get; set; }

        [Option('t', "to-page", Default = 1, HelpText = "Number of last page to print (may be used with --from-page).")]
        public int ToPage { get; set; }

        [Option('c', "count-pages", Default = false, Required = false, HelpText = "Exit code is set to numer of pages that would be printed. Use --verbose to diplsay the count.")]
        public bool CountPages { get; set; }

        // App Options
        [Option('v', "verbose", Default = false, HelpText = "Verbose console output.")]
        public bool Verbose { get; set; }

        [Option('g', "gui", Default = false, SetName = "gui", HelpText = "Show WinPrint GUI App. Priting will automatically start.")]
        public bool Gui { get; set; }

        [Option('x', "exit", Required = false, Default = true, SetName = "gui", HelpText = "Exit GUI App when done printing. Valid only with --gui.")]
        public bool Exit { get; set; }


        [Usage(ApplicationAlias = "winprint")]
        public static IEnumerable<Example> Examples {
            get {
                IEnumerable<string> list = new List<string>() { { "Program.cs" } };
                return new List<Example>() {
                    new Example("Print a single file in landscape mode", new Options {
                        Files = list,
                        Landscape = true
                    })
              };
            }
        }

    }
}

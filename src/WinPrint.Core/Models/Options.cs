using System;
using System.Collections.Generic;
using System.Text;
using CommandLine;
using CommandLine.Text;

namespace WinPrint.Core.Models {
    public class Options : ModelBase {

        // Files
        [Value(0, Required = true, MetaName = "<files>", HelpText = "One or more files to be printed.")]
        public IEnumerable<string> Files { get; set; }

        // Print options
        [Option('s', "sheet", Required = false, HelpText = "Sheet defintion to use for formatting. Use sheet ID or friendly name.")]
        public string Sheet { get; set; }

        [Option('l', "landscape", Required = false, Default = false, HelpText = "Force landscape orientation.")]
        public bool Landscape { get; set; }

        [Option('r', "portrait", Required = false, Default = false, HelpText = "Force portrait orientation.")]
        public bool Portrait { get; set; }

        [Option('p', "printer", HelpText = "Printer name.")]
        public string Printer { get; set; }

        [Option('z', "paper-size", HelpText = "Paper size name.")]
        public string PaperSize { get; set; }

        [Option('f', "from-sheet", Default = 0, HelpText = "Number of first sheet to print (may be used with --to-sheet).")]
        public int FromPage { get; set; }

        [Option('t', "to-sheet", Default = 0, HelpText = "Number of last sheet to print (may be used with --from-sheet).")]
        public int ToPage { get; set; }

        [Option('c', "count-sheets", Default = false, Required = false, HelpText = "Exit code is set to numer of sheets that would be printed. Use --verbose to diplsay the count.")]
        public bool CountPages { get; set; }

        // App Options
        [Option('v', "verbose", Default = false, HelpText = "Verbose console output (log is always verbose).")]
        public bool Verbose { get; set; }

        [Option('d', "debug", Default = false, HelpText = "Debug-level console & log output.")]
        public bool Debug { get; set; }

        [Option('g', "gui", Default = false, SetName = "gui", HelpText = "Show WinPrint GUI (to preview or change sheet settings).")]
        public bool Gui { get; set; }

        [Usage(ApplicationAlias = "winprint")]
        public static IEnumerable<Example> Examples {
            get {
                return new List<Example>() {
                    new Example("Print Program.cs in landscape mode", new Options {
                        Files = new List<string>() { { "Program.cs" } },
                        Landscape = true
                    }),
                    new Example("Print all .cs files on a specific printer with a specific paper size", new Options {
                        Files = new List<string>() { { "*.cs" } },
                        Printer = "Fabricam 535",
                        PaperSize = "A4"
                    }),                    
                    new Example("Print the first two sheets of Program.cs", new Options {
                        Files = new List<string>() { { "Program.cs" } },
                        FromPage = 1,
                        ToPage = 2
                    }),
                    new Example("Print Program.cs using the 2 Up sheet defintion", new Options {
                        Files = new List<string>() { { "Program.cs" } },
                        Sheet = "2 Up"
                    })

              };
            }
        }

    }
}

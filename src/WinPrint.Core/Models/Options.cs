using System.Collections.Generic;
using System.Linq;
using System.Text.Json.Serialization;
using CommandLine;
using CommandLine.Text;

namespace WinPrint.Core.Models {
    public class Options : ModelBase {

        // Files
        [JsonIgnore]
        [Value(0, Required = true, MetaName = "<files>", HelpText = "One or more files to be printed.")]
        public IEnumerable<string> Files { get; set; }

        /// <summary>
        /// Provides the count of files specified on the command line for telemetry purposes.
        /// </summary>
        [JsonIgnore]
        [SafeForTelemetry]
        // TODO: This won't work once we support wildcards
        public int NumFiles => Files.Count();

        // Print options
        [SafeForTelemetry]
        [Option('s', "sheet", Required = false, HelpText = "Sheet definition to use for formatting. Use sheet ID or friendly name.")]
        public string Sheet { get; set; }

        [SafeForTelemetry]
        [Option('l', "landscape", Required = false, Default = false, HelpText = "Force landscape orientation.")]
        public bool Landscape { get; set; }

        [SafeForTelemetry]
        [Option('r', "portrait", Required = false, Default = false, HelpText = "Force portrait orientation.")]
        public bool Portrait { get; set; }

        [SafeForTelemetry]
        [Option('p', "printer", HelpText = "Printer name.")]
        public string Printer { get; set; }

        [SafeForTelemetry]
        [Option('z', "paper-size", HelpText = "Paper size name.")]
        public string PaperSize { get; set; }

        [SafeForTelemetry]
        [Option('f', "from-sheet", Default = 0, HelpText = "Number of first sheet to print (may be used with --to-sheet).")]
        public int FromPage { get; set; }

        [SafeForTelemetry]
        [Option('t', "to-sheet", Default = 0, HelpText = "Number of last sheet to print (may be used with --from-sheet).")]
        public int ToPage { get; set; }

        [SafeForTelemetry]
        [Option('c', "count-sheets", Default = false, Required = false, HelpText = "Exit code is set to number of sheets that would be printed. Use --verbose to display the count.")]
        public bool CountPages { get; set; }

        [SafeForTelemetry]
        [Option('e', "content-type-engine", Default = "", Required = false, HelpText = "Name of the Content Type Engine to use for rendering (\"text/plain\", \"text/html\", or \"<language>\".")]
        public string ContentType { get; set; }

        // App Options
        [SafeForTelemetry]
        [Option('v', "verbose", Default = false, HelpText = "Verbose console output (log is always verbose).")]
        public bool Verbose { get; set; }

        [SafeForTelemetry]
        [Option('d', "debug", Default = false, HelpText = "Debug-level console & log output.")]
        public bool Debug { get; set; }

        [SafeForTelemetry]
        [Option('g', "gui", Default = false, SetName = "gui", HelpText = "Show WinPrint GUI (to preview or change sheet settings).")]
        public bool Gui { get; set; }

        [Usage(ApplicationAlias = "winprint")]
        public static IEnumerable<Example> Examples => new List<Example>() {
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
                    new Example("Print Program.cs using the 2 Up sheet definition", new Options {
                        Files = new List<string>() { { "Program.cs" } },
                        Sheet = "2 Up"
                    }),
                    new Example("Print tapes.pas using C-like syntax highlighting.", new Options {
                        Files = new List<string>() { { "tapes.pas" } },
                        ContentType= "clike"
                    })
              };

    }
}

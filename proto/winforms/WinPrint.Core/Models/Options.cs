using System;
using System.Collections.Generic;
using System.Text;
using CommandLine;

namespace WinPrint.Core.Models {
    public class Options {


        [Value(1, Required = false, MetaName = "Files", HelpText = "Files to be printed.")]
        public IEnumerable<string> Files { get; set; }

        [Option('l', "landscape", Required = false, HelpText = "Print in landscape mode.")]
        public bool Landscape { get; set; }

        [Option('p', "printer", Default = "", HelpText = "Printer name to print to.")]
        public string Printer { get; set; }

        [Option('s', "papersize", Default = "", HelpText = "Paper size to print to.")]
        public string PaperSize { get; set; }

        [Option('g', "gui", Default = false, SetName = "gui", HelpText = "Show GUI.")]
        public bool Gui{ get; set; }

        [Option('x', "exit", Default = true, SetName = "gui", HelpText = "Exit when done printing. Valid on with -g.")]
        public bool Exit { get; set; }

    }
}

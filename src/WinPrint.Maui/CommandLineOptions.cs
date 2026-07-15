using CommandLine;
using CommandLine.Text;
using WinPrint.Core.Models;

namespace WinPrint.Maui;

/// <summary>
///     CommandLineParser-attributed surface for MAUI. Maps onto <see cref="Options" /> after parse.
/// </summary>
public sealed class CommandLineOptions
{
    [Value(0, Required = true, MetaName = "<files>", HelpText = "One or more files to be printed.")]
    public IEnumerable<string>? Files { get; set; }

    [Option('s', "sheet", Required = false,
        HelpText = "Sheet definition to use for formatting. Use sheet ID or friendly name.")]
    public string? Sheet { get; set; }

    [Option('l', "landscape", Required = false, Default = false, HelpText = "Force landscape orientation.")]
    public bool Landscape { get; set; }

    [Option('r', "portrait", Required = false, Default = false, HelpText = "Force portrait orientation.")]
    public bool Portrait { get; set; }

    [Option('p', "printer", HelpText = "Printer name.")]
    public string? Printer { get; set; }

    [Option('z', "paper-size", HelpText = "Paper size name.")]
    public string? PaperSize { get; set; }

    [Option('f', "from-sheet", Default = 0,
        HelpText = "Number of first sheet to print (may be used with --to-sheet).")]
    public int FromPage { get; set; }

    [Option('t', "to-sheet", Default = 0, HelpText = "Number of last sheet to print (may be used with --from-sheet).")]
    public int ToPage { get; set; }

    [Option('c', "count-sheets", Default = false, Required = false,
        HelpText = "Exit code is set to number of sheets that would be printed. Use --verbose to display the count.")]
    public bool CountPages { get; set; }

    [Option('e', "content-type", Default = "", Required = false,
        HelpText =
            "Content type engine / language override for rendering (\"text/plain\", \"text/html\", or \"<language>\").")]
    public string? ContentType { get; set; }

    [Option("rows", HelpText = "Number of page rows per sheet.")]
    public int Rows { get; set; }

    [Option("columns", HelpText = "Number of page columns per sheet.")]
    public int Columns { get; set; }

    [Option("header-on", HelpText = "Enable the header.")]
    public bool HeaderOn { get; set; }

    [Option("header-off", HelpText = "Disable the header.")]
    public bool HeaderOff { get; set; }

    [Option("footer-on", HelpText = "Enable the footer.")]
    public bool FooterOn { get; set; }

    [Option("footer-off", HelpText = "Disable the footer.")]
    public bool FooterOff { get; set; }

    [Option("header-text", HelpText = "Header text (may include macros).")]
    public string? HeaderText { get; set; }

    [Option("footer-text", HelpText = "Footer text (may include macros).")]
    public string? FooterText { get; set; }

    [Option("header-font", HelpText = "Header font, e.g. \"Cascadia Code, 9, bold\".")]
    public string? HeaderFont { get; set; }

    [Option("footer-font", HelpText = "Footer font, e.g. \"Comic Sans MS, 10, bold\".")]
    public string? FooterFont { get; set; }

    [Option("header-border-top-on")] public bool HeaderBorderTopOn { get; set; }
    [Option("header-border-top-off")] public bool HeaderBorderTopOff { get; set; }
    [Option("header-border-bottom-on")] public bool HeaderBorderBottomOn { get; set; }
    [Option("header-border-bottom-off")] public bool HeaderBorderBottomOff { get; set; }
    [Option("header-border-left-on")] public bool HeaderBorderLeftOn { get; set; }
    [Option("header-border-left-off")] public bool HeaderBorderLeftOff { get; set; }
    [Option("header-border-right-on")] public bool HeaderBorderRightOn { get; set; }
    [Option("header-border-right-off")] public bool HeaderBorderRightOff { get; set; }
    [Option("footer-border-top-on")] public bool FooterBorderTopOn { get; set; }
    [Option("footer-border-top-off")] public bool FooterBorderTopOff { get; set; }
    [Option("footer-border-bottom-on")] public bool FooterBorderBottomOn { get; set; }
    [Option("footer-border-bottom-off")] public bool FooterBorderBottomOff { get; set; }
    [Option("footer-border-left-on")] public bool FooterBorderLeftOn { get; set; }
    [Option("footer-border-left-off")] public bool FooterBorderLeftOff { get; set; }
    [Option("footer-border-right-on")] public bool FooterBorderRightOn { get; set; }
    [Option("footer-border-right-off")] public bool FooterBorderRightOff { get; set; }

    [Option('v', "verbose", Default = false, HelpText = "Verbose console output (log is always verbose).")]
    public bool Verbose { get; set; }

    [Option('d', "debug", Default = false, HelpText = "Debug-level console & log output.")]
    public bool Debug { get; set; }

    [Option('g', "gui", Default = false, SetName = "gui",
        HelpText = "Show WinPrint GUI (to preview or change sheet settings).")]
    public bool Gui { get; set; }

    [Usage(ApplicationAlias = "winprint")]
    public static IEnumerable<Example> Examples => new List<Example>
    {
        new("Print Program.cs in landscape mode",
            new CommandLineOptions { Files = new List<string> { "Program.cs" }, Landscape = true }),
        new("Print all .cs files on a specific printer with a specific paper size",
            new CommandLineOptions { Files = new List<string> { "*.cs" }, Printer = "Fabricam 535", PaperSize = "A4" }),
        new("Print the first two sheets of Program.cs",
            new CommandLineOptions { Files = new List<string> { "Program.cs" }, FromPage = 1, ToPage = 2 }),
        new("Print Program.cs using the 2 Up sheet definition",
            new CommandLineOptions { Files = new List<string> { "Program.cs" }, Sheet = "2 Up" }),
        new("Print tapes.pas using C-like syntax highlighting.",
            new CommandLineOptions { Files = new List<string> { "tapes.pas" }, ContentType = "clike" })
    };

    public void ApplyTo(Options target)
    {
        target.Files = Files is null ? null : Files.ToList();
        target.Sheet = Sheet;
        target.Landscape = Landscape;
        target.Portrait = Portrait;
        target.Printer = Printer;
        target.PaperSize = PaperSize;
        target.FromPage = FromPage;
        target.ToPage = ToPage;
        target.CountPages = CountPages;
        target.ContentType = ContentType;
        target.Rows = Rows;
        target.Columns = Columns;
        target.HeaderOn = HeaderOn;
        target.HeaderOff = HeaderOff;
        target.FooterOn = FooterOn;
        target.FooterOff = FooterOff;
        target.HeaderText = HeaderText;
        target.FooterText = FooterText;
        target.HeaderFont = HeaderFont;
        target.FooterFont = FooterFont;
        target.HeaderBorderTopOn = HeaderBorderTopOn;
        target.HeaderBorderTopOff = HeaderBorderTopOff;
        target.HeaderBorderBottomOn = HeaderBorderBottomOn;
        target.HeaderBorderBottomOff = HeaderBorderBottomOff;
        target.HeaderBorderLeftOn = HeaderBorderLeftOn;
        target.HeaderBorderLeftOff = HeaderBorderLeftOff;
        target.HeaderBorderRightOn = HeaderBorderRightOn;
        target.HeaderBorderRightOff = HeaderBorderRightOff;
        target.FooterBorderTopOn = FooterBorderTopOn;
        target.FooterBorderTopOff = FooterBorderTopOff;
        target.FooterBorderBottomOn = FooterBorderBottomOn;
        target.FooterBorderBottomOff = FooterBorderBottomOff;
        target.FooterBorderLeftOn = FooterBorderLeftOn;
        target.FooterBorderLeftOff = FooterBorderLeftOff;
        target.FooterBorderRightOn = FooterBorderRightOn;
        target.FooterBorderRightOff = FooterBorderRightOff;
        target.Verbose = Verbose;
        target.Debug = Debug;
        target.Gui = Gui;
    }
}

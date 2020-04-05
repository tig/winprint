---
title: User's Guide
---
## Features

* Print source code with syntax highlighting and line numbering for over 200 programming langauges and file formats.
* Print HTML files.
* Print "multiple-pages-up" on one piece of paper (saves trees!)
* Complete control over page formatting options, including headers and footers, margins, fonts, page orientation, etc...
* Headers and Footers support detailed file and print information macros with rich date/time formatting.
* Simple and elegant graphical user interface with accurate print preview.
* PowerShell-based command line interface. Allows winprint to be used from other applications or solutions. `out-winprint` is a drop-in replacement for `out-printer`.
* Sheet Definitions make it easy to save settings for frequent print jobs.
* Comprehensive logging.
* Cross platform. Even though it's named **win**print, it works on Windows, Linux (command line only; some assembly required), and (not yet tested) Mac OS.

## Command Line Interfaces

winprint 2.0 alpha supports two command line interfaces: a traditional interface implemented in `winprint.exe` and a PowerShell CmdLet (`out-winprint`). The `winprint.exe` version is really just a simple wrapper that invokes the `out-winprint` PowerShell Cmdlet. 

### Powershell out-winprint CmdLet

The CmdLet version of **winprint** is designed to be a stand-in for the `out-printer` CmdLet PowerShell already provides.

Invoke the CmdLet using either `out-winprint` or the shortcut, `wp`. 

#### Examples:

Print `Program.cs` using the default sheet definition and default printer:

    get-content Program.cs | out-winprint

Or:

    cat program.cs | wp

Print all `.c` and `.h` files in the current directory to the "HP LaserJet" printer, showing verbose output along the way:

    ls .\* -include ('*.c', '*.h') | foreach { cat $_.FullName | out-winPrint -p "HP LaserJet" -FileName $_.FullName}


#### CmdLet Help

Access the built-in help by typing `get-help out-winprint` or `get-help out-winprint -full`.

To create a nice PDF version of the help, do this:

    get-help out-winprint -full | out-winprint -p "Microsoft Print to PDF" -s "Default 1-Up" -Title "winprint Help" -LineNumbers No



```
NAME
    Out-WinPrint

SYNTAX
    Out-WinPrint [[-Name] <string>] [-SheetDefintion <string>] [-ContentTypeEngine <string>] [-InputObject <psobject>]
    [<CommonParameters>]


PARAMETERS
    -ContentTypeEngine <string>
        Name of the WinPrint Content Type Engine to use (default is "text/plain")

        Required?                    false
        Position?                    Named
        Accept pipeline input?       false
        Parameter set name           (All)
        Aliases                      cte
        Dynamic?                     false

    -InputObject <psobject>

        Required?                    false
        Position?                    Named
        Accept pipeline input?       true (ByValue)
        Parameter set name           (All)
        Aliases                      None
        Dynamic?                     false

    -Name <string>
        Printer name.

        Required?                    false
        Position?                    0
        Accept pipeline input?       false
        Parameter set name           (All)
        Aliases                      PrinterName
        Dynamic?                     false

    -SheetDefintion <string>
        Name of the WinPrint sheet definition to use (e.g. "Default 2-Up"

        Required?                    false
        Position?                    Named
        Accept pipeline input?       false
        Parameter set name           (All)
        Aliases                      Sheet
        Dynamic?                     false

    <CommonParameters>
        This cmdlet supports the common parameters: Verbose, Debug,
        ErrorAction, ErrorVariable, WarningAction, WarningVariable,
        OutBuffer, PipelineVariable, and OutVariable. For more information, see
        about_CommonParameters (https://go.microsoft.com/fwlink/?LinkID=113216).


INPUTS
    System.Management.Automation.PSObject


OUTPUTS
    System.Object

ALIASES
    wp
```

### Traditional winprint.exe Command Line

Tunning `winprint.exe` from any command-line is effectively the same as doing the following in PowerShell:

```powershell
pwsh.exe -noprofile -command "import-module .\winprint.dll; out-winprint $args"
```

Examples:

Print Program.cs in landscape mode:

    winprint --landscape Program.cs

Print all .cs files on a specific printer with a specific paper size:

    winprint --printer "Fabricam 535" --paper-size A4 *.cs

Print the first two pages of Program.cs:

    winprint --from-sheet 1 --to-sheet 2 Program.cs

Print Program.cs using the 2 Up sheet defintion:

    winprint --sheet "2 Up" Program.cs

* `-s`, `--sheet` - Sheet defintion to use for formatting. Use sheet ID or friendly name.

* `-l`, `--landscape` - Force landscape orientation.

* `-r`, `--portrait` - Force portrait orientation.

* `-p`, `--printer` - Printer name.

* `-z`, `--paper-size` - Paper size name.

* `-f`, `--from-sheet` - (Default: 0) Number of first sheet to print (may be used with `--to-sheet`).

* `-t`, `--to-sheet` - (Default: 0) Number of last sheet to print (may be used with `--from-sheet`).

* `-c`, `--count-sheet` - (Default: false) Exit code is set to numer of sheet that would be printed. Use `--verbose` to diplsay the count.

* `-e`, `--content-type-engine` - Name of the Content Type Engine to use for rendering (`text/plain`, `text/html`, or `<language>`).

* `-v`, `--verbose` - (Default: false) Verbose console output (log is always verbose).

* `-d`, `--debug` - (Default: false) Debug-level console & log output.

* `-g`, `--gui` - (Default: false) Show *winprint* GUI (to preview or change sheet settings).

* `--help` - Display this help screen.

* `--version` - Display version information.

* `<files>` - Required. One or more files to be printed.



## Graphical User Interface

When run as a Windows app (`winprintgui.exe`), *winprint* provides an easy to use GUI for previewing how a file will be printed and changing many settings.

The **File button** opens a File Open Dialog for choosing the file to preview and/or print. The GUI app can print a single file at a time. Use the console verion (`winprint.exe`) to print multiple files at once.

The **Print button** prints the currently selected file.

The **Settings (⚙) button** will open `WinPrint.config.json` in your favorite text editor. Changes made to the file will be reflected in the GUI automatically.

## Sheet Definitions

Font choices, header/footer options, and other print-job settings are defined in *winprint* as *Sheet Definitions*. In the *winprint* world a **Sheet** is a side of a sheet of paper. Depending on how its configured, *winprint* will print one or more **Pages** on each **Sheet**.

This is called "n-up" printing. The most common form of "n-up" printing is "2-up" where the page orientation is set to landscape and there are two columns of pages.

The layout and format of the **Sheet** is defined by a set of configuration settings called a **Sheet Definition**. Out of the box *winprint* comes with two: `Default 1 Up` and `Default 2 Up`.

**Sheet Definitions** are defined and stored in the `WinPrint.config.json` configuration file found in `%appdata%\Kindel Systems\winprint`.

### Headers & Footers Macros

The format for header & footer specifiers is:

    <left part>|<center part>|<right part>

where `<left part>`,`<center part>`, and `<right part>` can be composed of text and any of the **Macros** described below. For example:

    {DateRevised:D}|{FullyQualifiedPath}|{FileType}

* `{NumPages}` - The total number of **Sheets** in the file.

* `{Page}` - The current **Sheet** number.

* `{FileExtension}` - The file extension of the file.

* `{FileName}` - The name of file without the extension.

* `{FilePath}` - The path to the file without the filename or extension.

* `{FullyQualifiedPath}` - The full path to the file, inclidng the filename and extension.

* `{FileType}` - The file type (for `text/plain` and `text/html`) or language (for `sourcecode`) of the file. 

* `{DatePrinted}` - The current date & time (see formatting below).

* `{DateRevised}` - The date & time the file was last revised.

The `{DatePrinted}` and `{DateRevised}` macros support the full set of [standard .NET date and time formatting modifiers](https://docs.microsoft.com/en-us/dotnet/standard/base-types/standard-date-and-time-format-strings). For example `{DateRevised:M}` will generate just the month and date (e.g. `September 10`) while `{DateRevised:t}` will generate just the `short time` (`4:55 PM`).

### Modifying Sheet Definitions

The *winprint* GUI can be used to change most Sheet Definition settings. All settings can be changed by editing the `WinPrint.config.json` file. Note if `WinPrint.config.json` is changed while the *winprint* GUI App is running, it will detect the change and re-flow the currently loaded file. In other-words, a text editor can be used as the UI for advanced settings.

### Creating new Sheet Definitions

*winprint* starts with two "built-in" Sheet Definitions: `Default 1-Up` and `Default 2-Up`. Additional Sheet Definitions can be created by editing `WinPrint.config.json`, copying one of the existing Sheet Defintions and giving it a new unique `name` and unique `ID`.

## Content Types

*winprint* supports three types of files. Support for each is provided by a *winprint* Content Type Engine (CTE):

1. **`text/plain`** - This CTE knows only how to print raw `text/plain` files. The format of the printed text can be changed (e.g. to turn off line numbers or use a differnt font). Lines that are too long for a page are wrapped at character boundaries. `\r` (formfeed) characters can be made to cause following text to print on the next page (this is off by default). Settings for the `text/plain` can be changed by editing the `textFileSettings` section of a Sheet Definition in the `WinPrint.config.json` file. 

2. **`text/html`** - This CTE can render html files. Any CSS specified inline in the HTML file will be honored. External CSS files must be local. For HTML without CSS, the default CSS used can be overridden by providing a file named `winprint.css` in the `%appdata%\Kindel Systems\winprint` folder. `text/html` does not support line numbers.

3. **`text/sourcecode`** - The sourcecode CTE supports syntax highlighting (pretty printing), with optional line numbering, of over 200 programming languages. The style of the printing can be changed by providing a file named `winprint-prism.css` in the `%appdata%\Kindel Systems\winprint` folder. The styles defined in this format shold match those defined for [PrismJS](https://prismjs.com). Any PrismJS style sheet will work with *winprint*.

The extension of the file being printed (e.g. `.cs`) is determines which Content Type rendering engine will be used. *winprint* has a built-in library of hundreds of file extension to content type/language mappings.

To associate a file extension with a particular Content Type Engine modify the `files.associations` section of `WinPrint.config.json` appropriately. For example to associate files with a `.htm` extension with the `text/html` Content Type Engine add a line as shown below (the `WinPrint.config.json` generated when *winprint* runs the first time already provides this example, as an example):

    "files.associations": {
      "*.htm": "text/html",
    }

For associating file extentions with a particular programming language using the `text/sourcecode` Content Type Engine see below.

The commandline option `-e`/`--content-type-engine` overrides content type and language detection.

## Language Associations

*winprint*'s `text/sourcecode` Content Type Engine knows how to syntax highlight (pretty print) over 200 programming languages. It has a built-in file extension to language mapping that should work for most modern scenarios. For example it knows that `.cs` files hold `C#` and `.bf` files hold `brainfuck`.

### Adding or Changing `text/sourcecode` Language Associations

To associate a file extension with a language spported by `text/sourcecode` modify the `files.associations` and `languages` sections of `WinPrint.config.json` appropriately. For example to associate files with a `.config` extension with the JSON langauge  add a line as shown below (the `WinPrint.config.json` generated when *winprint* runs the first time already provides this example, as an example):

    "files.associations": {
      "*.config": "json"
    }

To determine the name to use (e.g. `json`) see the [PrismJS](https://prismjs.com/#supported-languages) list of languages.

A new langauge can be defined by aliasing it to an existing language by modifying the `languages` section of `WinPrint.config.json`. 

For example to enable the [Icon Programming Language](https://en.wikipedia.org/wiki/Icon_%28programming_language%29) which is a very C-like language the `files.associations` and `languages` sections would look like the following:

    "files.associations": {
      "*.icon": "icon"
    },
    "languages": [
      {
        "id": "icon",
        "extensions": [
          ".icon"
        ],
        "aliases": [
          "clike"
        ]
      }
    ]

## Logging & Diagnostics

**winprint** writes extensive diagnostic logs to `%appdata%/Kindel Systems/WinPrint/logs`. When using the command line (`winprint.exe`) specifiying the `-d`/`--debug` command line switch will cause all disagnostic log entries to go to the console as well as the log file.

Additional printing diagnostics can be turned on via settings in the configuration file. 

To help diagnose printer-related rendering issues, or issues with Sheet Definitions, **winprint** set the appropriate diagnostic flags found at the end of the config file to `true` (each flag has a print and print preview variant):

```
  "previewPrintableArea": false,
  "printPrintableArea": false,
  "previewPaperSize": false,
  "printPaperSize": false,
  "previewMargins": false,
  "printMargins": false,
  "previewHardMargins": false,
  "printHardMargins": false,
  "printBounds": false,
  "previewBounds": false,
  "printContentBounds": false,
  "previewContentBounds": false,
  "printHeaderFooterBounds": false,
  "previewHeaderFooterBounds": false,
  "previewPageBounds": false,
  "printPageBounds": false
```

To help daignose why content may be rendering incorrectly, set `diagnostics` to `true` in any `contentSettings` section. This will cause the Content Type Engine to print/display diagnostic rules.

```
      "contentSettings": {
        "diagnostics": false
      },
```


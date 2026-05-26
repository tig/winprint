# User's Guide

## Features

* Prints source code with syntax highlighting and line numbering using bundled TextMate grammars.
* Prints HTML files.
* Prints "multiple-pages-up" on one piece of paper (saves trees!)
* Complete control over page formatting options, including headers and footers, margins, fonts, page orientation, etc...
* Headers and Footers support detailed file and print information macros with rich date/time formatting.
* Simple and elegant graphical user interface with accurate print preview.
* `winprint` provides a Terminal.Gui.Cli-based command line with JSON output and OpenCLI metadata for agents.
* The legacy PowerShell `out-winprint` CmdLet remains available as `WinPrint.PowerShell.dll`, but is deprecated in favor of `winprint.exe`.
* Sheet Definitions make it easy to save settings for frequent print jobs.
* Comprehensive logging.
* Cross platform. Even though it's named **win**print, it works on Windows, Linux (command line only; some assembly required), and (not yet tested) Mac OS.

## Command Line Interfaces

**winprint** has two command line surfaces:

* **`winprint`** - a Terminal.Gui.Cli-based executable intended for scripts, automation, and agents. It can return a JSON envelope and exposes OpenCLI metadata.
* **`out-winprint`** - a deprecated PowerShell CmdLet designed as a stand-in for PowerShell's built-in `out-printer`.

### `winprint`

Use `winprint` to print files or redirected text without importing a PowerShell module.

Count sheets without printing:

```powershell
winprint Program.cs --what-if
```

Return a JSON envelope:

```powershell
winprint Program.cs --what-if --json
```

Print to a named printer and override the detected language:

```powershell
winprint Program.cs --printer "Microsoft Print to PDF" --language csharp --title "Program.cs"
```

Pipe text through stdin:

```powershell
Get-Content $profile.CurrentUserAllHosts | winprint --language powershell --title "PowerShell profile"
```

Open the GUI preview for a file:

```powershell
winprint Program.cs --gui
```

Expose machine-readable command metadata:

```powershell
winprint --opencli
```

Common `winprint` options include `--printer`, `--paper-size`, `--sheet`, `--language`, `--content-type`, `--orientation`, `--line-numbers`, `--from-sheet`, `--to-sheet`, `--what-if`, `--gui`, and `--config`. The Terminal.Gui.Cli framework also provides `--help`, `--version`, `--json`, `--output`, `--initial`, `--timeout`, and `--opencli`.

### Deprecated PowerShell CmdLet

The PowerShell CmdLet (`out-winprint`) is deprecated. Prefer the `winprint` executable for new scripts and automation.

**`out-winprint`** is designed to be a stand-in for the `out-printer` CmdLet built into PowerShell.

If you still need the deprecated CmdLet, import it into PowerShell by adding an `import-module` statement in the Powershell profile:

```powershell
import-module '.\src\WinPrint.PowerShell\bin\x64\Debug\net10.0-windows\WinPrint.PowerShell.dll'
```

There is no installer in this branch; import the module from the build output folder if you still need the deprecated CmdLet.

Invoke the deprecated CmdLet using either `out-winprint`, `winprint`, or the shortcut, `wp`.

Specify the `-Verbose` parameter switch to have **winprint** show progress. Otherwise, **winprint** is pretty darn quiet, respecting the norms of PowerShell.

#### Examples

See what version is installed:

```powershell
PS > out-winprint -verbose
VERBOSE: Out-WinPrint 2.0.4.1 - Copyright Kindel Systems, LLC - https://tig.github.io/winprint
PS >
```

Print `Program.cs` using the default sheet definition and default printer:

```powershell
get-content Program.cs | out-winprint
```

Or, more tersely:

```powershell
cat program.cs | wp
```

```powershell
cat $profile.CurrentUserAllHosts | wp -Language powershell
```

Or, using more features:

```powershell
PS > cat Program.cs | wp -PrinterName PDF -Orientation Portrait -Verbose -Title Program.cs
VERBOSE: Out-WinPrint 2.0.0.3912 - Copyright Kindel Systems, LLC - https://tig.github.io/winprint
VERBOSE:     Printer:          PDF
VERBOSE:     Paper Size:       Letter
VERBOSE:     Orientation:      Portrait
VERBOSE:     Sheet Definition: Default 2-Up (0002a500-0000-0000-c000-000000000046)
VERBOSE: Printing sheet 1
VERBOSE: Printing sheet 2
VERBOSE: Printed a total of 2 sheets.
PS >
```

The following all do the same thing:

```powershell
out-winprint -FileName program.cs
wp program.cs
winprint program.cs
cat program.cs | wp -Title "program.cs"
```

Print all `.c` and `.h` files in the current directory to the "HP LaserJet" printer, ensuring the `{Title`} in the header/footers shows the filename. Present verbose output along the way:

```powershell
ls .\* -include ('*.c', '*.h') | foreach { cat $_.FullName | out-winPrint -p "HP LaserJet" -title $_.FullName -verbose}
```

Some **`out-winprint`** parameters support *Intellisense*, meaning that you can use `tab` on the command line to automatically cycle through possible options instead of typing. These paramters support *Intellisense*:

* **`-PrinterName`** - *Intellisense* values are the names of all currently installed printers.

* **`-PaperSize`** - *Intellisense* values are the names of all papers sizes supported by whatever printer has been specified by `-PrinterName`.

* **`-SheetDefinition`** - *Intellisense* values are the names of all current Sheet Definitions defined in the `WinPrint.config.json` file. E.g. `"Default 2-Up"`.

* **`-ContentTypeEngine`** - *Intellisense* values are the names of all current Content Type Engines **winprint** supports (`text/plain`, `text/html`, and `text/ansi`).

* **`-Language`** - *Intellisense* values are the names of languages supported for syntax highlighting.

#### CmdLet Help

Access the built-in help by typing `get-help out-winprint` or `get-help out-winprint -full`.

To create a nice PDF version of the help, do this:

```powershell
get-help out-winprint -full | out-winprint -p 'Microsoft Print to PDF' -s 'Default 1-Up' -Title 'winprint Help' -LineNumbers No
```

```powershell

NAME
    Out-WinPrint

SYNTAX
    Out-WinPrint [<CommonParameters>]

    Out-WinPrint [[-FileName] <string>] [-PrinterName <string>] [-Orientation {Portrait | Landscape}] [-LineNumbers {No | Yes}] [-Language <string>] [-Title <string>] [-FromSheet <int>] [-ToSheet <int>] [-Gui] [-InputObject <psobject>] [-WhatIf] [-PaperSize <string>] [-SheetDefinition {Default 2-Up | Default 1-Up}] [-ContentTypeEngine {text/html | text/ansi | text/plain}] [<CommonParameters>]

    Out-WinPrint [-InstallUpdate] [-Force] [<CommonParameters>]

    Out-WinPrint [-Config] [<CommonParameters>]

```

## Graphical User Interface

When run as a Windows app (`winprintgui.exe`), **winprint** provides an easy to use GUI for previewing how a file will be printed and changing many settings. Start **winprint** from the Windows Start Menu like any other app.

The **File button** opens a File Open Dialog for choosing the file to preview and/or print. The GUI app can print a single file at a time. Use the `winprint` command line to print multiple files at once.

The **Print button** prints the currently selected file.

The **Settings (⚙) button** will open `WinPrint.config.json` in your favorite text editor. Changes made to the file will be reflected in the GUI automatically.

## Sheet Definitions

Font choices, header/footer options, and other print-job settings are defined in **winprint** as *Sheet Definitions*. In the **winprint** world a **Sheet** is a side of a sheet of paper. Depending on how its configured, **winprint** will print one or more **Pages** on each **Sheet**.

This is called "n-up" printing. The most common form of "n-up" printing is "2-up" where the page orientation is set to landscape and there are two columns of pages.

The layout and format of the **Sheet** is defined by a set of configuration settings called a **Sheet Definition**. Out of the box **winprint** comes with two: `Default 1 Up` and `Default 2 Up`.

**Sheet Definitions** are defined and stored in the `WinPrint.config.json` configuration file found in `%appdata%\Kindel Systems\winprint`.

### Headers & Footers Macros

The format for header & footer specifiers is:

    <left part>|<center part>|<right part>

where `<left part>`,`<center part>`, and `<right part>` can be composed of text and any of the **Macros** described below. For example, this is the default header specifier:

    {DateRevised:D}|{Title}|Type: {FileType}

This is the default footer specifier:

    Printed with love by WinPrint||Page {Page} of {NumPages}

The following macros are supported:

* **`{NumPages}`** - The total number of **Sheets** in the file.

* **`{Page}`** - The current **Sheet** number.

* **`{FileExtension}`** - The file extension of the file.

* **`{FileNameWithoutExtension}`** - The file name of the file without the extension and period ".".

* **`{FileName}`** - The name of file without the extension.

* **`{Title}`** - The Title of the print request. If Title was not provided, the FileName will be used.

* **`{FileDirectoryName}`** - The directory for the specified string without the filename or extension.

* **`{FullPath}`** - The full path to the file, including the filename and extension.

* **`{Language}`** - The friendly name of the language used to syntax highlight the file. E.g. `C#` or `Smalltalk`.

* **`{ContentType}`** - The file's content type (e.g. `text/plain`, `text/x-csharp`, or `text/x-smalltalk`).

* **`{CteName}`** - The name of the **winprint** Content Type Engine used to render the file.

* **`{DatePrinted}`** - The current date & time (see formatting below).

* **`{DateRevised}`** - The date & time the file was last written to (see formatting below).

* **`{DateCreated}`** - The date & time the file was created (see formatting below).

The numeric macros (`{NumPages}` and `{Page}`) support [standard .NET numeric formatting modifiers](https://docs.microsoft.com/en-us/dotnet/standard/base-types/standard-numeric-format-strings). For example, using `Page {Page:D3} of {NumPages:D3}` in a footer will print `Page 001 of 004` on the first page of 4.

The `{DatePrinted}` and `{DateRevised}` macros support the [standard .NET date and time formatting modifiers](https://docs.microsoft.com/en-us/dotnet/standard/base-types/standard-date-and-time-format-strings). For example `{DateRevised:M}` will generate just the month and date (e.g. `September 10`) while `{DateRevised:t}` will generate just the `short time` (`4:55 PM`).

### Modifying Sheet Definitions

The **winprint** GUI can be used to change many Sheet Definition settings. All settings can be changed by editing the `WinPrint.config.json` file. Note if `WinPrint.config.json` is changed while the **winprint** GUI App is running, it will detect the change and re-flow the currently loaded file. In other-words, a text editor can be used as the UI for advanced settings.

### Creating new Sheet Definitions

**winprint** includes two pre-defined Sheet Definitions: `Default 1-Up` and `Default 2-Up`. Additional Sheet Definitions can be created by editing `WinPrint.config.json`, copying one of the existing Sheet Definitions and giving it a new unique `name` and unique `ID` (in the form of a UUID).

## Content Types

**winprint** supports three types of files. Support for each is provided by a **winprint** Content Type Engine (CTE):

1. **`TextMateCte`** - This is the default CTE used for most text and source files. It uses bundled TextMate grammars for syntax highlighting and does not require Python or Pygments.

2. **`AnsiCte`** - This legacy CTE can format `text/plain` files and files with embedded `ANSI Escape Sequences`. It uses [Pygments](https://pygments.org/) for syntax highlighting when explicitly configured as the default syntax highlighter.

3. **`TextCte`** - This CTE knows only how to print raw `text/plain` files. The format of the printed text can be changed (e.g. to turn off line numbers or use a different font). Lines that are too long for a page are wrapped at character boundaries. `\r` (formfeed) characters can be made to cause following text to print on the next page (this is off by default). Settings for `text/plain` can be changed by editing the `textContentTypeEngineSettings` section or a Sheet Definition in `WinPrint.config.json`.

4. **`text/html`** - This CTE can render html files. Any CSS specified inline in the HTML file will be honored. External CSS files must be local. For HTML without CSS, the default CSS used can be overridden by providing a file named `winprint.css` in the `%appdata%\Kindel Systems\winprint` folder. `text/html` does not support line numbers.

When using **winprint** from the command line, the `-ContentTypeEngine` parameter can be used specify a CTE to use.

The extension of the file being printed (e.g. `.cs`) determines which Content Type rendering engine will be used. **winprint** has a built-in library of hundreds of file extension to content type/language mappings. When using **winprint** from the command line, the `-Langauge` parameter can be used to override this behavior.

To associate a file extension with a particular Content Type Engine, modify the `fileTypeMapping.filesAssociations` section of `WinPrint.config.json`. For example, to associate files with a `.htm` extension with the `text/html` Content Type Engine add a line as shown below:

```json
    "fileTypeMapping": {
      "filesAssociations": {
        "*.htm": "text/html"
      }
    }
```

For associating file extensions with a particular programming language see below.

The `out-winprint` parameter `-ContentTypeEngine` and the `winprint --content-type` option override content type and language detection.

## Language Associations

**winprint** has a built-in file extension to language mapping that should work for most modern scenarios. For example it knows that `.cs` files hold `C#` and `.bf` files hold `brainfuck`.

### Adding or Changing Language Associations

To associate a file extension with a language, modify the `fileTypeMapping.filesAssociations` and `fileTypeMapping.contentTypes` sections of `WinPrint.config.json`. For example, to associate files with a `.INTERCAL` extension with the JSON language add a line as shown below:

```json
    "fileTypeMapping": {
      "filesAssociations": {
        "*.intercal": "JSON"
      }
    }
```

A new language can be defined by modifying the `fileTypeMapping.contentTypes` section of `WinPrint.config.json`. For example, to enable the [Icon Programming Language](https://en.wikipedia.org/wiki/Icon_%28programming_language%29), the `fileTypeMapping.filesAssociations` and `fileTypeMapping.contentTypes` sections would look like the following:

```json
    "fileTypeMapping": {
      "filesAssociations": {
        "*.intercal": "text/x-INTERCAL"
      },
      "contentTypes": [
        {
          "id": "text/x-INTERCAL",
          "title": "Compiler Language With No Pronounceable Acronym",
          "extensions": [
            "*.intercal"
          ],
          "aliases": [
              "INTERCAL"
          ]
        }
      ]
    }
```

For TextMate highlighting to work, the mapped language must resolve to a bundled TextMate grammar. If it does not, **winprint** still prints the file as plain text.


```json
    "languages": [
      {
        "id": "text/x-INTERCAL",
        "title": "Smalltalk",
        "extensions": [
          "*.intercal"
        ],
        "aliases": [
            "INTERCAL"
        ]
      }
    ]
```

## Logging & Diagnostics

**winprint** writes extensive diagnostic logs to `%appdata%/Kindel Systems/WinPrint/logs`. When using `winprint`, specify `--debug`. The deprecated PowerShell CmdLet still supports `-Debug`.

Additional printing diagnostics can be turned on via settings in the configuration file.

To help diagnose printer-related rendering issues, or issues with Sheet Definitions, **winprint** set the appropriate diagnostic flags found at the end of the config file to `true` (each flag has a print and print preview variant):

```json
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

To help diagnose why content may be rendering incorrectly, set `diagnostics` to `true` in any `contentSettings` section. This will cause the Content Type Engine to print/display diagnostic rules.

```json
      "contentSettings": {
        "diagnostics": false
      },
```

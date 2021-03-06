# User's Guide

## Features

* Prints source code with syntax highlighting and line numbering for [over 200 programming languages and file formats](https://pygments.org/languages/).
* Prints HTML files.
* Prints "multiple-pages-up" on one piece of paper (saves trees!)
* Complete control over page formatting options, including headers and footers, margins, fonts, page orientation, etc...
* Headers and Footers support detailed file and print information macros with rich date/time formatting.
* Simple and elegant graphical user interface with accurate print preview.
* The most capable PowerShell printing tool enabling printing from the command line.
  * Complete control of printing features with dozens of parameters, including *Intellicode* parameter completion (using `tab` key).
  * Allows **winprint** to be used from other applications or solutions. The **winpprint** PowerShell `out-winprint` CmdLet is a drop-in replacement for `out-printer`.
* Sheet Definitions make it easy to save settings for frequent print jobs.
* Comprehensive logging.
* Cross platform. Even though it's named **win**print, it works on Windows, Linux (command line only; some assembly required), and (not yet tested) Mac OS.

## Command Line Interface

Use **winprint** from the command line as a PowerShell CmdLet (`out-winprint`).

**`out-winprint`** is designed to be a stand-in for the `out-printer` CmdLet built into PowerShell.

The **winprint** CmdLet must be imported into PowerShell for `out-winprint` to work. This is done by adding an `import-module` statement in the Powershell profile:

```powershell
import-module 'C:\Program Files\Kindel Systems\winprint\winprint.dll'
```

The **winprint** installer does not currently add this line.

Invoke the CmdLet using either `out-winprint`, `winprint`, or the shortcut, `wp`.

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

* **`-ContentTypeEngine`** - *Intellisense* values are the names of all current Content Type Engines **winprint** supports (`text/plain`, `text/html`, and `text/code`).

* **`-Language`** - *Intellisense* values are the names of all (over 200) languages supported for syntax highlighting with the **`text/code`** Content Type Engine.

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

    Out-WinPrint [[-FileName] <string>] [-PrinterName <string>] [-Orientation {Portrait | Landscape}] [-LineNumbers {No | Yes}] [-Language <string>] [-Title <string>] [-FromSheet <int>] [-ToSheet <int>] [-Gui] [-InputObject <psobject>] [-WhatIf] [-PaperSize <string>] [-SheetDefinition {Default 2-Up | Default 1-Up}] [-ContentTypeEngine {text/html | text/code | text/plain}] [<CommonParameters>]

    Out-WinPrint [-InstallUpdate] [-Force] [<CommonParameters>]

    Out-WinPrint [-Config] [<CommonParameters>]

```

## Graphical User Interface

When run as a Windows app (`winprintgui.exe`), **winprint** provides an easy to use GUI for previewing how a file will be printed and changing many settings. Start **winprint** from the Windows Start Menu like any other app.

The **File button** opens a File Open Dialog for choosing the file to preview and/or print. The GUI app can print a single file at a time. Use the PowerShell version (`out-winprint`) to print multiple files at once.

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

1. **`AnsiCte`** - This is the default CTE used for most file types. `AnsiCte` can format `text/plain` files as well as files with embedded `ANSI Escape Sequences`.  `AnsiCte` provides **winprint**'s syntax highlighting (pretty printing) with support for over 200 programming languages. Over a dozen styles (colors, bold, italic, etc...) are supported (change the `style` entry in the `WinPrint.config.json` file). All styles supported by [Pygments](https://pygments.org/) are supported.

2. **`TextCte`** - This CTE knows only how to print raw `text/plain` files. The format of the printed text can be changed (e.g. to turn off line numbers or use a different font). Lines that are too long for a page are wrapped at character boundaries. `\r` (formfeed) characters can be made to cause following text to print on the next page (this is off by default). Settings for the `text/plain` can be changed by editing the `textFileSettings` section of a Sheet Definition in the `WinPrint.config.json` file. `TextCte` is not enabled by default and may be removed from future versions as `AnsiCte` does almost everything `TextCte` does (with the exception of form-feeds [#31](https://github.com/tig/winprint/issues/31)).

3. **`text/html`** - This CTE can render html files. Any CSS specified inline in the HTML file will be honored. External CSS files must be local. For HTML without CSS, the default CSS used can be overridden by providing a file named `winprint.css` in the `%appdata%\Kindel Systems\winprint` folder. `text/html` does not support line numbers.

When using **winprint** from the command line, the `-ContentTypeEngine` parameter can be used specify a CTE to use.

The extension of the file being printed (e.g. `.cs`) determines which Content Type rendering engine will be used. **winprint** has a built-in library of hundreds of file extension to content type/language mappings. When using **winprint** from the command line, the `-Langauge` parameter can be used to override this behavior.

To associate a file extension with a particular Content Type Engine modify the `files.associations` section of `WinPrint.config.json` appropriately. For example to associate files with a `.htm` extension with the `text/html` Content Type Engine add a line as shown below (the `WinPrint.config.json` generated when **winprint** runs the first time already provides this example, as an example):

```json
    "files.associations": {
      "*.htm": "text/html",
    }
```

For associating file extensions with a particular programming language using the `text/code` Content Type Engine see below.

The `out-winprint` parameter `-ContentTypeEngine` overrides content type and language detection.

## Language Associations

**winprint** knows how to syntax highlight (pretty print) over 200 programming languages. It has a built-in file extension to language mapping that should work for most modern scenarios. For example it knows that `.cs` files hold `C#` and `.bf` files hold `brainfuck`.

### Adding or Changing `text/code` Language Associations

To associate a file extension with a language supported by `text/ansi` (which uses [Pygments](https://pygments.org) to do the heavy lifting) modify the `files.associations` and `languages` sections of `WinPrint.config.json`. For example to associate files with a `.INTERCAL` extension with the JSON language (which would make no sense, since INTERCAL is unlike any other language) add a line as shown below:

```json
    "files.associations": {
      "*.intercal": "JSON"
    }
```

A new language can be defined by modifying the `languages` section of `WinPrint.config.json`. For example to enable the [Icon Programming Language](https://en.wikipedia.org/wiki/Icon_%28programming_language%29) which is a very C-like language the `files.associations` and `languages` sections would look like the following:

```json
    "languages": [
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
```

For this to works, `Pygmentize` needs to support the language defined by `title`. Since `Pygments` does not yet support `INTERCAL` the above won't actually work. Set the `Title` to a known `Pygments` langauge and that will be used:


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

**winprint** writes extensive diagnostic logs to `%appdata%/Kindel Systems/WinPrint/logs`. When using the command line (`out-winprint`) specifying the `-Debug` parameter switch will cause all diagnostic log entries to go to the console as well as the log file.

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

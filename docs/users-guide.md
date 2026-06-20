# User's Guide

## Features

* Prints source code with syntax highlighting and line numbering using bundled TextMate grammars.
* Prints HTML files.
* Prints "multiple-pages-up" on one piece of paper (saves trees!)
* Complete control over page formatting options, including headers and footers, margins, fonts, page orientation, etc.
* Headers and Footers support detailed file and print information macros with rich date/time formatting.
* Simple and elegant graphical user interface with accurate print preview on Windows and macOS.
* `wp` provides a Terminal.Gui-based terminal UI on Windows, macOS, and Linux.
* `wp gui` launches the MAUI GUI on Windows and macOS.
* The legacy PowerShell `Out-WinPrint` CmdLet remains available as `WinPrint.PowerShell.dll`, but is deprecated in favor of `wp`.
* Sheet Definitions make it easy to save settings for frequent print jobs.
* Comprehensive logging.
* Cross-platform TUI; Windows and macOS GUI; Linux GUI is deferred.

## Command Line Interface

The primary command for WinPrint is `wp`. It provides a Terminal.Gui-based terminal UI.

```bash
wp [options] [file...]
```

### Launching Modes

| Command | Description |
|---------|-------------|
| `wp file.cs` | Open a file in the TUI |
| `wp` | Launch the TUI (terminal user interface) |
| `wp gui` | Launch the graphical user interface on Windows/macOS |

### Examples

Check the installed version:

```bash
wp --version
```

Open a file in the terminal UI:

```bash
wp Program.cs
```

Pass print preview options:

```bash
wp Program.cs --printer "Microsoft Print to PDF" --sheet "Default 2-Up"
```

Launch the GUI:

```bash
wp gui
```

List catalogued TUI views:

```bash
wp views
```

Get help:

```bash
wp --help
```

### CLI Options

**winprint** exposes one **canonical set of print options across every front end** — the `wp` CLI, the
TUI, and the `gui` (Windows WinForms and macOS/Windows MAUI). The same name, short alias, and meaning
apply everywhere:

| Option | Alias | Description |
| --- | --- | --- |
| `--sheet` | `-s` | Sheet definition to use (ID or friendly name). |
| `--landscape` | `-l` | Force landscape orientation. |
| `--portrait` | `-r` | Force portrait orientation. |
| `--printer` | `-p` | Printer name. |
| `--paper-size` | `-z` | Paper size name. |
| `--from-sheet` | `-f` | First sheet to print (use with `--to-sheet`). |
| `--to-sheet` | `-t` | Last sheet to print (use with `--from-sheet`). |
| `--content-type` | `-e` | Content type engine / language override (e.g. `text/plain`, `text/html`, or a `<language>`). |

Front ends add their own *appropriate* extras: the TUI adds `--view`, `--width`, `--height`; the `wp`
CLI adds `--line-numbers`, `--what-if` (`-w`, count sheets without printing), `--gui` (`-g`), and
`--config`. The Terminal.Gui.Cli framework — which the `wp` CLI/TUI uses for command-line handling —
also provides `--help`, `--version`, `--opencli`, `--json`, `--output`, `--initial`, `--timeout`, and
`--cat`.

This consistency is enforced by tests: the canonical surface lives in `WinPrint.Core` and every front
end derives its parser from it (see `WinPrintOptionsConsistencyTests`).

### Deprecated PowerShell CmdLet

The PowerShell CmdLet (`Out-WinPrint`) is deprecated. Prefer `wp` for new scripts and automation.

If you still need the deprecated CmdLet, import it into PowerShell:

```powershell
Import-Module '<install-path>\WinPrint.PowerShell.dll'
```

## Graphical User Interface

Launch the GUI with:

```bash
wp gui
```

Or find **WinPrint** in the Start Menu (Windows) or via Spotlight (macOS).

The GUI provides an easy-to-use interface for previewing how a file will be printed and changing settings.

* **File button** — Opens a File Open Dialog for choosing the file to preview and/or print.
* **Print button** — Prints the currently selected file.
* **Settings (⚙) button** — Opens `WinPrint.config.json` in your text editor. Changes are reflected automatically.

## Auto-Update

WinPrint packages are built with Velopack, which provides the install/update engine for packaged builds.

You can also update manually using your package manager:

```bash
# Windows
winget upgrade Kindel.WinPrint

# macOS
brew upgrade --cask winprint

# Linux
brew upgrade winprint
```

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

The `out-winprint` parameter `-ContentTypeEngine` and the `wp --content-type` option override content type and language detection.

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

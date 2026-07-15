# User's Guide

## Features

* Beautifully Prints:
  * Source code in hundreds of programming languages with syntax highlighting and line numbering.
  * HTML files.
  * Markdown files as formatted documents (headings, lists, blockquotes, code blocks, mermaid diagrams, and inline images).
  * ANSI-encoded text and colorized console captures (`.ans`/`.ansi`), decoding ANSI escape sequences to color.
* Saves trees printing "multiple-pages-up" on one piece of paper.
* Complete control over page formatting options, including headers and footers, margins, fonts, page orientation, etc.
* Headers and Footers support detailed file and print information macros with rich date/time formatting.
* Sheet Definitions make it easy to save settings for frequent print jobs.
* Accurate print preview in the GUI *and* TUI.
* `wp` provides a rich command line interface (CLI) on Windows, macOs, and Linux
* Comprehensive logging.

## Command Line Interface

The primary command for WinPrint is `wp`. It provides a terminal UI.

```bash
wp [options] [file...]
```

### Launching Modes

| Command | Description |
|---------|-------------|
| `wp file.cs` | Open a file in the TUI |
| `wp` | Launch the TUI |
| `wp print file.cs` | Print one or more files without opening the UI |
| `wp gui` | Launch the graphical user interface on Windows/macOS |
| `wp gui file.cs` | Launch the GUI with a file (and options) loaded |

### Examples

Turn Markdown into a PDF (the classic move; see [the overview](index.md#how-to-turn-markdown-into-a-pdf) for it in action). Markdown prints as a formatted document, with images, tables, syntax-highlighted code, and mermaid fences rendered as diagrams:

```bash
wp print README.md --printer "Microsoft Print to PDF" --sheet "Proportional 1-Up"
```

Or skip the printer entirely; `--pdf` writes the PDF straight to a file on every platform (no printer, no driver, no save dialog):

```bash
wp print README.md --pdf readme.pdf --sheet "Proportional 1-Up"
```

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

Print without opening the UI (use `--what-if` to count sheets without printing):

```bash
wp print Program.cs --printer "Microsoft Print to PDF" --sheet "Default 2-Up"
wp print *.cs --landscape
wp print Program.cs --what-if
```

`--printer` names an OS print queue, so to print **to a PDF file** point it at your platform's print-to-PDF target:

- **Windows** — the built-in **Microsoft Print to PDF**; a Save-As dialog chooses the file.
- **macOS** — install a virtual PDF printer once with [RWTS PDFwriter](https://github.com/rodyager/RWTS-PDFwriter): `brew install --cask rwts-pdfwriter`, then add it in **Printers & Scanners** (name it e.g. `CUPS-PDF`). Printed PDFs land in `/var/spool/pdfwriter/$USER/`. (`brew install cups-pdf` does **not** exist on macOS — that is a Linux package.)
- **Linux** — `sudo apt install printer-driver-cups-pdf` (Debian/Ubuntu; the queue is named **`PDF`** — confirm with `lpstat -p`) or `sudo dnf install cups-pdf` (Fedora; often **`Cups-PDF`**). Printed PDFs land in **`~/PDF/`** (or check `/etc/cups/cups-pdf.conf` `Out`). See **[Linux & WSL printing](linux.md)** for CUPS setup, network printers (IPP), and WSL notes.

Prefer **`wp print … --pdf out.pdf`** when you want a file and do not need a real queue: it writes the Skia PDF straight to disk on every platform (no printer, no driver, no dialog). Stock cups-pdf with its PPD may run a PDF→PS→PDF filter chain (Ghostscript); mermaid diagrams are embedded rasters and survive that path, but for bit-identical Skia output use `--pdf`.

Launch the GUI:

```bash
wp gui
```

Launch the GUI with a file (and options) loaded:

```bash
wp gui ./testfiles/Program.cs --sheet "Default 2-Up"
```

Get help:

```bash
wp --help
```

### CLI Options

**winprint** exposes one **canonical set of print options across every front end** — the `wp` CLI, the
TUI, and the `gui` on Windows and macOS. The same name, short alias, and meaning
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

Front ends add their own *appropriate* extras: the interactive TUI adds `--view`, `--width`,
`--height`; the `wp print` command adds `--what-if` (`-w`, count sheets without printing) and `--pdf <file>` (write a PDF file instead of printing); and the
GUI launches through the separate `wp gui` command. The `wp` command line also provides `--help`,
`--version`, `--opencli`, `--json`, `--output`, `--initial`, `--timeout`, and `--cat`.

The same print options behave identically across the `wp` CLI, the TUI, and the GUI.

### Overriding Content Type / Language

Use `--content-type` (alias `-e`) to override **winprint**'s automatic content type and language
detection. The value can be a content type (e.g. `text/plain`, `text/html`) or a language alias:

```bash
wp print README --content-type text/x-markdown
wp print notes.txt -e text/html
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

WinPrint's packaged builds include a built-in install/update engine.

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

The layout and format of the **Sheet** is defined by a set of configuration settings called a **Sheet Definition**. Out of the box **winprint** comes with two: `Default 1-Up` and `Default 2-Up`.

**Sheet Definitions** are defined and stored in the `WinPrint.config.json` configuration file. On Windows this file lives in `%appdata%\Kindel\winprint`; on macOS and Linux **winprint** runs in portable mode and the file sits next to the application (the `wp` executable, or inside `WinPrint.app` for the GUI).

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

* **`{FileName}`** - The name of the file including the extension. If a Title was not provided, the Title will be used.

* **`{Title}`** - The Title of the print request. If Title was not provided, the FileName will be used.

* **`{FileDirectoryName}`** - The directory for the specified string without the filename or extension.

* **`{FullPath}`** - The full path to the file, including the filename and extension.

* **`{Language}`** - The friendly name of the language used to syntax highlight the file. E.g. `C#` or `Smalltalk`.

* **`{ContentType}`** - The file's content type (e.g. `text/plain`, `text/x-csharp`, or `text/x-smalltalk`).

* **`{CteName}`** - The name of the **winprint** Content Type Engine used to render the file.

* **`{Style}`** - The style used for formatting (e.g. `default` or `colorful`).

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

**winprint** supports five types of files. Support for each is provided by a **winprint** Content Type Engine (CTE):

### `TextMateCte`

This is the default CTE used for most text and source files. It uses bundled TextMate grammars for syntax highlighting.

### `MarkdownCte`

Renders Markdown files (`text/x-markdown`; e.g. `.md`) as formatted documents, including inline images. ` ```mermaid ` fenced code blocks are rendered as diagrams by default, entirely in-process via the Mermaider library — nothing is sent over the network, and every Mermaid diagram type except ZenUML is supported (as of Mermaider 0.9.0). Diagrams that fail to render print as regular code blocks.

Options in the `markdownContentTypeEngineSettings` section of `WinPrint.config.json`: set `renderMermaidDiagrams` to `false` to always print fences as code, or set `mermaidBackend` to `"service"` to render via the remote `mermaid.ink` service instead (full Mermaid.js fidelity, but the diagram source is sent over the network). See the support matrix in `testfiles/mermaid.md`.

Upgrading from 3.1.x: those versions wrote their then-default `"mermaidBackend": "service"` into the config file, so on first load WinPrint rewrites that leftover to `"builtin"` (and stamps the file with `schemaVersion`) — nothing is sent over the network unless you set `"service"` yourself afterwards, which then sticks.

   ```json
       "markdownContentTypeEngineSettings": {
         "renderMermaidDiagrams": true,
         "mermaidBackend": "builtin",
         "mermaidServiceUrl": "https://mermaid.ink"
       }
   ```

### `AnsiCte`

Decodes files containing `ANSI Escape Sequences` (`text/ansi`; e.g. `.ans`/`.ansi` ANSI-art and colorized console captures), reflowing them for the page.

### `TextCte`

This CTE knows only how to print raw `text/plain` files. The format of the printed text can be changed (e.g. to turn off line numbers or use a different font). Lines that are too long for a page are wrapped at character boundaries. `\f` (form feed) characters can be made to cause following text to print on the next page (this is off by default). Settings for `text/plain` can be changed by editing the `textContentTypeEngineSettings` section or a Sheet Definition in `WinPrint.config.json`.

### `HtmlCte`

Renders HTML files (`text/html`; e.g. `.html`/`.htm`, as well as `.mhtml`/`.mht` web archives) by laying out the HTML/CSS. Any CSS specified inline in the HTML file will be honored. Local (document-relative) files, `data:` URIs, and MHTML-embedded resources always load; `http(s)` resources are only fetched when `AllowRemoteResources` is enabled. `text/html` does not support line numbers.

### Overriding Default CTE selection

When using **winprint** from the command line, the `--content-type` (`-e`) option can be used to specify the content type / CTE to use.

The extension of the file being printed (e.g. `.cs`) determines which Content Type rendering engine will be used. **winprint** has a built-in library of hundreds of file extension to content type/language mappings. When using **winprint** from the command line, the `--content-type` (`-e`) option can be used to override this behavior.

To associate a file extension with a particular Content Type Engine, modify the `fileTypeMapping.filesAssociations` section of `WinPrint.config.json`. For example, to associate files with a `.htm` extension with the `text/html` Content Type Engine add a line as shown below:

```json
    "fileTypeMapping": {
      "filesAssociations": {
        "*.htm": "text/html"
      }
    }
```

For associating file extensions with a particular programming language see below.

The `wp --content-type` (`-e`) option overrides content type and language detection.

## Language Associations

**winprint** has a built-in file extension to language mapping that should work for most modern scenarios. For example it knows that `.cs` files hold `C#` and `.bf` files hold `brainfuck`.

### Adding or Changing Language Associations

To associate a file extension with a content type, modify the `fileTypeMapping.filesAssociations` section of `WinPrint.config.json`. For example, to associate files with a `.json5` extension with the JSON content type add a line as shown below:

```json
    "fileTypeMapping": {
      "filesAssociations": {
        "*.json5": "text/x-json"
      }
    }
```

A new content type can be defined by modifying the `fileTypeMapping.contentTypes` section of `WinPrint.config.json`. For example, to enable the [Icon Programming Language](https://en.wikipedia.org/wiki/Icon_%28programming_language%29), the `fileTypeMapping.filesAssociations` and `fileTypeMapping.contentTypes` sections would look like the following:

```json
    "fileTypeMapping": {
      "filesAssociations": {
        "*.icn": "text/x-icon"
      },
      "contentTypes": [
        {
          "id": "text/x-icon",
          "title": "Icon Programming Language",
          "extensions": [
            "*.icn"
          ],
          "aliases": [
              "icon"
          ]
        }
      ]
    }
```

For TextMate highlighting to work, the mapped language must resolve to a bundled TextMate grammar. If it does not, **winprint** still prints the file as plain text.

## Logging & Diagnostics

**winprint** writes diagnostic logs to a `logs` folder alongside its settings. On Windows that's `%appdata%\Kindel\winprint\logs` (or next to the executable when running in portable mode); on macOS and Linux the `logs` folder sits next to the `wp` executable (inside `WinPrint.app` for the GUI). Run the `wp` command line with `--debug` for more detail.

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

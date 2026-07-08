# WinPrint

*A modern take on the classic source code printing app from [1988](about.md).*

Advanced source code, html, markdown, and text file printing for terminals (all platforms) and Windows/macOS GUIs. 

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

See [User's Guide](users-guide.md) for more details.

### Terminal UI (`wp`)

![wp TUI — sixel print preview](hero-tui.gif)

### Graphical UI (`wp gui`)

<p>
  <img height="330" alt="WinPrint GUI on Windows" src="hero-gui-win.gif" />
  <img height="330" alt="WinPrint GUI on macOS" src="hero-gui-mac.gif" />
</p>

### Turn Markdown into a PDF

One command. Markdown goes in; a formatted, paginated PDF comes out: headings, lists, tables, images, syntax-highlighted code, and ` ```mermaid ` fences rendered as real diagrams, entirely in-process (no browser, no cloud).

```powershell
wp print mermaid.md --printer "Microsoft Print to PDF" --sheet "Proportional 1-Up"
```

![wp print turning mermaid.md into a PDF, then viewing it](cli.gif)

## Quick Start

```bash
# Install (Windows) — winget gives you the GUI + `wp` TUI in one command
winget install Kindel.WinPrint
# (or download Kindel.WinPrint-win-x64-Setup.exe from the latest GitHub release)

# Install (Mac) — GUI cask, which also bundles the `wp` TUI
brew tap kindel/winprint && brew install winprint
# Want only the `wp` CLI (no GUI)? Install the formula instead — pick one, they collide:
#   brew install kindel/winprint/wp
# The .app is Apple Developer ID-signed and notarized, so Gatekeeper accepts it normally.

# Open a file in the TUI
wp program.cs

# Pass print preview options
wp Program.cs --printer "Microsoft Print to PDF" --sheet "Default 2-Up"

# Launch the GUI on Windows or macOS
wp gui

# The GUI can also be run from the Windows Start Menu or Spotlight on the Mac after installing
```

## History

See [About](about.md) for the full history of WinPrint.

For the latest changes, see the [GitHub Releases](https://github.com/tig/winprint/releases) page.

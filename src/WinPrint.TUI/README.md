# WinPrint.TUI

A full character-mode Terminal.Gui front end for WinPrint, mirroring the MAUI/WinForms layout.

## Overview

`WinPrint.TUI` produces an executable named `print` that provides a full-screen print preview
interface in the terminal. It uses [gui-cs/Cli](https://github.com/gui-cs/Cli) as the hosting
framework with `AppModel.FullScreen`.

## Running

```bash
dotnet run --project src/WinPrint.TUI -- <file>
```

Or after building:

```bash
print <file>
```

### Command-line options

| Option | Short | Description |
|--------|-------|-------------|
| `--printer` | `-P` | Printer name (defaults to system default) |
| `--paper-size` | | Paper size supported by the selected printer |
| `--sheet` | `-s` | Sheet definition name or ID |
| `--content-type` | `-c` | Content type engine override |
| `--language` | `-l` | Language override for syntax highlighting |

## Layout

The TUI mirrors the desktop front ends:

- **Left panel**: Settings rail with collapsible groups (Sheet Definition, Margins, Pages Up,
  Fonts, Printer) matching the MAUI/WinForms ordering and grouping.
- **Right panel**: Header bar (above), print preview (center), footer bar (below).
- **Status bar**: Keyboard shortcuts for common operations.

## Terminal requirements

### Sixel support (optional)

For image-based page preview, the terminal must support sixel graphics. Known compatible
terminals: WezTerm, iTerm2, mlterm, Contour, foot.

Terminals without sixel support show a text-based preview fallback with line numbers and
page navigation.

### Minimum terminal size

The TUI is usable at 80×24 but works best at 120×40 or larger. The left settings panel
is fixed at 36 columns; the remaining width is allocated to the preview area.

## Phase 1 limitations

This is the initial implementation (issue #68, phase 1):

- **Preview rendering**: Currently shows a text-mode line-numbered preview. Full PNG
  rendering through the CTE pipeline with sixel display is wired but pending complete
  cross-platform graphics integration.
- **Printing**: Displays an informational message. Use `winprint` CLI for actual printing
  in phase 1.
- **Printer/paper enumeration**: Uses text fields rather than dynamic enumeration
  (platform-specific printer discovery is a follow-up).
- **Font selection**: Uses "Family, Sizept" text entry rather than a font picker dialog.

## Future work

- **#66 — Native AOT**: The project is designed with AOT compatibility in mind but does
  not yet publish with `<PublishAot>true</PublishAot>`.
- **#64 — Cross-platform CI**: The TUI will be included in cross-platform build/test
  validation once that infrastructure is in place.
- **#63 — Installer/distribution**: The `print` executable will be included in
  distribution packages.

## Architecture

- **Hosting**: `gui-cs/Cli` (`Terminal.Gui.Cli`) provides the command registration,
  lifecycle management, and `AppModel.FullScreen` integration.
- **Views**: `MainView` (top-level Window), `SettingsPanel` (left rail), `PreviewPanel`
  (center), `HeaderFooterBar` (above/below preview).
- **Services**: `PreviewRenderer` (page count and PNG rendering, testable without a
  terminal), `SixelDetector` (terminal capability detection).
- **Models**: Shares `WinPrint.Core` models (`Settings`, `SheetSettings`, etc.) with the
  other front ends.

## Relationship to WinPrint.cli

Both `WinPrint.cli` and `WinPrint.TUI` coexist in the repository. `WinPrint.TUI` will
eventually replace `WinPrint.cli`, but migration is a separate future effort.

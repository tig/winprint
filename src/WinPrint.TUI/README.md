# WinPrint.TUI

Full-screen Terminal User Interface for WinPrint, built with [Terminal.Gui](https://github.com/gui-cs/Terminal.Gui) and hosted via [gui-cs/Cli](https://github.com/gui-cs/Cli) (`Terminal.Gui.Cli` NuGet package).

## Quick Start

```bash
# Run directly
dotnet run --project src/WinPrint.TUI -- myfile.cs

# Or build and run the 'print' executable
dotnet build src/WinPrint.TUI
./src/WinPrint.TUI/bin/Debug/net10.0/print myfile.cs
```

## Architecture

- **Hosting**: Uses `Terminal.Gui.Cli` (`CliHost`) with `AppModel.FullScreen`
- **Executable name**: `print` (via `<AssemblyName>print</AssemblyName>`)
- **Layout**: Mirrors MAUI/WinForms — left rail (settings/controls), right preview area

### Layout Structure

```
┌─ Settings ──────────┐┌─ Preview ───────────────────────────────────┐
│ 📂 File  🖨 Print   ││ [✓] Hdr  {FileName} - Printed with WinPrint│
│ ▼ Sheet Definition  ││                                             │
│   Sheet: Default    ││                                             │
│   ☐ Landscape       ││         +-------------------+               │
│   ☐ Line Numbers    ││         |   Page Preview    |               │
│ ▼ Margins (inches)  ││         |  (sixel/fallback) |               │
│   Top: 0.50         ││         +-------------------+               │
│   Left: 0.50        ││                                             │
│   Right: 0.50       ││                                             │
│   Bot: 0.50         ││                                             │
│ ▼ Pages Up          ││                                             │
│   Rows: 1  Cols: 1  ││                                             │
│ ▼ Fonts             ││                                             │
│ ▼ Printer           ││ [✓] Ftr  Page {PageNumber} of {NumPages}    │
│ Help & About (F1)   │└─────────────────────────────────────────────┘
└─────────────────────┘
```

### Keyboard Shortcuts

| Key | Action |
|-----|--------|
| Ctrl+O | Open file |
| Ctrl+P | Print |
| Ctrl+Q / Esc | Quit |
| PgUp / PgDn | Navigate pages |
| Tab / Shift+Tab | Move between panes |
| Enter / Space | Toggle expanders |

## Preview Rendering

Pages are rendered to PNG images via the `PreviewRenderer` service (in `Services/`). The rendering pipeline is decoupled from Terminal.Gui widgets for testability.

### Sixel Support

For terminals with sixel graphics support (e.g., WezTerm, mlterm, mintty, foot, contour), page images are displayed inline using sixel escape sequences.

For terminals without sixel support, a text-based placeholder with page metadata is shown.

Detection is via `TERM` and `TERM_PROGRAM` environment variables — see `SixelEncoder.IsSupported()`.

## Phase 1 Limitations

- Preview rendering shows placeholder content (sixel encoding is stubbed)
- Print action is not wired (use `winprint` CLI for actual printing)
- Font selection UI is display-only (not yet connected to WinPrint.Core models)
- No printer enumeration on non-Windows platforms

## Relationship to Other Projects

- **WinPrint.cli** (`winprint`): Remains the headless CLI for automated printing — unchanged
- **WinPrint.Core**: Shared engine — TUI references it for the CTE pipeline
- **WinPrint.WinForms / WinPrint.Maui**: GUI front ends — TUI mirrors their layout

## Requirements

- .NET 10 SDK
- Terminal with reasonable size (80×24 minimum, 120×40 recommended)
- For sixel preview: terminal with sixel support (WezTerm, foot, etc.)

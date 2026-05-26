# WinPrint WinForms UI Behavioral Specification

This documents the exact behavioral nuances of the WinForms UI that MUST be preserved in the MAUI port.

## Startup Sequence (MainWindow_Load)

1. Check for updates (async, non-blocking)
2. Load settings from `ModelLocator.Current.Settings`
3. Restore window state (Size, Location, WindowState) from settings
4. Wire F5 = Refresh (reloads file)
5. Populate Sheet combobox from `Settings.Sheets`
6. Subscribe to `Settings.PropertyChanged` → when `DefaultSheet` changes, call `SheetChanged()`
7. Populate Printers combobox from installed printers, select default
8. Apply `--p` (printer) option override
9. Populate Paper Sizes from selected printer
10. Apply `--z` (paper size) option override
11. Enable printer/paper combos (kept disabled during load to prevent spurious events)
12. **Create SheetViewModel & wire notifications** (`SetupSheetViewModelNotifications`)
13. Apply `--from` and `--to` page range options
14. Select default sheet (or `--s` override), call `SetSheet()` → triggers UI property cascade
15. Apply `--l` (landscape) / `--o` (portrait) AFTER sheet is set
16. Focus PrintPreview
17. Set `activeFile` from first file in options
18. Verify Pygments install if AnsiCte is configured
19. Call `Start()` → `LoadFile()`

**Key ordering constraint**: Landscape/Portrait is set AFTER SetSheet AND after printer/paper selection.

## LoadFile Workflow (the core rendering pipeline)

```
1. Reset SheetViewModel (svm.Reset())
2. Invalidate PrintPreview
3. Show "Loading..." message
4. On background thread:
   a. Fully qualify file path if relative
   b. await svm.LoadFileAsync(file, contentType)
   c. Set printDoc landscape from svm.Landscape
   d. svm.SetPrinterPageSettings(printDoc.DefaultPageSettings)
   e. Show "Rendering..." message  
   f. await svm.ReflowAsync()
5. On error: show error in PrintPreview.Text, return
6. Finally: Focus PrintPreview
```

**Critical sequence**: LoadFile → SetPrinterPageSettings → ReflowAsync (must be this order)

## ReadyChanged Behavior

- `printButton.Enabled = ready`
- When ready && no active file: show HelloMsg, invalidate preview
- When ready && has file: set `CurrentSheet = 1`, invalidate, clear message

## SettingsChanged Behavior

- If `e.Reflow == true`: call `LoadFile()` (full re-render)
- If `e.Reflow == false`: just `PrintPreview.Invalidate()` (repaint only)

## PropertyChanged → UI Binding Map

| ViewModel Property | UI Control | Action |
|---|---|---|
| Landscape | landscapeCheckbox | `.Checked = svm.Landscape` |
| Header | headerTextBox, enableHeader, headerFooterFontLink | `.Text`, `.Checked`, font string |
| Footer | footerTextBox, enableFooter | `.Text`, `.Checked` |
| Margins | topMargin, leftMargin, rightMargin, bottomMargin | `.Value = margin/100` (display as inches) |
| Margins | printDoc.DefaultPageSettings.Margins | Keep PrintDocument synced |
| PageSeparator | pageSeparator | `.Checked` |
| Rows | rows (NumericUpDown) | `.Value` |
| Columns | columns (NumericUpDown) | `.Value` |
| Padding | padding (NumericUpDown) | `.Value = padding/100` |
| File | Window title, CurrentSheet | `"winprint - {file}"`, reset to sheet 1 |
| ContentSettings | contentFontLink, lineNumbers | font string, `.Checked` |
| ContentEngine | (nothing currently) | Was: set sheet 1, enable print |
| ContentType, Language, Title, DiagnosticRulesFont, Encoding, Loading, Ready | (nothing) | No UI reaction |

**Unhandled PropertyChanged throws InvalidOperationException** (line 352)

## UI → Model Binding (user edits propagate to model)

| UI Control | Target | Notes |
|---|---|---|
| landscapeCheckbox | Sheet.Landscape + printDoc.DefaultPageSettings.Landscape | Sets BOTH |
| headerTextBox | Sheet.Header.Text | Direct model write |
| footerTextBox | Sheet.Footer.Text | Direct model write |
| enableHeader | Sheet.Header.Enabled | Guarded by `printersCB.Enabled` |
| enableFooter | Sheet.Footer.Enabled | Guarded by `printersCB.Enabled` |
| topMargin | Sheet.Margins (clone, set Top) | `value * 100` (inches to hundredths) |
| leftMargin | Sheet.Margins (clone, set Left) | Same |
| rightMargin | Sheet.Margins (clone, set Right) | Same |
| bottomMargin | Sheet.Margins (clone, set Bottom) | Same |
| rows | Sheet.Rows | Direct cast to int |
| columns | Sheet.Columns | Direct cast to int |
| padding | Sheet.Padding | `value * 100` |
| pageSeparator | Sheet.PageSeparator | Direct |
| lineNumbers | Sheet.ContentSettings.LineNumbers | Direct |
| comboBoxSheet | Settings.DefaultSheet = Guid | Triggers SheetChanged() |
| printersCB | printDoc.PrinterSettings.PrinterName | Repopulates paper sizes |
| paperSizesCB | printDoc.DefaultPageSettings.PaperSize | Triggers LoadFile() |
| contentFontLink | Sheet.ContentSettings.Font | Via FontDialog |
| headerFooterFontLink | Sheet.Header.Font AND Footer.Font | Sets BOTH to same value |

**Guard pattern**: `enableHeader`, `enableFooter`, `comboBoxSheet` changes only take effect when `printersCB.Enabled == true` (prevents spurious events during initialization)

## Print Workflow

1. Create new `Core.Print()` instance  
2. Set landscape from checkbox
3. `print.SheetViewModel.SetSheet(current sheet)`
4. `await print.SheetViewModel.LoadFileAsync(current file, contentType)`
5. `print.SetPrinter(printerName)`
6. `print.SetPaperSize(paperSize)`
7. Parse from/to page range text boxes
8. If `Settings.ShowPrintDialog`: show PrintDialog, get updated range
9. Show "Printing to..." message
10. `await print.DoPrint()`
11. Clear message

## PrintPreview Control Behavior

### Navigation
- **PageUp**: `CurrentSheet--` if > 1, then Invalidate
- **PageDown**: `CurrentSheet++` if < NumSheets, then Invalidate
- **Home**: `CurrentSheet = 1`, Invalidate
- **End**: `CurrentSheet = NumSheets`, Invalidate
- **Mouse wheel (no modifier)**: PageDown/PageUp
- **Ctrl+Mouse wheel**: ZoomIn/ZoomOut

### Zoom Rules
- Step = 10 when Zoom < 200
- Step = 50 when Zoom >= 200
- Minimum zoom = 10 (floor, never goes to 0)
- No maximum zoom (unbounded)
- After zoom change: Invalidate

### Keyboard Shortcuts
- PageDown → next sheet
- PageUp → prev sheet
- `+` (Oemplus) → ZoomIn
- `-` (OemMinus) → ZoomOut
- Down arrow → PageDown (or cursor move in TERMINAL mode)
- Up arrow → PageUp (or cursor move in TERMINAL mode)
- Home → first sheet
- End → last sheet
- F5 → Refresh (full reload)

### Rendering (OnPaint)
- If `svm.Ready`:
  - Calculate scale to fit: `min(scaleX, scaleY) * (Zoom/100)`
  - If Zoom <= 100: **center** the preview in both axes
  - If Zoom > 100: **top-centered** (horizontal center, top-aligned with padding)
  - Paint white background at svm.Bounds
  - Call `svm.PrintSheet(graphics, CurrentSheet)`
- If Zoom != 100: overlay "{Zoom}%" text in large gray font, centered
- Always: paint status message (`Text`) centered in client area

### Click Behavior
- Click on PrintPreview when no file loaded → opens File dialog
- Click always calls `Select()` (takes focus)

## Window Close (FormClosing)
1. Unsubscribe update service events
2. Save window state:
   - If Normal: save current Size/Location
   - If Maximized/Minimized: save RestoreBounds
   - Save WindowState enum
3. `SettingsService.SaveSettings()`

## Sheet Change Flow
1. User selects new sheet in combobox
2. Sets `Settings.DefaultSheet` to new GUID
3. `Settings.PropertyChanged("DefaultSheet")` fires
4. `SheetChanged()`:
   - Gets new sheet from settings
   - Updates combobox text
   - Calls `svm.SetSheet(newSheet)` → triggers PropertyChanged cascade
   - Calls `LoadFile()` → full re-render

## Font Change
- Content font and Header/Footer font use separate FontDialog
- **Header and Footer always share the same font** (both set together)
- Font size is rounded to nearest integer point
- FontDialog: no effects, no script change, no simulations, no vector fonts

## Margin Display
- Internal model uses hundredths of an inch (int)
- UI displays as decimal inches (e.g., 0.50 = 50/100)
- Conversion: UI = model / 100; model = UI * 100

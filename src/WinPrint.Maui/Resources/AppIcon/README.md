# WinPrint app icon

This folder holds the **editable source** for the WinPrint app icon. It's a
hand-authored SVG — there is no generator program. If you want to change the
icon, edit the SVG; do **not** start from scratch.

## Files & pipeline

| File | Role |
|------|------|
| `appicon.svg` | **The icon.** The complete, full-bleed 1024×1024 design. Single source of truth. |
| `appiconfg.svg` | Intentionally **empty** (transparent). MAUI requires a `ForegroundFile`, but keeping the whole design in `appicon.svg` makes the composite predictable across platforms (no foreground safe-zone scaling surprises). |
| `render-preview.sh` | Renders `appicon.svg` to PNGs (1024 + small sizes) for quick visual review on macOS. |
| `appicon.png` | A **1024×1024 PNG** rendered from `appicon.svg`, committed for reuse (READMEs, web, social, etc.). Regenerate with `render-preview.sh` then copy `_preview/appicon-1024.png` over it. |

Wired up in `WinPrint.Maui.csproj`:

```xml
<MauiIcon Include="Resources\AppIcon\appicon.svg"
          ForegroundFile="Resources\AppIcon\appiconfg.svg" Color="#2B4FC8" />
```

At build time MAUI's **resizetizer** rasterizes `appicon.svg` into every
platform size (`obj/.../resizetizer/.../appicon.appiconset/*.png`, Windows
assets, etc.). You never edit those generated PNGs — only `appicon.svg`.
The same icon is used on **Windows and MacCatalyst**.

## Concept

"**B+C**": a syntax-highlighted **code page** (what WinPrint exists to print
beautifully) whose **top line is a `>_` prompt** representing the `wp` command —
so the code below reads as the command's output. Details that carry meaning:

- **Line-number gutter** on the left → WinPrint's line-numbering feature.
- **Syntax colours** → its syntax highlighting (the icon's only colour "pop").
- **Folded corner + drop shadow** → a real sheet of paper.
- **Blue→indigo background** → a nod to the legacy WinPrint blue (`#2B579A`).

Chosen from six explored directions (Printer & page / Syntax sheet / Prompt mark
/ 2-up·saves-trees / Code-in-flight / Printed-with-love) at a **+5° tilt**.

## How `appicon.svg` is structured (read the header comment in the file too)

- **Full-bleed, no rounded corners, no margin.** macOS/iOS apply their own
  rounded-rect mask + shadow at display time, so the art must reach the edges.
  Never bake rounding/margins into this file.
- **Coordinate trick:** the art is drawn in an original "squircle space"
  (x/y from 92→932, 840 wide). The outer
  `transform="translate(-112.19 -112.19) scale(1.2190476)"` maps that onto the
  full 0→1024 canvas (`scale = 1024/840`, `translate = -92*scale`). **Edit using
  the original numbers**; the mapping handles full-bleed.
- **Tilt:** `transform="rotate(5 518 511)"` (+5° about the page centre).
- Each code token is a single `<line>` (`x1→x2` = token width, `stroke` = its
  syntax colour). Rows are spaced 58 units apart, y = 320,378,…,726.

## Tweak recipes

| Want to… | Edit |
|----------|------|
| Change the **tilt** | the `5` in `rotate(5 518 511)` (try 0 for upright; ±5 was the chosen sweet spot) |
| Change the **background** | the `#bg` gradient stops (`#4A6CF7` → `#2336A8`) |
| Adjust **syntax colours** | the per-token `stroke=` values; palette legend is in the SVG header comment |
| Change the **prompt accent** | `polyline` stroke `#2B59C3` and cursor `rect` fill `#4078F2` |
| Give the page **more breathing room** | shrink the scale in the outer map (e.g. `1.13`) and re-centre the translate, or move the page path inward |
| Remove the **folded corner** | replace the dog-eared page `<path>` pair with a rounded `<rect>` and delete the `#fold` triangle |
| Re-flow the **code lines** | edit the `<line>` tokens (and keep the 9 gutter ticks aligned to the 9 rows) |

After any edit, run the preview and eyeball it (and remember macOS will round the
corners — the square render is expected).

## Preview locally

```bash
./render-preview.sh            # writes _preview/appicon-{1024,256,128,64,32,16}.png
```

Uses `qlmanage` + `sips` (built into macOS). On Linux/CI use
`rsvg-convert -w 1024 appicon.svg -o out.png` or Inkscape instead.

To preview the **macOS-masked** look (rounded + shadow), clip the art to a
rounded rect of radius `0.2237 × size` on a neutral backdrop — but the square
preview is enough for normal iteration.

## Which apps use this icon

- **MAUI (Windows + MacCatalyst)** — uses `appicon.svg` directly via the
  `MauiIcon` in the csproj. Nothing else to do.
- **WinForms (`src/WinPrint.WinForms`)** — its `<ApplicationIcon>` is
  `Document.ico`, generated from this same art. **Regenerate it** whenever
  `appicon.svg` changes:

  ```bash
  ./render-preview.sh   # produces _preview/appicon-1024.png
  python3 -c "from PIL import Image; Image.open('_preview/appicon-1024.png').convert('RGBA').save('../../../WinPrint.WinForms/Document.ico', format='ICO', sizes=[(16,16),(32,32),(48,48),(64,64),(128,128),(256,256)])"
  ```

  (The CLI, `src/WinPrint.cli/winprint.ico`, is a separate icon and is **not**
  updated by the above — update it too if you want full consistency.)

## Regenerate the platform icon set

It's automatic — just build the MAUI project. To force a clean regen (the
resizetizer caches), rebuild:

```bash
dotnet build src/WinPrint.Maui/WinPrint.Maui.csproj -f net10.0-maccatalyst -t:Rebuild
```

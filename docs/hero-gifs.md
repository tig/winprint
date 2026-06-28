# Hero GIFs — what they must show off, and how to regenerate them

The README and `docs/index.md` lead with one hero per front end. They are **marketing**:
each must *demonstrate the front end actually being driven*, not just sit on a static page.
Treat the choreography below as a spec — if a regenerated GIF doesn't tell its story, it's
not done. Hold every hero to the bar set by the TUI hero (the richest of the three).

| Hero | File | Source artifact | Producer |
|------|------|-----------------|----------|
| TUI (`wp`) | `docs/hero-tui.gif` | `wp <file>` in a terminal | `scripts/record-hero-gifs.sh` (tuirec) |
| Headless print | `docs/hero-print.gif` | `wp print … --what-if` | `scripts/record-hero-gifs.sh` (tuirec) |
| GUI on macOS | `docs/hero-gui-mac.gif` | Mac Catalyst `winprint` | `scripts/capture-gui-hero-macos.py` |
| GUI on Windows | `docs/hero-gui-win.gif` | WinUI3 `winprint.exe` | `scripts/capture-gui-hero-windows.ps1` + `scripts/assemble-gui-hero.py` |

All heroes use the same sample file — `src/WinPrint.Core/ViewModels/SheetViewModel.cs` —
so the three front ends are visibly rendering the *same* document. The README GUI section
shows Windows and macOS **side by side**.

## What each hero must show off

**TUI (`wp`)** — the full interactive story (the bar):
page through the document → **zoom in** → **pan** with the mouse → switch sheet
definitions → open another file. (The exact keystroke choreography lives in the
`--keystrokes` string in `scripts/record-hero-gifs.sh`; keep it rich.)

**Headless print (`wp print`)** — that printing needs no UI: show the command
(`wp print SheetViewModel.cs --what-if --sheet "Default 2-Up"`) and its output. Short.

**GUI (`wp gui`, both platforms)** — the visual print-preview experience. The choreography
**must drive the settings, zoom/pan, and open a second file**, mirroring the TUI's energy:

1. **Load** — the document rendered fit-to-window (2-up landscape), settings panel visible.
2. **Toggle Line Numbers** — uncheck then re-check, so the preview visibly loses/regains the
   line-number gutter (proves settings drive a live re-render).
3. **Toggle Landscape** — uncheck (reflow to 1-up portrait) then re-check (back to 2-up
   landscape).
4. **Zoom in → pan → reset** — zoom into the page (the zoom-% indicator should read e.g.
   `200%`), pan around, then return to fit. **Keep this part FAST** — short frame durations,
   no lingering — so it reads as a snappy flourish, not a slow crawl.
5. **Open another file** — use the File button + Open dialog to load a *different* document
   (the Windows producer opens `README.md`, which renders as formatted **Markdown** — a nice
   "not just source code" beat).
6. **Hold** — linger on the final page so the loop reads cleanly.

The macOS producer (`capture-gui-hero-macos.py`) historically only did page/page/arrow —
**that is the weak baseline, do not copy it.** Both GUI heroes should follow the spec above.
Settings toggles and file-open frames linger; the zoom/pan flourish stays fast.

## Regenerating the Windows GUI hero

Requires an **unlocked, interactive Windows session** (the capture injects real input) and
Pillow (`pip install --user Pillow`).

```powershell
# 1. Build the unpackaged WinUI3 app (use the RID this machine runs; win-arm64 on ARM).
dotnet build src/WinPrint.Maui/WinPrint.Maui.csproj -f net10.0-windows10.0.19041.0 -c Release

# 2. Drive the app and capture frames into artifacts/hero/gui-frames/.
#    Pass -Exe if your RID/config differs from the default (Release win-arm64).
scripts/capture-gui-hero-windows.ps1

# 3. Assemble the frames into docs/hero-gui-win.gif at README width.
python scripts/assemble-gui-hero.py
```

Then eyeball the frames in `artifacts/hero/gui-frames/` (and the final GIF) and confirm the
zoom/pan/reset story actually reads.

### Windows capture gotchas (the producer encodes these — don't regress them)

- **Zoom uses the plain TUI-consistent keys** (`=`/`+` in, `-` out, `0` fits). On Windows
  WinUI sends `VirtualKey` strings that don't match the handler's WPF-style tokens — the OEM
  `+`/`-` keys arrive as `"187"`/`"189"` and `0` as `"Number0"`/`"NumberPad0"` — so
  `OnNativeKeyDown` normalizes them to `OemPlus`/`OemMinus`/`D0` before dispatch. (This was a
  real bug: PR #199 made the GUI accept plain `+/-/0` like the TUI but only built MacCatalyst,
  so the keys still didn't route on Windows until the normalization landed.)
- **Reset-to-fit is plain `0`** (matches the TUI's fit key).
- **Focus the preview with a real mouse click first.** Keys only route when a XAML
  element has focus; the click on the `FocusablePlatformGraphicsView` both focuses it and
  forces a re-present. UIA `SetFocus` is unreliable here (it can land on a ComboBox, which
  then eats arrows/zoom).
- **Settings toggles are driven by clicking the sidebar *label*** (its `TapGestureRecognizer`
  flips the bound `CheckBox`), located via UIA `BoundingRectangle` — there's no automation id.
- **The Open dialog has no UIA-settable filename field.** It's the modern `IFileOpenDialog`:
  autoid `1148`/`1` are Panes, and cross-process `ValuePattern.SetValue` times out. The
  filename field has focus on open, so the producer pastes the path from the clipboard
  (`Set-Clipboard` + `Ctrl`+V) and presses Enter.
- **Screenshots use `PrintWindow` + `PW_RENDERFULLCONTENT`** (WinUI3 composition); see
  `.claude/skills/run-maui-app/scripts/Capture-Window.ps1`.

## Regenerating the macOS GUI hero

Producer: `scripts/capture-gui-hero-macos.py` (there is no macOS equivalent of the
`run-maui-app` skill — this script + `osascript`/`screencapture` **is** the Mac harness).
Today it only does the weak page/page/arrow baseline; **bring it up to the full spec above**
(load → toggle Line Numbers → toggle Landscape → fast zoom/pan/reset → open a 2nd file → hold).
Same sample file, same story as the Windows hero, so the two sit side by side in the README.

```bash
# 1. Build the Mac Catalyst app (arm64).
dotnet build src/WinPrint.Maui/WinPrint.Maui.csproj -c Release \
  -f net10.0-maccatalyst -r maccatalyst-arm64 /p:CreatePackage=false /p:EnableCodeSigning=false

# 2. Drive + capture (extend the script to the full choreography), then assemble the GIF.
python3 scripts/capture-gui-hero-macos.py --output docs/hero-gui-mac.gif
```

### macOS mechanics (the Mac analogs of the Windows gotchas — verify on a Mac)

The good news: macOS has **no PrintWindow/composition-cache problem** (`screencapture` grabs
the real screen), so there's no "force a present" dance — just keep the app **frontmost**
(`osascript -e 'tell application "winprint" to activate'`) before each key/shot.

- **Zoom uses the same plain keys as the TUI/Windows** — `=`/`+` in, `-` out, `0` fits — and
  on Mac they're already wired correctly (`AppDelegate.KeyCommands` maps `=`/`+`→`Add`,
  `-`→`Subtract`, `0`→`D0`; the Cmd+ variants also work). Send them with System Events, e.g.
  `osascript -e 'tell application "System Events" to keystroke "="'`. The plain keys are only
  claimed when **no text field is focused**, so focus the preview first.
- **Focus the preview before keys:** press **Escape** — `AppDelegate` maps it to
  `MainPage.FocusPreview()` — or click the preview area. This is the Mac analog of the Windows
  "click the preview to focus" step. At fit zoom the arrows page-navigate; once zoomed they pan
  (identical to Windows).
- **Pan + page with key codes** via `key code` (System Events): arrows = 123/124/125/126
  (←/→/↓/↑), Page Down = 121, Page Up = 116.
- **Settings toggles (Line Numbers, Landscape):** same model as Windows — the sidebar *label*
  has a `TapGestureRecognizer` that flips the bound `CheckBox`. Click it. Prefer macOS
  Accessibility if the element is addressable (`System Events`: `click checkbox …` / `AXPress`);
  otherwise compute the label's screen point from the window bounds
  (`osascript … get bounds of window 1`) and click coordinates — exactly what the Windows
  producer does via UIA `BoundingRectangle`.
- **Open another file:** **Cmd+O** opens the File ▸ Open… panel (registered in `AppDelegate`).
  In the open panel press **Cmd+Shift+G**, type/paste the absolute path, **Return**, then
  **Return** to open — the Mac equivalent of the Windows clipboard-paste-into-the-filename-field
  trick. Open `README.md` so it renders as Markdown (the "not just source code" beat).
- **Capture just the window**, not a full-screen crop: get the window id from the window list
  and `screencapture -o -l <windowID> frame.png` (cleaner than the current margin-crop
  heuristic). Resize to the README hero width (1102) when assembling.

## Regenerating the TUI / headless-print heroes

`scripts/record-hero-gifs.sh` regenerates the TUI and print heroes on any OS with tuirec
(it also invokes the macOS GUI capture when run on macOS).

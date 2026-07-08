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
| GUI on Windows | `docs/hero-gui-win.gif` | Installed WinPrint, driven by MCEC | agent-driven MCEC MCP tour; see [`docs/hero-gif-win.md`](hero-gif-win.md) |

All heroes use the same sample file — `src/WinPrint.Core/ViewModels/SheetViewModel.cs` —
so the three front ends are visibly rendering the *same* document. The README GUI section
shows Windows and macOS **side by side**.

## What each hero must show off

**TUI (`wp`)** — the full interactive story (the bar), recorded under the **`Anders`** theme:
show the source file and **zoom in/out** (no pan) → open **`./testfiles/demo.md`** through the
File dialog (it renders as formatted **Markdown** — the "not just source code" beat) → page
through it → switch the **Sheet Definition** to **Proportional 1-Up** (single-column reflow) → open
the **Content Font** dialog and bump the size to **12pt** → page again → **End** (hold on the
rendered **Mermaid diagram** on demo.md's last page — needs `renderMermaidDiagrams` enabled, see
["Enabling the Mermaid beat"](#enabling-the-mermaid-beat)) → Home → **quit** (Esc → *Don't
Save*). The exact keystroke choreography — button hotkeys (`Alt+F`/`Alt+N`), the Sheet-Definition
dropdown pick, and the pixel-calibrated Content-Font-dialog clicks — lives in the `--keystrokes`
string in `scripts/record-hero-gifs.sh`; keep it rich. To open `demo.md` robustly, the choreography
clicks the Open dialog's **search (Find) box** and types `demo.md`: that box does a **recursive**
search, so after a couple of seconds the tree narrows to the single matching file (no dependency on
where it sorts in the listing), which a double-click then opens. Note that **switching the sheet
reloads the document**, so the script waits a beat before driving the font dialog — otherwise the
clicks race the reload and desync. (Settings edits dirty the sheet, so the quit raises the save
prompt — the hero answers *Don't Save*.)

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
5. **Open `testfiles/demo.md` and switch to Proportional 1-Up** — open the
   purpose-built Markdown showcase (`demo.md`: headings, lists, a table, code blocks, an
   image, a Mermaid block), then change the Sheet Definition picker to **Proportional 1-Up**
   so the preview reflows to single-column prose with a proportional font. This is the "not
   just source code" beat — the same file and sheet the TUI hero uses.
6. **Print to PDF and open** — print the current document to PDF, save it as
   `winprintdemo.pdf`, open the result so the loop ends on real printed output — **ending on the
   last page's rendered Mermaid diagram** (needs `renderMermaidDiagrams` enabled, see
   ["Enabling the Mermaid beat"](#enabling-the-mermaid-beat)).
   Windows (MCEC hero): select **Microsoft Print to PDF**, print, save the file, open in Edge.
   macOS (`capture-gui-hero-macos.py`): Cmd+P → **PDF** button in the native print panel →
   **Save as PDF…** → type the path → Return → `open` (Preview); capture the Preview window
   for the PDF frames (page 1 → Page Down → End, holding on the Mermaid page).
7. **Hold** — linger on the final page so the loop reads cleanly.

Both GUI producers drive the full choreography above (the macOS one was once a weak
page/page/arrow baseline — don't regress it back). Settings toggles and file-open frames
linger; the zoom/pan flourish stays fast.

## Enabling the Mermaid beat

`demo.md`'s Mermaid fence only renders as a live diagram when `renderMermaidDiagrams` is `true`
in `markdownContentTypeEngineSettings` (off by default — the diagram source is sent to a remote
rendering service, mermaid.ink unless `mermaidServiceUrl` says otherwise, so recording needs
network). **Seed it before recording or the fence prints as a code block** and the End/last-page
beats show code instead of a picture. On macOS/Linux winprint runs in **portable mode** — the
config sits next to the executable, and each subject has its own:

- **TUI (`wp`)**: `src/WinPrint.TUI/bin/Release/net10.0/WinPrint.config.json`
- **Mac Catalyst GUI**: `WinPrint.app/Contents/MonoBundle/WinPrint.config.json` (inside the
  built bundle)
- **Windows (MCEC hero)**: the installed app's co-located config — see
  [`docs/hero-gif-win.md`](hero-gif-win.md)

Recipe: **run the subject once** (e.g. `wp print testfiles/demo.md --what-if`, or launch + quit
the GUI) so it writes its full default config, then flip the flag
(`"renderMermaidDiagrams": true`). Don't hand-author a partial config — the app expects the
complete default document. The bundle config **survives incremental builds but not clean ones**
(deleting `bin`/`obj` deletes it) — re-seed after every clean build.

## Regenerating the Windows GUI hero

The Windows hero is **agent-driven**: MCEC drives installed WinPrint over MCP through the full
choreography and records the desktop region — there is **no producer script in this repo**. The whole
recipe (stand up the MCEC controller from the mcec repo, then the numbered WinPrint tour) lives in
[`docs/hero-gif-win.md`](hero-gif-win.md). Requires an **unlocked, interactive Windows session** and
**WinPrint installed** (Velopack/winget). The legacy frame-capture path
(`scripts/capture-gui-hero-windows.ps1` + `scripts/assemble-gui-hero.py`) is deprecated for the README
hero — it recorded window-only frames without desktop/PDF context.

## Regenerating the macOS GUI hero

Producer: `scripts/capture-gui-hero-macos.py` (there is no macOS equivalent of the
`run-maui-app` skill — this script + `osascript`/`cliclick`/`screencapture` **is** the Mac
harness). It drives the full spec above (load → toggle Line Numbers → toggle Landscape → fast
zoom/pan/reset → open `testfiles/demo.md` as Markdown + Proportional 1-Up → **print to PDF →
open in Preview, ending on the Mermaid page** → hold), same sample/story as the Windows hero so
the two sit side by side in the README. Needs `cliclick` (`brew install cliclick`) and an
**interactive, unlocked** session with Screen-Recording + Accessibility permission.

```bash
# 1. CLEAN-build the Mac Catalyst app (arm64). Always clean first: a stale incremental
#    Mono-AOT build can SIGABRT in load_aot_module at launch, and the capture then dies at
#    window_rect() ("invalid literal for int() ... ''") because the app never gets a window.
rm -rf src/WinPrint.Maui/bin src/WinPrint.Maui/obj
dotnet build src/WinPrint.Maui/WinPrint.Maui.csproj -c Release \
  -f net10.0-maccatalyst -r maccatalyst-arm64 /p:CreatePackage=false /p:EnableCodeSigning=false

# 2. Seed renderMermaidDiagrams (see "Enabling the Mermaid beat") — the clean build wiped it.

# 3. Drive + capture + assemble the GIF (variable per-frame durations are baked in).
python3 scripts/capture-gui-hero-macos.py --output docs/hero-gui-mac.gif
```

**Pre-flight, every run (crash dialogs photobomb the GIF):** any abnormal winprint exit — a
crash, or `pkill` of a still-running instance (the capture script leaves the app running when
done, and the *next* run's cleanup pkills it) — queues a macOS **"winprint quit unexpectedly"**
alert that floats over the app window and lands in **every captured frame**.
`killall UserNotificationCenter` does **not** clear them — the process respawns and re-displays
the queued alerts. Dismiss them by clicking, and quit any leftover instance *gracefully*:

```bash
osascript -e 'tell application "winprint" to quit'   # graceful — no dialog
while osascript -e 'tell application "System Events" to tell process
  "UserNotificationCenter" to click button "Ignore" of window 1' 2>/dev/null; do sleep 1; done
```

Then **verify the first captured frame** (`artifacts/hero/gui-frames-mac/00-loaded.png`) is
dialog-free before accepting the GIF.

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
- **Settings toggles (Line Numbers, Landscape):** the sidebar *label* has a
  `TapGestureRecognizer` that flips the bound `CheckBox` — click it. **MAUI Catalyst does not
  expose these via Accessibility** (`entire contents of window 1` finds nothing addressable), so
  the producer clicks **window-relative coordinates** with `cliclick` (offsets calibrated to the
  default 1000×820 window, added to the live window origin from `osascript … position of window 1`).
- **Open another file:** **Cmd+O** → File ▸ Open…, then **Cmd+Shift+G** for the go-to-folder
  sheet, **type the absolute path with `cliclick t:`**, **Return** (resolve), **Return** (open).
  Use `cliclick` to type — synthetic **Cmd+V doesn't land** in the go-to field, and an `osascript`
  keystroke of the path loses characters to the `/` autocomplete. Open `testfiles/demo.md` so it
  renders as Markdown (the "not just source code" beat; chosen over `README.md`, which is now
  gif-heavy — a wall of images).
- **Capture just the window** with `screencapture -x -R<x,y,w,h>` using the window's points rect
  (`osascript … {position, size} of window 1`); `-R` outputs native (Retina ×2) pixels, no
  CGWindowID needed (pyobjc/Quartz isn't installed). Resize to the README hero width (1102) when
  assembling.
- **Print to PDF:** **Cmd+P** → `UIPrintInteractionController.Present()` bridges to the native
  **NSPrintPanel**. The panel may appear as a sheet on `window 1` or as a floating window
  depending on macOS version. The script uses an AppleScript loop over all process windows to
  find and click the **PDF** popup button, then selects **Save as PDF…**, types the path with
  `cliclick t:`, and presses Return. After the PDF is written, `open <path>` launches Preview.
  The Preview window bounds are read via System Events and captured with `screencapture -R` for
  the PDF frames. Close Preview after capturing so its file lock doesn't block the next run's
  delete of `winprintdemo.pdf`. The `--pdf-out` flag sets the destination (default:
  `~/Documents/winprintdemo.pdf`).

## The CLI showcase (`docs/cli.gif`) and printing to PDF on macOS

`docs/cli.gif` is the README's **"How to turn Markdown into a PDF"** beat — `wp print` turning
`testfiles/mermaid.md` into a paginated PDF from a real shell, then opening the result. It is
**produced on Windows** (`--printer "Microsoft Print to PDF"`); the full recording recipe is
[`docs/hero-gif-win.md`](hero-gif-win.md) step 12. This section documents the **macOS analog** —
how to give `wp print --printer …` a real "print to PDF" target on a Mac.

**Why a printer is needed at all.** On macOS/Linux `wp print` renders the *entire* document to a
PDF in-process (`SkiaPdfRenderer`, mermaid already rasterized) and then hands that PDF to CUPS with
`lpr -P "<printer>" -T <doc>` (see `LprClient`/`UnixPrintJob`). So `--printer` must name a CUPS
queue, and to capture a file you need a queue whose backend *writes the job to disk*. macOS ships
**no** such queue, and **`brew install cups-pdf` does not exist** — `cups-pdf` is a Linux package
(`printer-driver-cups-pdf`); there is no Homebrew formula and macOS provides no `file`/`pdf` CUPS
backend (`/usr/libexec/cups/backend/` has only network/usb backends). The macOS drop-in is
**RWTS PDFwriter**, a virtual-PDF print driver (the CutePDF/​cups-pdf analog).

**Setup (one time).**

1. **Install the driver:** `brew install --cask rwts-pdfwriter` (v3.1d; it drops a signed `.pkg`,
   so it prompts for admin). This installs the PPD only — it does **not** create a printer queue.
2. **Add the queue:** System Settings ▸ **Printers & Scanners** ▸ **Add Printer** ▸ pick the
   **PDFwriter** driver. In the **Name** field type **`CUPS-PDF`** so it matches the README command
   verbatim (or leave it `PDFwriter` and pass `--printer "PDFwriter"`).
   - CLI alternative: `lpadmin -p CUPS-PDF -E -v pdfwriter:/ -P <PDFwriter.ppd>` — but first confirm
     the exact device URI (`lpinfo -v | grep -i pdfwriter`) and PPD path
     (`/Library/Printers/PPDs/…` or `lpinfo -m | grep -i pdfwriter`); these weren't verified on a Mac
     here, so check them before scripting.

**Record the beat** (mirrors the Windows step-12 choreography):

```bash
wp print mermaid.md --printer "CUPS-PDF" --sheet "Proportional 1-Up"
open /var/spool/pdfwriter/$USER/mermaid.pdf     # then page through it on-record
```

Use a **`net10.0`-TFM `wp`** (the macOS CLI print path is `lpr`; the portable build is the correct
one here — unlike Windows there is no separate `-windows` TFM to worry about). The Homebrew `wp`
may be too old to have the in-process Mermaider renderer — build `develop` if the diagrams print as
code (`dotnet build src/WinPrint.TUI/WinPrint.TUI.csproj -f net10.0` → `bin/Debug/net10.0/wp`).

**Gotchas.**

- **Output path is fixed:** PDFwriter always writes to **`/var/spool/pdfwriter/<username>/`**, named
  after the CUPS job title (`wp` passes `-T mermaid.md`). macOS driver sandboxing makes this
  location **unchangeable** — you can't tell it to write next to the source file; symlink/alias it
  if the recording needs the file elsewhere.
- **It round-trips the PDF:** the job goes PDF → PostScript → PDF through the CUPS filter chain
  (ghostscript). `wp`'s mermaid diagrams are embedded raster and survive as raster; text stays
  vector — fine for a screen-recorded viewer. Remember `wp` already produced a finished PDF; the
  printer only exists to make the `wp print --printer` path *real* for the gif.
- **Verified vs. not:** the `wp` render → `lpr` half was verified on this Mac by capturing the exact
  bytes `wp` pipes to `lpr -P "CUPS-PDF"` (10-page PDF, every mermaid type rendered in-process, the
  unsupported **gantt** falling back to a code block, Proportional 1-Up reflow). The PDFwriter
  install + round-trip is documented from upstream and **not yet exercised on a Mac in this repo** —
  confirm the queue name, output path, and fidelity on the first real run before recording.

### Linux

Linux is the easy case — the `wp` print path is identical (`lpr -P …`), and `cups-pdf` is a **real
distro package** here (this is where the name comes from; it never existed on macOS/Homebrew).

- **Install:** `sudo apt install printer-driver-cups-pdf` (Debian/Ubuntu) or `sudo dnf install
  cups-pdf` (Fedora). It **auto-creates the CUPS queue** on install — no manual "Add Printer".
- **Queue name differs from the README command:** it is **`PDF`** on Debian/Ubuntu and **`Cups-PDF`**
  on Fedora — **not** `CUPS-PDF`. So the command is `wp print mermaid.md --printer "PDF" --sheet
  "Proportional 1-Up"` (adjust the name per distro, or `lpadmin`-create a `CUPS-PDF` alias). Confirm
  with `lpstat -p` after install.
- **Output:** default **`~/PDF/`** on Debian/Ubuntu (upstream default is
  `/var/spool/cups-pdf/${USER}/`); set in `/etc/cups/cups-pdf.conf`. **AppArmor caveat:** if you
  change the output dir there, you must also edit `/etc/apparmor.d/usr.sbin.cupsd` or the writes are
  blocked.
- **`wp`:** the `net10.0` build (SkiaSharp render path — no `libgdiplus`/System.Drawing needed here).

The `wp` render → `lpr` half is the *same code* verified on macOS, so it should behave identically; a
real end-to-end run against `cups-pdf` on Linux is tracked separately (needs a Linux box with CUPS).

## Regenerating the TUI / headless-print heroes

`scripts/record-hero-gifs.sh` regenerates the TUI and print heroes — **on macOS/Linux only**
(it also invokes the macOS GUI capture when run on macOS). Before recording, seed
`renderMermaidDiagrams` in `wp`'s co-located `WinPrint.config.json` (and, since the script also
runs the GUI capture on macOS, in the app bundle's) — see
["Enabling the Mermaid beat"](#enabling-the-mermaid-beat) — or the TUI hero's End beat shows a
code block instead of the diagram.

### Theming the hero (`TUI_CONFIG`)

**The TUI hero must be recorded under the `Anders` theme.** This is the standard going
forward — always regenerate the hero with `Anders`, never the default, so the hero stays
visually consistent across regenerations. `wp` enables Terminal.Gui's `ConfigurationManager`
(`ConfigLocations.All`, see `src/WinPrint.TUI/Program.cs`), which includes the **`TUI_CONFIG`
environment variable** as a config source. `record-hero-gifs.sh` themes the app by passing it to
tuirec:

```bash
--env 'TUI_CONFIG={"Theme":"Anders"}'
```

**Gotcha — `TUI_CONFIG` holds the JSON *inline*, not a file path.** Terminal.Gui routes the env
var's value through its JSON-*content* loader (`SourcesManager.cs`, the same `Load` overload used
for `RuntimeConfig`), **not** the file-*path* loader that the `./.tui/…` and `~/.tui/…` locations
use. So `TUI_CONFIG=/path/to/config.json` is parsed as JSON literally, fails, and is **silently
ignored** — you must give it the JSON document itself (`TUI_CONFIG={"Theme":"Anders"}`).

We deliberately use the env var rather than a `./.tui/wp.config.json` file: a `.tui/` dir in the
working directory **shows up in wp's Open-file dialog** and shifts the row the choreography
double-clicks to open `demo.md`. The env var themes the app while writing **nothing** to disk, so
the cwd (and the file dialog's listing) stays clean. Keep `Anders` — do not change the theme when
regenerating unless you are deliberately restyling every hero.

(For a persistent, non-hero theme you can instead drop `{"Theme":"Anders"}` at `~/.tui/config.json`
(user-wide) or `./.tui/wp.config.json` (per-directory) — those locations *are* file paths and can
also carry other Terminal.Gui settings, e.g. `"Application.ForceDriver": "dotnet"`.)

**Windows cannot record the TUI hero.** Under tuirec's ConPTY, Terminal.Gui's default
`windows` driver sees a console output handle without `ENABLE_VIRTUAL_TERMINAL_PROCESSING`
and flags it `IsLegacyConsole`, which disables all raster graphics (Kitty/sixel) and
truecolor — the preview pane records as a blank box. Even forcing the `dotnet` driver
(`"Application.ForceDriver": "dotnet"` via wp's `.tui/wp.config.json`, honored now that wp
enables Terminal.Gui's ConfigurationManager) restores truecolor and clears the legacy flag,
but the Kitty image emission still never reaches the recorded output. The document loads and
the settings panel renders fine — it's specifically the raster preview that's lost, which is
the whole point of the hero. Record on macOS or Linux.

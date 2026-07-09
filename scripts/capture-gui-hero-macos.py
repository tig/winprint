#!/usr/bin/env python3
"""Capture the macOS GUI hero GIF of the Mac Catalyst WinPrint app.

tuirec only records terminal sessions, so the native MAUI GUI is captured with
window screenshots assembled into a GIF. Requires the GUI built
(Release/maccatalyst-arm64), Pillow, and `cliclick`
(`brew install cliclick`), plus an **interactive, unlocked** macOS session with
Screen-Recording and Accessibility permission for the terminal running this.

Choreography (mirrors the Windows hero + the spec in docs/hero-gifs.md):
  load -> toggle Line Numbers off/on -> toggle Landscape off/on ->
  focus + fast zoom-in/pan/reset -> open testfiles/demo.md (Markdown + Proportional 1-Up) ->
  print to PDF -> open PDF in Preview (page 1 Mermaid -> Page Down -> End) -> hold.

macOS mechanics (see docs/hero-gifs.md):
  - Zoom uses the plain TUI keys: `=`/`+` in, `-` out, `0` fit. Only claimed when
    no text field is focused, so press Escape (-> MainPage.FocusPreview) first.
  - Settings toggles: click the sidebar *label* (its TapGestureRecognizer flips
    the bound CheckBox). MAUI Catalyst doesn't expose these via Accessibility, so
    we click window-relative coordinates (calibrated below).
  - Open a file: Cmd+O, then Cmd+Shift+G, paste the path, Return, Return.
  - Sheet Definition picker: click at SHEET_PICKER offset to open the dropdown,
    then click PROP_1UP_ITEM offset (22pt below) to select "Proportional 1-Up".
    Both clicks use the live window origin so they survive window moves. Do NOT
    call activate() between the two clicks — it would dismiss the open dropdown.
  - Print to PDF: Cmd+P opens the NSPrintPanel as a sheet on window 1.
      - PDF button:  menu button 1 of group 2 of splitter group 1 of sheet 1 of window 1
      - Save as PDF: menu item "Save as PDF…" of menu 1 of that menu button
      - Save dialog: sheet 1 of sheet 1 of window 1 (nested sheet on the print sheet)
      - Filename field: text field "Save As:" of splitter group 1 of that nested sheet
      - Directory via Cmd+Shift+G in the save dialog
  - Capture: screencapture -R <window rect in points> (outputs native pixels).
"""

from __future__ import annotations

import argparse
import subprocess
import sys
import time
from pathlib import Path

try:
    from PIL import Image
except ImportError:
    print("Pillow is required: pip install --user Pillow", file=sys.stderr)
    sys.exit(1)

APP = "winprint"

# Sidebar click targets as (x, y) offsets in *window points* from the window's
# top-left. Calibrated against the 1000x820 window; clicks scale with the window
# origin (read live) but assume the default window size.
LANDSCAPE_LABEL = (65, 151)
LINE_NUMBERS_LABEL = (77, 581)
# Sheet Definition picker and dropdown item. Calibrated against the 1000×820
# window: picker sits 108pt from the top; "Proportional 1-Up" is 22pt below it
# (one menu-item row beneath the pre-selected "Proportional 2-Up" entry).
SHEET_PICKER = (125, 108)
PROP_1UP_ITEM = (125, 130)


def run(cmd: list[str], check: bool = False) -> subprocess.CompletedProcess[str]:
    return subprocess.run(cmd, check=check, text=True, capture_output=True)


def osa(script: str) -> str:
    return run(["osascript", "-e", script]).stdout.strip()


def quit_app() -> None:
    run(["osascript", "-e", f'tell application "{APP}" to quit'])
    run(["pkill", "-f", "WinPrint.app/Contents/MacOS"])
    time.sleep(1.0)


def launch(exe: Path, sample: Path) -> None:
    subprocess.Popen([str(exe), str(sample)], stdout=subprocess.DEVNULL, stderr=subprocess.DEVNULL)


def activate() -> None:
    run(["osascript", "-e", f'tell application "{APP}" to activate'])
    time.sleep(0.35)


def window_rect() -> tuple[int, int, int, int]:
    out = osa(
        'tell application "System Events" to tell process "%s" '
        "to get {position, size} of window 1" % APP
    )
    # e.g. "86, 58, 1000, 820"
    nums = [int(n) for n in out.replace(" ", "").split(",")]
    return nums[0], nums[1], nums[2], nums[3]


def key_char(ch: str) -> None:
    osa('tell application "System Events" to keystroke "%s"' % ch)
    time.sleep(0.45)


def key_code(code: int) -> None:
    osa("tell application \"System Events\" to key code %d" % code)
    time.sleep(0.45)


def key_combo(ch: str, mods: str) -> None:
    osa('tell application "System Events" to keystroke "%s" using {%s}' % (ch, mods))
    time.sleep(0.6)


def focus_preview() -> None:
    activate()
    key_code(53)  # Escape -> MainPage.FocusPreview()


def click_label(offset: tuple[int, int]) -> None:
    activate()
    x, y, _, _ = window_rect()
    run(["cliclick", "c:%d,%d" % (x + offset[0], y + offset[1])])
    time.sleep(0.6)


def open_file(path: Path) -> None:
    activate()
    key_combo("o", "command down")              # File ▸ Open…
    time.sleep(1.6)
    key_combo("g", "command down, shift down")  # Go to folder… (dedicated path sheet)
    time.sleep(1.3)
    # Type the path with cliclick (CGEvents) — synthetic Cmd+V doesn't land in the
    # go-to field, and osascript keystroke loses chars to the "/" autocomplete.
    run(["cliclick", "-w", "20", "t:" + str(path)])
    time.sleep(1.0)
    key_code(36)                                # Return: resolve the path
    time.sleep(1.0)
    key_code(36)                                # Return: open the selected file
    time.sleep(2.0)


def switch_to_proportional_1up() -> None:
    """Open the Sheet Definition picker and select Proportional 1-Up.

    Both cliclick calls share the same window_rect() read so the second
    click stays valid even if the window moved.  activate() is deliberately
    NOT called between the two clicks — it would dismiss the open dropdown.
    """
    activate()
    x, y, _, _ = window_rect()
    run(["cliclick", "c:%d,%d" % (x + SHEET_PICKER[0], y + SHEET_PICKER[1])])
    time.sleep(0.8)
    run(["cliclick", "c:%d,%d" % (x + PROP_1UP_ITEM[0], y + PROP_1UP_ITEM[1])])
    time.sleep(3.5)  # document re-renders after a sheet switch


def capture_rect(path: Path, x: int, y: int, w: int, h: int) -> None:
    run(["screencapture", "-x", "-R%d,%d,%d,%d" % (x, y, w, h), str(path)])


def capture(path: Path) -> None:
    activate()
    time.sleep(0.3)
    x, y, w, h = window_rect()
    capture_rect(path, x, y, w, h)


def trigger_print_dialog() -> bool:
    """Send Cmd+P and wait for the NSPrintPanel sheet to appear on window 1.

    Returns True if the print sheet appeared, False on timeout.
    AppDelegate maps Cmd+P to InvokePrint() -> PerformPrintAsync() ->
    MacPrintJob.EndAsync() (SkiaPdfRenderer renders the whole doc) ->
    UIPrintInteractionController.Present(), which bridges to NSPrintPanel
    and shows it as a sheet on window 1.
    """
    activate()
    key_combo("p", "command down")
    # SkiaPdfRenderer renders the document first, THEN the sheet appears;
    # allow enough time for both steps.
    time.sleep(5.5)
    count = osa(
        'tell application "System Events" to tell process "winprint" '
        'to return count of sheets of window 1'
    )
    return count.strip() == "1"


def save_print_as_pdf(pdf_path: Path) -> bool:
    """With the NSPrintPanel sheet already open, click PDF -> Save as PDF… and save.

    Accessibility paths verified against Mac Catalyst on macOS Sequoia:
      Print sheet:  sheet 1 of window 1
      PDF button:   menu button 1 of group 2 of splitter group 1 of sheet 1 of window 1
      Save as PDF…: menu item "Save as PDF…" of menu 1 of that menu button
      Save dialog:  sheet 1 of sheet 1 of window 1  (nested sheet)
      Filename:     text field "Save As:" of splitter group 1 of that nested sheet
    """
    # Click the PDF popup menu button in the print sheet.
    osa(
        'tell application "System Events" to tell process "winprint" '
        'to click menu button 1 of group 2 of splitter group 1 of sheet 1 of window 1'
    )
    time.sleep(0.7)

    # Select "Save as PDF…" from the dropdown.
    osa(
        'tell application "System Events" to tell process "winprint" '
        'to click menu item "Save as PDF…" of menu 1 '
        'of menu button 1 of group 2 of splitter group 1 of sheet 1 of window 1'
    )
    time.sleep(1.5)  # nested save sheet animates in

    # Focus the "Save As:" filename field and replace its content.
    osa(
        'tell application "System Events" to tell process "winprint" '
        'to set focused of (text field "Save As:" of splitter group 1 '
        'of sheet 1 of sheet 1 of window 1) to true'
    )
    time.sleep(0.3)
    key_combo("a", "command down")  # select all existing text
    time.sleep(0.2)
    # Type the stem only — the "Save as PDF…" panel auto-appends ".pdf",
    # so "winprintdemo" -> "winprintdemo.pdf"; "winprintdemo.pdf" -> double extension.
    run(["cliclick", "-w", "20", "t:" + pdf_path.stem])
    time.sleep(0.3)

    # Navigate to the target directory via the Go to Folder sheet (Cmd+Shift+G).
    key_combo("g", "command down, shift down")
    time.sleep(1.0)
    run(["cliclick", "-w", "20", "t:" + str(pdf_path.parent)])
    time.sleep(0.4)
    key_code(36)   # Return: navigate to the folder
    time.sleep(0.8)
    key_code(36)   # Return: confirm Save
    time.sleep(3.0)  # wait for the PDF to be written and both sheets to dismiss

    if not pdf_path.exists():
        print(f"WARNING: PDF not found at {pdf_path} — print may have failed", file=sys.stderr)
        return False

    print(f"PDF written: {pdf_path} ({pdf_path.stat().st_size:,} bytes)")
    return True


def open_pdf_in_preview(pdf_path: Path) -> tuple[int, int, int, int] | None:
    """Open a PDF with `open` (defaults to Preview), wait for it to load.

    Returns the Preview window rect as (x, y, w, h), or None if not found.
    """
    run(["open", str(pdf_path)])
    print("Waiting for Preview to load the PDF…")
    time.sleep(3.5)

    out = osa(
        'tell application "System Events" to tell process "Preview" '
        "to get {position, size} of window 1"
    )
    try:
        nums = [int(n) for n in out.replace(" ", "").split(",")]
        return nums[0], nums[1], nums[2], nums[3]
    except (ValueError, IndexError):
        print("WARNING: could not get Preview window bounds", file=sys.stderr)
        return None


def main() -> int:
    ap = argparse.ArgumentParser(description=__doc__)
    ap.add_argument(
        "--exe",
        type=Path,
        default=Path(
            "src/WinPrint.Maui/bin/Release/net10.0-maccatalyst/maccatalyst-arm64/"
            "WinPrint.app/Contents/MacOS/winprint"
        ),
    )
    ap.add_argument("--sample", type=Path, default=Path("src/WinPrint.Core/ViewModels/SheetViewModel.cs"))
    ap.add_argument("--second-file", type=Path, default=Path("testfiles/demo.md"))
    ap.add_argument(
        "--pdf-out",
        type=Path,
        default=Path.home() / "Documents" / "winprintdemo.pdf",
        help="Where to save the printed PDF (deleted before each run)",
    )
    ap.add_argument("--output", type=Path, default=Path("docs/hero-gui-mac.gif"))
    ap.add_argument("--workdir", type=Path, default=Path("artifacts/hero/gui-frames-mac"))
    ap.add_argument("--width", type=int, default=1102, help="README hero width")
    args = ap.parse_args()

    if not args.exe.is_file():
        print(f"GUI executable not found: {args.exe}", file=sys.stderr)
        print(
            "Build: dotnet build src/WinPrint.Maui/WinPrint.Maui.csproj -c Release "
            "-f net10.0-maccatalyst -r maccatalyst-arm64 /p:CreatePackage=false /p:EnableCodeSigning=false",
            file=sys.stderr,
        )
        return 1

    args.workdir.mkdir(parents=True, exist_ok=True)
    args.output.parent.mkdir(parents=True, exist_ok=True)
    sample = args.sample.resolve()
    second = args.second_file.resolve()

    if args.pdf_out.exists():
        args.pdf_out.unlink()

    quit_app()
    launch(args.exe.resolve(), sample)
    print("Waiting for GUI to load…")
    time.sleep(11)
    activate()

    # (label, action, frame_ms). Settings/file frames linger; zoom/pan stay fast.
    frames: list[tuple[Image.Image, int]] = []

    def shot(label: str, ms: int) -> None:
        p = args.workdir / f"{len(frames):02d}-{label}.png"
        capture(p)
        img = Image.open(p).convert("RGB")
        if args.width and img.width != args.width:
            r = args.width / img.width
            img = img.resize((args.width, int(img.height * r)), Image.LANCZOS)
        frames.append((img, ms))
        print(f"captured {p.name} ({img.size[0]}x{img.size[1]})")

    def shot_rect(label: str, ms: int, x: int, y: int, w: int, h: int) -> None:
        p = args.workdir / f"{len(frames):02d}-{label}.png"
        capture_rect(p, x, y, w, h)
        img = Image.open(p).convert("RGB")
        if args.width and img.width != args.width:
            r = args.width / img.width
            img = img.resize((args.width, int(img.height * r)), Image.LANCZOS)
        frames.append((img, ms))
        print(f"captured {p.name} ({img.size[0]}x{img.size[1]})")

    shot("loaded", 900)

    # Toggle Line Numbers off then on (preview loses/regains the gutter).
    click_label(LINE_NUMBERS_LABEL); shot("linenums-off", 800)
    click_label(LINE_NUMBERS_LABEL); shot("linenums-on", 700)

    # Toggle Landscape off (reflow to 1-up portrait) then on (back to 2-up).
    click_label(LANDSCAPE_LABEL); shot("portrait", 900)
    click_label(LANDSCAPE_LABEL); shot("landscape", 750)

    # Fast zoom-in / pan / reset flourish.
    focus_preview()
    key_char("="); key_char("="); key_char("="); shot("zoom", 350)
    key_code(124); key_code(125); shot("pan1", 300)   # Right, Down
    key_code(123); key_code(126); shot("pan2", 300)   # Left, Up
    key_char("0"); shot("reset", 400)                 # fit

    # Open testfiles/demo.md and switch to Proportional 1-Up — the "not just
    # source code" beat (mirrors the TUI hero and the Windows hero).
    open_file(second)
    shot("demo-loaded", 800)

    switch_to_proportional_1up()
    shot("prop1up", 1200)

    focus_preview()
    key_code(121); shot("prop1up2", 1000)             # Page Down through prose

    # Print to PDF and open — mirrors the Windows hero's final beat.
    print("Triggering print dialog…")
    dialog_visible = trigger_print_dialog()
    if dialog_visible:
        # Capture one frame with the print sheet visible (proves printing is driven).
        shot("print-dialog", 1000)

        pdf_ok = save_print_as_pdf(args.pdf_out)
        if pdf_ok:
            # App is back to its normal state after the sheets dismiss.
            shot("printed", 800)

            # Open the PDF in Preview and capture its window.
            preview_rect = open_pdf_in_preview(args.pdf_out)
            if preview_rect is not None:
                run(["osascript", "-e", 'tell application "Preview" to activate'])
                time.sleep(0.5)
                px, py, pw, ph = preview_rect
                shot_rect("pdf-page1", 1100, px, py, pw, ph)
                # Page through the PDF to show the full printed output.
                osa('tell application "System Events" to tell process "Preview" to key code 121')
                time.sleep(0.5)
                shot_rect("pdf-page2", 1000, px, py, pw, ph)
                # End on the last page: the rendered Mermaid diagram atop demo.md's final page —
                # the beat the Windows hero also closes on (docs/hero-gif-win.md step 11).
                osa('tell application "System Events" to tell process "Preview" to key code 119')
                time.sleep(0.5)
                shot_rect("pdf-mermaid", 1600, px, py, pw, ph)
                # Close Preview so its file lock doesn't block the next run's delete.
                run(["osascript", "-e", 'tell application "Preview" to close window 1'])
            else:
                print("Skipping PDF preview frames (Preview window not found).", file=sys.stderr)
        else:
            print("Skipping PDF preview frames (save failed).", file=sys.stderr)
    else:
        print("WARNING: print dialog did not appear — skipping PDF beat.", file=sys.stderr)

    # Return focus to WinPrint for the final hold frame.
    activate()
    shot("hold", 1400)

    imgs = [f[0] for f in frames]
    durs = [f[1] for f in frames]
    imgs[0].save(
        args.output,
        save_all=True,
        append_images=imgs[1:],
        duration=durs,
        loop=0,
        optimize=True,
    )
    print(f"wrote {args.output} ({args.output.stat().st_size:,} bytes, {len(imgs)} frames)")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())

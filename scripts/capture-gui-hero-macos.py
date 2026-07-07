#!/usr/bin/env python3
"""Capture the macOS GUI hero GIF of the Mac Catalyst WinPrint app.

tuirec only records terminal sessions, so the native MAUI GUI is captured with
window screenshots assembled into a GIF. Requires the GUI built
(Release/maccatalyst-arm64), Pillow, and `cliclick`
(`brew install cliclick`), plus an **interactive, unlocked** macOS session with
Screen-Recording and Accessibility permission for the terminal running this.

Choreography (mirrors the Windows hero + the spec in docs/hero-gifs.md):
  load -> toggle Line Numbers off/on -> toggle Landscape off/on ->
  focus + fast zoom-in/pan/reset -> open README.md (Markdown) ->
  print to PDF -> open PDF in Preview -> hold.

macOS mechanics (see docs/hero-gifs.md):
  - Zoom uses the plain TUI keys: `=`/`+` in, `-` out, `0` fit. Only claimed when
    no text field is focused, so press Escape (-> MainPage.FocusPreview) first.
  - Settings toggles: click the sidebar *label* (its TapGestureRecognizer flips
    the bound CheckBox). MAUI Catalyst doesn't expose these via Accessibility, so
    we click window-relative coordinates (calibrated below).
  - Open a file: Cmd+O, then Cmd+Shift+G, paste the path, Return, Return.
  - Print to PDF: Cmd+P -> click "PDF" button in print panel -> "Save as PDF..." ->
    type path -> Return. UIPrintInteractionController bridges to NSPrintPanel on
    Mac Catalyst; the PDF button is found via Accessibility across all process windows.
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


def capture_rect(path: Path, x: int, y: int, w: int, h: int) -> None:
    run(["screencapture", "-x", "-R%d,%d,%d,%d" % (x, y, w, h), str(path)])


def capture(path: Path) -> None:
    activate()
    time.sleep(0.3)
    x, y, w, h = window_rect()
    capture_rect(path, x, y, w, h)


def print_to_pdf(pdf_path: Path) -> bool:
    """Trigger Cmd+P -> PDF -> Save as PDF... -> type path -> Return.

    UIPrintInteractionController on Mac Catalyst bridges to NSPrintPanel. The panel
    may appear as a sheet on window 1 or as a floating panel depending on macOS
    version; we search all process windows for the PDF popup button via Accessibility.

    Returns True if the PDF was written, False if something went wrong.
    """
    if pdf_path.exists():
        pdf_path.unlink()

    activate()
    # Cmd+P: AppDelegate.KeyCommands routes this to MainPage.InvokePrint() ->
    # PerformPrintAsync() -> MacPrintJob.EndAsync() -> UIPrintInteractionController.Present()
    key_combo("p", "command down")
    time.sleep(4.0)  # SkiaPdfRenderer renders the document, then the panel appears

    # Find and click the "PDF" popup button in the print panel. Search all windows
    # because the panel may appear as a sheet (window 1) or a separate window.
    clicked = osa(
        'tell application "System Events"\n'
        '    tell process "winprint"\n'
        '        repeat with w in windows\n'
        '            try\n'
        '                click button "PDF" of w\n'
        '                return "ok"\n'
        '            end try\n'
        '        end repeat\n'
        '        return "not found"\n'
        '    end tell\n'
        'end tell'
    )
    if "ok" not in clicked:
        print(
            "WARNING: could not find PDF button in print dialog "
            "(got: %r) — dismissing print dialog" % clicked,
            file=sys.stderr,
        )
        key_code(53)  # Escape: dismiss the dialog so the script can continue
        return False

    time.sleep(0.7)

    # Click "Save as PDF…" in the PDF popup menu.
    osa(
        'tell application "System Events"\n'
        '    tell process "winprint"\n'
        '        repeat with w in windows\n'
        '            try\n'
        '                click menu item "Save as PDF…" of menu 1 of button "PDF" of w\n'
        '                return "ok"\n'
        '            end try\n'
        '        end repeat\n'
        '    end tell\n'
        'end tell'
    )
    time.sleep(1.5)  # NSSavePanel animates in

    # Select any pre-filled filename and replace with the output path.
    key_combo("a", "command down")
    time.sleep(0.2)
    run(["cliclick", "-w", "20", "t:" + str(pdf_path)])
    time.sleep(0.4)
    key_code(36)   # Return: confirm the save
    time.sleep(2.5)  # wait for the PDF to be written and the dialog to dismiss

    if not pdf_path.exists():
        print(f"WARNING: PDF not found at {pdf_path} — print may have failed", file=sys.stderr)
        return False

    print(f"PDF written: {pdf_path} ({pdf_path.stat().st_size} bytes)")
    return True


def open_pdf_in_preview(pdf_path: Path) -> tuple[int, int, int, int] | None:
    """Open a PDF with `open` (Preview), wait for it to load, return its window rect."""
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
    ap.add_argument("--second-file", type=Path, default=Path("README.md"))
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

    shot("loaded", 1200)

    # Toggle Line Numbers off then on (preview loses/regains the gutter).
    click_label(LINE_NUMBERS_LABEL); shot("linenums-off", 1000)
    click_label(LINE_NUMBERS_LABEL); shot("linenums-on", 900)

    # Toggle Landscape off (reflow to 1-up portrait) then on (back to 2-up).
    click_label(LANDSCAPE_LABEL); shot("portrait", 1100)
    click_label(LANDSCAPE_LABEL); shot("landscape", 900)

    # Fast zoom-in / pan / reset flourish.
    focus_preview()
    key_char("="); key_char("="); key_char("="); shot("zoom", 350)
    key_code(124); key_code(125); shot("pan1", 300)   # Right, Down
    key_code(123); key_code(126); shot("pan2", 300)   # Left, Up
    key_char("0"); shot("reset", 500)                 # fit

    # Open a second, different document (README -> Markdown).
    open_file(second)
    shot("markdown", 1300)
    key_code(121); shot("markdown2", 1100)            # Page Down through it

    # Print to PDF and open — mirrors the Windows hero's final beat.
    # Capture the WinPrint window while the print dialog is visible, then
    # switch to Preview to show the printed output.
    wx, wy, ww, wh = window_rect()
    pdf_ok = print_to_pdf(args.pdf_out)
    if pdf_ok:
        # Capture the WinPrint window region immediately after the dialog dismisses
        # (print job just finished; app is back in its normal state).
        shot("printed", 1100)

        # Open the PDF in Preview and capture its window.
        preview_rect = open_pdf_in_preview(args.pdf_out)
        if preview_rect is not None:
            run(["osascript", "-e", 'tell application "Preview" to activate'])
            time.sleep(0.5)
            px, py, pw, ph = preview_rect
            shot_rect("pdf-page1", 1300, px, py, pw, ph)
            # Page through the PDF to show the full printed output.
            osa('tell application "System Events" to tell process "Preview" to key code 121')
            time.sleep(0.5)
            shot_rect("pdf-page2", 1200, px, py, pw, ph)
            # Close Preview (don't leave it open for the next run).
            run(["osascript", "-e", 'tell application "Preview" to close window 1'])
        else:
            print("Skipping PDF frames (Preview window not found).", file=sys.stderr)
    else:
        print("Skipping PDF frames (print failed).", file=sys.stderr)

    # Return focus to WinPrint for the hold frame.
    activate()
    shot("hold", 1600)

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
    print(f"wrote {args.output} ({args.output.stat().st_size} bytes, {len(imgs)} frames)")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())

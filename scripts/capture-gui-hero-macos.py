#!/usr/bin/env python3
"""Capture a short hero GIF of the Mac Catalyst WinPrint GUI.

tuirec only records terminal sessions, so the native MAUI GUI is captured with
periodic window screenshots assembled into a GIF. Requires the GUI to already
be built (Release/maccatalyst-arm64) and Pillow (`pip install --user Pillow`).
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


def run(cmd: list[str], check: bool = True) -> subprocess.CompletedProcess[str]:
    return subprocess.run(cmd, check=check, text=True, capture_output=True)


def launch_gui(exe: Path, sample: Path) -> None:
    subprocess.Popen([str(exe), str(sample)], stdout=subprocess.DEVNULL, stderr=subprocess.DEVNULL)


def activate_gui() -> None:
    run(["osascript", "-e", 'tell application "winprint" to activate'], check=False)


def keystroke(key_code: int) -> None:
    script = f'tell application "System Events" to key code {key_code}'
    run(["osascript", "-e", script], check=False)


def capture_frame(path: Path) -> None:
    activate_gui()
    time.sleep(0.35)
    run(["screencapture", "-x", str(path)])


def crop_to_window(img: Image.Image, margin: float = 0.08) -> Image.Image:
    """Heuristic crop: trim uniform desktop margins, keep centered app window."""
    w, h = img.size
    left = int(w * margin)
    top = int(h * margin)
    right = int(w * (1 - margin))
    bottom = int(h * (1 - margin))
    return img.crop((left, top, right, bottom))


def main() -> int:
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument(
        "--exe",
        type=Path,
        default=Path("src/WinPrint.Maui/bin/Release/net10.0-maccatalyst/maccatalyst-arm64/WinPrint.app/Contents/MacOS/winprint"),
    )
    parser.add_argument(
        "--sample",
        type=Path,
        default=Path("src/WinPrint.Core/ViewModels/SheetViewModel.cs"),
    )
    parser.add_argument("--output", type=Path, default=Path("docs/hero-gui.gif"))
    parser.add_argument("--workdir", type=Path, default=Path("artifacts/hero/gui-frames"))
    parser.add_argument("--frame-ms", type=int, default=900)
    parser.add_argument("--width", type=int, default=1102, help="Resize output width (README hero size)")
    args = parser.parse_args()

    if not args.exe.is_file():
        print(f"GUI executable not found: {args.exe}", file=sys.stderr)
        print("Build first: dotnet build src/WinPrint.Maui/WinPrint.Maui.csproj -c Release -f net10.0-maccatalyst -r maccatalyst-arm64", file=sys.stderr)
        return 1

    args.workdir.mkdir(parents=True, exist_ok=True)
    args.output.parent.mkdir(parents=True, exist_ok=True)

    launch_gui(args.exe.resolve(), args.sample.resolve())
    print("Waiting for GUI to load…")
    time.sleep(10)
    activate_gui()

    # Page Down = 121, Right arrow = 124 (preview paging in MAUI)
    steps: list[tuple[str, float, int | None]] = [
        ("loaded", 0.0, None),
        ("page1", 1.2, 121),
        ("page2", 1.0, 121),
        ("page3", 1.0, 124),
        ("hold", 1.5, None),
    ]

    frames: list[Image.Image] = []
    for i, (label, delay, key_code) in enumerate(steps):
        if key_code is not None:
            keystroke(key_code)
            time.sleep(delay)
        frame_path = args.workdir / f"{i:02d}-{label}.png"
        capture_frame(frame_path)
        img = Image.open(frame_path).convert("RGB")
        img = crop_to_window(img)
        if args.width and img.width != args.width:
            ratio = args.width / img.width
            img = img.resize((args.width, int(img.height * ratio)), Image.LANCZOS)
        frames.append(img)
        print(f"captured {frame_path.name} ({img.size[0]}x{img.size[1]})")

    frames[0].save(
        args.output,
        save_all=True,
        append_images=frames[1:],
        duration=args.frame_ms,
        loop=0,
        optimize=True,
    )
    print(f"wrote {args.output} ({args.output.stat().st_size} bytes)")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
#!/usr/bin/env python3
"""Assemble the Windows GUI hero frames (from capture-gui-hero-windows.ps1) into a
looping GIF at the README hero width. Requires Pillow (`pip install --user Pillow`)."""
from __future__ import annotations

import argparse
from pathlib import Path

from PIL import Image

# (frame filename, duration_ms). Order/labels match capture-gui-hero-windows.ps1.
# Settings/file frames linger so the change reads; the zoom/pan flourish is deliberately
# FAST (short durations) so it feels snappy, not laboured.
PLAN = [
    ("00-loaded.png", 1400),
    ("01-linenums-off.png", 1300),
    ("02-linenums-on.png", 800),
    ("03-portrait.png", 1400),
    ("04-landscape.png", 900),
    ("05-zoom1.png", 250),
    ("06-zoom2.png", 450),
    ("07-pan.png", 250),
    ("08-pan2.png", 450),
    ("09-reset.png", 700),
    ("10-openfile.png", 1900),
    ("11-hold.png", 1500),
]


def main() -> int:
    ap = argparse.ArgumentParser(description=__doc__)
    ap.add_argument("--frames", type=Path, default=Path("artifacts/hero/gui-frames"))
    ap.add_argument("--output", type=Path, default=Path("docs/hero-gui-win.gif"))
    ap.add_argument("--width", type=int, default=1102, help="README hero width")
    args = ap.parse_args()

    frames, durations = [], []
    for name, dur in PLAN:
        img = Image.open(args.frames / name).convert("RGB")
        if img.width != args.width:
            ratio = args.width / img.width
            img = img.resize((args.width, round(img.height * ratio)), Image.LANCZOS)
        frames.append(img)
        durations.append(dur)

    args.output.parent.mkdir(parents=True, exist_ok=True)
    frames[0].save(
        args.output,
        save_all=True,
        append_images=frames[1:],
        duration=durations,
        loop=0,
        optimize=True,
    )
    print(f"wrote {args.output} ({args.output.stat().st_size} bytes, "
          f"{frames[0].size[0]}x{frames[0].size[1]}, {len(frames)} frames)")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())

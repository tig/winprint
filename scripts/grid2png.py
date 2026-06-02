#!/usr/bin/env python3
"""Render a text-grid golden snapshot to a PNG.

The WinPrint.TUI golden tests record the rendered screen as a plain-text character
grid (the box-drawing UI you'd see in a terminal). This script draws that grid to a
PNG with a monospace font so the UI can be reviewed as an image (e.g. on a phone)
without a terminal.

Usage:
    python3 scripts/grid2png.py <input.txt> <output.png> [--font-size N] [--bg R,G,B] [--fg R,G,B]
"""

import argparse
import sys

from PIL import Image, ImageDraw, ImageFont

FONT_CANDIDATES = [
    "/usr/share/fonts/truetype/dejavu/DejaVuSansMono.ttf",
    "/usr/share/fonts/truetype/liberation/LiberationMono-Regular.ttf",
]


def load_font(size):
    for path in FONT_CANDIDATES:
        try:
            return ImageFont.truetype(path, size)
        except OSError:
            continue
    return ImageFont.load_default()


def parse_rgb(text, default):
    if not text:
        return default
    parts = text.split(",")
    return tuple(int(p) for p in parts) if len(parts) == 3 else default


def main():
    ap = argparse.ArgumentParser()
    ap.add_argument("input")
    ap.add_argument("output")
    ap.add_argument("--font-size", type=int, default=32)
    ap.add_argument("--bg", default="18,18,26")
    ap.add_argument("--fg", default="225,228,235")
    args = ap.parse_args()

    bg = parse_rgb(args.bg, (18, 18, 26))
    fg = parse_rgb(args.fg, (225, 228, 235))

    rows = open(args.input, "r", encoding="utf-8").read().split("\n")
    while rows and rows[-1] == "":
        rows.pop()
    if not rows:
        rows = [""]

    font = load_font(args.font_size)
    cell_w = font.getlength("M")
    cell_h = args.font_size + 10
    pad = 12

    width = int(max(len(r) for r in rows) * cell_w) + pad * 2
    height = len(rows) * cell_h + pad * 2

    img = Image.new("RGB", (width, height), bg)
    draw = ImageDraw.Draw(img)
    for r, line in enumerate(rows):
        draw.text((pad, pad + r * cell_h), line, font=font, fill=fg)

    img.save(args.output)
    print(f"wrote {args.output} ({img.width}x{img.height})")


if __name__ == "__main__":
    sys.exit(main())

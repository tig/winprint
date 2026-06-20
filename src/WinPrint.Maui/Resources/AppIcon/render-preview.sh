#!/usr/bin/env bash
# Render the WinPrint app icon (appicon.svg) to PNGs for quick visual review.
#
# macOS only (uses qlmanage + sips, both built in). On Linux/CI, render with
# `rsvg-convert -w 1024 appicon.svg -o _preview/appicon-1024.png` instead.
#
# Output: ./_preview/appicon-{1024,256,128,64,32,16}.png  (disposable; gitignored)
# Note: the render is a full-bleed SQUARE — macOS rounds the corners at display
# time, so square output is expected.
set -euo pipefail

DIR="$(cd "$(dirname "$0")" && pwd)"
SVG="$DIR/appicon.svg"
OUT="$DIR/_preview"
mkdir -p "$OUT"

if ! command -v qlmanage >/dev/null 2>&1; then
  echo "qlmanage not found (macOS only). On Linux use rsvg-convert/inkscape." >&2
  exit 1
fi

rm -f "$OUT/appicon.svg.png"
qlmanage -t -s 1024 -o "$OUT" "$SVG" >/dev/null 2>&1
mv -f "$OUT/appicon.svg.png" "$OUT/appicon-1024.png"

for s in 256 128 64 32 16; do
  sips -z "$s" "$s" "$OUT/appicon-1024.png" --out "$OUT/appicon-$s.png" >/dev/null
done

echo "Wrote previews to $OUT"
ls -1 "$OUT"

#!/usr/bin/env bash
#
# Generate store-sized screenshots for WinPrint's App Store / Microsoft Store listings.
#
# These are git-ignored (see store/.gitignore) — regenerate them before a submission.
# Output (exact store canvas sizes):
#   store/macos/screenshots/*.png     2880 x 1800   (Mac App Store, 16:10)
#   store/windows/screenshots/*.png   2560 x 1440   (Microsoft Store, >=1366x768)
#
# WHAT THIS CAPTURES: the headless `wp` TUI (full-fidelity via tuirec + agg), composited
# onto the exact store canvas. The TUI shows off WinPrint's core value — syntax-highlighted
# code with line numbers — and is reproducible in CI.
#
# TODO (needs a running GUI, so not headless): add GUI captures of the MacCatalyst app
# (`dotnet build -t:Run -f net10.0-maccatalyst`) and the Windows MAUI app (run-maui-app
# skill) for listings that should show the desktop GUI. See store/README.md.
#
# PREREQUISITES:
#   - dotnet (.NET 10 SDK), DOTNET_ROOT exported
#   - tuirec >= 0.8 + agg  (set TUIREC=/path/to/tuirec; agg is bundled with the tuirec release)
#     https://github.com/gui-cs/tuirec/releases
#   - python3 with Pillow  (pip install --user Pillow)
set -euo pipefail

repo_root="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
tuirec="${TUIREC:-tuirec}"
sample="${1:-$repo_root/src/WinPrint.Core/ViewModels/SheetViewModel.cs}"

command -v "$tuirec" >/dev/null 2>&1 || { echo "tuirec not found; set TUIREC=/path/to/tuirec" >&2; exit 1; }
python3 -c "import PIL" 2>/dev/null || { echo "Pillow not found; pip install --user Pillow" >&2; exit 1; }
: "${DOTNET_ROOT:=$(dirname "$(readlink -f "$(command -v dotnet)")")}"; export DOTNET_ROOT

dotnet build "$repo_root/src/WinPrint.TUI/WinPrint.TUI.csproj" -c Release -tl:off -nologo >/dev/null
wp="$repo_root/src/WinPrint.TUI/bin/Release/net10.0/wp"

work="$(mktemp -d)"; trap 'rm -rf "$work"' EXIT

# Capture one zoomed TUI frame of $sample to a raw PNG.
"$tuirec" snapshot --binary "$wp" --args "tui,$sample" \
  --cols 156 --rows 44 --font-size 20 --startup-delay 7000 \
  --keystrokes "Ctrl+PageUp,wait:500,Ctrl+PageUp,wait:1000" \
  --drain 1500 --frame last --output "$work/raw.png" >/dev/null

# Composite the raw capture, centered with padding, onto each exact store canvas.
python3 - "$work/raw.png" "$repo_root/store" <<'PY'
import sys
from PIL import Image
raw_path, store = sys.argv[1], sys.argv[2]
raw = Image.open(raw_path).convert("RGBA")

def lerp(a,b,t): return tuple(int(a[i]+(b[i]-a[i])*t) for i in range(3))
TOP,BOT=(0x4A,0x6C,0xF7),(0x23,0x36,0xA8)  # brand gradient (matches the app icon)

def compose(w,h,out):
    bg=Image.new("RGB",(w,h)); px=bg.load()
    for y in range(h):
        c=lerp(TOP,BOT,y/h)
        for x in range(w): px[x,y]=c
    canvas=bg.convert("RGBA")
    pad=int(min(w,h)*0.06)
    bw,bh=w-2*pad,h-2*pad
    s=min(bw/raw.width, bh/raw.height)
    img=raw.resize((int(raw.width*s),int(raw.height*s)),Image.LANCZOS)
    canvas.alpha_composite(img,((w-img.width)//2,(h-img.height)//2))
    canvas.convert("RGB").save(out,"PNG")
    print("wrote",out)

compose(2880,1800, f"{store}/macos/screenshots/01-preview.png")
compose(2560,1440, f"{store}/windows/screenshots/01-preview.png")
PY
echo "Done. (Screenshots are git-ignored; review them, then submit.)"

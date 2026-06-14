#!/usr/bin/env bash
#
# Generate store-sized screenshots for WinPrint's App Store / Microsoft Store listings.
#
# These are git-ignored (see store/.gitignore) — regenerate them before a submission.
# Output goes into the LOCALE subfolders each tool expects:
#   store/macos/screenshots/en-US/*.png     2880 x 1800  (fastlane deliver scans <locale>/)
#   store/windows/screenshots/en-us/*.png   2560 x 1440  (StoreBroker resolves PDP media per <lang>)
# Two shots are produced, matching the PDP's ScreenshotCaptions:
#   01-preview.png  zoomed — syntax highlighting + line numbers
#   02-twoup.png    fit    — the multiple-pages-up (n-up) layout
#
# WHAT THIS CAPTURES: the headless `wp` TUI (full-fidelity via tuirec + agg), composited
# onto the exact store canvas. Reproducible in CI.
#
# TODO (needs a running GUI, not headless): add GUI captures of the MacCatalyst app
# (`dotnet build -t:Run -f net10.0-maccatalyst`) and the Windows MAUI app (run-maui-app
# skill) into the same locale folders. See store/README.md.
#
# PREREQUISITES:
#   - dotnet (.NET 10 SDK), DOTNET_ROOT exported
#   - tuirec >= 0.8 + agg  (set TUIREC=/path/to/tuirec; agg ships with the tuirec release)
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

cap() {  # cap <keystrokes> <out.png>
  "$tuirec" snapshot --binary "$wp" --args "tui,$sample" \
    --cols 156 --rows 44 --font-size 20 --startup-delay 7000 \
    --keystrokes "$1" --drain 1500 --frame last --output "$2" >/dev/null
}
cap "Ctrl+PageUp,wait:500,Ctrl+PageUp,wait:1000" "$work/zoom.png"   # detail
cap "wait:1200"                                   "$work/fit.png"   # whole n-up sheet

# Composite each raw capture onto the exact store canvases, into locale subfolders.
python3 - "$repo_root/store" "$work/zoom.png" "$work/fit.png" <<'PY'
import os, sys
from PIL import Image
store, zoom_p, fit_p = sys.argv[1], sys.argv[2], sys.argv[3]
zoom, fit = Image.open(zoom_p).convert("RGBA"), Image.open(fit_p).convert("RGBA")

def lerp(a,b,t): return tuple(int(a[i]+(b[i]-a[i])*t) for i in range(3))
TOP,BOT=(0x4A,0x6C,0xF7),(0x23,0x36,0xA8)  # brand gradient (matches the app icon)

def compose(raw,w,h,out):
    os.makedirs(os.path.dirname(out), exist_ok=True)
    bg=Image.new("RGB",(w,h)); px=bg.load()
    for y in range(h):
        c=lerp(TOP,BOT,y/h)
        for x in range(w): px[x,y]=c
    canvas=bg.convert("RGBA")
    pad=int(min(w,h)*0.06); bw,bh=w-2*pad,h-2*pad
    s=min(bw/raw.width, bh/raw.height)
    img=raw.resize((int(raw.width*s),int(raw.height*s)),Image.LANCZOS)
    canvas.alpha_composite(img,((w-img.width)//2,(h-img.height)//2))
    canvas.convert("RGB").save(out,"PNG"); print("wrote",out)

# macOS (deliver: <locale>/), 2880x1800
compose(zoom,2880,1800, f"{store}/macos/screenshots/en-US/01-preview.png")
compose(fit, 2880,1800, f"{store}/macos/screenshots/en-US/02-twoup.png")
# Windows (StoreBroker: <lang>/), 2560x1440
compose(zoom,2560,1440, f"{store}/windows/screenshots/en-us/01-preview.png")
compose(fit, 2560,1440, f"{store}/windows/screenshots/en-us/02-twoup.png")
PY
echo "Done. (Screenshots are git-ignored; review them, then submit.)"

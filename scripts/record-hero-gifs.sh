#!/usr/bin/env bash
#
# Record hero GIFs for winprint's three front ends:
#   1. TUI   — tuirec drives `wp <file>` (sixel print preview in the terminal)
#   2. print — tuirec drives `wp print … --what-if` (headless CLI)
#   3. gui   — macOS: screen-capture GIF via capture-gui-hero-macos.py
#              (tuirec is terminal-only; native GUI cannot be recorded with it)
#
# Prerequisites:
#   - .NET 10 SDK
#   - tuirec >= 0.8 on PATH (https://github.com/tui-cs/tuirec/releases)
#   - python3 + Pillow (for GUI GIF on macOS)
#   - macOS only for the GUI capture step
#
# Usage:
#   scripts/record-hero-gifs.sh [output-dir]
#
# Writes:
#   <output-dir>/hero-tui.gif
#   <output-dir>/hero-print.gif
#   <output-dir>/hero-gui.gif   (macOS only)

set -euo pipefail

repo_root="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
out_dir="${1:-$repo_root/docs}"
tuirec="${TUIREC:-tuirec}"
sample="${SAMPLE:-$repo_root/src/WinPrint.Core/ViewModels/SheetViewModel.cs}"

if [[ -z "${DOTNET_ROOT:-}" ]]; then
  DOTNET_ROOT="$(dirname "$(readlink -f "$(command -v dotnet)")")"
  export DOTNET_ROOT
fi

command -v "$tuirec" >/dev/null 2>&1 || {
  echo "tuirec not found; set TUIREC=/path/to/tuirec" >&2
  exit 1
}

mkdir -p "$out_dir"

echo "Building wp (Release)…"
dotnet build "$repo_root/src/WinPrint.TUI/WinPrint.TUI.csproj" \
  -c Release -f net10.0 -tl:off -nologo >/dev/null
wp="$repo_root/src/WinPrint.TUI/bin/Release/net10.0/wp"

dotnet_env=(--env "DOTNET_ROOT=$DOTNET_ROOT")

echo "Recording TUI hero → $out_dir/hero-tui.gif"
"$tuirec" record \
  --binary "$wp" \
  --args "$sample" \
  "${dotnet_env[@]}" \
  --cols 110 --rows 34 \
  --font-size 18 --line-height 1.25 \
  --startup-delay 12000 \
  --trim=false \
  --mouse-pointer clicks \
  --keystrokes 'wait:2500,PageDown,wait:700,PageDown,wait:700,+,wait:450,+,wait:450,+,wait:900,drag:84:16:54:16,wait:900,move:70:16,wait:400,scroll:down:70:16,wait:450,scroll:up:70:16,wait:700,click:11:3,wait:2500,Tab,Tab,Tab,wait:350,CursorDown,wait:450,Enter,wait:3500,click:11:3,wait:1800,Esc,wait:1200' \
  --drain 3500 \
  --speed 1.1 \
  --output "$out_dir/hero-tui.gif" \
  --max-duration 120

echo "Recording print CLI hero → $out_dir/hero-print.gif"
"$tuirec" record \
  --binary "$wp" \
  --args "print,$sample,--what-if,--sheet,Default 2-Up" \
  "${dotnet_env[@]}" \
  --show-command '`wp print SheetViewModel.cs --what-if --sheet "Default 2-Up"`' \
  --cols 96 --rows 22 \
  --font-size 17 \
  --startup-delay 4000 \
  --keystrokes 'wait:2000' \
  --drain 2500 \
  --output "$out_dir/hero-print.gif"

if [[ "$(uname -s)" == "Darwin" ]]; then
  echo "Building Mac Catalyst GUI…"
  dotnet build "$repo_root/src/WinPrint.Maui/WinPrint.Maui.csproj" \
    -c Release -f net10.0-maccatalyst -r maccatalyst-arm64 \
    /p:CreatePackage=false /p:EnableCodeSigning=false -tl:off -nologo >/dev/null

  echo "Capturing GUI hero → $out_dir/hero-gui.gif"
  python3 "$repo_root/scripts/capture-gui-hero-macos.py" \
    --output "$out_dir/hero-gui.gif"
else
  echo "Skipping GUI hero (macOS screen capture only). Capture on macOS or Windows separately." >&2
fi

echo "Done."
ls -la "$out_dir"/hero-*.gif
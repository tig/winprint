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

# Force the Kitty graphics protocol for the preview. tuirec (>= 0.9.0) advertises Kitty by default and
# its pinned agg renders it robustly; the sixel path is flaky under recording (desyncs into raw-escape
# garbage — tuirec #84). Terminal.Gui's ImageView prefers Kitty when supported, and wp exposes the
# WP_FORCE_KITTY override, so we pin it here. See Terminal.Gui Scripts/tuirec/README.md ("Raster
# graphics: Kitty (default) and sixel"). Verify with: grep -c 'u001b_G' <cast>  (Kitty) vs 'u001bPq' (sixel).
#
# Needs agg >= v1.11.2-sixel (tuirec's pinned DefaultAggVersion): earlier agg didn't render Kitty
# `a=p` cropped placements and over-occluded held below-text images, leaving the preview blank/flickery.
# `--select "2.."` trims the ~2s startup lead-in (settings panel building while the preview is still
# loading) so the GIF opens on the rendered page; keystrokes are keyboard-only (page/zoom) to keep
# focus on the preview — mouse drag/scroll and dialogs suspend or desync it.
echo "Recording TUI hero → $out_dir/hero-tui.gif"
"$tuirec" record \
  --binary "$wp" \
  --args "$sample" \
  "${dotnet_env[@]}" \
  --env "WP_FORCE_KITTY=1" \
  --cols 110 --rows 34 \
  --font-size 18 --line-height 1.25 \
  --startup-delay 4000 \
  --select "2.." \
  --trim=false \
  --keystrokes 'wait:1500,PageDown,wait:1100,+,wait:800,+,wait:900,+,wait:1100,-,wait:800,-,wait:900,-,wait:1100,PageDown,wait:1500' \
  --drain 1500 \
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
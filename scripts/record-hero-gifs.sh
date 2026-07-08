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
#   - agg >= v1.11.2-sixel from https://github.com/tig/agg/releases on PATH (pass --agg-path or
#     put it ahead of tuirec's bundled copy). tuirec 0.9.1 still bundles v1.11.0-sixel, which
#     renders the raster preview BLANK (the whole point of the TUI hero); v1.11.2-sixel fixes it.
#   - python3 + Pillow (for GUI GIF on macOS)
#   - macOS only for the GUI capture step
#
# Usage:
#   scripts/record-hero-gifs.sh [output-dir]
#
# Writes:
#   <output-dir>/hero-tui.gif
#   <output-dir>/hero-print.gif
#   <output-dir>/hero-gui-mac.gif   (macOS only)

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
out_dir="$(cd "$out_dir" && pwd)"   # absolutize before the cd below so a relative out-dir arg still resolves

# Theme the hero under Terminal.Gui's "Anders" theme (docs/hero-gifs.md, "Theming the hero"). wp
# enables ConfigLocations.All (see Program.cs), which includes the TUI_CONFIG env var — and TG reads
# that var's value as the JSON config *inline*, NOT as a path to a file (SourcesManager.cs routes it
# through the JSON-content Load overload, same as RuntimeConfig). So we pass the theme JSON directly.
# This is deliberately NOT a ./.tui/ file: a config dir in the cwd shows up in wp's Open-file dialog
# and shifts the row the choreography double-clicks. The env var themes the app while polluting
# nothing on disk.
tui_theme_env=(--env 'TUI_CONFIG={"Theme":"Anders"}')
cd "$repo_root"   # so wp's Open-file dialog (spawned by tuirec) starts here and lists ./testfiles

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
# Needs agg >= v1.11.2-sixel (from https://github.com/tig/agg/releases): earlier agg (incl. the
# v1.11.0-sixel that tuirec 0.9.1 bundles) didn't render Kitty `a=p` cropped placements and
# over-occluded held below-text images, leaving the raster preview BLANK — the whole point of the hero.
# Put v1.11.2-sixel ahead of tuirec's bundled copy on PATH, or pass --agg-path.
#
# Choreography (mirrors the spec in docs/hero-gifs.md): open on SheetViewModel.cs → page + zoom in/out
# (no pan) → open ./testfiles/demo.md via the File dialog (page 1 opens ON the rendered Mermaid
# diagram — demo.md leads with it since #246, and renderMermaidDiagrams defaults to true) → page 3× +
# Home (back to the diagram) → switch the Sheet Definition to "Proportional 1-Up" → set Content Font
# to 12pt (each re-render re-lands on the diagram) → page 5× → End (last-page tour beat) → Home
# (diagram once more) → quit (Esc → Don't Save).
# Alt+F/Alt+N are button hotkeys; the Sheet-Definition dropdown is opened with `click:20:6` and
# "Proportional 1-Up" (4th/last entry) picked at `click:15:10`; the Content-Font-dialog clicks are
# pixel-calibrated to demo.md's chooser layout (Helvetica Neue current → Size dropdown at row 19, "12"
# at row 26 when expanded, OK at row 27). With renderMermaidDiagrams on, every re-render refetches
# the diagram from mermaid.ink (the image cache is per-render), so the post-font-OK wait must absorb
# reflow + a network round-trip (wait:6500) or the End beat lands mid-reflow. (MarkdownCte publishes
# lines/cache only on render completion, so painting during that window shows the previous render —
# never a torn one.) Switching the sheet RELOADS the document, so give it a long
# settle (`wait:4500`) before opening the font dialog or the clicks race the reload and desync.
#
# Opening demo.md uses wp's Open-file dialog SEARCH (Find) box, which does a RECURSIVE search — this
# is deliberately row-independent so it doesn't depend on the repo's directory listing: `click:30:25`
# focuses the Find box (Tab-Tab-Tab also reaches it), then typing `demo.md` and waiting a couple of
# seconds narrows the tree to the single matching file (selected at the first list row, row 9), which
# `doubleclick:16:9` then opens. `--startup-delay 6000` lets the first-page raster finish before
# capture begins; `--select "2.."` trims the startup lead-in.
echo "Recording TUI hero → $out_dir/hero-tui.gif"
"$tuirec" record \
  --binary "$wp" \
  --args "$sample,--sheet,Default 2-Up" \
  "${dotnet_env[@]}" \
  "${tui_theme_env[@]}" \
  --env "WP_FORCE_KITTY=1" \
  --cols 110 --rows 34 \
  --font-size 18 --line-height 1.25 \
  --startup-delay 8500 \
  --select "2.." \
  --trim=false \
  --keystrokes 'wait:1500,PageDown,wait:1200,+,wait:700,+,wait:700,+,wait:1300,0,wait:1300,Alt+F,wait:1600,click:30:25,wait:600,`demo.md`,wait:5500,doubleclick:16:9,wait:5200,PageDown,wait:1100,PageDown,wait:1100,PageDown,wait:1300,Home,wait:1600,click:20:6,wait:1400,click:15:10,wait:4500,Alt+N,wait:2500,click:41:19,wait:1500,click:42:26,wait:1500,click:86:27,wait:6500,PageDown,wait:1100,PageDown,wait:1100,PageDown,wait:1100,PageDown,wait:1100,PageDown,wait:1300,End,wait:2400,Home,wait:1700,Esc,wait:1700,Alt+D,wait:1300' \
  --drain 1500 \
  --speed 1.1 \
  --output "$out_dir/hero-tui.gif" \
  --max-duration 165

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

  echo "Capturing GUI hero → $out_dir/hero-gui-mac.gif"
  python3 "$repo_root/scripts/capture-gui-hero-macos.py" \
    --output "$out_dir/hero-gui-mac.gif"
else
  echo "Skipping GUI hero (macOS screen capture only). Capture on macOS or Windows separately." >&2
fi

echo "Done."
ls -la "$out_dir"/hero-*.gif
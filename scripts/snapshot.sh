#!/usr/bin/env bash
#
# Capture a full-fidelity PNG of a wp view using tuirec 0.5.0 + agg.
#
# This is the "rich" layer of the agent/human TUI design loop (see
# docs/proposals/tui-agent-design-loop.md): tuirec drives the real `wp` binary through
# a PTY, records an asciinema .cast, and agg renders one frame to PNG with true terminal
# colors, font, and glyphs. (The fast "plain grid" layer is `wp --view <view> --cat`.)
#
# Usage:
#   scripts/snapshot.sh <view> [cols] [rows] [out.png]
#
# Example:
#   scripts/snapshot.sh settings 62 28 /tmp/settings.png
#
# Prerequisites / gotchas learned the hard way:
#   - tuirec >= 0.5.0 on PATH (or set TUIREC).            https://github.com/gui-cs/tuirec
#   - agg on PATH or set AGG (tuirec auto-downloads it if it can reach the network).
#   - DOTNET_ROOT must be exported so the wp apphost finds the runtime *inside tuirec's
#     child process* — it is not otherwise inherited.
#   - tuirec --args is COMMA-separated (it splits on commas, not spaces).

set -euo pipefail

view="${1:?usage: snapshot.sh <view> [cols] [rows] [out.png]}"
cols="${2:-62}"
rows="${3:-28}"
out="${4:-/tmp/wp-${view}.png}"

repo_root="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
tuirec="${TUIREC:-tuirec}"

# Resolve DOTNET_ROOT from the dotnet on PATH if not already set.
if [[ -z "${DOTNET_ROOT:-}" ]]; then
    DOTNET_ROOT="$(dirname "$(readlink -f "$(command -v dotnet)")")"
    export DOTNET_ROOT
fi

# Build wp (Release) and locate the apphost.
dotnet build "${repo_root}/src/WinPrint.TUI/WinPrint.TUI.csproj" -c Release -tl:off -nologo >/dev/null
wp="${repo_root}/src/WinPrint.TUI/bin/Release/net10.0/wp"

# Render one view at a fixed size; the panel is Dim.Auto so give it a slightly larger
# terminal than the content. --args is comma-separated. `wp --view <name> --width W --height H`
# opens that catalogued view interactively at the pinned size for tuirec to record.
agg_args=()
[[ -n "${AGG:-}" ]] && agg_args=(--agg-path "${AGG}")

"${tuirec}" snapshot \
    --binary "${wp}" \
    --args "--view,${view},--width,$((cols - 2)),--height,$((rows - 2))" \
    --output "${out}" \
    --cols "$((cols + 4))" --rows "$((rows + 2))" \
    --startup-delay 3500 --drain 1500 --frame last \
    "${agg_args[@]}"

echo "wrote ${out}"

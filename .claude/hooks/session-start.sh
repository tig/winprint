#!/bin/bash
# Claude Code on the web SessionStart hook for winprint.
#
# Installs the toolchain a fresh remote (Linux) container needs to build
# WinPrint.Core and run WinPrint.Core.UnitTests:
#   - the .NET 10 SDK (not preinstalled in web sessions)
#   - libgdiplus (System.Drawing.Common P/Invokes it; required by parts of the
#     test suite — note the cross-platform CTE rendering tests do NOT need it)
#   - the repo's local dotnet tools (JetBrains 'jb' for code-style checks)
#   - a warm NuGet restore (cached into the container image after this runs)
#
# Idempotent and non-interactive. Runs only in remote sessions.
set -euo pipefail

# Only run in Claude Code on the web (remote) environments. Local machines
# already have their own toolchain.
if [ "${CLAUDE_CODE_REMOTE:-}" != "true" ]; then
  exit 0
fi

DOTNET_DIR="$HOME/.dotnet"

# 1. .NET 10 SDK ------------------------------------------------------------
if ! "$DOTNET_DIR/dotnet" --version >/dev/null 2>&1 && ! command -v dotnet >/dev/null 2>&1; then
  echo "[session-start] Installing .NET 10 SDK..."
  curl -sSL https://dot.net/v1/dotnet-install.sh -o /tmp/dotnet-install.sh
  bash /tmp/dotnet-install.sh --channel 10.0 --install-dir "$DOTNET_DIR"
fi

# Persist dotnet on PATH for the rest of the session.
if [ -n "${CLAUDE_ENV_FILE:-}" ]; then
  echo "export PATH=\"$DOTNET_DIR:\$PATH\"" >> "$CLAUDE_ENV_FILE"
  echo "export DOTNET_CLI_TELEMETRY_OPTOUT=1" >> "$CLAUDE_ENV_FILE"
fi
export PATH="$DOTNET_DIR:$PATH"
export DOTNET_CLI_TELEMETRY_OPTOUT=1

# 2. libgdiplus -------------------------------------------------------------
# Best-effort: don't fail the whole session if the package can't be installed.
if ! ldconfig -p 2>/dev/null | grep -q libgdiplus; then
  echo "[session-start] Installing libgdiplus..."
  if command -v apt-get >/dev/null 2>&1; then
    (apt-get update -y && apt-get install -y libgdiplus) \
      || (command -v sudo >/dev/null 2>&1 && sudo apt-get update -y && sudo apt-get install -y libgdiplus) \
      || echo "[session-start] WARNING: could not install libgdiplus; some System.Drawing tests may fail."
  fi
fi

# 3. Local dotnet tools (jb / ReSharper command line) -----------------------
dotnet tool restore || echo "[session-start] WARNING: 'dotnet tool restore' failed; 'jb' style checks may be unavailable."

# 4. Warm the NuGet restore cache -------------------------------------------
dotnet restore src/WinPrint.Core/WinPrint.Core.csproj
dotnet restore tests/WinPrint.Core.UnitTests/WinPrint.Core.UnitTests.csproj

echo "[session-start] winprint web session ready: .NET $(dotnet --version)"

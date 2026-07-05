#!/usr/bin/env bash
#
# verify-style.sh — the canonical CI "Verify code style" gate.
#
# This is the SINGLE SOURCE OF TRUTH for the style check. CI (.github/workflows/ci.yml)
# runs `scripts/verify-style.sh --ci`, and you should run this locally BEFORE EVERY PUSH.
# The gate passes iff the working tree stays clean after formatting — exactly what CI
# enforces with `git diff --exit-code`. Running the bare `dotnet jb cleanupcode` /
# `dotnet format` commands WITHOUT the flags baked in here produces a DIFFERENT result
# than CI, so it "passes" locally and still fails the gate. Always go through this script.
#
# Usage:
#   scripts/verify-style.sh              # local: restore + build the target, then check style
#   scripts/verify-style.sh --ci         # CI: skip restore/build (--no-build for jb, --no-restore for format)
#   scripts/verify-style.sh <path>       # target one .csproj instead of the whole solution —
#                                        # use this on Linux/Mac, where WinPrint.Maui can't build
#                                        # (e.g. scripts/verify-style.sh src/WinPrint.Core/WinPrint.Core.csproj)
#   scripts/verify-style.sh --help       # print this usage and exit
#
# WinPrintCleanup is a custom profile (in WinPrint.slnx.DotSettings) = Full Cleanup minus
# "Optimize usings"/"Shorten references"/"Reorder members". Those fight this solution's
# multi-TFM `#if WINDOWS` conditional usings (hoisting them out of #if and breaking the other
# TFM). MAUI XAML code-behind files are excluded because generated partial declarations must
# stay partial, and jb's redundancy cleanup removes them; dotnet format still applies spacing.
set -euo pipefail
cd "$(dirname "$0")/.."

# Print the usage block (the leading comment lines) and exit.
usage() { sed -n '3,17p' "$0" | sed 's/^# \{0,1\}//'; }

TARGET="WinPrint.slnx"
CI=0
for arg in "$@"; do
  case "$arg" in
    --ci)          CI=1 ;;
    -h|--help)     usage; exit 0 ;;
    --*)           echo "verify-style.sh: unknown option '$arg'" >&2; usage >&2; exit 2 ;;
    *)             TARGET="$arg" ;;
  esac
done

NOBUILD=""
NORESTORE=""
if [[ $CI -eq 1 ]]; then
  # CI already restored + built; skip the redundant work.
  NOBUILD="--no-build"
  NORESTORE="--no-restore"
fi

dotnet tool restore

# These flags MUST stay identical to what CI runs (they're the same because CI calls this
# script). Change them here and nowhere else. --settings is passed explicitly so the
# WinPrintCleanup profile (defined in the solution's .DotSettings) still resolves when TARGET
# is a single .csproj rather than the whole solution.
dotnet jb cleanupcode "$TARGET" $NOBUILD --settings=WinPrint.slnx.DotSettings --profile="WinPrintCleanup" --exclude="**/*.xaml.cs" --verbosity=WARN
dotnet format "$TARGET" $NORESTORE

git diff --exit-code

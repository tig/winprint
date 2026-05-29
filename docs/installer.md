# WinPrint installer & distribution design

Design of record for [#63](https://github.com/tig/winprint/issues/63) — a modern, signed,
auto-updatable installer story. This document captures the agreed architecture and the
per-channel plan; it is intentionally ahead of the implementation (most channels are
blocked on prerequisites called out below).

## Decisions

| Topic | Decision |
|---|---|
| **Desktop install + auto-update engine** | **Velopack** — one tool for Windows/macOS/Linux that produces installers *and* delta auto-update. Replaces `UpdateService`. Non-Store; no MSIX packaging work required. |
| **Windows discoverability** | A **winget** manifest pointing at the Velopack Windows installer (`winget install Kindel.WinPrint`). |
| **CLI distribution** | `dotnet tool install -g winprint` — **blocked**, see below. |
| **Signing** | **Deferred.** Produce unsigned artifacts now; add signing later, gated behind CI secrets. |
| **Versioning** | Interim: use the existing assembly version (2.5.0). Formal scheme tracked in #62; the pipeline should read whatever #62 lands on. |

## Per-channel plan & status

### CLI — `dotnet tool` (BLOCKED, prerequisite identified)
`dotnet tool` packages **must** target a platform-neutral TFM (`net10.0`); the SDK rejects
`net*-windows` with `NETSDK1146`. `WinPrint.cli` currently targets `net10.0-windows` because
its `PrintCommand` is built on the Windows printing stack (`System.Drawing.Printing.PrintDocument`
/ `PrinterSettings` and Core's `Print` class, which is `Compile Remove`d for non-Windows).

**Prerequisite:** a cross-platform output path (e.g. render-to-PDF via a managed/Skia backend)
so the CLI can target `net10.0` and actually do something off-Windows. This rides on the
cross-platform rendering work (#65) and the Native AOT effort (#66). Until then, packaging the
CLI as a tool would either fail to build or ship a tool that installs cross-platform but only
functions on Windows — neither is acceptable, so the tool is deferred.

### Windows desktop — Velopack (+ winget)
Velopack installer built from the MAUI Windows head once the MAUI app is functional
(MAUI port: #54 / PR #59). Avoids wiring up MSIX (`WindowsPackageType` is currently `None`).
winget manifest references the released Velopack installer.

### macOS desktop — Velopack
Built from `dotnet publish -f net10.0-maccatalyst`. Requires an Apple Developer ID and
notarization for distribution outside the App Store — deferred with signing.

### Linux — Velopack and/or `.deb`/`.rpm`
For the CLI, once it targets `net10.0` (same prerequisite as the dotnet tool). Velopack can
produce Linux artifacts; native `.deb`/`.rpm` is an option if needed.

### Auto-update — Velopack replaces `UpdateService`
Velopack provides delta self-update on all three desktop platforms. This supersedes the
current `Octokit`-based `UpdateService`; note the update-check is slated for a ground-up
redesign and `Octokit` removal as part of #66.

## Release CI (planned, not yet implemented)
A tag-triggered (`v*`) `release.yml` with a win/macOS/Linux matrix:
1. Build each head (CLI nupkg, Velopack packs per OS).
2. **Sign** (gated on secrets — deferred; unsigned until certs are provided).
3. Publish a **GitHub Release** with artifacts + the Velopack update feed.

Signing mechanism is an open decision for that PR: **Azure Trusted Signing** (cert-less) vs
**bring-your-own certs** (Authenticode `.pfx` + Apple Developer ID via secrets).

## Dependency / sequencing summary
- **#62** — version scheme (the installer reads it).
- **#65** — cross-platform rendering (merged/in progress) — unblocks a cross-platform engine.
- **#66** — Native AOT + cross-platform output + `UpdateService`/Octokit redesign — unblocks the
  `dotnet tool` and Velopack auto-update.
- **#54 / PR #59** — MAUI port maturity — unblocks the desktop installers.

## What this PR delivers
This document only — the agreed design. Each channel above becomes its own follow-up PR once
its prerequisite is met, starting with the CLI `dotnet tool` when the CLI can target `net10.0`.

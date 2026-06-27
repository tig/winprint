# Scoop packaging

Makes WinPrint installable on Windows via [Scoop](https://scoop.sh) **without any Microsoft
approval** — unlike winget (whose first submission is moderated by the winget-pkgs team), a Scoop
bucket is just a Git repo you own. `winprint.json` here is the manifest **template** and the source
of truth; the release pipeline renders it with each stable version + SHA256 and pushes it to the
**`kindel/scoop-winprint`** bucket (the Scoop analog of the Homebrew tap).

## Install (for users)

```powershell
scoop bucket add winprint https://github.com/kindel/scoop-winprint
scoop install winprint
```

One install delivers **both** heads:

- `wp` is shimmed onto your `PATH` (the terminal UI).
- **WinPrint** (the GUI) gets a Start-Menu shortcut.

Upgrade / remove with `scoop update winprint` / `scoop uninstall winprint`.

## How it works

| Stage | Who | What |
|-------|-----|------|
| Bucket repo | **manual, once** | Create `kindel/scoop-winprint` and set the `SCOOP_BUCKET_TOKEN` secret (below). No Microsoft involvement, ever. |
| Every stable release | **automated** | The `scoop` job in `.github/workflows/release.yml` renders this template and pushes `bucket/winprint.json` to the bucket. |

The source artifact is the Velopack **Portable** zip (`Kindel.WinPrint-win-x64-Portable.zip`) — the
"run without installing" build — which is exactly the shape Scoop wants (extract, don't run an
installer, no admin). Its layout is:

```
WinPrint.exe        <- Velopack stub launcher (root); the Start-Menu shortcut target
Update.exe
.portable
current/
  winprint.exe      <- real MAUI GUI
  wp.exe            <- real terminal UI (Scoop shims this onto PATH)
  RestartAgent.exe
  *.dll             <- shared assemblies for both exes
```

So the manifest sets `bin: current\wp.exe` and a `shortcuts` entry on the root `WinPrint.exe`.
This mirrors the macOS Homebrew **cask**, which also delivers GUI + `wp` from one install.

## One-time setup

1. **Create the bucket repo.** A public GitHub repo named **`kindel/scoop-winprint`**. The release
   job writes the manifest to `bucket/winprint.json` in it (the job `mkdir -p bucket` on first run,
   so the repo can start empty apart from a README).

2. **Create the token.** A classic GitHub **PAT** with **`repo`** scope that can push to
   `kindel/scoop-winprint`, saved as the repo secret **`SCOOP_BUCKET_TOKEN`** (same pattern as
   `HOMEBREW_TAP_TOKEN`). Until this secret exists the `scoop` job **fails loudly** — it never
   silently skips into a green check.

After that, every stable release auto-updates the bucket. No moderation, no queue.

## Notes / things to validate on a real install

- **Auto-update ownership:** updates are managed by **Scoop** (`scoop update`). The Velopack
  in-app updater is not the path here; the portable build does not check on launch, so the two
  don't fight. (If a future build ever calls Velopack's `UpdateManager`, it would write into Scoop's
  app dir — keep updates on the Scoop side.)
- **arm64:** only **win-x64** ships today (the win-arm64 Velopack leg is still `experimental` in
  `release.yml`, and v2.8.10 published no `win-arm64-Portable.zip`). When that leg graduates, add an
  `arm64` block under both `architecture` and `autoupdate`, and render its SHA in the `scoop` job —
  same limitation winget currently has.
- **Validation in CI:** the job validates the rendered manifest is well-formed JSON (`jq empty`).
  Loading it under Scoop proper needs Windows/PowerShell; unlike Homebrew casks, Scoop manifests are
  plain JSON with no DSL footguns, so a parse check is sufficient before publishing.

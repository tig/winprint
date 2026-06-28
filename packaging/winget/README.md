# winget packaging

Makes `winget install Kindel.WinPrint` work by publishing manifests to the
[microsoft/winget-pkgs](https://github.com/microsoft/winget-pkgs) community repository
(winget's default source). The files here are the manifest **templates** and the source of
truth for the one-time bootstrap; ongoing version updates are automated by CI.

## How it works

| Stage | Who | What |
|-------|-----|------|
| First version | **manual, once** | Submit the initial manifests to winget-pkgs (winget-pkgs requires a package to already exist before it can be auto-updated). |
| Every later release | **automated** | The `winget` job in `.github/workflows/release.yml` runs `winget-releaser` on each **stable** tag, opening a winget-pkgs PR with the new version + signed installer. |

The installer is Authenticode-signed (Azure Trusted Signing — see `docs/code-signing.md`),
which smooths winget validation.

## One-time setup

1. **Create the token.** A classic GitHub **PAT** with **`public_repo`** scope
   (fine-grained PATs are *not* supported by winget-releaser). Save it as the repo secret
   **`WINGET_TOKEN`**. The PAT's account must have a fork of `microsoft/winget-pkgs`.
   Until this secret exists, the `winget` CI job is a no-op (gated on `HAS_WINGET`).

2. **Bootstrap the first version** into winget-pkgs (pick one):
   - `komac` (recommended):
     ```bash
     komac update Kindel.WinPrint \
       --version 3.0.0 \
       --urls https://github.com/tig/winprint/releases/download/v3.0.0/Kindel.WinPrint-win-x64-Setup.exe \
       --token <PAT> --submit
     ```
     (For a brand-new package use `komac new Kindel.WinPrint`.)
   - or render the templates in this folder (replace `{{version}}`, `{{installerUrl}}`,
     `{{installerSha256}}`, `{{releaseDate}}`) and open the PR under
     `manifests/k/Kindel/WinPrint/<version>/` by hand.

   First submissions for a new publisher are **manually reviewed** by winget-pkgs moderators.

After the package exists in winget-pkgs and `WINGET_TOKEN` is set, **no further manual steps
are needed** — each stable release auto-submits its update.

## Values for the bootstrap submission (fill from the release you bootstrap)

```
PackageVersion:  3.0.0
InstallerUrl:    https://github.com/tig/winprint/releases/download/v3.0.0/Kindel.WinPrint-win-x64-Setup.exe
InstallerSha256: <sha256 of that release's win-x64 Setup.exe>
ReleaseDate:     <release date, YYYY-MM-DD>
```

## Notes / things to validate on a real install

- **Silent switch:** Velopack's `Setup.exe` is invoked with `--silent` (used by winget's
  silent install mode and by winget-pkgs sandbox validation).
- **`AppsAndFeaturesEntries.ProductCode`** is set to the Velopack PackId (`Kindel.WinPrint`),
  which is the Add/Remove Programs registry key Velopack creates. If `winget upgrade` ever
  fails to detect an installed copy, confirm the actual ARP key on a real install and adjust.
- The release workflow installs the generated Windows Velopack setup on the GitHub runner and
  asserts the Start Menu shortcut points to the MAUI GUI (`winprint.exe`), not the bundled TUI
  (`wp.exe`).
- Only the **x64** Windows installer is published; add more `Installers` entries if other
  arches ship later.

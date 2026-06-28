# Installation Guide

## Windows

### Install with Scoop (Recommended)

[Scoop](https://scoop.sh) installs from a bucket we own, so it needs no app-store approval and works
the moment a release ships. One install gives you both the `wp` terminal UI (on your `PATH`) and the
**WinPrint** GUI (Start-Menu shortcut):

```powershell
scoop bucket add winprint https://github.com/kindel/scoop-winprint
scoop install winprint
```

Don't have Scoop yet? Install it first (no admin required):

```powershell
Set-ExecutionPolicy -ExecutionPolicy RemoteSigned -Scope CurrentUser
Invoke-RestMethod -Uri https://get.scoop.sh | Invoke-Expression
```

Only the x64 build is published via Scoop today.

### Install from GitHub Releases (download the installer)

If you'd rather not use a package manager, download and run the signed installer directly:

1. Open the [latest release](https://github.com/tig/winprint/releases/latest).
2. Under **Assets**, download **`Kindel.WinPrint-win-x64-Setup.exe`**.
3. Run it. The installer is Authenticode-signed (Azure Trusted Signing), so Windows identifies the
   publisher as **Kindel, LLC** — not "unknown publisher".

This installs the GUI to the Start Menu and puts `wp` on your `PATH`, and includes a built-in
updater for future versions.

> **SmartScreen "isn't commonly downloaded" prompt.** On brand-new releases, Microsoft Defender
> SmartScreen may still warn that the file *"isn't commonly downloaded"*. This is a **reputation**
> check based on download volume — **not** a problem with the signature (the publisher line correctly
> reads *Kindel, LLC*). It fades as each release accumulates downloads. To proceed: in Edge, on the
> download choose **••• → Keep**, then **Show more → Keep anyway**; if Windows prompts at launch,
> click **More info → Run anyway** and confirm the publisher is *Kindel, LLC*. Installing via
> **Scoop** (above) avoids this prompt entirely.

> Prefer a no-installer copy? The same release also ships **`Kindel.WinPrint-win-x64-Portable.zip`**
> — unzip it anywhere and run `WinPrint.exe` (GUI) or `current\wp.exe` (TUI). This is the same
> artifact Scoop uses.

### Install with winget (coming soon)

`winget install Kindel.WinPrint` isn't available yet — the package is pending its first submission to
the Microsoft [winget-pkgs](https://github.com/microsoft/winget-pkgs) community repository. Until
that lands, use **Scoop** or the **GitHub Releases** installer above. This page will be updated when
winget goes live.

### Prerequisites

No additional prerequisites are required on Windows. WinPrint is a self-contained application.

### Upgrade

- **Scoop:** `scoop update winprint`
- **Installer / Portable:** installed builds include a built-in updater that pulls new versions
  automatically; you can also re-download the latest `Setup.exe` and run it over the top.

### Uninstall

- **Scoop:** `scoop uninstall winprint`
- **Installer:** use **Settings → Apps → Installed apps**, search for "WinPrint", and uninstall — or
  delete the unzipped folder if you used the portable zip.

---

## macOS

### Install with Homebrew (Recommended)

```bash
brew tap kindel/winprint
brew install winprint
```

This installs the **WinPrint GUI** cask (the `.app`), which **also bundles the `wp`
terminal UI** — so one command gives you both the app and the `wp` command on your `PATH`. No
`--cask` flag needed. (Prefer a one-liner? `brew install kindel/winprint/winprint` taps and installs
in a single step.)

> Want only the `wp` CLI, without the GUI app? `brew install kindel/winprint/wp` installs the
> CLI-only **formula**. (Don't install both the `winprint` cask and the `wp` formula — they each
> provide `wp` and collide at link time; pick one on macOS.)

> The macOS `.app` is signed with an **Apple Developer ID** and **notarized by Apple** (and stapled),
> so Gatekeeper accepts it normally — no quarantine/`xattr` workaround is needed.

### Prerequisites

No additional prerequisites are required for basic operation. For printing, macOS uses the built-in CUPS print system.

### Upgrade

The macOS GUI is distributed via the Homebrew cask, so Homebrew handles updates (there is no in-app
self-updater on macOS):

```bash
brew upgrade --cask winprint
```

### Uninstall

```bash
brew uninstall --cask winprint
```

---

## Linux

### Install with Homebrew (Recommended)

Linux gets the `wp` terminal UI (the GUI is Windows/macOS only):

```bash
brew install kindel/winprint/wp
```

### Prerequisites

For actual printing (not just previewing), you need a working CUPS setup:

```bash
# Debian/Ubuntu
sudo apt install cups lpr

# Fedora/RHEL
sudo dnf install cups

# Verify CUPS is running
lpstat -p
```

### Upgrade

Linux installs are upgraded with Homebrew:

```bash
brew upgrade wp
```

### Uninstall

```bash
brew uninstall wp
```

---

## Verifying Installation

After installation, verify WinPrint is working:

```bash
wp --version
```

## Launching WinPrint

| Mode | Command | Description |
|------|---------|-------------|
| TUI  | `wp`    | Terminal user interface |
| GUI  | `wp gui` | Graphical user interface on Windows/macOS |
| Start Menu / Spotlight | Search "WinPrint" | Launch the GUI from your OS app launcher |

## Troubleshooting

If `wp` is not found after installation, ensure the install location is in your `PATH`. On Windows, you may need to restart your terminal. On macOS/Linux, Homebrew handles this automatically.

The macOS `.app` is Apple Developer ID–signed and notarized, so Gatekeeper accepts it without the
*"WinPrint is damaged"* / quarantine errors that unsigned apps trigger — no `xattr` workaround needed.

For additional help, see [Support](support.md) or file an [issue on GitHub](https://github.com/tig/winprint/issues).

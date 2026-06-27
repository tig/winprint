# Installation Guide

## Windows

### Install with winget (Recommended)

```bash
winget install Kindel.WinPrint
```

### Install with Scoop

[Scoop](https://scoop.sh) installs from a bucket we own, so it needs no Microsoft approval. One
install gives you both the `wp` terminal UI (on your `PATH`) and the **WinPrint** GUI (Start-Menu
shortcut):

```powershell
scoop bucket add winprint https://github.com/kindel/scoop-winprint
scoop install winprint
```

Upgrade with `scoop update winprint`; remove with `scoop uninstall winprint`. Only the x64 build is
published via Scoop today.

### Install from GitHub Releases

Download the latest installer from [GitHub Releases](https://github.com/tig/winprint/releases) and run it.

### Prerequisites

No additional prerequisites are required on Windows. WinPrint is a self-contained application.

### Upgrade

Installed builds include a built-in updater, and winget can also upgrade the installed package:

You can also upgrade manually:

```powershell
winget upgrade Kindel.WinPrint
```

### Uninstall

```powershell
winget uninstall Kindel.WinPrint
```

Or use **Settings → Apps → Installed apps** and search for "WinPrint".

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

> **Note:** the macOS `.app` is **not notarized yet** (Apple signing isn't configured), so it ships
> ad-hoc signed and Gatekeeper may quarantine it on first launch — see
> [Troubleshooting](#troubleshooting) below.

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

### macOS: "WinPrint is damaged…" / Gatekeeper quarantine

The macOS `.app` is **not notarized yet**, so it ships ad-hoc signed. On first launch macOS
Gatekeeper may quarantine it and report that **"WinPrint is damaged and can't be opened"**. Clear
the quarantine attribute to launch it:

```bash
xattr -dr com.apple.quarantine /Applications/WinPrint.app
```

Because the cask comes from a third-party tap, you may also need to trust it at install time:

```bash
brew install --cask kindel/winprint/winprint
```

For additional help, see [Support](support.md) or file an [issue on GitHub](https://github.com/tig/winprint/issues).

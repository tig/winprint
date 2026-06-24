# Installation Guide

## Windows

### Install with winget (Recommended)

```bash
winget install Kindel.WinPrint
```

### Install from GitHub Releases

Download the latest installer from [GitHub Releases](https://github.com/tig/winprint/releases) and run it.

### Prerequisites

No additional prerequisites are required on Windows. WinPrint is a self-contained application.

### Upgrade

WinPrint packages are built with Velopack. Installed builds can use Velopack-managed updates, and winget can also upgrade the installed package:

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
brew install winprint    
```

### Prerequisites

No additional prerequisites are required for basic operation. For printing, macOS uses the built-in CUPS print system.

### Upgrade

The macOS GUI is a notarized Developer ID `.app` distributed via the Homebrew cask, so Homebrew
handles updates (there is no in-app self-updater on macOS):

```bash
brew upgrade winprint
```

### Uninstall

```bash
brew uninstall winprint
```

---

## Linux

### Install with Homebrew (Recommended)

```bash
brew install winprint
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
brew upgrade winprint
```

### Uninstall

```bash
brew uninstall winprint
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

For additional help, see [Support](support.md) or file an [issue on GitHub](https://github.com/tig/winprint/issues).

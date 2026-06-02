# Installation Guide

## Windows

### Install with winget (Recommended)

```powershell
winget install Kindel.WinPrint
```

### Install from GitHub Releases

Download the latest installer from [GitHub Releases](https://github.com/tig/winprint/releases) and run it.

### Prerequisites

No additional prerequisites are required on Windows. WinPrint is a self-contained application.

### Upgrade

WinPrint includes automatic updates via Velopack. When a new version is available, you'll be prompted to update on launch.

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
brew install --cask winprint
```

### Prerequisites

No additional prerequisites are required for basic operation. For printing, macOS uses the built-in CUPS print system.

### Upgrade

WinPrint includes automatic updates via Velopack. You can also upgrade manually:

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

WinPrint includes automatic updates via Velopack. You can also upgrade manually:

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
| GUI  | `wp gui` | Graphical user interface |
| Start Menu / Spotlight | Search "WinPrint" | Launch the GUI from your OS app launcher |

## Troubleshooting

If `wp` is not found after installation, ensure the install location is in your `PATH`. On Windows, you may need to restart your terminal. On macOS/Linux, Homebrew handles this automatically.

For additional help, see [Support](support.md) or file an [issue on GitHub](https://github.com/tig/winprint/issues).

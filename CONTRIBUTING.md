# Contributing to winprint

Pull requests are welcome. This guide gets you from `git clone` to building, testing, and
debugging winprint in **VS Code** on Windows, macOS, or Linux.

> winprint started life as a Windows app. **On Windows everything builds, runs, and
> debugs today.** The cross-platform story (macOS/Linux) is in progress — see
> [issue #64](https://github.com/tig/winprint/issues/64) and the matrix below for what
> works where right now.

## Prerequisites

1. **.NET 10 SDK** — the version is pinned by [`global.json`](global.json). Get it from
   <https://dotnet.microsoft.com/download/dotnet/10.0>. Verify with `dotnet --version`.
2. **MAUI workload** (only needed to build/run the `WinPrint.Maui` app):
   ```bash
   dotnet workload install maui
   ```
   On **macOS** the Mac Catalyst build additionally needs full **Xcode** (not just the
   Command Line Tools), and the version must match what the workload pins — currently
   **Xcode 26.5** (the `26.5` Apple SDK). Install it, then:
   ```bash
   sudo xcode-select -s /Applications/Xcode.app
   sudo xcodebuild -license accept
   ```
   A mismatched Xcode fails with "requires the MacCatalyst 26.5 SDK"; Command Line Tools
   alone fail with "A valid Xcode installation was not found".
3. **libgdiplus** (macOS/Linux only) — the Windows `System.Drawing` measurement and the
   full test suite P/Invoke GDI+, which ships natively on Windows. On other platforms:
   ```bash
   brew install mono-libgdiplus          # macOS
   sudo apt-get install -y libgdiplus    # Debian/Ubuntu
   ```

## Open in VS Code

```bash
git clone https://github.com/tig/winprint.git
cd winprint
code .
```

VS Code will prompt to install the recommended extensions
([`.vscode/extensions.json`](.vscode/extensions.json)) — accept them:

- **C# Dev Kit** (`ms-dotnettools.csdevkit`) + **C#** (`ms-dotnettools.csharp`) — Roslyn
  language service, build, and debug.
- **.NET MAUI** (`ms-dotnettools.dotnet-maui`) — MAUI build/debug targets.
- **EditorConfig** (`editorconfig.editorconfig`) — applies [`.editorconfig`](.editorconfig).

The solution is loaded automatically from `src/WinPrint.slnx`
(`dotnet.defaultSolution` in [`.vscode/settings.json`](.vscode/settings.json)).

## Build, test, debug

### Tasks (Command Palette → **Tasks: Run Task**, or Ctrl/Cmd+Shift+B for the default)

| Task             | What it does                                                              |
| ---------------- | ------------------------------------------------------------------------- |
| `build` *(default)* | **Windows:** full solution. **macOS/Linux:** `WinPrint.Core` only.     |
| `build-cli`      | Builds `WinPrint.cli` (`winprint`).                                        |
| `build-solution` | Builds the whole `src/WinPrint.slnx` (needs the MAUI workload).           |
| `test`           | Runs the `WinPrint.Core.UnitTests` suite.                                 |
| `publish-cli`    | `dotnet publish -c Release` of the CLI.                                   |

Or from a terminal:

```bash
dotnet build src/WinPrint.Core/WinPrint.Core.csproj                       # both TFMs
dotnet test  tests/WinPrint.Core.UnitTests/WinPrint.Core.UnitTests.csproj
```

### Debug (Run and Debug panel — [`.vscode/launch.json`](.vscode/launch.json))

| Profile                         | Notes                                              |
| ------------------------------- | -------------------------------------------------- |
| **WinPrint.cli**                | Put CLI args (a file, `--what-if`) in `"args"`.    |
| **WinPrint.WinForms (Windows)** | The GUI (`winprintgui`). Windows only.             |
| **WinPrint.Maui (Windows)**     | MAUI app on Windows. Needs the MAUI workload.      |
| **WinPrint.Maui (Mac Catalyst)**| MAUI app on macOS. Needs the MAUI workload.        |

### What builds/runs where today

| Project              | Windows | macOS / Linux                                    |
| -------------------- | :-----: | ------------------------------------------------ |
| `WinPrint.Core`      |   ✅    | ✅ (`net10.0`)                                   |
| `WinPrint.Core.UnitTests` | ✅ | ✅ cross-platform suite; some Windows/GDI+ tests are skipped or env-dependent (see [CLAUDE.md](CLAUDE.md)) |
| `WinPrint.cli`       |   ✅    | 🟡 compiles (`net10.0-windows` via `EnableWindowsTargeting`) but won't run — printing is `System.Drawing.Printing`/Windows-only. Tracked in #64 |
| `WinPrint.WinForms`  |   ✅    | 🟡 compiles via `EnableWindowsTargeting`; Windows-only at runtime by design |
| `WinPrint.Maui`      |   ✅    | ✅ `net10.0-maccatalyst` builds with the MAUI workload + Xcode 26.5; the Windows head only builds on Windows. Runtime not yet verified (#64) |

## Before you push

CI ([`.github/workflows/ci.yml`](.github/workflows/ci.yml)) runs a **style gate** that
fails the build on any diff. Match it locally:

```bash
dotnet tool restore
dotnet jb cleanupcode src/WinPrint.slnx --profile="WinPrintCleanup" --exclude="**/MainPage.xaml.cs"
dotnet format src/WinPrint.slnx
git diff --exit-code
```

The repo also enforces **one top-level type per file** (WPA0001) and **no nested types**
(WPA0002) via analyzers. There's a `Build.ps1` helper at the repo root for scripted builds.

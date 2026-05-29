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
2. **MAUI workload** (only needed to build/run the `WinPrint.Maui` app). From the
   repo root, restore the workloads required by the MAUI project:
   ```bash
   dotnet workload restore src/WinPrint.Maui/WinPrint.Maui.csproj
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
   You can also run the VS Code task **restore-maui-workloads**. A fresh .NET 10 SDK
   may report `NETSDK1147` for a specific MAUI workload such as `maui-tizen`; that means
   the MAUI workload set has not been restored yet.
   On Windows, restart VS Code after installing the SDK or workload so the integrated
   terminal and extensions pick up the updated `PATH`.
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

If starting **WinPrint.Maui (Windows)** shows
`Couldn't find a debug adapter descriptor for debug type 'maui'`, the .NET MAUI extension
did not activate. In VS Code or VS Code Insiders, verify that **C# Dev Kit**, **C#**, and
**.NET MAUI** are installed and enabled, then run **Developer: Reload Window**. If it still
fails, check **Output → C# Dev Kit** and **Output → .NET MAUI**; the usual causes are
`dotnet` not being on VS Code's `PATH`, a missing `maui` workload, or C# Dev Kit not being
signed in/activated.

## Build, test, debug

### Tasks (Command Palette → **Tasks: Run Task**, or Ctrl/Cmd+Shift+B for the default)

| Task             | What it does                                                              |
| ---------------- | ------------------------------------------------------------------------- |
| `build` *(default)* | **Windows:** full solution. **macOS/Linux:** `WinPrint.Core` only.     |
| `build-cli`      | Builds `WinPrint.cli` (`winprint`).                                        |
| `build-solution` | Builds the whole `src/WinPrint.slnx` (needs the MAUI workload).           |
| `build-winforms` | Builds the WinForms GUI directly for debugging; does not require MAUI.    |
| `build-maui-windows` | Builds `WinPrint.Maui` for Windows (needs the MAUI workload).        |
| `restore-maui-workloads` | Restores the MAUI workloads required by `WinPrint.Maui`.        |
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
| **WinPrint.Maui (Windows)**     | Directly launches the unpackaged Windows MAUI EXE. Needs the MAUI workload. |
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

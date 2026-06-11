---
name: run-maui-app
description: Build, launch, screenshot, and drive the WinPrint.Maui Windows app (winprint.exe) — including UIA automation of the File button and native Open dialog, and pixel capture that works for WinUI3 content. Use when asked to run, verify, or screenshot the MAUI app.
---

# Run & drive the WinPrint MAUI app (Windows)

## Build and launch

```powershell
dotnet build src/WinPrint.Maui/WinPrint.Maui.csproj -f net10.0-windows10.0.19041.0
```

The app is **unpackaged** (`WindowsPackageType=None`), assembly name `winprint`:

```powershell
$exe = "src\WinPrint.Maui\bin\Debug\net10.0-windows10.0.19041.0\win-x64\winprint.exe"
# No args — empty preview; or pass a file to load it on startup:
$proc = Start-Process -FilePath $exe -ArgumentList '"C:\full\path\to\file.cs"' -PassThru
```

Give it ~6-8 seconds to start. Command-line options mirror the CLI (`--landscape`,
`--sheet`, `--printer`, `--paper-size`; files are positional). Relative file paths are
resolved against the launch CWD (`MauiProgram.cs` captures it before switching CWD to
the exe dir).

## Verify state (works even on a locked session)

Screenshots lie or fail when the session is locked — **UI Automation doesn't**. Use:

```powershell
& .claude\skills\run-maui-app\scripts\Get-UIState.ps1 -ProcessId $proc.Id
```

Signals:
- **File loaded?** The `🖨 Print...` button is enabled only when `IsFileLoaded && !IsBusy`
  (load failure clears `ActiveFile`, disabling it). This is the reliable check.
- **Title**: `(Get-Process -Id $proc.Id).MainWindowTitle` becomes `WinPrint - <name>`
  when the title sync fires. (Historically this did NOT fire for command-line-loaded
  files because the load starts before the page is attached to its Window — don't
  treat a stale title alone as "load failed"; check the Print button.)
- The page indicator ("Page x of y") and status text are drawn **inside the preview
  drawable** — invisible to UIA. Pixel capture is the only way to see them.

## Screenshot (WinUI3 needs PrintWindow + PW_RENDERFULLCONTENT)

```powershell
& .claude\skills\run-maui-app\scripts\Capture-Window.ps1 -ProcessId $proc.Id -OutFile out.png
```

- `Graphics.CopyFromScreen` returns wallpaper/black if the session is locked
  (`Get-Process LogonUI` running ⇒ locked) — don't trust it.
- `PrintWindow` flag must be `2` (`PW_RENDERFULLCONTENT`) or WinUI3 composition content
  comes back black. The first capture right after launch can still be black; retry a few
  seconds after the file loads.
- **Look at the screenshot**: a loaded file shows the rendered page with header
  (date | filename | language), line numbers, and footer ("Page x of y").

## Drive the File button + Open dialog

```powershell
& .claude\skills\run-maui-app\scripts\Drive-FileOpen.ps1 -ProcessId $proc.Id -FilePath "C:\full\path\to\README.md"
```

This UIA-invokes the `📂 File...` button, waits for the native **Open** dialog (a child
window of the app window), sets the filename edit (automation id `1148`), and clicks
Open (automation id `1`). Works on a locked session — no real mouse/keyboard input.

## Cleanup

```powershell
$proc.CloseMainWindow()   # polite close; prompts only if sheet settings were edited
# fall back to Stop-Process -Force if it lingers
```

Closing instances matters: the app persists window state and settings next to the exe
on exit, and a leftover instance holds the bin directory, breaking the next build.

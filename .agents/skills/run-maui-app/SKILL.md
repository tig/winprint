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
& .Codex\skills\run-maui-app\scripts\Get-UIState.ps1 -ProcessId $proc.Id
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
& .Codex\skills\run-maui-app\scripts\Capture-Window.ps1 -ProcessId $proc.Id -OutFile out.png
```

- `Graphics.CopyFromScreen` returns wallpaper/black if the session is locked
  (`Get-Process LogonUI` running ⇒ locked) — don't trust it.
- `PrintWindow` flag must be `2` (`PW_RENDERFULLCONTENT`) or WinUI3 composition content
  comes back black. Even then, captures stay black until the composition surface is
  poked — a **UIA `SetFocus` on any element in the window** reliably unsticks it
  (waiting and resizing do NOT). The script detects an all-black result, pokes focus,
  and recaptures automatically.
- **Look at the screenshot**: a loaded file shows the rendered page with header
  (date | filename | language), line numbers, and footer ("Page x of y").

## Inject keyboard input (works on a locked session)

`SendInput`/`SendKeys` can't reach a locked desktop, but posting `WM_KEYDOWN`/`WM_KEYUP`
to the app's **`InputSiteWindowClass`** child hwnd does enter the WinUI3 keyboard
pipeline (posting to the top-level hwnd or `DesktopChildSiteBridge` does NOT). Keys
route only if some XAML element has focus — use UIA `SetFocus` first if needed.

```powershell
# Find the input-site child hwnd via EnumChildWindows (class "InputSiteWindowClass"),
# then e.g. PageDown (VK_NEXT=0x22, scan 0x51, extended):
$lpDown = [IntPtr](0x1 -bor (0x51 -shl 16) -bor 0x01000000)
$lpUp   = [IntPtr]([long]0x1 -bor (0x51 -shl 16) -bor 0x01000000 -bor 0xC0000000)
[Win32]::PostMessage($inputSite, 0x0100, [IntPtr]0x22, $lpDown)  # WM_KEYDOWN
[Win32]::PostMessage($inputSite, 0x0101, [IntPtr]0x22, $lpUp)    # WM_KEYUP
```

Verify paging took effect by capturing and reading the drawn footer ("Page x of y").
Note: `GetKeyStateForCurrentThread`-based modifier checks (Ctrl/Shift) won't see
modifiers this way — posted messages don't update the thread keyboard state.

## Drive the File button + Open dialog

```powershell
& .Codex\skills\run-maui-app\scripts\Drive-FileOpen.ps1 -ProcessId $proc.Id -FilePath "C:\full\path\to\README.md"
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

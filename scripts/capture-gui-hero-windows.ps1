<#
.SYNOPSIS
    Capture the Windows MAUI GUI hero GIF frames for the README/docs.

.DESCRIPTION
    tuirec is terminal-only, so the native WinUI3/MAUI GUI is captured by driving the
    running app and screenshotting the window (PrintWindow + PW_RENDERFULLCONTENT). The
    choreography shows off the GUI the way the TUI hero does — see docs/hero-gifs.md:

        load -> toggle Line Numbers -> toggle Landscape (portrait/2-up)
             -> zoom in -> pan -> reset (FAST) -> open another file -> hold

    Hard-won mechanics (don't "simplify" these away):
      * Settings toggles are driven by CLICKING the sidebar label (its TapGestureRecognizer
        flips the bound CheckBox) located via UI Automation BoundingRectangle.
      * Zoom uses the plain TUI-consistent keys: '='/'+' zoom in, '-' out, '0' fits (the
        OnNativeKeyDown normalization makes these route on Windows — WinUI sends "187"/"189"
        for the OEM +/- keys and "Number0" for 0, which the handler maps to OemPlus/OemMinus/D0).
      * Keys only route when a XAML element has focus, so the preview
        (FocusablePlatformGraphicsView) is focused with a real mouse CLICK before input;
        a click also forces the GraphicsView to re-present (PrintWindow caches otherwise).
      * Requires an UNLOCKED, interactive session (real injected input).

    Assemble the frames into the GIF with scripts/assemble-gui-hero.py.
#>
param(
    [string] $Exe     = "src\WinPrint.Maui\bin\Release\net10.0-windows10.0.19041.0\win-arm64\winprint.exe",
    [string] $Sample  = "src\WinPrint.Core\ViewModels\SheetViewModel.cs",
    [string] $OpenFile = "README.md",   # second file opened via the File dialog (renders as Markdown)
    [string] $OutDir  = "artifacts\hero\gui-frames",
    [int]    $StartupMs = 9000
)
$ErrorActionPreference = 'Stop'
Add-Type -AssemblyName UIAutomationClient
Add-Type -AssemblyName UIAutomationTypes
Add-Type @"
using System;
using System.Runtime.InteropServices;
public class HeroIn {
    [DllImport("user32.dll")] public static extern void keybd_event(byte bVk, byte bScan, uint dwFlags, IntPtr dwExtra);
    [DllImport("user32.dll")] public static extern void mouse_event(uint dwFlags, uint dx, uint dy, uint dwData, IntPtr dwExtra);
    [DllImport("user32.dll")] public static extern bool SetCursorPos(int X, int Y);
    [DllImport("user32.dll")] public static extern bool SetForegroundWindow(IntPtr hWnd);
    [DllImport("user32.dll")] public static extern bool ShowWindow(IntPtr hWnd, int n);
    [DllImport("user32.dll")] public static extern bool GetWindowRect(IntPtr hWnd, out RECT r);
    [StructLayout(LayoutKind.Sequential)] public struct RECT { public int Left, Top, Right, Bottom; }
}
"@
$KEYUP=0x2; $EXT=0x1; $WHEEL=0x0800; $MDOWN=0x2; $MUP=0x4
$VK_CTRL=0x11; $VK_PAGEDOWN=0x22; $VK_LEFT=0x25; $VK_UP=0x26; $VK_RIGHT=0x27; $VK_DOWN=0x28
$A = [System.Windows.Automation.AutomationElement]
$TS = [System.Windows.Automation.TreeScope]

$exePath    = (Resolve-Path $Exe).Path
$samplePath = (Resolve-Path $Sample).Path
$openPath   = (Resolve-Path $OpenFile).Path
$capture    = (Resolve-Path ".claude\skills\run-maui-app\scripts\Capture-Window.ps1").Path
New-Item -ItemType Directory -Force -Path $OutDir | Out-Null
Get-ChildItem $OutDir -Filter *.png -ErrorAction SilentlyContinue | Remove-Item -Force

$proc = Start-Process -FilePath $exePath -ArgumentList "`"$samplePath`"" -PassThru
Write-Host "PID $($proc.Id); waiting ${StartupMs}ms for load..."
Start-Sleep -Milliseconds $StartupMs
$proc.Refresh()
$top = $proc.MainWindowHandle
if ($top -eq [IntPtr]::Zero) { throw "No main window for PID $($proc.Id)" }

function Get-AppWindow {
    $root = $A::RootElement
    $cond = New-Object System.Windows.Automation.PropertyCondition($A::ProcessIdProperty, $proc.Id)
    $root.FindFirst($TS::Children, $cond)
}
function Foreground { [HeroIn]::ShowWindow($top,9)|Out-Null; [HeroIn]::SetForegroundWindow($top)|Out-Null; Start-Sleep -Milliseconds 150 }
function Get-PreviewPoint {
    $r = New-Object HeroIn+RECT; [void][HeroIn]::GetWindowRect($top,[ref]$r)
    ,@([int]($r.Left + ($r.Right-$r.Left) * 0.62), [int]($r.Top + ($r.Bottom-$r.Top) * 0.30))
}
function Click-Point([int]$x,[int]$y) {
    [HeroIn]::SetCursorPos($x,$y) | Out-Null; Start-Sleep -Milliseconds 60
    [HeroIn]::mouse_event($MDOWN,0,0,0,[IntPtr]::Zero); Start-Sleep -Milliseconds 40
    [HeroIn]::mouse_event($MUP,0,0,0,[IntPtr]::Zero);   Start-Sleep -Milliseconds 120
}
function Focus-Preview { Foreground; $p = Get-PreviewPoint; Click-Point $p[0] $p[1]; Start-Sleep -Milliseconds 200 }

# Click a sidebar label by its text — its TapGestureRecognizer flips the bound CheckBox.
function Toggle-Setting([string]$labelText) {
    Foreground
    $win = Get-AppWindow
    $cond = New-Object System.Windows.Automation.PropertyCondition($A::NameProperty, $labelText)
    $el = $win.FindFirst($TS::Descendants, $cond)
    if ($el -eq $null) { throw "Label '$labelText' not found" }
    $r = $el.Current.BoundingRectangle
    Click-Point ([int]($r.X + $r.Width/2)) ([int]($r.Y + $r.Height/2))
}

# Press a key. Arrow/navigation keys are "extended" and need the flag; plain keys (zoom
# +/=/-/0) must NOT set it or the scancode maps to the wrong key.
function Key([byte]$vk, [bool]$ext = $false) {
    $flag = if ($ext) { $EXT } else { 0 }
    [HeroIn]::keybd_event($vk,0,$flag,[IntPtr]::Zero); Start-Sleep -Milliseconds 30
    [HeroIn]::keybd_event($vk,0,$flag -bor $KEYUP,[IntPtr]::Zero); Start-Sleep -Milliseconds 60
}

# Open a different file through the native Open dialog (File button -> filename edit -> Open).
function Open-File([string]$path) {
    Foreground
    $win = Get-AppWindow
    # File button: Name is "📂 File…"; match on the "File" substring to dodge ellipsis variants.
    $btns = $win.FindAll($TS::Descendants,
        (New-Object System.Windows.Automation.PropertyCondition($A::ControlTypeProperty,
            [System.Windows.Automation.ControlType]::Button)))
    $fileBtn = $null
    foreach ($b in $btns) { if ($b.Current.Name -like '*File*') { $fileBtn = $b; break } }
    if ($fileBtn -eq $null) { throw "File button not found" }
    $fileBtn.GetCurrentPattern([System.Windows.Automation.InvokePattern]::Pattern).Invoke()

    $dlg = $null
    $dlgCond = New-Object System.Windows.Automation.PropertyCondition($A::ControlTypeProperty,
        [System.Windows.Automation.ControlType]::Window)
    for ($i=0; $i -lt 20 -and $dlg -eq $null; $i++) { Start-Sleep -Milliseconds 400; $dlg = $win.FindFirst($TS::Children, $dlgCond) }
    if ($dlg -eq $null) { throw "Open dialog did not appear" }
    # The modern IFileOpenDialog exposes no settable filename Edit via UIA (autoid 1148/1 are
    # Panes; cross-process ValuePattern.SetValue times out). But the filename field has focus
    # on open, so paste the path from the clipboard and press Enter — simplest and reliable.
    Start-Sleep -Milliseconds 500
    Set-Clipboard -Value $path
    Start-Sleep -Milliseconds 300
    $VK_V=0x56; $VK_RETURN=0x0D
    [HeroIn]::keybd_event($VK_CTRL,0,0,[IntPtr]::Zero); Start-Sleep -Milliseconds 40        # Ctrl down
    [HeroIn]::keybd_event([byte]$VK_V,0,0,[IntPtr]::Zero); Start-Sleep -Milliseconds 40     # V
    [HeroIn]::keybd_event([byte]$VK_V,0,$KEYUP,[IntPtr]::Zero); Start-Sleep -Milliseconds 40
    [HeroIn]::keybd_event($VK_CTRL,0,$KEYUP,[IntPtr]::Zero); Start-Sleep -Milliseconds 600  # Ctrl up
    [HeroIn]::keybd_event([byte]$VK_RETURN,0,0,[IntPtr]::Zero); Start-Sleep -Milliseconds 40
    [HeroIn]::keybd_event([byte]$VK_RETURN,0,$KEYUP,[IntPtr]::Zero)
    Start-Sleep -Seconds 3   # let the new file render
}

$script:frame = 0
function Snap([string]$label) {
    Focus-Preview   # also forces the GraphicsView to present its latest frame
    & $capture -ProcessId $proc.Id -OutFile (Join-Path $OutDir ("{0:D2}-{1}.png" -f $script:frame, $label)) | Out-Null
    Write-Host ("frame {0:D2} {1}" -f $script:frame, $label)
    $script:frame++
}

Start-Sleep -Milliseconds 500
Snap "loaded"                                            # SheetViewModel.cs, landscape 2-up, line numbers on

Toggle-Setting "Line Numbers"; Start-Sleep -Milliseconds 1400
Snap "linenums-off"                                      # line numbers removed from the preview
Toggle-Setting "Line Numbers"; Start-Sleep -Milliseconds 1200
Snap "linenums-on"                                       # ...and back

Toggle-Setting "Landscape"; Start-Sleep -Milliseconds 1700
Snap "portrait"                                          # reflow to 1-up portrait
Toggle-Setting "Landscape"; Start-Sleep -Milliseconds 1500
Snap "landscape"                                         # ...and back to 2-up landscape

# --- FAST zoom/pan flourish using the consistent plain keys (match the TUI): '=' zooms in,
#     arrows pan, '0' fits. Minimal settle; short GIF durations downstream. ---
$VK_EQUALS=0xBB; $VK_DIGIT0=0x30
Focus-Preview; Key $VK_EQUALS; Key $VK_EQUALS; Snap "zoom1"
Key $VK_EQUALS; Key $VK_EQUALS; Snap "zoom2"
Key $VK_DOWN $true; Key $VK_DOWN $true; Key $VK_RIGHT $true; Snap "pan"
Key $VK_UP $true; Key $VK_LEFT $true; Key $VK_LEFT $true; Snap "pan2"
Key $VK_DIGIT0; Start-Sleep -Milliseconds 300
Snap "reset"                                             # plain '0' fits to window

Open-File $openPath
Snap "openfile"                                          # a different document (Markdown) opened via the dialog
Start-Sleep -Milliseconds 300
Snap "hold"

Write-Host "Done. Frames in $OutDir (PID $($proc.Id) still running — close it when finished)."

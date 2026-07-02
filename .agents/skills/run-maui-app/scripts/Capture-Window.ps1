# Captures a screenshot of a window by process ID.
# Uses PrintWindow with PW_RENDERFULLCONTENT (2), which is REQUIRED for WinUI3/MAUI
# (DirectComposition) content and works even when the session is locked or the window
# is occluded. Plain CopyFromScreen returns wallpaper/black on a locked session.
# Note: PrintWindow can come back black until the composition surface is poked — a UIA
# SetFocus on any element in the window reliably unsticks it. This script detects an
# (almost) all-black capture, pokes focus via UIA, and recaptures automatically.
param(
    [Parameter(Mandatory)] [int] $ProcessId,
    [Parameter(Mandatory)] [string] $OutFile
)
Add-Type -AssemblyName System.Drawing
Add-Type -AssemblyName UIAutomationClient
Add-Type -AssemblyName UIAutomationTypes
Add-Type @"
using System;
using System.Runtime.InteropServices;
public class Win32Cap {
    [DllImport("user32.dll")] public static extern bool SetForegroundWindow(IntPtr hWnd);
    [DllImport("user32.dll")] public static extern bool GetWindowRect(IntPtr hWnd, out RECT rect);
    [DllImport("user32.dll")] public static extern bool PrintWindow(IntPtr hWnd, IntPtr hdcBlt, uint nFlags);
    [DllImport("user32.dll")] public static extern bool IsIconic(IntPtr hWnd);
    [DllImport("user32.dll")] public static extern bool ShowWindow(IntPtr hWnd, int nCmdShow);
    [StructLayout(LayoutKind.Sequential)] public struct RECT { public int Left, Top, Right, Bottom; }
}
"@

function Invoke-Capture([IntPtr] $hwnd) {
    $rect = New-Object Win32Cap+RECT
    [Win32Cap]::GetWindowRect($hwnd, [ref]$rect) | Out-Null
    $w = $rect.Right - $rect.Left
    $h = $rect.Bottom - $rect.Top
    if ($w -le 0 -or $h -le 0) { throw "Bad window rect" }
    $bmp = New-Object System.Drawing.Bitmap($w, $h)
    $gfx = [System.Drawing.Graphics]::FromImage($bmp)
    $hdc = $gfx.GetHdc()
    $ok = [Win32Cap]::PrintWindow($hwnd, $hdc, 2)  # 2 = PW_RENDERFULLCONTENT
    $gfx.ReleaseHdc($hdc)
    $gfx.Dispose()
    if (-not $ok) { Write-Warning "PrintWindow returned false" }
    return $bmp
}

function Test-MostlyBlack([System.Drawing.Bitmap] $bmp) {
    # Sample a sparse grid; treat as black when nearly every sample is black.
    $samples = 0; $black = 0
    for ($y = 10; $y -lt $bmp.Height; $y += [Math]::Max(1, [int]($bmp.Height / 12))) {
        for ($x = 10; $x -lt $bmp.Width; $x += [Math]::Max(1, [int]($bmp.Width / 12))) {
            $px = $bmp.GetPixel($x, $y)
            $samples++
            if ($px.R -lt 8 -and $px.G -lt 8 -and $px.B -lt 8) { $black++ }
        }
    }
    return ($samples -gt 0 -and $black / $samples -gt 0.98)
}

$proc = Get-Process -Id $ProcessId -ErrorAction Stop
$hwnd = $proc.MainWindowHandle
if ($hwnd -eq [IntPtr]::Zero) { throw "Process $ProcessId has no main window" }
if ([Win32Cap]::IsIconic($hwnd)) { [Win32Cap]::ShowWindow($hwnd, 9) | Out-Null; Start-Sleep -Milliseconds 500 }
[Win32Cap]::SetForegroundWindow($hwnd) | Out-Null
Start-Sleep -Milliseconds 500

$bmp = Invoke-Capture $hwnd
if (Test-MostlyBlack $bmp) {
    $bmp.Dispose()
    # Poke the window via UIA focus to force the composition surface to materialize.
    $root = [System.Windows.Automation.AutomationElement]::RootElement
    $cond = New-Object System.Windows.Automation.PropertyCondition(
        [System.Windows.Automation.AutomationElement]::ProcessIdProperty, $ProcessId)
    $win = $root.FindFirst([System.Windows.Automation.TreeScope]::Children, $cond)
    if ($win -ne $null) {
        $focusable = New-Object System.Windows.Automation.PropertyCondition(
            [System.Windows.Automation.AutomationElement]::IsKeyboardFocusableProperty, $true)
        $el = $win.FindFirst([System.Windows.Automation.TreeScope]::Descendants, $focusable)
        if ($el -ne $null) { try { $el.SetFocus() } catch {} }
    }
    Start-Sleep -Seconds 3
    $bmp = Invoke-Capture $hwnd
    if (Test-MostlyBlack $bmp) { Write-Warning "Capture still looks black after UIA focus poke" }
}

$bmp.Save($OutFile, [System.Drawing.Imaging.ImageFormat]::Png)
$size = "$($bmp.Width)x$($bmp.Height)"
$bmp.Dispose()
Write-Output "Saved $size screenshot to $OutFile (title: '$($proc.MainWindowTitle)')"

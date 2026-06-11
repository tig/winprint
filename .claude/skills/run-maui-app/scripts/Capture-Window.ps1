# Captures a screenshot of a window by process ID.
# Uses PrintWindow with PW_RENDERFULLCONTENT (2), which is REQUIRED for WinUI3/MAUI
# (DirectComposition) content and works even when the session is locked or the window
# is occluded. Plain CopyFromScreen returns wallpaper/black on a locked session.
# Note: the first PrintWindow attempt right after launch can come back black; retry
# after the app has painted (a few seconds after the file loads).
param(
    [Parameter(Mandatory)] [int] $ProcessId,
    [Parameter(Mandatory)] [string] $OutFile
)
Add-Type -AssemblyName System.Drawing
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
$proc = Get-Process -Id $ProcessId -ErrorAction Stop
$hwnd = $proc.MainWindowHandle
if ($hwnd -eq [IntPtr]::Zero) { throw "Process $ProcessId has no main window" }
if ([Win32Cap]::IsIconic($hwnd)) { [Win32Cap]::ShowWindow($hwnd, 9) | Out-Null; Start-Sleep -Milliseconds 500 }
[Win32Cap]::SetForegroundWindow($hwnd) | Out-Null
Start-Sleep -Milliseconds 500
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
$bmp.Save($OutFile, [System.Drawing.Imaging.ImageFormat]::Png)
$bmp.Dispose()
Write-Output "Saved ${w}x${h} screenshot to $OutFile (title: '$($proc.MainWindowTitle)')"

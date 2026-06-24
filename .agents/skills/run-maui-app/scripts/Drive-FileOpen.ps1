# Drives the running WinPrint MAUI app's "📂 File..." button via UI Automation:
# clicks the button, waits for the native Open dialog, types the file path, clicks Open.
# Works even when the session is locked (UIA is in-session, no real input needed).
param(
    [Parameter(Mandatory)] [int] $ProcessId,
    [Parameter(Mandatory)] [string] $FilePath
)
Add-Type -AssemblyName UIAutomationClient
Add-Type -AssemblyName UIAutomationTypes

$root = [System.Windows.Automation.AutomationElement]::RootElement
$cond = New-Object System.Windows.Automation.PropertyCondition(
    [System.Windows.Automation.AutomationElement]::ProcessIdProperty, $ProcessId)
$win = $root.FindFirst([System.Windows.Automation.TreeScope]::Children, $cond)
if ($win -eq $null) { throw "No top-level window for process $ProcessId" }

$btnCond = New-Object System.Windows.Automation.PropertyCondition(
    [System.Windows.Automation.AutomationElement]::NameProperty, "📂 File...")
$fileBtn = $win.FindFirst([System.Windows.Automation.TreeScope]::Descendants, $btnCond)
if ($fileBtn -eq $null) { throw "File button not found" }
$fileBtn.GetCurrentPattern([System.Windows.Automation.InvokePattern]::Pattern).Invoke()

# Wait for the Open dialog (a child Window of the app window)
$dlg = $null
$dlgCond = New-Object System.Windows.Automation.PropertyCondition(
    [System.Windows.Automation.AutomationElement]::ControlTypeProperty,
    [System.Windows.Automation.ControlType]::Window)
for ($i = 0; $i -lt 20 -and $dlg -eq $null; $i++) {
    Start-Sleep -Milliseconds 500
    $dlg = $win.FindFirst([System.Windows.Automation.TreeScope]::Children, $dlgCond)
}
if ($dlg -eq $null) { throw "Open dialog did not appear" }

# Classic common-dialog automation IDs: 1148 = filename edit, 1 = Open button
$editCond = New-Object System.Windows.Automation.PropertyCondition(
    [System.Windows.Automation.AutomationElement]::AutomationIdProperty, "1148")
$edit = $dlg.FindFirst([System.Windows.Automation.TreeScope]::Descendants, $editCond)
if ($edit -eq $null) { throw "Filename edit (autoid 1148) not found" }
$edit.GetCurrentPattern([System.Windows.Automation.ValuePattern]::Pattern).SetValue($FilePath)
Start-Sleep -Milliseconds 500

$openCond = New-Object System.Windows.Automation.PropertyCondition(
    [System.Windows.Automation.AutomationElement]::AutomationIdProperty, "1")
$openBtn = $dlg.FindFirst([System.Windows.Automation.TreeScope]::Descendants, $openCond)
if ($openBtn -eq $null) { throw "Open button (autoid 1) not found" }
$openBtn.GetCurrentPattern([System.Windows.Automation.InvokePattern]::Pattern).Invoke()

Start-Sleep -Seconds 3
$proc = Get-Process -Id $ProcessId
Write-Output "Opened '$FilePath'. Window title now: '$($proc.MainWindowTitle)'"

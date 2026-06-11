# Dumps the UI Automation element tree (control type, name, value, enabled) of the
# running WinPrint MAUI app. Use this to verify app state without pixels — works on a
# locked session where screenshots fail.
# Key signals:
#   - "🖨 Print..." button enabled  => a file is loaded (PrintCommand CanExecute is
#     IsFileLoaded && !IsBusy). On load failure ActiveFile is cleared, disabling it.
#   - Window title "WinPrint - <name>" => title sync fired (file-button path).
# The page indicator / status text are drawn inside the preview GraphicsView drawable
# and are NOT visible to UIA.
param(
    [Parameter(Mandatory)] [int] $ProcessId
)
Add-Type -AssemblyName UIAutomationClient
Add-Type -AssemblyName UIAutomationTypes

$root = [System.Windows.Automation.AutomationElement]::RootElement
$cond = New-Object System.Windows.Automation.PropertyCondition(
    [System.Windows.Automation.AutomationElement]::ProcessIdProperty, $ProcessId)
$win = $root.FindFirst([System.Windows.Automation.TreeScope]::Children, $cond)
if ($win -eq $null) { throw "No top-level window for process $ProcessId" }
"Window: '$($win.Current.Name)'"
$all = $win.FindAll([System.Windows.Automation.TreeScope]::Descendants,
    [System.Windows.Automation.Condition]::TrueCondition)
$i = 0
foreach ($el in $all) {
    $c = $el.Current
    $type = $c.ControlType.ProgrammaticName -replace 'ControlType.', ''
    $val = ''
    try {
        $vp = $el.GetCurrentPattern([System.Windows.Automation.ValuePattern]::Pattern)
        $val = $vp.Current.Value
    } catch {}
    "{0,2}: {1} name='{2}' value='{3}' enabled={4}" -f $i, $type, $c.Name, $val, $c.IsEnabled
    $i++
}

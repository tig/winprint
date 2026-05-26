echo "If build fails, do this:"
echo "cmd.exe ""/K"" '""C:\Program Files\Microsoft Visual Studio\2022\Preview\Common7\Tools\VsDevCmd.bat"" && pwsh -noexit'"

$configuration = "Debug"
$platform = "x64"
$targetFramework = "net10.0-windows"

echo "Building"
$env:winprint_telemetryId="put Kindel Systems key here"
texttransform .\src\WinPrint.Core\Services\TelemetryService.tt
msbuild /p:Configuration=$configuration /p:Platform=$platform src/WinPrint.slnx

$cliPath = ".\src\WinPrint.cli\bin\$platform\$configuration\$targetFramework\winprint.exe"
$guiPath = ".\src\WinPrint.WinForms\bin\$platform\$configuration\$targetFramework\winprintgui.exe"

echo "Testing winprint CLI"
& $cliPath .\testfiles\Program.cs --what-if

echo "Testing winprintgui"
& $guiPath .\testfiles\Program.cs 

echo "Setting up VS2022 Shell"
#Import-Module -Verbose "C:\Program Files\Microsoft Visual Studio\2022\Preview\Common7\Tools\Microsoft.VisualStudio.DevShell.dll"
#Enter-VsDevShell dff05ec8 -DevCmdDebugLevel Basic -SkipAutomaticLocation -DevCmdArguments """-arch=x64 -host_arch=x64"""

 $Env:VCTargetsPath="C:\Program Files\Microsoft Visual Studio\2022\Preview\MSBuild\Microsoft\VC\v170\"

echo "Cleaning Deploy folder"
rm -Recurse -Path setup/Deploy -Include *.* 

echo "Building"
dotnet build src/WinPrint.sln

echo "Testing Out-winprint"
pwsh.exe -NoLogo -Command "Import-Module '.\setup\Deploy\winprint.dll'; Out-WinPrint .\testfiles\Program.cs -WhatIf -Verbose;"

echo "Testing winprintgui"
.\setup\Deploy\winprintgui.exe .\testfiles\Program.cs 

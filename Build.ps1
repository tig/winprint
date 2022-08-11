echo "Setting up VS2022 Shell"
#cmd.exe "/K" '"C:\Program Files\Microsoft Visual Studio\2022\Preview\Common7\Tools\VsDevCmd.bat" && pwsh -noexit' 

echo "Cleaning Deploy folder"
rm -Recurse -Path setup/Deploy -Include *.* 

echo "Building"
msbuild /p:Configuration=Debug /p:Platform=x64 src/WinPrint.sln 

echo "Testing Out-winprint"
pwsh.exe -NoLogo -Command "Import-Module '.\setup\Deploy\winprint.dll'; Out-WinPrint .\testfiles\Program.cs -WhatIf -Verbose;"

echo "Testing winprintgui"
.\setup\Deploy\winprintgui.exe .\testfiles\Program.cs 

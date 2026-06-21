#!/usr/bin/env pwsh
<#
.SYNOPSIS
  Reliable local build that avoids the WinPrint.Analyzers file-lock race.

.DESCRIPTION
  WinPrint.Analyzers (a Roslyn analyzer) is ProjectReference'd by every project
  (Directory.Build.props). The persistent C# build server (VBCSCompiler) loads it to run
  the WPA rules and holds a Windows file lock on the DLL *across* builds, so a plain
  `dotnet build` intermittently fails with MSB3026 / CS2012
  ("WinPrint.Analyzers.dll ... is being used by another process") whenever the analyzer
  gets rebuilt while a previous build's server still holds it.

  This script makes a local build reliable by doing what CI does, in order:
    1. Shut down the build/compiler servers, releasing any lingering lock on the analyzer.
    2. Pre-build the analyzer on its own (so the solution build never rebuilds it mid-flight).
    3. Build the requested target (default: WinPrint.slnx), forwarding any extra arguments.

.EXAMPLE
  ./scripts/build.ps1
  ./scripts/build.ps1 -c Release
  ./scripts/build.ps1 src/WinPrint.Core/WinPrint.Core.csproj -c Debug
#>

$ErrorActionPreference = 'Stop'
$repo = Split-Path -Parent $PSScriptRoot
$exitCode = 0

Push-Location $repo
try {
    Write-Host '==> dotnet build-server shutdown (release the analyzer lock)' -ForegroundColor Cyan
    dotnet build-server shutdown

    Write-Host '==> Pre-building WinPrint.Analyzers (mirrors CI; avoids the rebuild race)' -ForegroundColor Cyan
    dotnet build tools/WinPrint.Analyzers/WinPrint.Analyzers.csproj
    if ($LASTEXITCODE -ne 0) { $exitCode = $LASTEXITCODE; return }

    # Forward whatever the caller passed; default to the solution when nothing was given.
    $forward = if ($args.Count -gt 0) { $args } else { @('WinPrint.slnx') }
    Write-Host "==> dotnet build $($forward -join ' ')" -ForegroundColor Cyan
    dotnet build @forward
    $exitCode = $LASTEXITCODE
}
finally {
    Pop-Location
}

exit $exitCode

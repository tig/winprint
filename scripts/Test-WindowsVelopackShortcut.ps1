[CmdletBinding()]
param(
    [Parameter(Mandatory = $true)]
    [string] $ReleaseDir,

    [string] $PackId = $env:PACK_ID,

    [string] $PackTitle = $env:PACK_TITLE,

    [string] $ExpectedMainExe = "winprint.exe"
)

Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"

if ([string]::IsNullOrWhiteSpace($PackId)) {
    throw "PackId must be provided either as -PackId or PACK_ID."
}

if ([string]::IsNullOrWhiteSpace($PackTitle)) {
    throw "PackTitle must be provided either as -PackTitle or PACK_TITLE."
}

if (-not (Test-Path -LiteralPath $ReleaseDir -PathType Container)) {
    throw "ReleaseDir does not exist: $ReleaseDir"
}

$setupFiles = @(Get-ChildItem -LiteralPath $ReleaseDir -Filter "*-Setup.exe" -File)
if ($setupFiles.Count -ne 1) {
    $found = ($setupFiles | Select-Object -ExpandProperty FullName) -join [Environment]::NewLine
    throw "Expected exactly one Velopack Setup.exe in '$ReleaseDir', found $($setupFiles.Count):$([Environment]::NewLine)$found"
}

$installRoot = Join-Path $env:LOCALAPPDATA $PackId
if (Test-Path -LiteralPath $installRoot) {
    throw "Refusing to validate over an existing install: $installRoot"
}

function Get-ShortcutDetails {
    param(
        [Parameter(Mandatory = $true)]
        [string] $Root,

        [Parameter(Mandatory = $true)]
        [string] $InstallRoot
    )

    if (-not (Test-Path -LiteralPath $Root -PathType Container)) {
        return
    }

    $shell = New-Object -ComObject WScript.Shell
    Get-ChildItem -LiteralPath $Root -Recurse -Filter "*.lnk" -File |
        ForEach-Object {
            $shortcut = $shell.CreateShortcut($_.FullName)
            [pscustomobject]@{
                Name = $_.Name
                FullName = $_.FullName
                TargetPath = $shortcut.TargetPath
                Arguments = $shortcut.Arguments
                WorkingDirectory = $shortcut.WorkingDirectory
                PointsAtInstall = $shortcut.TargetPath.StartsWith($InstallRoot, [StringComparison]::OrdinalIgnoreCase) -or
                    $shortcut.Arguments.Contains($InstallRoot, [StringComparison]::OrdinalIgnoreCase)
            }
        }
}

function Remove-InstallArtifacts {
    param(
        [Parameter(Mandatory = $true)]
        [string] $InstallRoot,

        [Parameter(Mandatory = $true)]
        [string[]] $ShortcutRoots
    )

    foreach ($shortcutRoot in $ShortcutRoots) {
        Get-ShortcutDetails -Root $shortcutRoot -InstallRoot $InstallRoot |
            Where-Object { $_.PointsAtInstall } |
            ForEach-Object { Remove-Item -LiteralPath $_.FullName -Force -ErrorAction SilentlyContinue }
    }

    for ($attempt = 1; $attempt -le 5; $attempt++) {
        if (-not (Test-Path -LiteralPath $InstallRoot)) {
            return
        }

        Remove-Item -LiteralPath $InstallRoot -Recurse -Force -ErrorAction SilentlyContinue
        if (-not (Test-Path -LiteralPath $InstallRoot)) {
            return
        }

        Start-Sleep -Seconds 2
    }

    Write-Warning "Could not remove temporary install directory: $InstallRoot"
}

function Wait-ForCondition {
    param(
        [Parameter(Mandatory = $true)]
        [scriptblock] $Condition,

        [Parameter(Mandatory = $true)]
        [string] $FailureMessage,

        [int] $TimeoutSeconds = 60
    )

    $deadline = (Get-Date).AddSeconds($TimeoutSeconds)
    do {
        if (& $Condition) {
            return
        }

        Start-Sleep -Seconds 1
    } while ((Get-Date) -lt $deadline)

    throw $FailureMessage
}

$shortcutRoots = @(
    (Join-Path $env:APPDATA "Microsoft\Windows\Start Menu\Programs"),
    ([Environment]::GetFolderPath("DesktopDirectory"))
)

try {
    $currentDir = Join-Path $installRoot "current"
    $expectedTarget = Join-Path $currentDir $ExpectedMainExe
    $tuiExe = Join-Path $currentDir "wp.exe"

    & $setupFiles[0].FullName --silent
    if ($LASTEXITCODE -ne 0) {
        throw "Velopack setup failed with exit code $LASTEXITCODE."
    }

    Wait-ForCondition `
        -Condition { (Test-Path -LiteralPath $expectedTarget -PathType Leaf) -and (Test-Path -LiteralPath $tuiExe -PathType Leaf) } `
        -FailureMessage "Timed out waiting for Velopack to install '$ExpectedMainExe' and 'wp.exe' under '$currentDir'."

    if (-not (Test-Path -LiteralPath $expectedTarget -PathType Leaf)) {
        throw "Expected main executable was not installed: $expectedTarget"
    }

    if (-not (Test-Path -LiteralPath $tuiExe -PathType Leaf)) {
        throw "Expected bundled TUI executable was not installed: $tuiExe"
    }

    $packageShortcuts = @()
    Wait-ForCondition `
        -Condition {
            $script:packageShortcuts = @($shortcutRoots |
                    ForEach-Object { Get-ShortcutDetails -Root $_ -InstallRoot $installRoot } |
                    Where-Object { $_.PointsAtInstall })
            $script:packageShortcuts.Count -gt 0
        } `
        -FailureMessage "Timed out waiting for shortcuts to be created for install root '$installRoot'."

    if ($packageShortcuts.Count -eq 0) {
        throw "No shortcuts were created for install root '$installRoot'."
    }

    $startMenuRoot = Join-Path $env:APPDATA "Microsoft\Windows\Start Menu\Programs"
    $startMenuShortcuts = @($packageShortcuts | Where-Object { $_.FullName.StartsWith($startMenuRoot, [StringComparison]::OrdinalIgnoreCase) })
    if ($startMenuShortcuts.Count -eq 0) {
        throw "No Start Menu shortcut was created for '$PackTitle'."
    }

    $wrongTargets = @($startMenuShortcuts | Where-Object {
            -not $_.TargetPath.Equals($expectedTarget, [StringComparison]::OrdinalIgnoreCase)
        })

    if ($wrongTargets.Count -gt 0) {
        $details = ($wrongTargets | ForEach-Object { "$($_.FullName) -> $($_.TargetPath)" }) -join [Environment]::NewLine
        throw "Start Menu shortcut target mismatch. Expected '$expectedTarget':$([Environment]::NewLine)$details"
    }

    Write-Host "Validated Start Menu shortcut target: $($startMenuShortcuts[0].TargetPath)"
}
finally {
    Remove-InstallArtifacts -InstallRoot $installRoot -ShortcutRoots $shortcutRoots
}

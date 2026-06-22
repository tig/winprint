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

# Velopack's Update.exe is a GUI-subsystem binary, so `& $updateExe` may return without
# setting $LASTEXITCODE. Under StrictMode -Latest, reading an unset $LASTEXITCODE throws.
# Seed it so the exit-code checks never fault on the cleanup path.
$global:LASTEXITCODE = 0

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

    $currentDir = Join-Path $InstallRoot "current"
    $updateExe = Join-Path $InstallRoot "Update.exe"
    if (Test-Path -LiteralPath $updateExe -PathType Leaf) {
        Wait-ForInstallProcessesToExit -InstallRoot $InstallRoot -TimeoutSeconds 60

        & $updateExe --uninstall --silent
        if ($LASTEXITCODE -ne 0) {
            Write-Warning "Velopack uninstall failed with exit code $LASTEXITCODE; falling back to direct artifact cleanup."
        }

        Start-Sleep -Seconds 2
    }

    Remove-UserPathEntry -Entry $currentDir

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

function Get-InstallProcesses {
    param(
        [Parameter(Mandatory = $true)]
        [string] $InstallRoot
    )

    @(Get-CimInstance Win32_Process |
        Where-Object {
            $_.ExecutablePath -and $_.ExecutablePath.StartsWith($InstallRoot, [StringComparison]::OrdinalIgnoreCase)
        })
}

function Wait-ForInstallProcessesToExit {
    param(
        [Parameter(Mandatory = $true)]
        [string] $InstallRoot,

        [int] $TimeoutSeconds = 60
    )

    $deadline = (Get-Date).AddSeconds($TimeoutSeconds)
    do {
        if (@(Get-InstallProcesses -InstallRoot $InstallRoot).Count -eq 0) {
            return
        }

        Start-Sleep -Seconds 1
    } while ((Get-Date) -lt $deadline)

    $processes = Get-InstallProcesses -InstallRoot $InstallRoot
    $details = ($processes | ForEach-Object { "$($_.ProcessId): $($_.ExecutablePath)" }) -join [Environment]::NewLine
    Write-Warning "Timed out waiting for validation install processes to exit before uninstall:$([Environment]::NewLine)$details"
}

function Remove-UserPathEntry {
    param(
        [Parameter(Mandatory = $true)]
        [string] $Entry
    )

    $currentPath = [Environment]::GetEnvironmentVariable("PATH", [EnvironmentVariableTarget]::User)
    if ([string]::IsNullOrEmpty($currentPath)) {
        return
    }

    $normalizedEntry = [IO.Path]::TrimEndingDirectorySeparator($Entry.Trim().Trim('"'))
    $remainingEntries = @($currentPath.Split(";", [StringSplitOptions]::RemoveEmptyEntries) |
        Where-Object {
            $normalizedPathEntry = [IO.Path]::TrimEndingDirectorySeparator($_.Trim().Trim('"'))
            -not $normalizedPathEntry.Equals($normalizedEntry, [StringComparison]::OrdinalIgnoreCase)
        })
    $updatedPath = $remainingEntries -join ";"

    if (-not $updatedPath.Equals($currentPath, [StringComparison]::Ordinal)) {
        [Environment]::SetEnvironmentVariable("PATH", $updatedPath, [EnvironmentVariableTarget]::User)
    }
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

    $wpOutput = & $tuiExe views 2>&1
    if ($LASTEXITCODE -ne 0) {
        throw "Installed wp.exe failed to start with exit code ${LASTEXITCODE}:$([Environment]::NewLine)$($wpOutput -join [Environment]::NewLine)"
    }

    $startMenuRoot = Join-Path $env:APPDATA "Microsoft\Windows\Start Menu\Programs"
    $packageShortcuts = @()
    $startMenuShortcuts = @()
    Wait-ForCondition `
        -Condition {
            $script:packageShortcuts = @($shortcutRoots |
                    ForEach-Object { Get-ShortcutDetails -Root $_ -InstallRoot $installRoot } |
                    Where-Object { $_.PointsAtInstall })
            $script:startMenuShortcuts = @($script:packageShortcuts |
                    Where-Object { $_.FullName.StartsWith($startMenuRoot, [StringComparison]::OrdinalIgnoreCase) })
            $script:startMenuShortcuts.Count -gt 0
        } `
        -FailureMessage "Timed out waiting for a Start Menu shortcut to be created for install root '$installRoot'."

    if ($packageShortcuts.Count -eq 0) {
        throw "No shortcuts were created for install root '$installRoot'."
    }

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
    # Cleanup is best-effort — a teardown hiccup must not fail the release after the
    # shortcut validation above has already passed.
    try {
        Remove-InstallArtifacts -InstallRoot $installRoot -ShortcutRoots $shortcutRoots
    }
    catch {
        Write-Warning "Install-artifact cleanup failed (non-fatal): $_"
    }
}

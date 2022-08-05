# Build and Deploy

## Getting source ready

* `git config --global status.submoduleSummary true`
* `git clone tig:tig/winprint`
* `git submodule update --init --recursive`

# Pre-reqs

* VS 2022+
* Right verison of .NET Core (e.g. `winget install Microsoft.dotnetRuntime.6-x64` )
* Python: `winget install python`
* Pygments: `pip install Pygments`

# Enable telemtry/logging key

* Right click on `winprint\src\WinPrint.Core\Services\TelemetryService.tt` and "Run Custom Tool" to run T4 compiler

# Versions

* Managed by `msbump` https://github.com/BalassaMarton/MSBump
* Prime version is stored in `Winprint.Core.dll` via `src\WinPrint.Core\WinPrint.Core.csproj`: `<Version>2.1.0.0</Version>`
  * Must manually bump `major.minor.rel` in `WinPrint.WinForms.csproj`, `WinPrint.Console.csproj`, and `Winprint.LiteHtml.csproj` before release

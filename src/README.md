# Build

## Getting source ready

* `git config --global status.submoduleSummary true`
* `git clone --recurse-submodules -j8 tig:tig/winprint`

# Pre-reqs

* VS 2022+
* Python/Pygments are only needed when using the legacy `AnsiCte` highlighter.

# Enable telemtry/logging key

* Right click on `winprint\src\WinPrint.Core\Services\TelemetryService.tt` and "Run Custom Tool" to run T4 compiler
* MS ApplicationInsights used to be in the Kindel account, but is no longer there!

# Versions

* Versioning is handled by GitVersion from repository history and tags.

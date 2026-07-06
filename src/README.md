# Build

For full setup, build, test, and debug instructions, see [`../CONTRIBUTING.md`](../CONTRIBUTING.md).
That is the authoritative contributor guide; the notes below cover only source-tree specifics.

## Getting source ready

* `git config --global status.submoduleSummary true`
* `git clone --recurse-submodules -j8 tig:tig/winprint`

# Pre-reqs

* **.NET 10 SDK** (pinned by [`../global.json`](../global.json)) — winprint is VS Code-oriented;
  see [`../CONTRIBUTING.md`](../CONTRIBUTING.md) for the full prerequisites.
* `AnsiCte` uses a vendored, managed `libvt100` ANSI decoder — **no Python/Pygments required**.

# Versions

* Versioning is handled by GitVersion from repository history and tags.

# winprint — agent notes

Cross-platform source/document printing engine. .NET 10. The Markdown work and the
cross-platform CTE rendering refactor are described below because the "why" isn't
obvious from the code alone.

## Projects
- `src/WinPrint.Core` — engine. **Multi-targets `net10.0` and `net10.0-windows`.** The
  `WINDOWS` constant is defined for the `-windows` TFM only.
- `src/WinPrint.TUI` — Terminal.Gui front end and `wp` command.
- `src/WinPrint.Maui` — MAUI app (Windows/MacCatalyst; needs the `maui` workload — does
  **not** build on Linux).
- `tests/WinPrint.Core.UnitTests` — xUnit. Targets `net10.0-windows` but most tests run
  on Linux too (see below).
- Solution: `WinPrint.slnx`.

## Build & test
```bash
dotnet build src/WinPrint.Core/WinPrint.Core.csproj          # builds both TFMs
dotnet test  tests/WinPrint.Core.UnitTests/WinPrint.Core.UnitTests.csproj
dotnet test  ... --filter "FullyQualifiedName~CteRenderingTests"   # single class
```
CI (`.github/workflows/ci.yml`) runs on **windows-latest**, installs the `maui`
workload, builds `WinPrint.slnx`, then enforces a **style gate**. **Always run the gate
locally and get a clean tree before you push any C# change** — a failed style gate is the
single most common way agents waste a push/CI round-trip. There is exactly one command:

```bash
scripts/verify-style.sh                                            # whole solution
scripts/verify-style.sh src/WinPrint.Core/WinPrint.Core.csproj     # one project (Linux/Mac: MAUI won't build)
```

`scripts/verify-style.sh` **is** the CI gate (CI calls it via `--ci`), so a clean run there
means a clean run in CI. **Do not** hand-run bare `dotnet jb cleanupcode` / `dotnet format`:
they omit the exact profile/exclude flags CI uses (`--profile="WinPrintCleanup"
--exclude="**/*.xaml.cs"`), so they "pass" locally and still fail CI. If the script rewrites
files, commit those changes — that *is* the fix; re-run until `git diff` is empty. On Linux/Mac
(where `WinPrint.Maui` can't build) scope the script to the project(s) you touched; the full
solution gate over MAUI files is still verified by Windows CI. Code-style analyzers also enforce
**one top-level type per file** (WPA0001) and **no nested types** (WPA0002).

**CI mechanics & flakiness (don't mistake a flake for a real failure).** Every PR triggers
CI **twice** (a `push` run and a `pull_request` run). `maui-ui-tests` (Appium) and the
FlaUI / TUI-golden suites are **flaky**: the tell is the *same commit* passing in one of the
two sibling runs and failing in the other. Before chasing such a failure, confirm it's not the
flake; to clear it, `gh run rerun --failed <run-id>` — but you **can't** re-run a job while its
sibling workflow is still in progress (it's rejected), so wait for both to settle first.

## Remote (Claude Code on the web) environment
Fresh Linux containers have no toolchain. `.claude/hooks/session-start.sh` (registered
in `.claude/settings.json`) installs the .NET 10 SDK, `libgdiplus`, the local `jb` tool,
and warms NuGet restore. `global.json` pins .NET 10. The hook runs only when
`CLAUDE_CODE_REMOTE=true`.

## Release & distribution (read before cutting a release)
**Cutting a release.** Merge `develop` → `main`, create an **annotated** tag `vX.Y.Z` on the
merge commit, and `git push` the tag — that triggers `.github/workflows/release.yml`. There is
no release script; tags are manual. The pushed tag **also** triggers
`.github/workflows/back-merge.yml`, which opens a PR merging `main` back into `develop` — **merge
it** so `develop` doesn't silently drift behind `main` (it once fell ~43 commits behind). The tag
drives the brew/winget version; GitVersion drives the
Velopack `packVersion` — they coincide on a tagged commit. A pre-release label (`v…-rc.1`)
publishes as a GitHub *pre-release* (not "Latest"). A burned tag (release failed) can't be reused
— bump to the next patch. **A green release run can still mean "didn't publish":** if any
`Package <rid>` job fails, `Publish` / `winget` / `brew` are **skipped** and nothing ships, even
though the overall run may look done — always confirm a real GitHub release + tap update exist.

**Windows code signing.** Windows installers are signed with **Azure Trusted Signing** via
**GitHub OIDC** (no client secret). The full, reproducible setup lives in `scripts/`
(`Azure.Config.ps1` = single source of truth, `SetupAzure.ps1` = idempotent one-shot creator,
`ValidateAzure.ps1` = verifier) and is documented in **`docs/code-signing.md`**. An authorized
operator recreates the CI trust with `az login && pwsh scripts/SetupAzure.ps1 -SetGitHubSecrets`.
The Trusted Signing account + PublicTrust cert profile are a one-time **manual** prerequisite
(identity validation can't be scripted); everything else is automated. Read `docs/code-signing.md`
before touching signing.

**Windows package layout — a debugging gotcha.** The win-x64 Velopack package **co-locates
`wp.exe` (TUI) with `winprint.exe` (MAUI GUI)** in one folder: `Publish TUI` and `Publish Windows
GUI` write into the *same* dir, so the GUI's `net10.0-windows` assemblies (e.g. `WinPrint.Core.dll`)
**overwrite** the TUI's `net10.0` ones. So on Windows `wp.exe` runs against *different* DLLs than on
macOS/Linux (where `wp` ships standalone). **Windows-only `wp` failures often can't be reproduced on
a Mac** — don't conclude "works locally ⇒ fine." The release job installs the real `Setup.exe` and
smoke-runs the packaged `wp.exe` (`scripts/Test-WindowsVelopackShortcut.ps1`, currently
`wp --version`) precisely to catch this class of bug.

**Homebrew (the free distribution path).** Tap = `kindel/homebrew-winprint`, pushed by the release
`brew` job (needs the `HOMEBREW_TAP_TOKEN` PAT; a missing-token skip now *fails* loudly). TWO
artifacts in the tap:
- **Formula** `wp` → the `wp` TUI (Linux + CLI-only macOS).
- **Cask** `winprint` → the MAUI GUI, which **also embeds `wp`** at `WinPrint.app/Contents/Helpers/wp`
  (release.yml copies the self-contained CLI in *before* signing; a `binary` stanza symlinks it onto
  PATH), so one cask install gives GUI + `wp`. Both provide `wp`, so installing formula + cask
  collides on the symlink — pick one on macOS (casks **cannot** declare `conflicts_with formula:`).
- **The `wp` formula carries an `x86_64_linux` bottle** (issue #211). `wp` compiles nothing — the
  "install" just extracts the prebuilt AOT tarball — but Homebrew treats a *bottle-less* formula as a
  source build and **refuses it on any host with no C compiler** (fresh containers, minimal WSL). The
  bottle makes `brew install` *pour* a prebuilt tree, so toolchain-less Linux installs cleanly. Bottles
  are **formula-only**, so this does **not** touch the cask or the GUI+TUI dual install. The `brew` job
  source-builds the bottle on ubuntu-latest (it has gcc), uploads `wp-<ver>.x86_64_linux.bottle.tar.gz`
  to the release, renders its SHA into the formula, then **pour-tests** it (guards on `Pouring`) before
  pushing. **Only `x86_64_linux` is tagged**: every other platform has NO tag and keeps installing from
  the `url` blocks (macOS has Clang; arm64 Linux source-builds). **Never declare a bottle tag without
  publishing + pour-testing its file** — a declared-but-missing bottle hard-fails that platform with no
  source fallback. (arm64 Linux bottle = follow-up; needs bottling on an arm64 runner.)
  **Two hard-won gotchas (they left the tap stuck at 3.0.5 while releases reached 3.0.9 —
  the bottle step crashed the v3.0.6/v3.0.8/v3.0.9 brew jobs deterministically; v3.0.7's was
  skipped for unrelated reasons):** (1) the formula's `install` **must drop `wp.dbg`**
  (`rm_f Dir["*.dbg"]`). Homebrew's Linux install *and* pour scan every ELF in the keg
  (`load_tab` → `undeclared_runtime_dependencies` → `LinkageChecker`), and the vendored
  `elftools` crashes — or worse, **hangs** — on that 48 MB AOT debug file (`undefined method
  'header' for nil`) — the *actual* cause of the failed bottle job, not rpath relocation.
  `wp` itself pours fine; the `.dbg` is useless to users anyway. (2) the bottle build is now
  **best-effort**: it runs in an isolated subshell with `timeout`-bounded brew calls (a hang
  would otherwise outlive the job and strand the tap anyway), and on any failure the job
  publishes a **bottle-less** formula so the formula+cask tap push still lands. Never gate
  the tap update on the (fragile) bottle again.
- **Validate a cask by LOADING it, never `ruby -c`.** `ruby -c` checks Ruby *syntax* and happily
  passes invalid cask **DSL** (e.g. `conflicts_with formula:` — that key is cask-only-`cask:`), which
  once shipped a tap cask Homebrew couldn't parse and broke `brew install --cask` for everyone. The
  release `brew` job renders into a throwaway tap and `brew info --cask/--formula <name>` them before
  publishing (`brew audit [path]` is disabled; loading by name is the reliable check) — keep that guard.

**winget (the Windows package channel).** `Kindel.WinPrint` lives in microsoft/winget-pkgs
(bootstrapped manually via winget-pkgs PR #391003 at 2.8.6 — `winget-releaser` only *updates* an
existing package, so that first submission could not be automated). The release `winget` job
auto-submits a version PR on every stable release (needs `WINGET_TOKEN`; a missing token *fails*
loudly, like brew). **win-x64 only** today (win-arm64 Velopack leg is still experimental). Scoop
support (bucket `kindel/scoop-winprint`, `SCOOP_BUCKET_TOKEN`, `packaging/scoop/`) was removed once
winget was proven working on v3.0.4 — don't re-add it.

**macOS signing — don't mistake for a regression.**
- **macOS is signed + notarized:** the Apple signing secrets (`APPLE_*`) **are** configured, so the
  release pipeline signs the `.app` with an Apple Developer ID and notarizes + staples it (the
  `Sign, notarize, and zip macOS GUI` step). Gatekeeper accepts the cask normally — **no**
  quarantine/`xattr` workaround. (Set up in the 2.8.x line; #162.)
- **Per-arch cask size differs by design:** `maccatalyst-arm64` is Mono-**AOT** (~130 MB), `-x64` is
  Mono-**JIT** (~35 MB). The SDK gates AOT to `maccatalyst-arm64` only (the `RunAOTCompilation`
  property is ignored for MacCatalyst); see the comment in `release.yml`. Not a broken x64 build.

## Hero GIFs (README/docs marketing — read before regenerating)
The README and `docs/index.md` lead with one hero GIF per front end (TUI, headless print,
GUI). They are **marketing**: each must show the front end *being driven*, not a static page.
The full spec — what each hero must show off, the producer scripts, and the sample file — is
**[`docs/hero-gifs.md`](docs/hero-gifs.md)**. Key rules:
- **GUI heroes must drive settings + zoom/pan + open a 2nd file** (toggle Line Numbers,
  toggle Landscape, fast zoom→pan→reset, then open a different file), mirroring the TUI hero's
  energy. The old macOS page/page/arrow choreography is the weak baseline — **don't copy it.**
- All heroes render the **same** sample (`src/WinPrint.Core/ViewModels/SheetViewModel.cs`).
- **Windows GUI:** `scripts/capture-gui-hero-windows.ps1` (drives `winprint.exe`, needs an
  **unlocked interactive session**) → `scripts/assemble-gui-hero.py` → `docs/hero-gui-win.gif`.
  Zoom uses the plain TUI-consistent keys (`=`/`+` in, `-` out, `0` fits); `OnNativeKeyDown`
  normalizes the WinUI `VirtualKey` strings (`"187"`/`"189"`/`"Number0"`) so they route on
  Windows (PR #199 added plain zoom keys but only built MacCatalyst). README shows Windows +
  macOS side by side.

## Content Type Engines (CTEs)
CTEs live in `src/WinPrint.Core/ContentTypeEngines` and derive from
`ContentTypeEngineBase`. They are discovered via an **explicit registry**
(`ContentTypeEngineRegistry`, AOT/trim-safe explicit factories) — `GetDerivedClassesCollection`
returns `ContentTypeEngineRegistry.CreateAll()`, and `CreateContentTypeEngine` then matches an
engine by `SupportedContentTypes`. This replaced the old reflection scan (`GetTypes()` +
`Activator.CreateInstance`); the static `Create()` factories have no production callers. Engines:
- `TextCte` (`text/plain`), `MarkdownCte` (`text/x-markdown`, subclasses `TextCte` and
  flattens Markdown via Markdig), `TextMateCte` (syntax highlighting; the default),
  `AnsiCte` (`text/ansi`; decodes ANSI escape sequences via the vendored managed `libvt100`)
  and `HtmlCte` (`text/html` plus `.mhtml`/`.mht`; lays out HTML/CSS via the managed HtmlRenderer,
  with `http(s)` assets gated behind `AllowRemoteResources`).

### Cross-platform rendering — the key design
`System.Drawing.Common` is **Windows-only** (it P/Invokes GDI+/`gdiplus.dll`, absent on
Linux). To keep the engines cross-platform, **all drawing and measurement goes through
the `IGraphicsContext` abstraction** (`src/WinPrint.Core/Abstractions`), never
`System.Drawing` directly:
- `PaintPage(IGraphicsContext, ...)` paints through the abstraction.
- `RenderAsync` reflow/measurement uses `ContentTypeEngineBase.ResolveMeasurementContext`,
  which returns the injected `MeasurementContext` if set, else builds the Windows default
  (`WindowsMeasurementContext`, the only Windows-gated CTE file).
- `ContentTypeEngineBase.StringFormat` (a `System.Drawing` object) is **lazily**
  initialized so the type can load without GDI+ (e.g. the Windows-targeted test assembly
  running on Linux).
- `System.Drawing.Color`/`ColorTranslator` are managed (no GDI+) and are fine on any OS.

`TextCte`, `MarkdownCte`, and `TextMateCte` are no longer Windows-gated. Tests inject a
`RecordingGraphicsContext` (in `tests/.../TestSupport`) — a GDI+-free `IGraphicsContext`
double with a deterministic fixed-pitch measurement model — so the full
`SetDocument → RenderAsync → PaintPage` pipeline is verified cross-platform
(`CteRenderingTests`).

## Important caveats
- **Markdig is fully cross-platform** — it was never the platform constraint; the old
  Windows gating was due to `System.Drawing` in the render path.
- **The Windows `System.Drawing` measurement path is verified by CI (windows-latest),
  not on Linux.** This container can't load GDI+, so the production measurement math is
  preserved by construction and guarded by the older `TextCteTests`/`TextMateCteTests`
  which run on Windows CI.
- On Linux a handful of tests fail for **environment** reasons unrelated to logic: the
  older CTE render tests that construct real `System.Drawing` objects in test code,
  `PrintMarginsRegressionTests` (P/Invokes `USER32.dll`), and the Pygments/date-macro
  tests (external tooling / filesystem). The cross-platform `CteRenderingTests` and
  `MarkdownCteTests` pass on Linux.

## Native AOT roadmap (implementation in flight — verify before acting)
Goal: ship **`WinPrint.TUI`/`wp` as Native AOT** with **`WinPrint.Core` AOT/trim-compatible**.
`WinPrint.Maui` is **out of scope** — MAUI does not support Native AOT.

> **Largely landed (#66 closed; PR #156 merged).** `<IsAotCompatible>true</IsAotCompatible>` is set on
> `WinPrint.Core` (trim analyzers run on every build) and RID-gated `<PublishAot>true</PublishAot>` is
> set on `WinPrint.TUI` (active only when `RuntimeIdentifier` is set). The Macros hand-rolled resolver,
> explicit CTE registry, source-gen JSON, and `WinPrintServices` DI container are in. CI `aot-publish`
> gates trim regressions per RID. The "decisions" below are the design record those changes follow.

Decisions made (design record; most are now implemented):
- **Status: complete.** `<IsAotCompatible>` on Core and RID-gated `<PublishAot>` on the TUI are on main;
  CI `aot-publish` is green. Call sites use `WinPrintServices` directly (#215).
- **Target = cross-platform AOT** (Windows/Linux/macOS), not Windows-only. This requires a
  **non-`System.Drawing` measurement backend** (e.g. SkiaSharp) plugged into the existing
  `IGraphicsContext`/`MeasurementContext` seam — `System.Drawing` stays the Windows default.
- **DI: drop MvvmLight `SimpleIoc`** — **done** (`WinPrintServices` replaces SimpleIoc and the former
  `ModelLocator`/`ServiceLocator` facades; #215).
- **TUI arg parsing stays on `Terminal.Gui.Cli`** (vet Terminal.Gui itself for AOT/trim).
- **`Macros.cs`: rewrite with a hand-rolled resolver**, removing **`System.Linq.Dynamic.Core`**
  (runtime expression compiling — the one hard AOT blocker). May narrow exotic macro syntax to
  what's actually used.
- **JSON: move to source-generated `System.Text.Json`** (`WinPrintJsonSerializerContext`) — **done**.
- **Update-check: redesign from the ground up and remove `Octokit`** — **done**.
- **`CommandLineParser`: remove if unused** (verify no real callers, then drop the package).

Other AOT work from the original spike:
- `ModelBase.CopyPropertiesFrom` + telemetry — **done** (explicit per-type copies; no reflection).
- CTE discovery reflection — **done**: replaced by the explicit `ContentTypeEngineRegistry`
  (was `GetTypes()` + `Activator.CreateInstance` in `ContentTypeEngineBase`).

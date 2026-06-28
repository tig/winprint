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
workload, builds `WinPrint.slnx`, then enforces a **style gate**:
`dotnet jb cleanupcode` + `dotnet format` with `git diff --exit-code`. Run those
before pushing if you touched many files. Code-style analyzers also enforce
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
- **Formula** `winprint` → the `wp` TUI (Linux + CLI-only macOS).
- **Cask** `winprint` → the MAUI GUI, which **also embeds `wp`** at `WinPrint.app/Contents/Helpers/wp`
  (release.yml copies the self-contained CLI in *before* signing; a `binary` stanza symlinks it onto
  PATH), so one cask install gives GUI + `wp`. Both provide `wp`, so installing formula + cask
  collides on the symlink — pick one on macOS (casks **cannot** declare `conflicts_with formula:`).
- **Validate a cask by LOADING it, never `ruby -c`.** `ruby -c` checks Ruby *syntax* and happily
  passes invalid cask **DSL** (e.g. `conflicts_with formula:` — that key is cask-only-`cask:`), which
  once shipped a tap cask Homebrew couldn't parse and broke `brew install --cask` for everyone. The
  release `brew` job renders into a throwaway tap and `brew info --cask/--formula <name>` them before
  publishing (`brew audit [path]` is disabled; loading by name is the reliable check) — keep that guard.

**Scoop (the approval-free Windows path).** Bucket = `kindel/scoop-winprint`, pushed by the release
`scoop` job (needs `SCOOP_BUCKET_TOKEN`; a missing token *fails* loudly, like brew). Unlike winget it
needs no Microsoft moderation. The job renders `packaging/scoop/winprint.json` and pushes it to
`bucket/winprint.json`. Source artifact is the Velopack **Portable** zip
(`Kindel.WinPrint-win-x64-Portable.zip`): root holds the stub launcher `WinPrint.exe`; `current/`
holds the real `winprint.exe` (GUI) + `wp.exe` (TUI), so the manifest shims `wp` from
`current\wp.exe` and shortcuts the root `WinPrint.exe` — one install gives GUI + `wp`, like the cask.
**win-x64 only** today (win-arm64 Velopack leg is still experimental); see `packaging/scoop/README.md`.

**winget & macOS notarization — known gaps (don't mistake for regressions).**
- **winget:** `winget-releaser` only *updates* an existing winget-pkgs package, so the **first**
  submission must be bootstrapped manually. Until then the `winget` job **failing is expected**.
- **macOS is not notarized:** the Apple signing secrets (`APPLE_*`) aren't set, so the cask ships an
  unsigned/ad-hoc `.app` (Gatekeeper warns; 3rd-party-tap casks also need `brew trust --cask`).
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

> **Largely landed (the PR #156 work has merged to main).** `<IsAotCompatible>true</IsAotCompatible>`
> is set on `WinPrint.Core` (trim analyzers run on every build) and RID-gated `<PublishAot>true</PublishAot>`
> is set on `WinPrint.TUI` (active only when `RuntimeIdentifier` is set). The Macros hand-rolled
> resolver, explicit CTE registry, source-gen JSON, and `WinPrintServices` DI container are in.
> The per-item AOT/trim inventory lives in **[`docs/aot-inventory.md`](docs/aot-inventory.md)**; the
> "decisions" below are the design record those changes follow.

Decisions made (design record; most are now implemented):
- **Status: in flight / largely landed.** `<IsAotCompatible>` on Core and RID-gated `<PublishAot>`
  on the TUI are already on main, and most inventory items are cleared. Track remaining work in
  [`docs/aot-inventory.md`](docs/aot-inventory.md).
- **Target = cross-platform AOT** (Windows/Linux/macOS), not Windows-only. This requires a
  **non-`System.Drawing` measurement backend** (e.g. SkiaSharp) plugged into the existing
  `IGraphicsContext`/`MeasurementContext` seam — `System.Drawing` stays the Windows default.
- **DI: drop MvvmLight `SimpleIoc`** (`ModelLocator`/`ServiceLocator`) in favor of **manual
  construction**. MvvmLight is unmaintained and not trim-annotated. **Migration is partial:**
  `WinPrintServices` exists, but `ModelLocator` is still used in `WinPrint.Core` (e.g.
  `ContentTypeEngineBase`, view models) — not yet fully removed.
- **TUI arg parsing stays on `Terminal.Gui.Cli`** (vet Terminal.Gui itself for AOT/trim).
- **`Macros.cs`: rewrite with a hand-rolled resolver**, removing **`System.Linq.Dynamic.Core`**
  (runtime expression compiling — the one hard AOT blocker). May narrow exotic macro syntax to
  what's actually used.
- **JSON: move to source-generated `System.Text.Json`** (`JsonSerializerContext`); also replace
  the reflection-based `Microsoft.Extensions.Configuration` `.Bind()` path.
- **Update-check: redesign from the ground up and remove `Octokit`** (reflection/JSON-heavy).
- **`CommandLineParser`: remove if unused** (verify no real callers, then drop the package).

Other known AOT work (fall out of the spike; see [`docs/aot-inventory.md`](docs/aot-inventory.md)
for current per-item state):
- `ModelBase.CopyPropertiesFrom` + `TypeDescriptor.GetProperties` / `GetType().GetProperties()` —
  annotate with `[DynamicallyAccessedMembers]` or replace with generated/explicit copies.
- CTE discovery reflection — **done**: replaced by the explicit `ContentTypeEngineRegistry`
  (was `GetTypes()` + `Activator.CreateInstance` in `ContentTypeEngineBase`).

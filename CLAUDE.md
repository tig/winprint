# winprint — agent notes

Cross-platform source/document printing engine. .NET 10. The Markdown work and the
cross-platform CTE rendering refactor are described below because the "why" isn't
obvious from the code alone.

## Projects
- `src/WinPrint.Core` — engine. **Multi-targets `net10.0` and `net10.0-windows`.** The
  `WINDOWS` constant is defined for the `-windows` TFM only.
- `src/WinPrint.cli` — CLI.
- `src/WinPrint.WinForms` — Windows print UI.
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
### Local Terminal.Gui #5493 build (per-machine env vars — do NOT hard-code)
`src/WinPrint.TUI` consumes a **locally-built** Terminal.Gui from the #5493 PR worktree
(Kitty graphics). The feed path and the package version differ per machine (different
worktree locations / branch-derived version strings), so they are **not** committed —
hard-coding either one caused Mac⇄Windows build fights on every pull. Instead each
machine sets two environment variables (persisted in the shell profile):
- `WINPRINT_TG_FEED` — absolute path to the local `Terminal.Gui/bin/Debug`
  (consumed by `nuget.config`).
- `WINPRINT_TG_VERSION` — the version that worktree built (consumed by
  `Directory.Build.props` → `WinPrintTgVersion` → the TUI `PackageReference`).

If unset, the TUI build fails fast with an actionable message (see the
`_CheckWinPrintTgEnv` target). **Never** put a machine-specific path or version back
into `nuget.config` / the `.csproj` — set the env vars instead.

CI (`.github/workflows/ci.yml`) runs on **windows-latest**, installs the `maui`
workload, builds `WinPrint.slnx`, then enforces a **style gate**:
`dotnet jb cleanupcode` + `dotnet format` with `git diff --exit-code`. Run those
before pushing if you touched many files. Code-style analyzers also enforce
**one top-level type per file** (WPA0001) and **no nested types** (WPA0002).

## Remote (Claude Code on the web) environment
Fresh Linux containers have no toolchain. `.claude/hooks/session-start.sh` (registered
in `.claude/settings.json`) installs the .NET 10 SDK, `libgdiplus`, the local `jb` tool,
and warms NuGet restore. `global.json` pins .NET 10. The hook runs only when
`CLAUDE_CODE_REMOTE=true`.

## Content Type Engines (CTEs)
CTEs live in `src/WinPrint.Core/ContentTypeEngines` and derive from
`ContentTypeEngineBase`. They are discovered by reflection via `SupportedContentTypes`
(`CreateContentTypeEngine`), **not** by the static `Create()` factories (which have no
production callers). Engines:
- `TextCte` (`text/plain`), `MarkdownCte` (`text/x-markdown`, subclasses `TextCte` and
  flattens Markdown via Markdig), `TextMateCte` (syntax highlighting; the default),
  `AnsiCte`/`HtmlCte` (stubs after their native deps were removed).

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

## Native AOT roadmap (tracked — NOT yet implemented)
Goal: ship **`WinPrint.cli` as Native AOT** with **`WinPrint.Core` AOT/trim-compatible**.
`WinPrint.WinForms` and `WinPrint.Maui` are **out of scope** — neither supports Native AOT.

Decisions made (record of intent; revisit when work actually starts):
- **Status: track only for now.** No AOT code changes yet. When starting, the first step is a
  *spike*: set `<IsAotCompatible>true</IsAotCompatible>` on Core + `<PublishAot>true</PublishAot>`
  on the CLI, build, and collect the trim/AOT analyzer warnings into an inventory to size the work.
- **Target = cross-platform AOT** (Windows/Linux/macOS), not Windows-only. This requires a
  **non-`System.Drawing` measurement backend** (e.g. SkiaSharp) plugged into the existing
  `IGraphicsContext`/`MeasurementContext` seam — `System.Drawing` stays the Windows default.
- **DI: drop MvvmLight `SimpleIoc`** (`ModelLocator`/`ServiceLocator`) in favor of **manual
  construction**. MvvmLight is unmaintained and not trim-annotated.
- **CLI arg parsing stays on `Terminal.Gui.Cli`** (vet Terminal.Gui itself for AOT/trim).
- **`Macros.cs`: rewrite with a hand-rolled resolver**, removing **`System.Linq.Dynamic.Core`**
  (runtime expression compiling — the one hard AOT blocker). May narrow exotic macro syntax to
  what's actually used.
- **JSON: move to source-generated `System.Text.Json`** (`JsonSerializerContext`); also replace
  the reflection-based `Microsoft.Extensions.Configuration` `.Bind()` path.
- **Update-check: redesign from the ground up and remove `Octokit`** (reflection/JSON-heavy).
- **`CommandLineParser`: remove if unused** (verify no real callers, then drop the package).

Other known AOT work (fall out of the spike):
- `ModelBase.CopyPropertiesFrom` + `TypeDescriptor.GetProperties` / `GetType().GetProperties()` —
  annotate with `[DynamicallyAccessedMembers]` or replace with generated/explicit copies.
- CTE discovery (`GetTypes()` + `Activator.CreateInstance` in `ContentTypeEngineBase`) — root the
  engine types or replace reflection with an explicit registry.


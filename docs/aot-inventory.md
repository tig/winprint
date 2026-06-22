# Native AOT inventory (issue #66, PR 0 spike)

Spike date: 2026-06-21  
Target: `WinPrint.TUI` (`wp`) + `WinPrint.Core`, cross-platform (`net10.0` / `net10.0-windows`).

## Infrastructure changes (this PR)

| Change | File |
|--------|------|
| Stop `PublishAot` leaking to analyzer project | `Directory.Build.props` — added `PublishAot;IsAotCompatible` to `GlobalPropertiesToRemove` |
| Native AOT publish flags (RID-gated) | `WinPrint.TUI.csproj` — `PublishAot`, `StripSymbols`, `InvariantGlobalization` active only when `RuntimeIdentifier` is set (so `dotnet build` / `dotnet test` stay clean) |
| `IsAotCompatible` on Core | **Deferred** — setting it in .NET 10 SDK enables trim analyzers on every build and currently yields 35 errors. Add `<IsAotCompatible>true</IsAotCompatible>` to `WinPrint.Core.csproj` once the inventory below is cleared. |

## Spike command

Full inventory (all 35 IL diagnostics — requires `IsAotCompatible` on the dependency graph):

```bash
dotnet publish src/WinPrint.TUI/WinPrint.TUI.csproj \
  -c Release -r osx-arm64 -f net10.0 --self-contained true \
  /p:IsAotCompatible=true \
  -o artifacts/aot-spike/osx-arm64
```

Without `/p:IsAotCompatible=true`, only the TUI entry-point issues surface (currently `Program.cs` IL3000).

Full log: `artifacts/aot-spike/osx-arm64.log`

## Spike result

**Build failed at `WinPrint.Core` compile** (35 IL analyzer diagnostics, promoted to errors by `TreatWarningsAsErrors`). The TUI project and Native AOT linker were never reached.

| IL code | Count | Meaning |
|---------|-------|---------|
| IL2026 | 15 | `RequiresUnreferencedCode` — trim-unsafe API |
| IL3050 | 13 | `RequiresDynamicCode` — needs runtime codegen (AOT blocker) |
| IL3000 | 3 | `Assembly.Location` empty under single-file/AOT |
| IL2075 | 2 | `GetType().GetProperties()` — DAM annotation mismatch |
| IL2072 | 2 | `Activator.CreateInstance(Type)` — DAM annotation mismatch |

No warnings were emitted from `WinPrint.TUI`, `Terminal.Gui`, or third-party packages — compile did not get that far.

## Findings by work item

### P1 — Macros (`System.Linq.Dynamic.Core`) — DONE

Replaced `DynamicExpressionParser` with explicit lookup over `MacroChoices.Names` (15 properties). `System.Linq.Dynamic.Core` removed from `WinPrint.Core.csproj`.

### P2 — CTE registry (`GetTypes` + `Activator.CreateInstance`) — DONE

Replaced assembly scan with explicit `ContentTypeEngineRegistry` (AnsiCte, HtmlCte, MarkdownCte, TextCte, TextMateCte).

### P3 — Source-generated JSON + drop config `.Bind()`

| File | Line(s) | Codes | API |
|------|---------|-------|-----|
| `SettingsService.cs` | 34 | IL3050 | `JsonStringEnumConverter` (non-generic) |
| `SettingsService.cs` | 153 | IL2026, IL3050 | `IConfiguration.Bind` |
| `SettingsService.cs` | 245 | IL2026, IL3050 | `JsonSerializer.Serialize<Settings>` |
| `FileTypeMappingService.cs` | 41 | IL2026, IL3050 | `JsonSerializer.Deserialize<FileTypeMapping>` |
| `SheetDefinitionChangeTracker.cs` | 168, 186, 191-194, 199 | IL2026, IL3050 | JSON clone round-trips |
| `ModelBase.cs` | 31, 55, 59 | IL2026, IL3050 | `JsonSerializer.Serialize(object, GetType())` |

- Fix: `WinPrintJsonContext` (`JsonSerializerContext`), replace `.Bind()` with STJ deserialize
- Replace `JsonStringEnumConverter` with `JsonStringEnumConverter<TEnum>` per enum type
- Drop `Microsoft.Extensions.Configuration.*` from Core when `.Bind()` is gone

### P4 — `ModelBase` reflection (`CopyPropertiesFrom`, telemetry)

| File | Line(s) | Codes | API |
|------|---------|-------|-----|
| `ModelBase.cs` | 44, 46 | IL2026 | `TypeDescriptor.GetProperties`, `AttributeCollection.Contains` |
| `ModelBase.cs` | 83, 84 | IL2075 | `GetType().GetProperties()` |
| `ModelBase.cs` | 104 | IL2072 | `Activator.CreateInstance(destProp.PropertyType)` |

- Fix: explicit per-type copy methods or JSON round-trip via source-gen context

### P5 — `Assembly.Location` under AOT (IL3000) — DONE (Core + TUI)

Added `AppHostInfo` (`AppContext.BaseDirectory`, assembly attributes for version/company/product). Updated `SettingsService`, `LogService`, `UpdateService`, `TelemetryService`, `WinPrint.TUI/Program.cs`.

### P6 — MvvmLight `SimpleIoc` (no IL warnings yet)

`ServiceLocator` / `ModelLocator` use `GalaSoft.MvvmLight.Ioc.SimpleIoc` — not flagged at compile time but unmaintained and not trim-annotated. Expect link-time trimming issues once Core compiles.

- Fix: `WinPrintServices` with explicit manual construction (PR 4)

### P7 — CommandLineParser on `Options` (no IL warnings)

`Options.cs` carries `[Option]` attributes for WinForms/MAUI only. TUI uses `Terminal.Gui.Cli` + `WinPrintOptions` catalog. Package can leave Core once Options is a plain DTO.

### P8 — Cross-platform measurement — DONE (TUI wiring)

`SettingsContext` now injects `printService.CreateMeasurementContext()` (Skia on Unix; Windows uses GDI default via null). `PageRenderer` / ImageSharp remain preview-only.

### P9 — Terminal.Gui

Confirmed AOT-compatible by upstream. No diagnostics in this spike (TUI did not compile). No action beyond normal publish verification once Core is clean.

### P10 — Velopack / TextMateSharp / HtmlRenderer

No compile-time IL warnings in this spike. May surface at link time once Core builds — track during PR 8 smoke tests.

## Known non-blockers

- `libvt100/InvalidByteException.cs` — SYSLIB0051 obsolete serialization ctor (warning only, not IL)
- `WinPrint.cli` — out of scope (being removed); ignore its `Assembly.Location` usages

## Recommended fix order (unchanged from plan)

1. **P1 Macros** — only silent hard blocker (no analyzer coverage)
2. **P2 CTE registry** — clears 2 errors, unblocks engine discovery
3. **P3 JSON + config** — clears ~20 errors (largest batch)
4. **P5 Assembly.Location** — quick wins, 4+ sites
5. **P4 ModelBase copy/telemetry** — clears remaining reflection errors
6. **P6 DI**, **P7 Options**, **P8 Skia unify** — correctness + trim hygiene
7. Re-run spike per RID; add CI `aot-publish` job when green

## Re-run checklist

When the inventory is empty:

```bash
for rid in osx-arm64 osx-x64 linux-x64 linux-arm64; do
  dotnet publish src/WinPrint.TUI/WinPrint.TUI.csproj -c Release -r $rid -f net10.0 --self-contained
done
# Windows (needs windows host or cross-compile):
dotnet publish src/WinPrint.TUI/WinPrint.TUI.csproj -c Release -r win-x64 -f net10.0-windows --self-contained
```

Smoke: `wp --version`, `wp --cat testfiles/oneline.txt`, open TUI with a sample file.
Closes #218

## Summary

Markdown and HTML now open with **Proportional 2-Up** — a sans-serif sheet with page separators — instead of inheriting the monospace **Default 2-Up** code-printout layout. Users can still pick any sheet from the picker or override via `--sheet`.

## Changes

- **Built-in sheets:** Proportional 1-Up (`0002A503`) and Proportional 2-Up (`0002A502`) in `CreateDefaultSettings()`; existing Default 1-Up / 2-Up headers unchanged.
- **`DefaultSheetByContentType`:** Factory-seeded map (`text/x-markdown`, `text/html` → Proportional 2-Up); merged on config upgrade via existing `WinPrintJson` defaults merge.
- **`SheetResolution.ResolveSheetForOpen`:** Mapped content type → sheet; unmapped types fall back to **`Settings.DefaultSheet`** (not hardcoded Default 2-Up).
- **`AppViewModel.LoadFileAsync`:** Transient auto-select on fresh file open; never persists `Settings.DefaultSheet`. Honors `--sheet` and manual user sheet picks for the current file/session.
- **MHTML / `.mht`:** Resolves as `text/html` — same proportional default.

## Reviewer decisions (incorporated)

| Question | Decision |
|----------|----------|
| Fallback when no per-CTE entry | `Settings.DefaultSheet` |
| Header macro | New proportional defs only |
| Global default on open | Transient only |
| MHTML / `.mht` | Same as `text/html` |
| Settings UI v1 | Factory-only mapping |

## Test plan

- [x] `dotnet test tests/WinPrint.Core.UnitTests` — 255 passed
- [x] Factory defaults include proportional sheets + `DefaultSheetByContentType` seed
- [x] `ResolveSheetForOpen` for md/html/mht/unmapped
- [x] `AppViewModel` auto-select, `--sheet` override, user pick survives refresh, no `DefaultSheet` persistence

Manual: open `README.md` in GUI → Proportional 2-Up preview; `config.json` `DefaultSheet` unchanged after close.
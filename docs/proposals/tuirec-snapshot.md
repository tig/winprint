# Proposal: a `snapshot` (still-image) mode for tuirec

> **Status:** proposal / discussion starter, to be carried to [gui-cs/tuirec](https://github.com/gui-cs/tuirec).
> **Origin:** building winprint's TUI with an AI agent surfaced the need for *full-fidelity* (color/underline/hotkey) golden images of individual views. tuirec is the right home for that — it already owns the cast→image pipeline. This drafts the feature against tuirec's actual source (studied at `main`, built and run locally on Linux/Go 1.24).
> **Companion:** `docs/proposals/tui-agent-design-loop.md` (the agent/human design loop this feeds).

## The gap

tuirec turns a TUI binary into an **animated GIF**: it drives the binary through a PTY, plays keystrokes, records an asciinema `.cast`, and renders it to a GIF with `agg`. That is perfect for *demos* and *interaction flows*.

What it does **not** do is produce a **single still image of one frame** — which is what an agent/human design loop and visual golden tests want most of the time:

- A designer reviewing one subview (e.g. a margins editor) wants **one crisp PNG**, not a GIF.
- A golden test wants a **deterministic, full-fidelity image** of a known state — color, underline, hotkey highlighting and all — that a plain-text grid snapshot cannot capture.

Today the only way to get a still from tuirec is a one-frame GIF, which is awkward to diff, embed, and review.

## Why tuirec is the right place (not a bespoke tool)

While building winprint we hand-rolled a plain-text grid → PNG script (`scripts/grid2png.py`). It works for layout, but it is **fidelity-blind**: it cannot show color, underline, or the hotkey styling that `_Margins` produces. Re-implementing a full ANSI-attribute renderer would duplicate exactly what `agg` (which tuirec already orchestrates) does well. The capture format tuirec already produces — the asciinema `.cast` — **is** the full-fidelity representation. So the snapshot belongs here.

## tuirec already has the seam

From `pkg/record/record.go` (verbatim):

```go
// Renderer renders a cast file to a GIF.
type Renderer interface {
    Render(context.Context, string, string, gif.Config) error
}
```

The record pipeline calls it once, after the cast is written:

```go
if err := config.Renderer.Render(parent, config.CastOutput, config.Output, config.GIF); err != nil {
    ...
}
```

and defaults it when unset:

```go
if config.Renderer == nil {
    config.Renderer = gifRenderer{}
}
```

So **a still-image snapshot is a drop-in second `Renderer`** — no surgery to the PTY/record loop. The record half (drive binary → write `.cast`) is reused as-is; only the render half changes.

## Proposed design

### 1. CLI: a `snapshot` command (sibling of `record`)

```
tuirec snapshot \
  --binary ./wp \
  --args "--view margin --width 44 --height 8" \
  --name margin-editor \
  --keystrokes "wait:1500"        # optional: drive to a state, then capture
  [--frame last|<index>|at:<ms>]  # which frame to grab (default: last)
  [--startup-delay 2000 --drain 1000 --kitty-keyboard]
```

Produces `artifacts/margin-editor.png` (and keeps `artifacts/margin-editor.cast`, mirroring how `record --name` sets both `.gif` and `.cast`). The flag surface is mostly shared with `record`; `snapshot` simply defaults to a static capture (no/short keystrokes) and a PNG renderer.

### 2. A `pngRenderer` implementing `Renderer`

Two viable implementations, in order of preference:

- **(a) agg single-frame.** If/when `agg` gains a "render frame N / last frame to PNG" mode, `pngRenderer` shells out to it exactly as `gifRenderer` does (`pkg/gif/gif.go` already builds the arg list and manages the agg download/cache). This keeps one renderer engine. *This needs a small upstream agg feature; see "Dependency" below.*
- **(b) self-contained frame compositor.** Parse the `.cast` (asciinema v2/v3 JSON: a header + `[time, "o", data]` output events), feed the concatenated output up to the chosen frame through a vt100/ANSI parser to reconstruct the final cell grid **with attributes**, and rasterize with a monospace font (the same idea as winprint's `grid2png.py`, but attribute-aware). No agg dependency for stills; pure Go.

Recommendation: ship **(b)** so stills work even without an agg still-frame feature, and optionally route to **(a)** when available so GIF and PNG share an engine.

### 3. Frame selection

`--frame last` (default) covers the design-loop case (drive to a state, snap it). `--frame <index>` and `--frame at:<ms>` support grabbing a mid-interaction moment from the same `.cast`. Because the `.cast` already has per-event timestamps (see `parseOutputEvent`/`marshalOutputEvent` in `record.go`), frame selection is a cast-replay concern, independent of the renderer.

## What this unlocks (the winprint dogfood)

winprint's `wp` binary is already built to be driven this way (`docs/proposals/tui-agent-design-loop.md`):

```
tuirec snapshot --binary ./wp --args "--view margin --width 44 --height 8" --name margin-editor --keystrokes "wait:1500"
```

would yield a **full-fidelity** `margin-editor.png` — showing the `_Margins` hotkey underline and any color — to sit **alongside** winprint's fast plain-text grid golden:

- **plain-text `.txt` grid** (winprint, in-process, no deps): fast, diffable CI regression check.
- **tuirec `.png` snapshot** (full fidelity): human review + catches color/attribute regressions the text grid is blind to.

"Keep both" by design: the cheap layer guards layout in every CI run; the rich layer guards appearance and feeds the human loop.

## Dependency note (agg)

`agg` (`v1.8.1`, auto-downloaded by `pkg/gif/download.go`) currently emits **GIF only** — confirmed from its README. Implementation **(b)** avoids depending on an agg still mode. If the gui-cs/asciinema relationship makes it easy, a tiny agg feature — "render last frame (or frame N) to PNG" — would let GIF and PNG share one engine via implementation **(a)**; worth raising upstream but not a blocker.

## Suggested rollout

1. Add `pngRenderer` (implementation **(b)**) in a new `pkg/png` package mirroring `pkg/gif`'s shape (`Render`, `Validate`, config).
2. Add the `snapshot` cobra command in `cmd/tuirec/main.go` (reuse the record flag wiring; default renderer = `pngRenderer`).
3. Extend `opencli` output and the agent guide (`agent/RECORDING-AGENT.md`) so agents can discover and drive `snapshot`.
4. Add an e2e test using the bundled `internal/testapp` (snapshot last frame → assert non-empty PNG with pixel variance, paralleling `gif.Validate`).

## Open questions for the maintainers

- Prefer a separate `snapshot` command, or a `--still`/`--format png` flag on `record`?
- Is a pure-Go frame compositor (no agg for stills) acceptable, or is a single agg engine strongly preferred even if it needs an upstream agg change?
- Should PNG snapshots carry the same theme/font config as GIFs (share `gif.Config`), or get their own?

# Agent/Human Iterative TUI Design Loop

> **Status:** proposal / discussion starter
> **Origin:** discovered while building the `MarginEditor` for [winprint](https://github.com/tig/winprint) with an AI agent (Claude Code) against Terminal.Gui `2.4.1-develop.11` and `Terminal.Gui.Cli 0.1.0-develop.5`, .NET 10, on a headless Linux container.
> **Audience:** Terminal.Gui maintainers first; TUI-framework authors and agent-tooling builders second.

## TL;DR

A human designer and an AI agent can **co-design a TUI in a tight visual loop**:

```
agent edits view code
      ↓
agent renders the view headlessly  →  full character grid   (framework's offscreen/test driver)
      ↓
agent rasterizes the grid           →  PNG                   (tiny monospace image writer)
      ↓
human sees the image (e.g. on a phone), reacts in natural language
      ↓
agent re-edits  … repeat (seconds per turn)
      +  every accepted frame is saved as a golden snapshot → the design doubles as a regression test
```

The human never has to open a terminal; the agent never has to guess at layout. Each agreed-upon
frame is simultaneously a **design artifact** and a **regression test**. This document describes the
loop, shows a complete worked example (designing winprint's margins editor), embeds the actual
mechanism in code, and proposes what Terminal.Gui could do to make it first-class — and how the same
idea generalizes to any TUI framework.

## The vision

AI agents can already write TUI view code competently. The thing they have lacked is **sight**: a
way to see what they drew and to show it to a human collaborator who is not sitting at a terminal.
That gap forces a slow, error-prone loop — the agent describes what it *thinks* it built; the human
imagines it; misunderstandings compound.

TUIs are **uniquely suited** to closing this gap, more so than pixel-based GUIs:

- The entire UI is a small grid of character cells. Rendering it offscreen is cheap and exact.
- A textual screen dump is trivially **diffable** — perfect for golden/snapshot testing and for an
  agent to reason about ("the `0.75` moved one row up").
- Rasterizing that grid to an image is a few lines of code (monospace font, one `draw.text` per row).
  No browser, no display server, no screenshot harness.

So the medium that agents are *best* at producing also happens to be the medium that is *cheapest* to
render, diff, and show. That is the opportunity.

## Why now

- Agents run in headless cloud containers with no TTY. They need an **offscreen render**, not a real
  terminal.
- Designers increasingly review work on a phone. A **PNG** is the universal "show me" format; an ANSI
  dump in a chat window is not.
- Snapshot testing is a solved idea in other ecosystems (Jest, insta, syrupy). TUIs can have the same
  — and get *visual* review for free on top.

## The loop, and the regression-test dividend

Each turn of the loop produces a frame. When the human says "yes, that's right," the agent saves that
frame as a golden file. From then on, the same render that drives design also runs in CI: if the view
changes unexpectedly, the golden diff fails and prints the before/after grids. **The design
conversation and the test suite are the same artifacts.** You do not write the tests separately; you
*accept* them as you design.

## Worked example: designing winprint's `MarginEditor`

This is the real session, including the wrong turns — the mistakes are the most instructive part.

### Round 0 — first render, and two bugs

The agent built `MarginEditor` (four margin spinners) and tried to show it. The first two images were
**wrong**, in two independent ways worth flagging because anyone building this loop will hit them:

1. **Captured the wrong stream.** The agent first captured `IDriver.ToAnsi()` during the run loop.
   That stream is an **incremental diff** of changed cells, not the whole screen — so a mid-loop
   capture produced a *partial* frame (only two of the four margins rendered). The fix: capture
   `IDriver.ToString()`, which returns the **complete** cell grid for the current frame.
2. **Rasterized via the wrong path.** An early throwaway script replayed the raw ANSI escape stream
   through `pyte` and rendered *that*. `pyte` mis-decoded the `NumericUpDown<decimal>` output, showing
   `0.5 / 1 / 0.75` where the app actually held `50 / 100 / 75`. There was **no value bug in the
   app** — the rasterizer lied. The fix: render the framework's own text grid, never a
   re-interpretation of the escape stream.

Lesson baked into the final design: **capture `ToString()` (the grid), and rasterize *that* grid
directly.** Do not round-trip through ANSI.

### Round 1 — "decimal inches, not 1/100"

First correct render showed margins as raw hundredths-of-an-inch (`50`, `75`, …) with a "1/100"
label. The human's instruction:

> Suppose to be in a ... editor and decimal. No 1/100. See how the legacy GUI did it.

The agent looked at the legacy editor (`DecimalPlaces = 2`;
displaying `Margins.Top / 100M` and writing back `* 100M`) and reworked the editor to show
**decimal inches** while keeping hundredths-inch storage. Result:

```
┌──────────────────────────────┐
│Margins (inches)              │
│Top:    ▼0.50▲                │
│Left:   ▼0.75▲                │
│Right:  ▼1.00▲                │
│Bottom: ▼0.25▲                │
└──────────────────────────────┘
```

### Round 2 — "arrange it in a diamond"

> See how the legacy GUI and the thickness editor arrange the elements in a Diamond?

The agent read the legacy group-box coordinates (Top centered up top, Left/Right facing each other
on the middle row, Bottom centered at the bottom — the same arrangement as Terminal.Gui's
adornment/thickness editor) and switched `MarginEditor` to a relative diamond layout using
`Pos.Center()`, `Pos.AnchorEnd()`, and `Pos.Bottom(...)`. Current render:

```
┌┤Margins├───────────────────────────────┐
│              Top: ▼0.50▲                │
│Left: ▼0.75▲               Right: ▼1.00▲ │
│             Bottom: ▼0.25▲              │
└─────────────────────────────────────────┘
```

Each round was **one sentence from the human and a few minutes from the agent**, with an image at the
end of each. That is the loop working.

## The mechanism, in code

Three small pieces, all built on existing Terminal.Gui APIs. They are reproduced verbatim from the
winprint repo.

### 1. Headless render path (`HeadlessRenderer` + `AppFixture`)

The core is a single static helper that boots a `Application.Create()` instance (thread-local — no
process-global `Application.Init()`), inits the **ANSI driver**, sets a fixed screen size, hosts the
view in a `Window`, runs the loop, and captures the **full grid** via `IDriver.ToString()` on the
`Iteration` event. It lives in the `WinPrint.TUI` assembly so the binary (`wp --view <name> --cat`), the scratch
host, and the tests all share **one** render path:

```csharp
using Terminal.Gui.App;
using Terminal.Gui.Drivers;
using Terminal.Gui.ViewBase;
using Terminal.Gui.Views;

namespace WinPrint.TUI;

public static class HeadlessRenderer
{
    /// <summary>Renders <paramref name="content" /> at the given size and returns the LF-normalized grid.</summary>
    public static string RenderToGrid(View content, int width, int height)
    {
        ArgumentNullException.ThrowIfNull(content);

        using IApplication app = Application.Create();
        app.AppModel = AppModel.FullScreen;
        app.Init(DriverRegistry.Names.ANSI);
        app.Driver!.SetScreenSize(width, height);

        var window = new Window { X = 0, Y = 0, Width = Dim.Fill(), Height = Dim.Fill() };
        window.Add(content);

        var iterations = 0;
        const int stableFrame = 4;
        var grid = string.Empty;

        app.Iteration += OnIteration;
        app.Run(window);
        app.Iteration -= OnIteration;

        return grid;

        void OnIteration(object? sender, EventArgs<IApplication?> e)
        {
            iterations++;
            // ToString() returns the COMPLETE cell grid for the frame; ToAnsi() would be a partial diff.
            grid = Canonicalize(app.Driver?.ToString());
            if (iterations >= stableFrame)
            {
                app.RequestStop();
            }
        }
    }

    public static string Canonicalize(string? text) =>
        (text ?? string.Empty).Replace("\r\n", "\n").Replace("\r", "\n");
}
```

The test fixture is then a thin wrapper — it just calls the shared helper, so the binary's `dump`
output is byte-identical to what the tests assert:

```csharp
using Terminal.Gui.ViewBase;
using WinPrint.TUI;

namespace WinPrint.TUI.UnitTests.Testing;

public sealed class AppFixture
{
    public AppFixture(View content, int width = 40, int height = 12) =>
        Screen = HeadlessRenderer.RenderToGrid(content, width, height);

    /// <summary>The captured full-screen render as a newline-separated character grid.</summary>
    public string Screen { get; }
}
```

The Terminal.Gui APIs that make this possible: `Application.Create()`, `DriverRegistry.Names.ANSI`,
`IDriver.SetScreenSize`, `IApplication.Run`/`RequestStop`, `IApplication.Iteration`, and crucially
`IDriver.ToString()` for the complete grid.

### 2. Golden snapshot (`GridSnapshot`)

Records/compares `__snapshots__/<name>.txt`. First run (or `UPDATE_SNAPSHOTS=1`) writes the golden;
later runs compare and, on mismatch, write a `.actual` sibling and throw with an inline diff.

```csharp
using System.Runtime.CompilerServices;
using System.Text;
using Xunit.Sdk;

namespace WinPrint.TUI.UnitTests.Testing;

/// <summary>
///     Golden-file snapshot of a rendered screen as a plain-text character grid (e.g. from
///     <c>AppFixture.Screen</c>). The grid is the full set of cell glyphs for a frame, so it is stable
///     and diffable; convert one to a PNG for visual review with <c>scripts/grid2png.py</c>.
/// </summary>
/// <remarks>
///     First run records the golden under <c>__snapshots__/&lt;name&gt;.txt</c> and passes; later runs
///     compare. Accept an intended change by re-running with <c>UPDATE_SNAPSHOTS=1</c>. On mismatch the
///     failure shows the diff inline and writes a sibling <c>.txt.actual</c>.
/// </remarks>
public static class GridSnapshot
{
    private static bool UpdateRequested =>
        Environment.GetEnvironmentVariable("UPDATE_SNAPSHOTS") is "1" or "true";

    /// <summary>Compares a captured <paramref name="grid" /> render against the golden named <paramref name="name" />.</summary>
    /// <param name="grid">The captured character grid (e.g. <c>AppFixture.Screen</c>).</param>
    /// <param name="name">Stable snapshot name (becomes <c>&lt;name&gt;.txt</c>).</param>
    /// <param name="callerFile">Compiler-supplied; locates <c>__snapshots__/</c> in the test project.</param>
    public static void Verify(string grid, string name, [CallerFilePath] string callerFile = "")
    {
        ArgumentNullException.ThrowIfNull(grid);
        ArgumentException.ThrowIfNullOrWhiteSpace(name);

        string actual = Canonicalize(grid);
        string dir = SnapshotDir(callerFile);
        Directory.CreateDirectory(dir);
        string path = Path.Combine(dir, name + ".txt");

        if (UpdateRequested || !File.Exists(path))
        {
            WriteRaw(path, actual);
            return;
        }

        string expected = Canonicalize(File.ReadAllText(path));

        if (string.Equals(expected, actual, StringComparison.Ordinal))
        {
            return;
        }

        string actualPath = path + ".actual";
        WriteRaw(actualPath, actual);

        throw new XunitException(
            $"""
             Grid snapshot '{name}' does not match {path}.

             Expected:
             ----------------------------------------------------------------------
             {expected}
             ----------------------------------------------------------------------
             Actual:
             ----------------------------------------------------------------------
             {actual}
             ----------------------------------------------------------------------

             Wrote the actual render to: {actualPath}
             If this change is intended, accept it by re-running with UPDATE_SNAPSHOTS=1.
             """);
    }

    // Walk up from the caller source file to the directory containing the test .csproj, so goldens
    // always resolve to <project>/__snapshots__ regardless of where the helper file lives.
    private static string SnapshotDir(string callerFile)
    {
        string? dir = Path.GetDirectoryName(callerFile);

        while (!string.IsNullOrEmpty(dir))
        {
            if (Directory.EnumerateFiles(dir, "*.csproj").Any())
            {
                return Path.Combine(dir, "__snapshots__");
            }

            dir = Path.GetDirectoryName(dir);
        }

        throw new InvalidOperationException(
            $"Could not locate the test project directory from caller path '{callerFile}'.");
    }

    private static string Canonicalize(string? text)
    {
        return (text ?? string.Empty).Replace("\r\n", "\n").Replace("\r", "\n");
    }

    private static void WriteRaw(string path, string text)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        File.WriteAllText(path, text, new UTF8Encoding(false));
    }
}
```

A test then reads as plainly as:

```csharp
[Fact]
public void InitialRender_MatchesGolden()
{
    var editor = new MarginEditor { Value = new PrintMargins(75, 100, 50, 25) };
    var fixture = new AppFixture(editor, width: 32, height: 7);
    GridSnapshot.Verify(fixture.Screen, "margin-editor");
}
```

### 3. Grid → PNG (`grid2png.py`)

The only non-.NET piece, and deliberately tiny: **PIL only**, one `draw.text` per row in a monospace
font. This is what makes the result reviewable on a phone.

```python
#!/usr/bin/env python3
"""Render a text-grid golden snapshot to a PNG.

The WinPrint.TUI golden tests record the rendered screen as a plain-text character
grid (the box-drawing UI you'd see in a terminal). This script draws that grid to a
PNG with a monospace font so the UI can be reviewed as an image (e.g. on a phone)
without a terminal.

Usage:
    python3 scripts/grid2png.py <input.txt> <output.png> [--font-size N] [--bg R,G,B] [--fg R,G,B]
"""

import argparse
import sys

from PIL import Image, ImageDraw, ImageFont

FONT_CANDIDATES = [
    "/usr/share/fonts/truetype/dejavu/DejaVuSansMono.ttf",
    "/usr/share/fonts/truetype/liberation/LiberationMono-Regular.ttf",
]


def load_font(size):
    for path in FONT_CANDIDATES:
        try:
            return ImageFont.truetype(path, size)
        except OSError:
            continue
    return ImageFont.load_default()


def parse_rgb(text, default):
    if not text:
        return default
    parts = text.split(",")
    return tuple(int(p) for p in parts) if len(parts) == 3 else default


def main():
    ap = argparse.ArgumentParser()
    ap.add_argument("input")
    ap.add_argument("output")
    ap.add_argument("--font-size", type=int, default=32)
    ap.add_argument("--bg", default="18,18,26")
    ap.add_argument("--fg", default="225,228,235")
    args = ap.parse_args()

    bg = parse_rgb(args.bg, (18, 18, 26))
    fg = parse_rgb(args.fg, (225, 228, 235))

    rows = open(args.input, "r", encoding="utf-8").read().split("\n")
    while rows and rows[-1] == "":
        rows.pop()
    if not rows:
        rows = [""]

    font = load_font(args.font_size)
    cell_w = font.getlength("M")
    cell_h = args.font_size + 10
    pad = 12

    width = int(max(len(r) for r in rows) * cell_w) + pad * 2
    height = len(rows) * cell_h + pad * 2

    img = Image.new("RGB", (width, height), bg)
    draw = ImageDraw.Draw(img)
    for r, line in enumerate(rows):
        draw.text((pad, pad + r * cell_h), line, font=font, fill=fg)

    img.save(args.output)
    print(f"wrote {args.output} ({img.width}x{img.height})")


if __name__ == "__main__":
    sys.exit(main())
```

### 4. The "scratch render host" — iterate outside the test runner

For fast iteration the agent renders one view to stdout in a single command. The same capability ships
in the `wp` binary as `wp --view <view> --cat [--width W --height H]`, and a throwaway console app can call the identical
shared render path so the scratch host, the binary, and the tests never diverge:

```csharp
// Scratch render host. Delegates to the shared HeadlessRenderer + ViewCatalog so it
// stays in lockstep with `wp --view <view> --cat` and the golden tests. Usage: render <view> [w] [h]
using WinPrint.TUI;

string view = args.Length > 0 ? args[0] : "margin";
int w = args.Length > 1 ? int.Parse(args[1]) : 44;
int h = args.Length > 2 ? int.Parse(args[2]) : 8;
Console.Out.Write(HeadlessRenderer.RenderToGrid(ViewCatalog.Create(view), w, h));
```

where `HeadlessRenderer.RenderToGrid` is the create/init-ANSI/host/run/capture-`ToString` helper (the
same one `AppFixture` uses), and `ViewCatalog` names each view + its sample value in one place.

```
wp --view margin --cat --width 44 --height 8 | python3 grid2png.py /dev/stdin out.png   # an image in seconds
```

The fact that `HeadlessRenderer` must be hand-rolled — and shared by hand across the binary and the
tests — is itself the argument for Terminal.Gui shipping it as `Application.RenderToString(...)`.

## Two fidelity layers (and where tuirec fits)

The plain-text grid above is deliberately **fidelity-blind**: it captures glyphs and layout, but not
color, underline, or hotkey styling. That is a feature for the fast inner loop (it makes goldens tiny
and diffable) but a gap for appearance review — e.g. setting the editor title to `_Margins` adds a
hotkey underline on the **M** that the text grid cannot show at all (the `_` marker is consumed and
the grid text is byte-identical before and after).

So the loop wants **two layers**, kept side by side:

| Layer | Source | Captures | Role |
|---|---|---|---|
| **Plain-text grid** | in-process `IDriver.ToString()` → `.txt` | glyphs + layout | fast, diffable CI regression check (every run) |
| **Full-fidelity image** | [tuirec](https://github.com/gui-cs/tuirec) `.cast` → image | color, underline, hotkey, attrs | human review + appearance regressions |

The full-fidelity layer is **tuirec's** job, not a bespoke rasterizer's. tuirec already drives a real
binary through a PTY and records an asciinema `.cast` — which *is* the full-fidelity capture (ANSI
with all attributes). winprint's `wp --view <view>` command exists precisely so tuirec can drive a
single view headlessly and snapshot it. tuirec today renders `.cast → animated GIF` (via `agg`); a
**still-image `snapshot` mode** would close the loop. That feature is specced separately in
`docs/proposals/tuirec-snapshot.md` — and notably it drops into tuirec's existing `Renderer`
interface seam, so it reuses the whole record pipeline.

This is the "keep both" decision in practice: the cheap text grid guards layout on every CI run; the
rich tuirec image guards appearance and feeds the human's eyes.

## What Terminal.Gui could do to make this first-class

Terminal.Gui *already* has every primitive this loop needs — which is exactly why it makes such a
good reference implementation. The asks are about making the path supported, discoverable, and
ergonomic:

1. **A one-line "render this view to a grid string" helper.** Something like
   `Application.RenderToString(View view, int width, int height)` that does the
   create/init-ANSI/host/run/capture/dispose dance the `AppFixture` above does by hand. This is the
   single highest-value addition.
2. **A sanctioned snapshot-test helper** (or at least a documented recipe) so projects don't each
   reinvent `GridSnapshot`. Snapshot/golden testing of views would become a one-liner.
3. **A documented headless-capture recipe** for the plain grid, and a clear hand-off to **tuirec**
   for full-fidelity images (see `docs/proposals/tuirec-snapshot.md`). The plain grid → image step is
   intentionally trivial (one monospace `draw.text` per row); the *fidelity* story belongs in tuirec,
   which already owns the `.cast` capture and the `agg` render engine.
4. **Document the `ToAnsi` vs `ToString` distinction** prominently for headless capture. We lost real
   time to capturing the incremental `ToAnsi` stream mid-loop; a note saying "for a full-frame
   snapshot use `ToString()`" would save others the same bug.

These would turn an ad-hoc harness into a supported workflow: *"snapshot-test your views, and review
them as images, with the framework's blessing."*

## Beyond Terminal.Gui — a framework-agnostic project

The loop is not specific to Terminal.Gui or .NET. Any TUI framework can participate if it exposes
**one primitive**:

> **Render a view/component offscreen at a given size and return the screen as text** (a grid of
> cells, ideally with optional per-cell style info).

Given that, the rest is framework-neutral:

- **Grid → image** is the same monospace rasterizer everywhere.
- **Snapshot golden files** are the same idea everywhere (and many ecosystems already have a snapshot
  library to plug into).
- **The agent/human protocol** — edit, render, rasterize, show, react — is pure orchestration.

Candidates that already have an offscreen/test-render mode and could be reference targets:
**Textual** (Python, has `App.run_test()` + export), **Bubble Tea** (Go), **ratatui** (Rust, has a
`TestBackend` buffer), **ink** (JS). A small shared layer — "give me your text grid; I'll handle the
image and the loop" — could make iterative agent/human design a **portable workflow across the whole
TUI landscape**.

If this resonates, I'd love to collaborate on (a) the Terminal.Gui first-class helpers above, and
(b) sketching the cross-framework primitive and shared image/protocol layer.

## Appendix — environment & references

- **Terminal.Gui** `2.4.1-develop.11`; **Terminal.Gui.Cli** `0.1.0-develop.5`; **.NET** 10.
- Headless **Linux** container, no TTY; image step uses **Python + PIL (Pillow)** with the bundled
  **DejaVu Sans Mono** font.
- Reference implementation (winprint):
  - `tests/WinPrint.TUI.UnitTests/Testing/AppFixture.cs` — headless render harness
  - `tests/WinPrint.TUI.UnitTests/Testing/GridSnapshot.cs` — golden snapshot helper
  - `tests/WinPrint.TUI.UnitTests/MarginEditorGoldenTests.cs` — example tests
  - `scripts/grid2png.py` — grid → PNG
  - `src/WinPrint.TUI/Views/Editors/` — `EditorBase<T>` / `SizeEditor` / `MarginEditor`

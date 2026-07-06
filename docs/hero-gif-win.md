# Recreating the Windows GUI hero (`docs/hero-gui-win.gif`)

`docs/hero-gui-win.gif` is the Windows half of the README's side-by-side GUI hero (the macOS half is
`docs/hero-gui-mac.gif`). It is **MCEC driving WinPrint** through the full print-preview story and
recording the window as a GIF:

> launch -> load `SheetViewModel.cs` (2-up landscape, line numbers) -> toggle **Line Numbers** -> toggle
> **Landscape** (reflow to 1-up portrait and back) -> **zoom in, pan, reset** -> open `testfiles/demo.md` and
> switch to the **Proportional 2-Up** sheet so Markdown reads as prose -> **Print to PDF and open the result
> in the browser** -> hold on the PDF.

Needs a WinPrint new enough to have the **Proportional** sheet definitions and the markdown/HTML defaults
(recent `develop`); the Velopack-installed release may be older, so run a `develop` build as the subject
(see `.claude/skills/run-maui-app`). The MCEC **controller** must also be recent `develop` (the `focus` tool
that the zoom beat needs, and the literal-`chars:` fix, both postdate v3.0.17).

Unlike the MCEC hero, the on-screen command **overlay is OFF** here: the MCEC hero narrates with the
overlay because it is dogfooding MCEC, but this hero's marketing subject is **WinPrint**, so the window is
shown clean, with no narration.

## How it is made

This reuses MCEC's agent-driven flow; **read
[mcec's `docs/hero-gif.md`](https://github.com/tig/mcec/blob/develop/docs/hero-gif.md) first** -- it owns
the MCP mechanics (standing up the controller, the double-wrapped result envelope, integer pixels, absolute
paths, the keyboard primitives, teardown). This page only adds the **WinPrint-specific** parts.

Differences from the MCEC hero:

- The subject is your **installed WinPrint**, launched by exe path -- so there is **no `provision-session` /
  `end-session` of the subject**, and nothing to reap. Only the disposable MCEC **controller** is torn down.
- The **overlay is disabled** (see the controller-config note below).
- **Window placement/sizing is done with the mouse** (drag the title bar to move, drag a sizing border to
  resize) -- there is no move/resize tool. For the hero, WinPrint's default position is fine as-is.

The agent connects to the controller and drives WinPrint with `launch`, `windows`, `query`, `click`,
`drag`, `record`, `capture`, and `send_command`.

## Setup (the only script lives in the mcec repo)

There is **no hero script in this repo**. Stand up the controller from a clone of
[tig/mcec](https://github.com/tig/mcec):

```powershell
# from the mcec repo root
pwsh -NoProfile -File scripts/Generate-HeroGif.ps1     # prints HERO_MCP_URL=http://127.0.0.1:<port>/mcp
# ... drive the playbook below ...
pwsh -NoProfile -File scripts/Generate-HeroGif.ps1 -Stop
```

**Controller config for this hero.** `Generate-HeroGif.ps1` is tuned for the *MCEC* hero: overlay ON, and
only ~10 commands enabled. For the WinPrint hero, adjust the throwaway controller it stands up (its temp
dir is `%TEMP%\mcec-hero-controller-*`):

- **`mcec.settings`**: set `<CommandOverlayEnabled>false</CommandOverlayEnabled>` (overlay is read at
  startup, so relaunch the controller's `mcec.exe` after editing).
- **`mcec.commands`**: enable the commands the tour uses beyond the bootstrap's set --
  **`windows`** (dialog discovery, issue #77) and the `send_input` VK builtins **`right` / `down` / `enter`
  / `run`** (the `mcec.commands` file is hot-reloaded by the controller's file watcher). Ideally the mcec
  bootstrap grows a `-NoOverlay` switch and enables these by default (an mcec-side improvement).

**Prerequisites (operator):** an **unlocked, interactive Windows session** (real injected mouse/keyboard)
and **WinPrint installed** at `%LOCALAPPDATA%\Kindel.WinPrint\current\winprint.exe` (Velopack/winget; do not
build from source for the hero).

## The playbook (the agent drives, all via MCEC's MCP tools)

Send **integer** pixels. For envelope-unwrapping, keyboard primitives, and the JSON-RPC driver, follow
mcec's `hero-gif.md`. Two WinPrint-specific input rules:

- **Paths go through `chars:` as raw text (single backslashes).** As of mcec #269 `chars:` types its
  argument **verbatim** (no `Regex.Unescape`), so send `C:\Users\...\tig\...` as-is. (Do NOT double the
  backslashes — older guidance did, back when `chars:` unescaped and mangled `\t` into a TAB; that is gone.)
- **`chars:` is text; `send_input` (VK builtins) is a keydown.** Use `chars:` for filenames; use the VK
  builtins (`right`, `down`, `enter`, `run`, ...) for keys/shortcuts. Submit the Open/Save (`#32770`)
  dialogs with **`enter`** rather than clicking the button, since each has three `Open`/`Save`-named
  split-button parts that trip up by-name matching.

1. **Screen size.** `displays` -> primary `bounds` as `SX, SY, SW, SH`.
2. **Clear the backdrop.** Win+D (`shiftdown:lwin` + `d` + `shiftup:lwin`) so only WinPrint is in frame.
3. **Launch WinPrint.** `launch { "path": "<abs %LOCALAPPDATA%>\\Kindel.WinPrint\\current\\winprint.exe",
   "timeout": 8000 }` -> `result.handle`. Drive by that `handle`. Wait ~2 s for the window + preview.
4. **Read its bounds.** `query { "handle": <handle>, "maxDepth": 1 }` (or `windows { "process": "winprint" }`)
   -> `WX, WY, WW, WH`.
5. **Load `SheetViewModel.cs`.** Click WinPrint's **File** button (its UIA name is `📂 File…`, so match by
   bounds). Find the Open dialog with `windows { "window": "Open", "timeout": 5000 }` (classic `#32770`),
   `query` it for the **File name** `Edit`, `click` the field,
   `send_command { "command": "chars:<abs path, single backslashes>" }`, then submit with **`enter`**
   (not a click -- the button is a split button with three `Open`-named parts). ~1.5 s to render.
6. **Start recording -- the window only** (no overlay band to include):
   `record { "action": "start", "x": WX-8, "y": WY-8, "width": WW+16, "height": WH+16, "fps": 2,
   "maxWidth": 820 }`. Then `capture { "handle": <handle> }` and dwell ~0.9 s. (Syntax-highlighted code is a
   worst case for the GIF palette; `fps:2`/`maxWidth:820` lands ~2.8 MB. See Tuning.)
7. **Toggle Line Numbers, then Landscape.** Each is a sidebar **label** (its `TapGestureRecognizer` flips the
   bound checkbox; no automation id -- target its bounding rectangle). Click **Line Numbers** twice (gutter
   off, then on) and **Landscape** twice (reflow to 1-up portrait, then back to 2-up landscape), ~1 s dwell
   each so the re-render reads.
8. **Zoom in, pan, reset (FAST).** WinPrint's zoom (`=`/`+` in, `0` fit) only fires when its MAUI
   `FocusablePlatformGraphicsView` has keyboard focus, which a bare `click` does not set -- so first
   `focus { "handle": <handle>, "at": { "x": <preview-x>, "y": <preview-y> } }` (the `focus` tool clicks the
   surface and verifies real keyboard focus; #91/#270). Then `key_equals` x3 (zoom in), an arrow or two to
   pan, and `key_0` to fit. Keep dwells short -- this is a snappy flourish, not a crawl.
9. **Open `testfiles/demo.md`, then switch to `Proportional 2-Up`.** Open the file as in step 5, then select
   the sheet explicitly: `click` the **Sheet Definition** `ComboBox` (top of the sidebar), then `click` the
   **Proportional 2-Up** `ListItem` in the dropdown. The preview reflows to prose with a proportional font --
   the "not just source code" beat. `demo.md` is a purpose-built Markdown showcase (~3 printed pages:
   headings, lists, a table, syntax-highlighted code, an image, a Mermaid block WinPrint renders as code),
   chosen over `README.md` (now gif-heavy, a wall of images). Dwell ~1.5 s.
10. **Print to PDF.** Delete any prior `%USERPROFILE%\Documents\winprintdemo.pdf` first. **Confirm the printer
    is Microsoft Print to PDF** -- WinPrint prints to `viewModel.SelectedPrinter`; if the machine default is
    something else, `query` the sidebar **Printer** `ComboBox`, `click` it, and `click` the **Microsoft Print
    to PDF** item. Click the toolbar **Print** button (`🖨 Print…`), find the save dialog with
    `windows { "window": "Save Print Output As", "timeout": 5000 }`, `query` its filename `Edit`, `click` it,
    type the PDF path with `chars:` (**single** backslashes), and submit with **`enter`**. Assert the PDF exists.
11. **Open, show, and scroll the printed PDF (final beat).** Open `winprintdemo.pdf` in the browser (the
    default handler is Edge) so the loop ends on a real document output; maximize it so the PDF fills the
    frame, click the page to focus it, then `key_pagedown` a few times to **scroll through the whole ~3-page
    doc**, holding briefly at the end. Then **stop and write the GIF:**
    `record { "action": "stop", "file": "<winprint repo abs>\\docs\\hero-gui-win.gif" }` (absolute path -- a
    relative one lands in the controller's temp copy and is lost). Assert `result.frames` (~45) and
    `result.bytes` (~4-5 MB). After the stop, close the PDF **tab** with `ctrl-f4` (not the whole browser).
12. **Tidy.** Close WinPrint; tear down the controller.

## Gotchas (WinPrint-specific; the generic ones are in mcec's `hero-gif.md`)

- **`chars:` types verbatim -- send raw single-backslash paths** (see above); doubling them (old guidance)
  now yields a literal `\\` and the load/save silently does nothing.
- **Submit `#32770` dialogs with `enter`**, not a click -- the Open/Save button is a split button with
  three `Open`/`Save`-named parts, so a by-name click can land on the dropdown arrow and never submit.
- **Open/Save are classic `#32770` dialogs** (title **Open** / **Save Print Output As**), each with a real
  **File name** `Edit`. Discover them (and their live bounds) with `windows`; don't hardcode a pixel.
- **Settings are sidebar labels, not checkboxes** -- click the **label** (no automation id; target by
  bounding rectangle).
- **Close the PDF viewer** if you add an open-the-PDF beat, or its file lock blocks the next run's delete of
  `winprintdemo.pdf`.

## Verify, then commit

Spot-check keyframes (extract with the snippet in mcec's `hero-gif.md`). Confirm the story is legible: the
2-up landscape load, the Line Numbers gutter toggling, the Landscape reflow to portrait and back, the
`README.md` Markdown render, and the save-to-PDF dialog. It must sit convincingly beside `hero-gui-mac.gif`
in the README (same sample, same story, matched width). Commit `docs/hero-gui-win.gif` on the operator's
say-so.

## Tuning size

File size ~= frame count x frame area, and syntax-highlighted code is a GIF worst case: `fps:4`/`maxWidth:1102`
produced **~17 MB**, while `fps:2`/`maxWidth:820` produced **~2.8 MB** (near the macOS hero's ~1.7 MB) with the
tour still legible. Lower `fps`/`maxWidth` or trim dwells to shrink; raise them to enrich. Keep it in the
same ballpark as `hero-gui-mac.gif` so the README pair matches.

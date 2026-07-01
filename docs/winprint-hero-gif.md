# WinPrint Windows hero GIF (Customer 1, issue #84)

`docs/hero-gui-win.gif` in [tig/winprint](https://github.com/tig/winprint) is produced by **MCEC driving a real
Windows desktop** — Start Menu launch, settings/zoom tour, Print to Microsoft Print to PDF, save
`winprintdemo.pdf`, open the PDF — recorded as a **desktop region** GIF with the MCEC command overlay
narrating each step.

Run from the **winprint** repo root (`scripts/Generate-WinPrint-HeroGif.ps1`; paths are repo-relative).

## Prerequisites (operator)

| Requirement | Notes |
|-------------|--------|
| **Unlocked interactive session** | Real injected mouse/keyboard. |
| **MCEC installed** | The harness **checks** for an existing install (default dir, PATH, winget registration) and **does not** download or install MCEC. If missing: `winget install Kindel.mcec` when published, or run the signed setup.exe until then. **Do not build from source** for hero runs. |
| **WinPrint installed** | Start Menu search must find **WinPrint** (Velopack/winget install — operator responsibility). |
| **Demo PDF path** | `%USERPROFILE%\Documents\winprintdemo.pdf` |

### Harness-only prep (not MCP choreography)

- **Delete prior `winprintdemo.pdf`** — the harness runs `Remove-Item` before `record start`. When
  [issue #138](https://github.com/tig/mcec/issues/138) (disposable `provision-session`) lands, demo
  artifact cleanup becomes semi-automatic inside session provisioning.
- **Disposable MCEC session** — the harness copies the installed MCEC into
  `%LOCALAPPDATA%\MCEC\sessions\winprint-hero`, writes agent config **only there**, and deletes the
  dir afterward. The core install's `mcec.settings` / `mcec.commands` are never touched (same idea as
  #138; formal tool replaces this copy/delete dance later).

## One-shot regeneration

From the **winprint repo root**:

```powershell
pwsh -NoProfile -File scripts/Generate-WinPrint-HeroGif.ps1
```

Evidence bundles land under `artifacts/customer1/` in this repo.

## Manual MCP recipe (agent-playbook)

Connect to the **disposable session** MCEC HTTP floor (`POST http://127.0.0.1:5151/mcp`) after the
operator/harness has provisioned it (agent commands enabled in the session copy only).

| Step | Tool call |
|------|-----------|
| Start record | `record { action:"start", x, y, width, height, fps:4, maxWidth:880 }` (desktop region) |
| Launch | `send_command winkey` → `send_command chars:winprint` → `send_command enter` → `wait-for` / `query { process:"winprint" }` |
| Open sample | `click` File button → `clipboard { action:"set", text:"…SheetViewModel.cs" }` → Ctrl+V → Enter |
| Settings | `click` **Line Numbers** (twice), **Landscape** (twice) |
| Zoom | `click` preview → `key_equals` ×4 → arrows → `key_0` |
| Second file | File → clipboard `README.md` → Ctrl+V → Enter |
| Print | `click` **Microsoft Print to PDF** → `click` **Print** |
| Save PDF | `clipboard { action:"set", text:"…winprintdemo.pdf" }` → Ctrl+V → Enter |
| Open PDF | `send_command winr` → `send_command chars:<pdf path>` (backslashes doubled) → Enter |
| Stop | `record { action:"stop", file:"docs/hero-gui-win.gif" }` |

**Not in the agent recipe:** deleting the old PDF (harness `Remove-Item` before connect), installing
MCEC/WinPrint, copying the session dir, evidence zip — see [issue #138](https://github.com/tig/mcec/issues/138).

Agent connect-time guidance: `AgentServer.Instructions` in `src/Services/AgentServer.cs` and
[Agents.md](../Agents.md).
# Using keywords in issues and pull requests

Use keywords to link a pull request to an issue or to mark something as a duplicate.
These are conversation semantics on GitHub; WinPrint will typically show them as plain text.

> **How to read these fixtures:** for each feature you get (a) a fenced **source** code block,
> (b) a **GitHub** screenshot of how GitHub renders it, and (c) the same Markdown **live** so
> WinPrint can render it. Compare (b) and (c) to see what works and what does not yet.


## Linking a PR to an issue (auto-close keywords)

**Source:**

```markdown
Closes #10

Fixes octo-org/octo-repo#100

Resolves #42
```

**WinPrint (live):**

Closes #10

Fixes octo-org/octo-repo#100

Resolves #42

Supported keywords: `close`, `closes`, `closed`, `fix`, `fixes`, `fixed`, `resolve`, `resolves`, `resolved`.

## Marking a duplicate

**Source:**

```markdown
Duplicate of #123
```

**WinPrint (live):**

Duplicate of #123

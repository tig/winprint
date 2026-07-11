# About tasklists

You can use tasklists to break work into smaller tasks and track completion.

> **How to read these fixtures:** for each feature you get (a) a fenced **source** code block,
> (b) a **GitHub** screenshot of how GitHub renders it, and (c) the same Markdown **live** so
> WinPrint can render it. Compare (b) and (c) to see what works and what does not yet.


## Task list with issue refs

**Source:**

```markdown
- [x] #739
- [ ] https://github.com/octo-org/octo-repo/issues/740
- [ ] Add delight to the experience when all tasks are complete :tada:
```

**GitHub:**

![Screenshot of a rendered task list with issue titles unfurled.](task-list-rendered-simple.png)

**WinPrint (live):**

- [x] #739
- [ ] https://github.com/octo-org/octo-repo/issues/740
- [ ] Add delight to the experience when all tasks are complete :tada:

## Issue tasklist UI (progress / convert)

**Source:** (GitHub issue UI)

```markdown
- [ ] Feature A
- [ ] Feature B
- [ ] Feature C
```

**GitHub:**

![Screenshot of an issue showing a tasklist under Features.](task-list-rendered.png)

![Screenshot of reordering a tasklist item via a six-dot grip.](task-list-reorder.png)

![Screenshot of converting a task to an issue.](convert-task-lists-into-issues.png)

![Screenshot of a tracked-by issue reference.](task-list-tracked.png)

**WinPrint (live):**

- [ ] Feature A
- [ ] Feature B
- [ ] Feature C

> [!IMPORTANT]
> Tasklist *blocks* are retired on GitHub in favor of sub-issues. Markdown `- [ ]` task lists still work in comments and files.

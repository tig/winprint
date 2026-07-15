# Autolinked references and URLs

References to URLs, issues, pull requests, and commits are automatically shortened and converted into links on GitHub (in conversations; not always in files).

> **How to read these fixtures:** for each feature you get (a) a fenced **source** code block,
> (b) a **GitHub** screenshot of how GitHub renders it, and (c) the same Markdown **live** so
> WinPrint can render it. Compare (b) and (c) to see what works and what does not yet.


## URL autolink

**Source:**

```markdown
Visit https://github.com
```

**GitHub:**

![Screenshot of a URL rendered as a blue clickable link.](url-autolink-rendered.png)

**WinPrint (live):**

Visit https://github.com

## Issue and pull request references

**Source:**

```markdown
* https://github.com/jlord/sheetsee.js/issues/26
* #26
* GH-26
* jlord/sheetsee.js#26
* github-linguist/linguist#4039
```

**WinPrint (live):**

* https://github.com/jlord/sheetsee.js/issues/26
* #26
* GH-26
* jlord/sheetsee.js#26
* github-linguist/linguist#4039

## Label URL

**Source:**

```markdown
https://github.com/github/docs/labels/enhancement
```

**WinPrint (live):**

https://github.com/github/docs/labels/enhancement

## Commit SHA references

**Source:**

```markdown
* https://github.com/jlord/sheetsee.js/commit/a5c3785ed8d6a35868bc169f07e40e889087fd2e
* a5c3785ed8d6a35868bc169f07e40e889087fd2e
* jlord@a5c3785ed8d6a35868bc169f07e40e889087fd2e
* jlord/sheetsee.js@a5c3785ed8d6a35868bc169f07e40e889087fd2e
```

**WinPrint (live):**

* https://github.com/jlord/sheetsee.js/commit/a5c3785ed8d6a35868bc169f07e40e889087fd2e
* a5c3785ed8d6a35868bc169f07e40e889087fd2e
* jlord@a5c3785ed8d6a35868bc169f07e40e889087fd2e
* jlord/sheetsee.js@a5c3785ed8d6a35868bc169f07e40e889087fd2e

# Organizing information with tables

You can build tables to organize information in comments, issues, pull requests, and wikis.

> **How to read these fixtures:** for each feature you get (a) a fenced **source** code block,
> (b) a **GitHub** screenshot of how GitHub renders it, and (c) the same Markdown **live** so
> WinPrint can render it. Compare (b) and (c) to see what works and what does not yet.


## Basic table

**Source:**

```markdown
| First Header  | Second Header |
| ------------- | ------------- |
| Content Cell  | Content Cell  |
| Content Cell  | Content Cell  |
```

**GitHub:**

![Screenshot of a GitHub Markdown table rendered as two equal columns.](table-basic-rendered.png)

**WinPrint (live):**

| First Header  | Second Header |
| ------------- | ------------- |
| Content Cell  | Content Cell  |
| Content Cell  | Content Cell  |

## Varied column widths

**Source:**

```markdown
| Command | Description |
| --- | --- |
| git status | List all new or modified files |
| git diff | Show file differences that haven't been staged |
```

**GitHub:**

![Screenshot of a GitHub Markdown table with two columns of differing width.](table-varied-columns-rendered.png)

**WinPrint (live):**

| Command | Description |
| --- | --- |
| git status | List all new or modified files |
| git diff | Show file differences that haven't been staged |

## Inline formatting inside cells

**Source:**

```markdown
| Command | Description |
| --- | --- |
| `git status` | List all *new or modified* files |
| `git diff` | Show file differences that **haven't been** staged |
```

**GitHub:**

![Screenshot of a GitHub Markdown table with commands as code and styled descriptions.](table-inline-formatting-rendered.png)

**WinPrint (live):**

| Command | Description |
| --- | --- |
| `git status` | List all *new or modified* files |
| `git diff` | Show file differences that **haven't been** staged |

## Column alignment

**Source:**

```markdown
| Left-aligned | Center-aligned | Right-aligned |
| :---         |     :---:      |          ---: |
| git status   | git status     | git status    |
| git diff     | git diff       | git diff      |
```

**GitHub:**

![Screenshot of a Markdown table with left, center, and right aligned columns.](table-aligned-text-rendered.png)

**WinPrint (live):**

| Left-aligned | Center-aligned | Right-aligned |
| :---         |     :---:      |          ---: |
| git status   | git status     | git status    |
| git diff     | git diff       | git diff      |

## Escaped pipe character

**Source:**

```markdown
| Name     | Character |
| ---      | ---       |
| Backtick | `         |
| Pipe     | \|        |
```

**GitHub:**

![Screenshot of a Markdown table showing escaped pipe characters.](table-escaped-character-rendered.png)

**WinPrint (live):**

| Name     | Character |
| ---      | ---       |
| Backtick | `         |
| Pipe     | \|        |

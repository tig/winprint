# Basic writing and formatting syntax

Create sophisticated formatting for your prose and code on GitHub with simple syntax.
This fixture is a visual regression tour for MarkdownCte.

> **How to read these fixtures:** for each feature you get (a) a fenced **source** code block,
> (b) a **GitHub** screenshot of how GitHub renders it, and (c) the same Markdown **live** so
> WinPrint can render it. Compare (b) and (c) to see what works and what does not yet.


## Headings

**Source:**

```markdown
# A first-level heading
## A second-level heading
### A third-level heading
```

**GitHub:**

![Screenshot of rendered GitHub Markdown showing sample h1, h2, and h3 headers.](headings-rendered.png)

**WinPrint (live):**

# A first-level heading
## A second-level heading
### A third-level heading

## Table of contents UI (GitHub only)

**Source:** (GitHub file header UI)

```text
Outline menu in the file header when a document has two or more headings
```

**GitHub:**

![Screenshot of a README with the table of contents drop-down exposed.](headings-toc.png)

## Styling text

**Source:**

```markdown
**This is bold text**

_This text is italicized_

~~This was mistaken text~~

**This text is _extremely_ important**

***All this text is important***

This is a <sub>subscript</sub> text

This is a <sup>superscript</sup> text

This is an <ins>underlined</ins> text
```

**WinPrint (live):**

**This is bold text**

_This text is italicized_

~~This was mistaken text~~

**This text is _extremely_ important**

***All this text is important***

This is a <sub>subscript</sub> text

This is a <sup>superscript</sup> text

This is an <ins>underlined</ins> text

## Quoting text

**Source:**

```markdown
Text that is not a quote

> Text that is a quote
```

**GitHub:**

![Screenshot of rendered GitHub Markdown showing normal vs quoted text.](quoted-text-rendered.png)

**WinPrint (live):**

Text that is not a quote

> Text that is a quote

## Inline code

**Source:**

```markdown
Use `git status` to list all new or modified files that haven't yet been committed.
```

**GitHub:**

![Screenshot of inline code in a fixed-width typeface.](inline-code-rendered.png)

**WinPrint (live):**

Use `git status` to list all new or modified files that haven't yet been committed.

## Fenced code block

**Source:**

````markdown
Some basic Git commands are:
```
git status
git add
git commit
```
````

**GitHub:**

![Screenshot of a simple code block without syntax highlighting.](code-block-rendered.png)

**WinPrint (live):**

Some basic Git commands are:

```
git status
git add
git commit
```

## Supported color models

**Source:**

```markdown
The background color is `#ffffff` for light mode and `#000000` for dark mode.

Also: `#0969DA`, `rgb(9, 105, 218)`, `hsl(212, 92%, 45%)`
```

**GitHub:**

![Screenshot of HEX color swatches rendered next to color codes.](supported-color-models-rendered.png)

![HEX swatch](supported-color-models-hex-rendered.png)
![RGB swatch](supported-color-models-rgb-rendered.png)
![HSL swatch](supported-color-models-hsl-rendered.png)

**WinPrint (live):**

The background color is `#ffffff` for light mode and `#000000` for dark mode.

Also: `#0969DA`, `rgb(9, 105, 218)`, `hsl(212, 92%, 45%)`

## Links

**Source:**

```markdown
This site was built using [GitHub Pages](https://pages.github.com/).
```

**GitHub:**

![Screenshot of a blue hyperlink for GitHub Pages.](link-rendered.png)

**WinPrint (live):**

This site was built using [GitHub Pages](https://pages.github.com/).

## Section links

**Source:** (GitHub UI for heading anchors)

```text
Hover a heading and click the link icon to copy the section URL
```

**GitHub:**

![Screenshot of a heading with a section link icon.](readme-links.png)

**WinPrint (live):**

See [Headings](#headings) and [Links](#links) in this file.

## Images

**Source:**

```markdown
![Screenshot of an Octocat image embedded in Markdown.](image-rendered.png)
```

**GitHub:**

![Screenshot of a comment showing an embedded Octocat image.](image-rendered.png)

**WinPrint (live):**

![Screenshot of an Octocat image embedded in Markdown.](image-rendered.png)

## Unordered list

**Source:**

```markdown
- George Washington
* John Adams
+ Thomas Jefferson
```

**GitHub:**

![Screenshot of a bulleted list of the first three American presidents.](unordered-list-rendered.png)

**WinPrint (live):**

- George Washington
* John Adams
+ Thomas Jefferson

## Ordered list

**Source:**

```markdown
1. James Madison
2. James Monroe
3. John Quincy Adams
```

**GitHub:**

![Screenshot of a numbered list of the fourth through sixth American presidents.](ordered-list-rendered.png)

**WinPrint (live):**

1. James Madison
2. James Monroe
3. John Quincy Adams

## Nested list

**Source:**

```markdown
1. First list item
   - First nested list item
     - Second nested list item
```

**GitHub:**

![Screenshot of nested list alignment in an editor.](nested-list-alignment.png)

![Screenshot of nested list rendering with two levels of bullets under a numbered item.](nested-list-example-1.png)

**WinPrint (live):**

1. First list item
   - First nested list item
     - Second nested list item

## Nested under large numbers

**Source:**

```markdown
100. First list item
     - First nested list item
```

**GitHub:**

![Screenshot of nesting under item 100.](nested-list-example-3.png)

**WinPrint (live):**

100. First list item
     - First nested list item

**Source (two levels):**

```markdown
100. First list item
     - First nested list item
       - Second nested list item
```

**GitHub:**

![Screenshot of two nesting levels under item 100.](nested-list-example-2.png)

**WinPrint (live):**

100. First list item
     - First nested list item
       - Second nested list item

## Task lists

**Source:**

```markdown
- [x] #739
- [ ] https://github.com/octo-org/octo-repo/issues/740
- [ ] Add delight to the experience when all tasks are complete :tada:
```

**GitHub:**

![Screenshot of a rendered task list with issue titles.](task-list-rendered-simple.png)

**WinPrint (live):**

- [x] #739
- [ ] https://github.com/octo-org/octo-repo/issues/740
- [ ] Add delight to the experience when all tasks are complete :tada:

## Mentions

**Source:**

```markdown
@github/support What do you think about these updates?
```

**GitHub:**

![Screenshot of a team mention rendered as bold clickable text.](mention-rendered.png)

**WinPrint (live):**

@github/support What do you think about these updates?

## Emoji shortcodes

**Source:**

```markdown
@octocat :+1: This PR looks great - it's ready to merge! :shipit:
```

**GitHub:**

![Screenshot of emoji shortcodes rendered as emoji.](emoji-rendered.png)

**WinPrint (live):**

@octocat :+1: This PR looks great - it's ready to merge! :shipit:

## Footnotes

**Source:**

```markdown
Here is a simple footnote[^1].

A footnote can also have multiple lines[^2].

[^1]: My reference.
[^2]: To add line breaks within a footnote, add 2 spaces to the end of a line.  
This is a second line.
```

**GitHub:**

![Screenshot of rendered footnotes with superscript numbers.](footnote-rendered.png)

**WinPrint (live):**

Here is a simple footnote[^1].

A footnote can also have multiple lines[^2].

[^1]: My reference.
[^2]: To add line breaks within a footnote, add 2 spaces to the end of a line.  
This is a second line.

## Alerts

**Source:**

```markdown
> [!NOTE]
> Useful information that users should know, even when skimming content.

> [!TIP]
> Helpful advice for doing things better or more easily.

> [!IMPORTANT]
> Key information users need to know to achieve their goal.

> [!WARNING]
> Urgent info that needs immediate user attention to avoid problems.

> [!CAUTION]
> Advises about risks or negative outcomes of certain actions.
```

**GitHub:**

![Screenshot of Note, Tip, Important, Warning, and Caution alerts.](alerts-rendered.png)

**WinPrint (live):**

> [!NOTE]
> Useful information that users should know, even when skimming content.

> [!TIP]
> Helpful advice for doing things better or more easily.

> [!IMPORTANT]
> Key information users need to know to achieve their goal.

> [!WARNING]
> Urgent info that needs immediate user attention to avoid problems.

> [!CAUTION]
> Advises about risks or negative outcomes of certain actions.

## Escaping Markdown

**Source:**

```markdown
Let's rename \*our-new-project\* to \*our-old-project\*.
```

**GitHub:**

![Screenshot of escaped asterisks not becoming italics.](escaped-character-rendered.png)

**WinPrint (live):**

Let's rename \*our-new-project\* to \*our-old-project\*.

## Viewing source (GitHub UI)

**Source:** (GitHub file viewer)

```text
Click Code at the top of a Markdown file to disable rendering
```

**GitHub:**

![Screenshot of the Code button on a Markdown file in a repository.](display-markdown-as-source-global-nav-update.png)

## Further reading

* [Working with advanced formatting](working-with-advanced-formatting.md)
* [GitHub Flavored Markdown Spec](https://github.github.com/gfm/)

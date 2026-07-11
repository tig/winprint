# Creating and highlighting code blocks

Share samples of code with fenced code blocks and syntax highlighting.

> **How to read these fixtures:** for each feature you get (a) a fenced **source** code block,
> (b) a **GitHub** screenshot of how GitHub renders it, and (c) the same Markdown **live** so
> WinPrint can render it. Compare (b) and (c) to see what works and what does not yet.


## Fenced code block

**Source:**

````markdown
```
function test() {
  console.log("notice the blank line before this function?");
}
```
````

**GitHub:**

![Screenshot of rendered GitHub Markdown showing a fenced code block.](fenced-code-block-rendered.png)

**WinPrint (live):**

```
function test() {
  console.log("notice the blank line before this function?");
}
```

## Showing backticks inside a fence

**Source:**

`````markdown
````
```
Look! You can see my backticks.
```
````
`````

**GitHub:**

![Screenshot of triple backticks visible inside a quadruple-backtick fence.](fenced-code-show-backticks-rendered.png)

**WinPrint (live):**

````
```
Look! You can see my backticks.
```
````

## Syntax highlighting (Ruby)

**Source:**

````markdown
```ruby
require 'redcarpet'
markdown = Redcarpet.new("Hello World!")
puts markdown.to_html
```
````

**GitHub:**

![Screenshot of Ruby code with syntax highlighting on GitHub.](code-block-syntax-highlighting-rendered.png)

**WinPrint (live):**

```ruby
require 'redcarpet'
markdown = Redcarpet.new("Hello World!")
puts markdown.to_html
```

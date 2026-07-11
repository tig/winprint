# Organizing information with collapsed sections

You can streamline your Markdown by creating a collapsed section with the <details> tag.

## Creating a collapsed section

You can temporarily obscure sections of your Markdown by creating a collapsed section that the reader can choose to expand. For example, when you want to include technical details in an issue comment that may not be relevant or interesting to every reader, you can put those details in a collapsed section.

Any Markdown within the `<details>` block will be collapsed until the reader expands the details.

Within the `<details>` block, use the `<summary>` tag to let readers know what is inside.

````markdown
<details>

<summary>Tips for collapsed sections</summary>

### You can add a header

You can add text within a collapsed section.

You can add an image or a code block, too.

```ruby
   puts "Hello World"
```

</details>
````

The Markdown inside the `<summary>` label will be collapsed by default:

![Screenshot of the Markdown above on this page as rendered on GitHub, showing a right-facing arrow and the header "Tips for collapsed sections."](collapsed-section-view.png)

After a reader expands the details, they look like this:

![Screenshot of the Markdown above on this page as rendered on GitHub. The collapsed section contains headers, text, images, and code blocks.](open-collapsed-section.png)

Here is a live collapsed section for WinPrint to exercise:

<details>

<summary>Tips for collapsed sections</summary>

### You can add a header

You can add text within a collapsed section.

You can add an image or a code block, too.

```ruby
   puts "Hello World"
```

</details>

Optionally, to make the section display as open by default, add the `open` attribute to the `<details>` tag:

```html
<details open>
```

<details open>

<summary>This section starts open</summary>

Printed documents cannot toggle open/closed, so engines may always expand details — or always show summary only. Either behavior is worth testing.

</details>

## Further reading

* [GitHub Flavored Markdown Spec](https://github.github.com/gfm/)
* [Basic writing and formatting syntax](github.md)

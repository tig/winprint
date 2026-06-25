# wp — interactive preview (default)

Running `wp` with no command opens the winprint interactive Terminal.Gui viewer. Pass a file to open
it there directly — `wp Program.cs` is the normal way to preview a file. (`tui` is the internal name
of this default command; you don't need to type it.)

```sh
wp [options] [file…]
```

## Options

{{OPTIONS}}

## Examples

```sh
wp Program.cs
wp Program.cs --printer "Microsoft Print to PDF" --sheet "Default 2-Up"
wp Program.cs --landscape --from-sheet 1 --to-sheet 4
```

# wp print — headless printing

Print one or more files directly to a printer **without opening the UI**. Files are loaded and the
shared print options applied through the same engine and sheet definitions the TUI/GUI use, so the
output matches the preview. With `--what-if`, `wp print` reports how many sheets each file would
produce without sending anything to a printer.

```sh
wp print [options] [file…]
```

## Options

{{OPTIONS}}

## Examples

```sh
wp print Program.cs
wp print Program.cs --printer "Microsoft Print to PDF" --sheet "Default 2-Up"
wp print *.cs --landscape --from-sheet 1 --to-sheet 4
wp print Program.cs --what-if      # count sheets without printing
```

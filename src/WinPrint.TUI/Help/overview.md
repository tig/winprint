# winprint — `wp` {{VERSION}}

`wp` is winprint's cross-platform front end. Run it with **no command** to open the interactive
print-preview TUI; pass a file to open it there directly. Use a subcommand to print headlessly or
open the GUI.

```sh
wp [options] [file…]          # open the interactive TUI (optionally on a file) — the default
wp print [options] [file…]    # print file(s) without opening the UI
wp gui [options] [file…]      # open the MAUI GUI (Windows/macOS)
```

## Commands

{{COMMANDS}}

Run `wp help <command>` (e.g. `wp help print`) for a command's full options and examples.

## Global Options

These apply to every command. The canonical print options (`--sheet`, `--landscape`, …) are shared
by the `wp` CLI, the TUI, and the MAUI GUI, so a name means the same thing everywhere.

{{GLOBAL_OPTIONS}}

## Examples

```sh
wp Program.cs                                      # preview a file in the TUI
wp print Program.cs --printer "Microsoft Print to PDF" --sheet "Default 2-Up"
wp print *.cs --landscape                          # print without the UI
wp gui ./testfiles/Program.cs                      # open the GUI on a file
wp --version
```

See `docs/users-guide.md` for sheet definitions, header/footer macros, and content types.

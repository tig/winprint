# winprint.cli agent guide

`winprint.cli` prints source code, text, and HTML through WinPrint's core renderer.

Use `winprint.cli print <file> --what-if --json` to count sheets without printing and return a JSON envelope. Use `--language` to override automatic language detection, `--printer` to select a printer, `--sheet` to select a sheet definition, and `--gui` to open the WinPrint preview UI.

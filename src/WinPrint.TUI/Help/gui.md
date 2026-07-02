# wp gui — graphical preview

Launch the WinPrint MAUI GUI (Windows and macOS), optionally on one or more files. Any files and
shared print options are forwarded to the GUI, which loads the first file and applies the options.

```sh
wp gui [options] [file…]
```

## Options

{{OPTIONS}}

## Examples

```sh
wp gui                                 # open the GUI
wp gui ./testfiles/Program.cs          # open the GUI with a file loaded
wp gui report.cs --sheet "Default 2-Up" --landscape
```

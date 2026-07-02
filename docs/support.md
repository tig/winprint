# Support

## Get Help

* Read the [User's Guide](users-guide.md)
* Ask a question using [GitHub Issues](https://github.com/tig/winprint/issues).

## Submit bugs or Request Improvements

* Submit bugs: Use [GitHub Issues](https://github.com/tig/winprint/issues).
* Suggest improvements: Use [GitHub Issues](https://github.com/tig/winprint/issues).
* Download the source and submit pull requests: [winprint on GitHub](https://github.com/tig/winprint).

**winprint** writes diagnostic logs to a `logs` folder alongside its settings. On Windows that's `%appdata%\Kindel\winprint\logs` (or next to the executable when running in portable mode); on macOS and Linux the `logs` folder sits next to the `wp` executable (inside `WinPrint.app` for the GUI). Run the `wp` command line with `--debug` for more detail.

Additional printing diagnostics can be turned on via settings in the `WinPrint.config.json` configuration file.
# Installation Instructions

***WinPrint*** works well (on my machine), and I've had a dozen or so users verify it works well for them; see [Issues](https://github.com/tig/winprint/issues).*

*Please report any problems or feature requests [here](https://github.com/tig/winprint/issues).*

## Installing

### On Windows

There is no installer in this branch. Build from source and run `winprint.exe` from the `src\WinPrint.cli\bin` output folder until the installer is recreated.

The legacy PowerShell CmdLet (`out-winprint`) remains available as `WinPrint.PowerShell.dll`, but is deprecated and no longer the preferred command-line surface.

### On Linux

Good luck. Start by cloning the *winprint* repo, installing .NET Core 3, and building. Then I have some C++ libraries I'm using under the covers that you'll have to grab and build too. You'll need JUST THE RIGHT version of `libgdiplus`. It might help if you re-compile your kernel. You know, all the stuff reqruied to make anything work on one of several Linux distros. Did I mention how much [I hate linux](https://ceklog.kindel.com/2011/10/21/i-sincerely-tried-but-i-still-hate-linux/)? Seriously, it does work on Linux. But until someone begs me for it, I'm not spending another second on trying to build an installer. Submit an Issue (or Pull Request!) if you really want help.

### On Mac

I haven't even tried as my old Macbook Air died and Apple wont fix it. I'll buy beer for someone who contributes to getting the Mac version working. It should not be hard given I've proven the stupid things works on Linux already.

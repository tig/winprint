---
title: Install
---
***winprint** 2.0 is in pre-beta (alpha) stage. It works well (on my machine), but I'm embarrased by a few bugs and performance issues that I want to fix before I declare beta; see [Issues](https://github.com/tig/winprint/issues).*

*Please report any problems or feature requests [here](https://github.com/tig/winprint/issues).* 

## Installing

### On Windows

Go to [Releases](https://github.com/tig/winprint/releases) to grab the latest MSI file for Windows. Download the MSI and run it. Once installed, *winprint* can be started from the Start menu or command line. The installer adds *winprint* to the `PATH` so that typing `winprint` from any command prompt will run the app.

### On Linux

 Good luck. Start by cloning the *winprint* repo, installing .NET Core 3, and buildng. Then I have some C++ libraries I'm using under the covers that you'll have to grab and build too. You'll need JUST THE RIGHT version of `libgdiplus`. It might help if you re-compile your kernel. You know, all the stuff reqruied to make anything work on one of several Linux distros. Did I mention how much [I hate linux](https://ceklog.kindel.com/2011/10/21/i-sincerely-tried-but-i-still-hate-linux/)? Seriously, it does work on Linux. But until someone begs me for it, I'm not spending another second on trying to build an installer. Submit an Issue (or Pull Request!) if you really want help.

### On Mac

I haven't even tried as my old Macbook Air died and Apple wont fix it. I refuse to buy anything from Apple (except for for my family who are all all-in with Apple...sigh). I'll buy beer for someone who contributes to getting the Mac version working. It should not be hard given I've proven the stupid things works on Linux already.
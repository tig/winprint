// Captures the process's working directory at load time, before the Xamarin/Mac
// Catalyst startup code inside main() chdirs to the .app bundle. dyld runs library
// constructors before main, so this still sees the directory the app was launched
// from (the shell sets it when spawning — even pwsh, whose own process cwd and PWD
// env var are unreliable). Managed code reads WINPRINT_LAUNCH_CWD to resolve
// relative file arguments.
//
// Rebuild (both slices, from this directory — commit the resulting dylib):
//   SDK=$(xcrun --sdk macosx --show-sdk-path)
//   clang -dynamiclib -O2 -target arm64-apple-ios15.0-macabi  -isysroot "$SDK" \
//         -install_name @rpath/liblaunchcwd.dylib -Wl,-headerpad_max_install_names \
//         launchcwd.c -o liblaunchcwd-arm64.dylib
//   clang -dynamiclib -O2 -target x86_64-apple-ios15.0-macabi -isysroot "$SDK" \
//         -install_name @rpath/liblaunchcwd.dylib -Wl,-headerpad_max_install_names \
//         launchcwd.c -o liblaunchcwd-x64.dylib
//   lipo -create liblaunchcwd-arm64.dylib liblaunchcwd-x64.dylib -output liblaunchcwd.dylib
//   rm liblaunchcwd-arm64.dylib liblaunchcwd-x64.dylib

#include <limits.h>
#include <stdlib.h>
#include <unistd.h>

__attribute__((constructor))
static void winprint_capture_launch_cwd(void)
{
    char cwd[PATH_MAX];
    if (getcwd(cwd, sizeof cwd) != NULL) {
        setenv("WINPRINT_LAUNCH_CWD", cwd, 1);
    }
}

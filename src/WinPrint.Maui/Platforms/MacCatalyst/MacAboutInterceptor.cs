// Copyright Kindel, LLC - http://www.kindel.com
// Published under the MIT License at https://github.com/tig/winprint

using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Foundation;

namespace WinPrint.Maui;

/// <summary>
///     Fixes the Mac About box, which otherwise shows "1 (1)" as the version (issue #138).
///     <para>
///         The Catalyst Info.plist <c>CFBundleShortVersionString</c>/<c>CFBundleVersion</c> are not
///         wired to the real (GitVersion-stamped) version, so the standard About panel shows
///         "1 (1)". The real version lives on the built assembly. .NET's Catalyst bindings don't
///         expose <c>NSApplication</c>, so — like <see cref="MacQuitInterceptor" /> — we reach it
///         through the Objective-C runtime and override
///         <c>-[NSApplication orderFrontStandardAboutPanel:]</c> to present the standard panel with
///         options carrying the assembly version and the CPU architecture, e.g.
///         "3.0.0 (Apple Silicon · arm64)".
///     </para>
/// </summary>
internal static class MacAboutInterceptor
{
    private const string Libobjc = "/usr/lib/libobjc.A.dylib";

    [DllImport(Libobjc)]
    private static extern IntPtr objc_getClass(string name);

    [DllImport(Libobjc)]
    private static extern IntPtr sel_registerName(string name);

    [DllImport(Libobjc)]
    private static extern IntPtr class_getInstanceMethod(IntPtr cls, IntPtr sel);

    [DllImport(Libobjc)]
    private static extern IntPtr method_setImplementation(IntPtr method, IntPtr imp);

    [DllImport(Libobjc)]
    private static extern bool class_addMethod(IntPtr cls, IntPtr sel, IntPtr imp, string types);

    [DllImport(Libobjc)]
    private static extern IntPtr objc_msgSend(IntPtr receiver, IntPtr selector, IntPtr arg);

    /// <summary>
    ///     Overrides the About menu's panel action on NSApplication. Call once, after launch.
    /// </summary>
    public static unsafe void Install()
    {
        try
        {
            IntPtr nsAppClass = objc_getClass("NSApplication");
            if (nsAppClass == IntPtr.Zero)
            {
                return;
            }

            IntPtr sel = sel_registerName("orderFrontStandardAboutPanel:");
            IntPtr imp = (IntPtr)(delegate* unmanaged[Cdecl]<IntPtr, IntPtr, IntPtr, void>)&ShowAboutPanel;

            // Replace the existing implementation (the common case); add it if absent.
            IntPtr method = class_getInstanceMethod(nsAppClass, sel);
            if (method != IntPtr.Zero)
            {
                method_setImplementation(method, imp);
            }
            else
            {
                class_addMethod(nsAppClass, sel, imp, "v@:@");
            }
        }
        catch (Exception)
        {
            // Best-effort (mirrors MacQuitInterceptor): if the runtime shifts in a future macOS we
            // fall back to the stock panel rather than crash on launch.
        }
    }

    [UnmanagedCallersOnly(CallConvs = [typeof(CallConvCdecl)])]
    private static void ShowAboutPanel(IntPtr self, IntPtr cmd, IntPtr sender)
    {
        // "ApplicationVersion" is shown after the app name; "Version" is shown in parentheses.
        // Unset keys (name, icon, copyright) fall back to Info.plist. => "3.0.0 (Apple Silicon · arm64)".
        using var options = new NSMutableDictionary
        {
            [(NSString)"ApplicationVersion"] = (NSString)GetVersion(),
            [(NSString)"Version"] = (NSString)GetArchitecture(),
        };

        objc_msgSend(self, sel_registerName("orderFrontStandardAboutPanelWithOptions:"), options.Handle);
    }

    private static string GetVersion()
    {
        string? version = FileVersionInfo.GetVersionInfo(typeof(MacAboutInterceptor).Assembly.Location).ProductVersion;
        if (string.IsNullOrWhiteSpace(version))
        {
            version = typeof(MacAboutInterceptor).Assembly.GetName().Version?.ToString();
        }

        // Drop SemVer build metadata (e.g. "+Branch.main.Sha.1234abcd").
        int plus = version?.IndexOf('+') ?? -1;
        return plus >= 0 ? version![..plus] : version ?? "unknown";
    }

    private static string GetArchitecture()
    {
        return RuntimeInformation.ProcessArchitecture switch
        {
            Architecture.Arm64 => "Apple Silicon · arm64",
            Architecture.X64 => "Intel · x86_64",
            var other => other.ToString(),
        };
    }
}

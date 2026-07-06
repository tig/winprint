// Copyright Kindel, LLC - http://www.kindel.com
// Published under the MIT License at https://github.com/tig/winprint

using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace WinPrint.Maui;

/// <summary>
///     Gives the Mac Catalyst app the same "prompt to save changed sheet settings on exit" behavior the
///     Windows build gets from WinUI's <c>AppWindow.Closing</c>.
///     <para>
///         Catalyst exposes no cancellable window-close/quit event to managed code: a ⌘Q (or App ▸ Quit, or
///         the red close button) is handled by AppKit and terminates the process before any
///         <c>UIApplicationDelegate</c> callback can stop it — verified empirically (a <c>UIKeyCommand</c>
///         for ⌘Q and an exported <c>applicationShouldTerminate:</c> on the <c>UIApplicationDelegate</c> both
///         fail to fire). The only gate macOS offers is AppKit's
///         <c>-[NSApplicationDelegate applicationShouldTerminate:]</c>. We reach the real
///         <c>NSApplication</c> delegate through the Objective-C runtime and install that method on it,
///         returning <c>NSTerminateLater</c> so the (async) save prompt can run, then calling
///         <c>replyToApplicationShouldTerminate:</c> with the user's decision.
///     </para>
/// </summary>
internal static class MacQuitInterceptor
{
    private const string Libobjc = "/usr/lib/libobjc.A.dylib";

    // NSApplicationTerminateReply
    private const nuint TerminateCancel = 0;
    private const nuint TerminateNow = 1;
    private const nuint TerminateLater = 2;

    [DllImport(Libobjc)]
    private static extern IntPtr objc_getClass(string name);

    [DllImport(Libobjc)]
    private static extern IntPtr sel_registerName(string name);

    [DllImport(Libobjc)]
    private static extern IntPtr objc_msgSend(IntPtr receiver, IntPtr selector);

    [DllImport(Libobjc)]
    private static extern IntPtr object_getClass(IntPtr obj);

    [DllImport(Libobjc)]
    private static extern bool class_addMethod(IntPtr cls, IntPtr sel, IntPtr imp, string types);

    [DllImport(Libobjc)]
    private static extern IntPtr class_getInstanceMethod(IntPtr cls, IntPtr sel);

    [DllImport(Libobjc)]
    private static extern IntPtr method_setImplementation(IntPtr method, IntPtr imp);

    [DllImport(Libobjc, EntryPoint = "objc_msgSend")]
    private static extern void objc_msgSend_bool(IntPtr receiver, IntPtr selector,
        [MarshalAs(UnmanagedType.I1)] bool arg);

    /// <summary>
    ///     Installs the <c>applicationShouldTerminate:</c> hook on the live NSApplication delegate. Call once,
    ///     after the app has launched (the delegate exists by <c>FinishedLaunching</c>).
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

            IntPtr nsApp = objc_msgSend(nsAppClass, sel_registerName("sharedApplication"));
            IntPtr del = nsApp == IntPtr.Zero ? IntPtr.Zero : objc_msgSend(nsApp, sel_registerName("delegate"));
            if (del == IntPtr.Zero)
            {
                return;
            }

            IntPtr delClass = object_getClass(del);
            IntPtr sel = sel_registerName("applicationShouldTerminate:");
            IntPtr imp = (IntPtr)(delegate* unmanaged[Cdecl]<IntPtr, IntPtr, IntPtr, nuint>)&ShouldTerminate;

            // "Q@:@" = returns NSUInteger; args are self, _cmd, sender. Add it if the AppKit delegate doesn't
            // already implement it (the common case); otherwise replace the existing implementation.
            if (!class_addMethod(delClass, sel, imp, "Q@:@"))
            {
                IntPtr method = class_getInstanceMethod(delClass, sel);
                if (method != IntPtr.Zero)
                {
                    method_setImplementation(method, imp);
                }
            }
        }
        catch (Exception)
        {
            // Reaching AppKit through the runtime is best-effort; if anything changes in a future macOS we
            // simply fall back to the old (no-prompt) behavior rather than crash on launch.
        }
    }

    /// <summary>
    ///     Finalizes a deferred termination (after <see cref="TerminateLater" />) with the user's decision.
    /// </summary>
    public static void ReplyToTerminate(bool shouldTerminate)
    {
        IntPtr nsApp = objc_msgSend(objc_getClass("NSApplication"), sel_registerName("sharedApplication"));
        if (nsApp != IntPtr.Zero)
        {
            objc_msgSend_bool(nsApp, sel_registerName("replyToApplicationShouldTerminate:"), shouldTerminate);
        }
    }

    [UnmanagedCallersOnly(CallConvs = [typeof(CallConvCdecl)])]
    private static nuint ShouldTerminate(IntPtr self, IntPtr cmd, IntPtr sender)
    {
        // Runs on the main thread (AppKit calls applicationShouldTerminate: there).
        MainPage? page = MainPage.Current;
        if (page is null || !page.HasUnsavedSheetChangesForExit)
        {
            return TerminateNow;
        }

        // Defer termination, run the save prompt, then reply with the user's choice.
        page.BeginExitPromptThenReply();
        return TerminateLater;
    }
}

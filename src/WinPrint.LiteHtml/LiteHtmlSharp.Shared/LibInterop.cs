using System;
using System.Runtime.InteropServices;

#if AUTO_UTF8
using Utf8Str = System.String;
#else
using Utf8Str = System.IntPtr;
#endif

namespace LiteHtmlSharp {

    public static class NativeMethods {
        // here we just use "Foo" and at runtime we load "Foo.dll" dynamically
        // from any path on disk depending on the logic you want to implement
        [DllImport("litehtml_", EntryPoint = "Init")]
        private static extern void Init();

        [DllImport("kernel32")]
        private static extern IntPtr LoadLibrary(string dllname);

        [DllImport("kernel32")]
        private static extern void FreeLibrary(IntPtr handle);

        private sealed class LibraryUnloader {
            internal LibraryUnloader(IntPtr handle) {
                this.handle = handle;
            }

            ~LibraryUnloader() {
                if (handle != null) {
                    FreeLibrary(handle);
                }
            }

            private readonly IntPtr handle;

        } // LibraryUnloader

        private static readonly LibraryUnloader unloader;

        static NativeMethods() {
            string path;

            if (IntPtr.Size == 4) {
                path = "path/to/the/32/bit/Foo.dll";
            }
            else {
                path = "path/to/the/64/bit/Foo.dll";
            }

            var handle = LoadLibrary(path);

            if (handle == null) {
                throw new DllNotFoundException("unable to find the native Foo library: " + path);
            }

            unloader = new LibraryUnloader(handle);
        }
    }
    public class LibInterop : ILibInterop {

#if WINDOWS
        private const string LiteHtmlLibFile = "LiteHtmlLib.dll";
        public const CharSet cs = CharSet.Unicode;
#else
        const string LiteHtmlLibFile = "litehtml";
        public const CharSet cs = CharSet.Ansi;
#endif

        private const string LiteHtmlLibFile_x64 = "x64\\LiteHtmlLib.dll";
        private const string LiteHtmlLibFile_x86 = "x86\\LiteHtmlLib.dll";

        public const CallingConvention cc = CallingConvention.Cdecl;
        private static readonly Lazy<LibInterop> _instance = new Lazy<LibInterop>(() => new LibInterop());
        public static LibInterop Instance => _instance.Value;

        private LibInterop() {
            if (Environment.OSVersion.Platform == PlatformID.Win32NT) {
                LoadLibrary(Environment.Is64BitProcess ? LiteHtmlLibFile_x64 : LiteHtmlLibFile_x86);
            }
        }

        public void InitDocument(ref DocumentCalls document, InitCallbacksFunc initFunc) {
            Init(ref document, initFunc);
        }

        public Utf8Str LibEchoTest(Utf8Str testStr) {
            return EchoTest(testStr);
        }

        [DllImport("kernel32.dll")]
        private static extern IntPtr LoadLibrary(string dllToLoad);

        [DllImport(LiteHtmlLibFile, CallingConvention = cc, SetLastError = true)]
        private static extern void Init(ref DocumentCalls document, InitCallbacksFunc initFunc);

        [DllImport(LiteHtmlLibFile, CallingConvention = cc, SetLastError = true)]
        private static extern int GetWidthTest(Utf8Str docContainer);

        [DllImport(LiteHtmlLibFile, CallingConvention = cc, SetLastError = true)]
        private static extern Utf8Str CreateDocContainer();

        [DllImport(LiteHtmlLibFile, CallingConvention = cc, SetLastError = true)]
        private static extern Utf8Str EchoTest(Utf8Str testStr);


    }
}


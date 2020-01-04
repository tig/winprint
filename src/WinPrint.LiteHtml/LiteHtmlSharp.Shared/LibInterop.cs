using System;
using System.Runtime.InteropServices;
using System.Text;

#if AUTO_UTF8
using Utf8Str = System.String;
#else
using Utf8Str = System.IntPtr;
#endif

namespace LiteHtmlSharp
{
    public class LibInterop : ILibInterop
    {

#if WINDOWS
        const string LiteHtmlLibFile = "LiteHtmlLib.dll";
        public const CharSet cs = CharSet.Unicode;
#else
        const string LiteHtmlLibFile = "litehtml";
        public const CharSet cs = CharSet.Ansi;
#endif

        const string LiteHtmlLibFile_x64 = "x64\\LiteHtmlLib.dll";
        const string LiteHtmlLibFile_x86 = "x86\\LiteHtmlLib.dll";

        public const CallingConvention cc = CallingConvention.Cdecl;

        readonly static Lazy<LibInterop> _instance = new Lazy<LibInterop>(() => new LibInterop());
        public static LibInterop Instance => _instance.Value;

        LibInterop()
        {
            if (Environment.OSVersion.Platform == PlatformID.Win32NT)
            {
                LoadLibrary(Environment.Is64BitProcess ? LiteHtmlLibFile_x64 : LiteHtmlLibFile_x86);
            }
        }

        public void InitDocument(ref DocumentCalls document, InitCallbacksFunc initFunc) => Init(ref document, initFunc);

        public Utf8Str LibEchoTest(Utf8Str testStr) => EchoTest(testStr);

        [DllImport("kernel32.dll")]
        private static extern IntPtr LoadLibrary(string dllToLoad);

        [DllImport(LiteHtmlLibFile, CallingConvention = cc, SetLastError = true)]
        static extern void Init(ref DocumentCalls document, InitCallbacksFunc initFunc);

        [DllImport(LiteHtmlLibFile, CallingConvention = cc, SetLastError = true)]
        static extern int GetWidthTest(Utf8Str docContainer);

        [DllImport(LiteHtmlLibFile, CallingConvention = cc, SetLastError = true)]
        static extern Utf8Str CreateDocContainer();

        [DllImport(LiteHtmlLibFile, CallingConvention = cc, SetLastError = true)]
        static extern Utf8Str EchoTest(Utf8Str testStr);


    }
}


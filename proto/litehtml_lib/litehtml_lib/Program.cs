using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using Utf8Str = System.IntPtr;

namespace litehtml_lib
{
    class Program
    {
        static void Main(string[] args)
        {
            Console.WriteLine("Calling CreateDocContainer...");
            Utf8Str str = NativeMethods.CreateDocContainer();
            Console.WriteLine($"Result: {str}");

        }
    }

    public static class NativeMethods
    {
        const string LibFile = "litehtml";
        public const CharSet cs = CharSet.Unicode;
        public const CallingConvention cc = CallingConvention.Cdecl;

        // here we just use LibFile and at runtime we load "{LibFile}.dll" dynamically
        // from any path on disk depending on the logic you want to implement

        [DllImport(LibFile, CallingConvention = cc, SetLastError = true)]
        public static extern Utf8Str CreateDocContainer();
 
        [DllImport("kernel32")]
        private static extern IntPtr LoadLibrary(string dllname);

        static NativeMethods()
        {
            Console.WriteLine("NativeMethods()");
            string path;

            // 64bit?
            if (Environment.Is64BitProcess)
                path = $"x64/{LibFile}.dll";
            else
                path = $"x86/{LibFile}.dll";

            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                path = "win/" + path;
                Console.WriteLine($"Windows! Path = {path}");
                IntPtr handle = LoadLibrary(path);

                if (handle == null)
                    throw new DllNotFoundException("unable to find the native Foo library: " + path);
            }
            else
            {
                path = "linux/" + path;
                Console.WriteLine($"Not Windows! Path = {path}");
            }

        }
    }
}

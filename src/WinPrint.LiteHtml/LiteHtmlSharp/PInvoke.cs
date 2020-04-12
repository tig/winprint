using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Text;

namespace LiteHtmlSharp
{
    public static class PInvoke
    {
#if AUTO_UTF8
        public const CharSet cs = CharSet.Ansi;
#else
        public const CharSet cs = CharSet.Unicode;
#endif

        public const CallingConvention cc = CallingConvention.Cdecl;
    }
}

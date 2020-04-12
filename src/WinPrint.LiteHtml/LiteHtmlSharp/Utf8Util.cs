using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Text;

#if AUTO_UTF8
using Utf8Str = System.String;
#else
using Utf8Str = System.IntPtr;
#endif

namespace LiteHtmlSharp
{
    public static class Utf8Util
    {

#if AUTO_UTF8
        public static string Utf8PtrToString(string utf8)
        {
            return utf8;
        }

        public static string StringToHGlobalUTF8(string str)
        {
            return str;
        }
#else

        public static IntPtr StringToHGlobalUTF8(string str)
        {
            if (str == null)
            {
                return IntPtr.Zero;
            }
            var bytes = Encoding.UTF8.GetBytes(str);
            var ptr = Marshal.AllocHGlobal(bytes.Length + 1);
            Marshal.Copy(bytes, 0, ptr, bytes.Length);
            Marshal.WriteByte(ptr, bytes.Length, 0);
            return ptr;
        }

        public static string Utf8PtrToString(IntPtr utf8)
        {
            if (utf8 == IntPtr.Zero)
            {
                return null;
            }
            var i = 0;
            while (Marshal.ReadByte(utf8, i) != 0)
            {
                i++;
            }
            var bytes = new byte[i];
            Marshal.Copy(utf8, bytes, 0, i);
            return Encoding.UTF8.GetString(bytes, 0, i);
        }

#endif

    }
}

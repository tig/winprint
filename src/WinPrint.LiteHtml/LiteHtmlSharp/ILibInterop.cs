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
    public interface ILibInterop
    {
        void InitDocument(ref DocumentCalls document, InitCallbacksFunc initFunc);

        Utf8Str LibEchoTest(Utf8Str testStr);
    }
}


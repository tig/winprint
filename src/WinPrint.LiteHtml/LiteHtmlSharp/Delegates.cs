using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Runtime.InteropServices;

#if AUTO_UTF8
using Utf8Str = System.String;
#else
using Utf8Str = System.IntPtr;
#endif

namespace LiteHtmlSharp
{
    // test methods
    [UnmanagedFunctionPointer(PInvoke.cc, CharSet = PInvoke.cs)]
    public delegate void TestCallbackFunc(int x, Utf8Str testString);

    [UnmanagedFunctionPointer(PInvoke.cc, CharSet = PInvoke.cs)]
    public delegate void TriggerTestCallbackFunc(IntPtr container, int x, Utf8Str testString);

    // callbacks
    [UnmanagedFunctionPointer(PInvoke.cc, CharSet = PInvoke.cs)]
    public delegate void SetCaptionFunc(Utf8Str caption);

    [UnmanagedFunctionPointer(PInvoke.cc, CharSet = PInvoke.cs)]
    public delegate int GetDefaultFontSizeFunc();

    [UnmanagedFunctionPointer(PInvoke.cc, CharSet = PInvoke.cs)]
    public delegate Utf8Str GetDefaultFontNameFunc();

    [UnmanagedFunctionPointer(PInvoke.cc)]
    public delegate void DrawBordersFunc(UIntPtr hdc, ref borders borders, ref position draw_pos, [MarshalAs(UnmanagedType.I1)] bool root);

    [UnmanagedFunctionPointer(PInvoke.cc, CharSet = PInvoke.cs)]
    public delegate void DrawBackgroundFunc(UIntPtr hdc, Utf8Str image, background_repeat repeat, ref web_color color, ref position pos, ref border_radiuses borderRadiuses, ref position borderBox, [MarshalAs(UnmanagedType.I1)] bool isRoot);

    [UnmanagedFunctionPointer(PInvoke.cc, CharSet = PInvoke.cs)]
    public delegate void GetImageSizeFunc(Utf8Str image, ref size size);

    [UnmanagedFunctionPointer(CallingConvention.Cdecl, CharSet = PInvoke.cs)]
    public delegate void DrawTextFunc(Utf8Str text, UIntPtr font, ref web_color color, ref position pos);

    [UnmanagedFunctionPointer(CallingConvention.Cdecl, CharSet = PInvoke.cs)]
    public delegate int GetTextWidthFunc(Utf8Str text, UIntPtr font);

    [UnmanagedFunctionPointer(CallingConvention.Cdecl, CharSet = PInvoke.cs)]
    public delegate UIntPtr CreateFontFunc(Utf8Str faceName, int size, int weight, font_style italic, uint decoration, ref font_metrics fm);

    [UnmanagedFunctionPointer(CallingConvention.Cdecl, CharSet = PInvoke.cs)]
    public delegate Utf8Str ImportCssFunc(Utf8Str url, Utf8Str baseurl);

    [UnmanagedFunctionPointer(CallingConvention.Cdecl, CharSet = PInvoke.cs)]
    public delegate void GetClientRectFunc(ref position client);

    [UnmanagedFunctionPointer(CallingConvention.Cdecl, CharSet = PInvoke.cs)]
    public delegate void GetMediaFeaturesFunc(ref media_features media);

    [UnmanagedFunctionPointer(CallingConvention.Cdecl, CharSet = PInvoke.cs)]
    public delegate void SetBaseURLFunc(Utf8Str base_url);

    [UnmanagedFunctionPointer(CallingConvention.Cdecl, CharSet = PInvoke.cs)]
    public delegate void OnAnchorClickFunc(Utf8Str url);

    [UnmanagedFunctionPointer(CallingConvention.Cdecl, CharSet = PInvoke.cs)]
    public delegate int PTtoPXFunc(int pt);

    [UnmanagedFunctionPointer(CallingConvention.Cdecl, CharSet = PInvoke.cs)]
    public delegate bool ShouldCreateElementFunc(Utf8Str tag);

    [UnmanagedFunctionPointer(CallingConvention.Cdecl, CharSet = PInvoke.cs)]
    public delegate int CreateElementFunc(Utf8Str tag, Utf8Str attributes, [Out, In] ref ElementInfoStruct elementInfo);

    [UnmanagedFunctionPointer(CallingConvention.Cdecl, CharSet = PInvoke.cs)]
    public delegate void SetCursorFunc(Utf8Str cursor);

    [UnmanagedFunctionPointer(CallingConvention.Cdecl, CharSet = PInvoke.cs)]
    public delegate void DrawListMarkerFunc(Utf8Str image, Utf8Str baseURL, list_style_type marker_type, ref web_color color, ref position pos);

    [UnmanagedFunctionPointer(CallingConvention.Cdecl, CharSet = PInvoke.cs)]
    public delegate Utf8Str TransformTextFunc(Utf8Str text, text_transform tt);

    [UnmanagedFunctionPointer(CallingConvention.Cdecl, CharSet = PInvoke.cs)]
    public delegate void InitCallbacksFunc(ref Callbacks callbacks);

    // Invoke methods

    [UnmanagedFunctionPointer(CallingConvention.Cdecl, CharSet = PInvoke.cs)]
    public delegate void CreateFromStringFunc(IntPtr container, Utf8Str html);

    [UnmanagedFunctionPointer(CallingConvention.Cdecl, CharSet = PInvoke.cs)]
    public delegate int RenderFunc(IntPtr container, int maxWidth);

    [UnmanagedFunctionPointer(CallingConvention.Cdecl, CharSet = PInvoke.cs)]
    public delegate void DrawFunc(IntPtr container, int x, int y, position clip);

    [UnmanagedFunctionPointer(CallingConvention.Cdecl, CharSet = PInvoke.cs)]
    public delegate void SetMasterCSSFunc(IntPtr container, Utf8Str css);

    [UnmanagedFunctionPointer(CallingConvention.Cdecl, CharSet = PInvoke.cs)]
    [return: MarshalAs(UnmanagedType.I1)]
    public delegate bool OnMouseMoveFunc(IntPtr container, int x, int y);

    [UnmanagedFunctionPointer(CallingConvention.Cdecl, CharSet = PInvoke.cs)]
    [return: MarshalAs(UnmanagedType.I1)]
    public delegate bool OnMouseLeaveFunc(IntPtr container);

    [UnmanagedFunctionPointer(CallingConvention.Cdecl, CharSet = PInvoke.cs)]
    [return: MarshalAs(UnmanagedType.I1)]
    public delegate bool OnLeftButtonUpFunc(IntPtr container, int x, int y);

    [UnmanagedFunctionPointer(CallingConvention.Cdecl, CharSet = PInvoke.cs)]
    [return: MarshalAs(UnmanagedType.I1)]
    public delegate bool OnLeftButtonDownFunc(IntPtr container, int x, int y);

    [UnmanagedFunctionPointer(CallingConvention.Cdecl, CharSet = PInvoke.cs)]
    public delegate IntPtr GetElementInfoFunc(IntPtr container, int ID);

    [UnmanagedFunctionPointer(CallingConvention.Cdecl, CharSet = PInvoke.cs)]
    [return: MarshalAs(UnmanagedType.I1)]
    public delegate bool OnMediaChangedFunc(IntPtr container);

    [UnmanagedFunctionPointer(CallingConvention.Cdecl, CharSet = PInvoke.cs)]
    public delegate void DeleteFunc(IntPtr container);

    [UnmanagedFunctionPointer(CallingConvention.Cdecl, CharSet = PInvoke.cs)]
    public delegate int GetWidthFunc(IntPtr container);

    [UnmanagedFunctionPointer(CallingConvention.Cdecl, CharSet = PInvoke.cs)]
    public delegate int GetHeightFunc(IntPtr container);
}

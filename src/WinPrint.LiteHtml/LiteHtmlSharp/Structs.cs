using System;
using System.Runtime.InteropServices;

#if AUTO_UTF8
using Utf8Str = System.String;
#else
using Utf8Str = System.IntPtr;
#endif

namespace LiteHtmlSharp
{
   [StructLayout(LayoutKind.Sequential)]
   public struct position
   {
      public int x;
      public int y;
      public int width;
      public int height;

      public void Scale(int scaleFactor)
      {
         x *= scaleFactor;
         y *= scaleFactor;
         width *= scaleFactor;
         height *= scaleFactor;
      }

      public override string ToString()
      {
         return string.Format("x: {0}, y: {1}, w: {2}, h: {3}", x, y, width, height);
      }
   }

   [StructLayout(LayoutKind.Sequential)]
   public struct border_radiuses
   {
      public int top_left_x;
      public int top_left_y;

      public int top_right_x;
      public int top_right_y;

      public int bottom_right_x;
      public int bottom_right_y;

      public int bottom_left_x;
      public int bottom_left_y;

      public void Scale(int scaleFactor)
      {
         top_left_x *= scaleFactor;
         top_left_y *= scaleFactor;
         top_right_x *= scaleFactor;
         top_right_y *= scaleFactor;
         bottom_right_x *= scaleFactor;
         bottom_right_y *= scaleFactor;
         bottom_left_x *= scaleFactor;
         bottom_left_y *= scaleFactor;
      }

      public override string ToString()
      {
         return string.Format(
            "tlx: {0}, tly: {1}, trx: {2}, try: {3}, brx: {4}, bry: {5}, blx: {6}, bly: {7}", 
            top_left_x, top_left_y, top_right_x, top_right_y, bottom_right_x, bottom_right_x, bottom_left_x, bottom_left_y
         );
      }
   }

   [StructLayout(LayoutKind.Sequential)]
   public struct borders
   {
      public border left;
      public border top;
      public border right;
      public border bottom;
      public border_radiuses radius;

      public void Scale(int scaleFactor)
      {
         left.Scale(scaleFactor);
         top.Scale(scaleFactor);
         right.Scale(scaleFactor);
         bottom.Scale(scaleFactor);
         radius.Scale(scaleFactor);
      }
   }

   [StructLayout(LayoutKind.Sequential)]
   public struct border
   {
      public int width;
      public border_style style;
      public web_color color;

      public void Scale(int scaleFactor)
      {
         width *= scaleFactor;
      }
   }

   [StructLayout(LayoutKind.Sequential)]
   public struct web_color
   {
      public byte blue;
      public byte green;
      public byte red;
      public byte alpha;
   }

   [StructLayout(LayoutKind.Sequential)]
   public struct size
   {
      public int width;
      public int height;

      public void Scale(int scaleFactor)
      {
         width *= scaleFactor;
         height *= scaleFactor;
      }
   }

   [StructLayout(LayoutKind.Sequential)]
   public struct font_metrics
   {
      public int height;
      public int ascent;
      public int descent;
      public int x_height;
      public bool draw_spaces;
   }

   [StructLayout(LayoutKind.Sequential)]
   public struct media_features
   {
      public media_type type;
      public int width;
      // (pixels) For continuous media, this is the width of the viewport including the size of a rendered scroll bar (if any). For paged media, this is the width of the page box.
      public int height;
      // (pixels) The height of the targeted display area of the output device. For continuous media, this is the height of the viewport including the size of a rendered scroll bar (if any). For paged media, this is the height of the page box.
      public int device_width;
      // (pixels) The width of the rendering surface of the output device. For continuous media, this is the width of the screen. For paged media, this is the width of the page sheet size.
      public int device_height;
      // (pixels) The height of the rendering surface of the output device. For continuous media, this is the height of the screen. For paged media, this is the height of the page sheet size.
      public int color;
      // The number of bits per color component of the output device. If the device is not a color device, the value is zero.
      public int color_index;
      // The number of entries in the color lookup table of the output device. If the device does not use a color lookup table, the value is zero.
      public int monochrome;
      // The number of bits per pixel in a monochrome frame buffer. If the device is not a monochrome device, the output device value will be 0.
      public int resolution;
      // The resolution of the output device (in DPI)
   };

   [Flags]
   public enum text_transform
   {
      text_transform_none,
      text_transform_capitalize,
      text_transform_uppercase,
      text_transform_lowercase
   }

   public enum media_type
   {
      media_type_none,
      media_type_all,
      media_type_screen,
      media_type_print,
      media_type_braille,
      media_type_embossed,
      media_type_handheld,
      media_type_projection,
      media_type_speech,
      media_type_tty,
      media_type_tv,
   }

   public enum border_style
   {
      border_style_none,
      border_style_hidden,
      border_style_dotted,
      border_style_dashed,
      border_style_solid,
      border_style_double,
      border_style_groove,
      border_style_ridge,
      border_style_inset,
      border_style_outset
   }

   public enum background_repeat
   {
      background_repeat_repeat,
      background_repeat_repeat_x,
      background_repeat_repeat_y,
      background_repeat_no_repeat
   }

   public enum font_style
   {
      fontStyleNormal,
      fontStyleItalic
   }

   public enum font_variant
   {
      font_variant_normal,
      font_variant_italic
   }

   [Flags]
   public enum font_decoration
   {
      font_decoration_none = 0x00,
      font_decoration_underline = 0x01,
      font_decoration_linethrough = 0x02,
      font_decoration_overline = 0x04
   }

   [StructLayout(LayoutKind.Sequential)]
   public struct ElementInfoStruct
   {
      public int PosX;
      public int PosY;
      public int Width;
      public int Height;
      public Utf8Str Attributes;
      public Utf8Str Text;
   }

   public class ElementInfo
   {
      public int PosX;
      public int PosY;
      public int Width;
      public int Height;
      public string Attributes;
      public string Text;

      public ElementInfo() { }

      public ElementInfo(ElementInfoStruct raw)
      {
         PosX = raw.PosX;
         PosY = raw.PosY;
         Width = raw.Width;
         Height = raw.Height;
         Attributes = Utf8Util.Utf8PtrToString(raw.Attributes);
         Text = Utf8Util.Utf8PtrToString(raw.Text);
      }
   }


   public enum list_style_type
   {
      list_style_type_none,
      list_style_type_circle,
      list_style_type_disc,
      list_style_type_square,
      list_style_type_armenian,
      list_style_type_cjk_ideographic,
      list_style_type_decimal,
      list_style_type_decimal_leading_zero,
      list_style_type_georgian,
      list_style_type_hebrew,
      list_style_type_hiragana,
      list_style_type_hiragana_iroha,
      list_style_type_katakana,
      list_style_type_katakana_iroha,
      list_style_type_lower_alpha,
      list_style_type_lower_greek,
      list_style_type_lower_latin,
      list_style_type_lower_roman,
      list_style_type_upper_alpha,
      list_style_type_upper_latin,
      list_style_type_upper_roman,
   }

   [StructLayout(LayoutKind.Sequential)]
   public struct Callbacks
   {
      // Test Methods
      public TestCallbackFunc TestCallback;

      // Callbacks
      public SetCaptionFunc SetCaption;
      public GetDefaultFontNameFunc GetDefaultFontName;
      public GetDefaultFontSizeFunc GetDefaultFontSize;

      public DrawBordersFunc DrawBorders;
      public DrawBackgroundFunc DrawBackground;
      public GetImageSizeFunc GetImageSize;
      public ImportCssFunc ImportCss;

      public DrawTextFunc DrawText;
      public GetTextWidthFunc GetTextWidth;
      public CreateFontFunc CreateFont;

      public GetClientRectFunc GetClientRect;
      public GetMediaFeaturesFunc GetMediaFeatures;

      public OnAnchorClickFunc OnAnchorClick;
      public SetBaseURLFunc SetBaseURL;
      public PTtoPXFunc PTtoPX;
      public ShouldCreateElementFunc ShouldCreateElement;
      public CreateElementFunc CreateElement;

      public DrawListMarkerFunc DrawListMarker;
      public SetCursorFunc SetCursor;

      public TransformTextFunc TransformText;
   }

   [StructLayout(LayoutKind.Sequential)]
   public struct DocumentCalls
   {
      public IntPtr ID;

      public TriggerTestCallbackFunc TriggerTestCallback;

      // Invoke Methods
      public DeleteFunc Delete;

      public OnMouseMoveFunc OnMouseMove;
      public OnMouseLeaveFunc OnMouseLeave;
      public OnLeftButtonUpFunc OnLeftButtonUp;
      public OnLeftButtonDownFunc OnLeftButtonDown;

      public CreateFromStringFunc CreateFromString;
      public RenderFunc Render;
      public DrawFunc Draw;
      public SetMasterCSSFunc SetMasterCSS;
      public GetElementInfoFunc GetElementInfo;
      public OnMediaChangedFunc OnMediaChanged;

      public GetWidthFunc GetWidth;
      public GetHeightFunc GetHeight;
   }
}

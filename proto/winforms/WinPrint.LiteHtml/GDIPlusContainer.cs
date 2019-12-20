using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using LiteHtmlSharp;

namespace WinPrint.LiteHtml {

    public interface IResourceLoader {
        byte[] GetResourceBytes(string resource);
        string GetResourceString(string resource);
    }

    public class GDIPlusContainer : ViewportContainer {

        IResourceLoader _loader;


        class ResourceLoader : IResourceLoader {
            Func<string, string> _getStringResource;
            Func<string, byte[]> _getBytesResource;

            public ResourceLoader(Func<string, string> getStringResource, Func<string, byte[]> getBytesResource) {
                _getStringResource = getStringResource;
                _getBytesResource = getBytesResource;
            }

            public byte[] GetResourceBytes(string resource) => _getBytesResource(resource);
            public string GetResourceString(string resource) => _getStringResource(resource);
        }

        public string DefaultFontName { get; set; } = "Cascadia Code";
        public int DefaultFontSize { get; set; } = 10;
        public Graphics Graphics { get => _graphics; set => _graphics = value; }

        private Graphics _graphics;


        static Dictionary<UIntPtr, FontInfo> _fonts = new Dictionary<UIntPtr, FontInfo>();
        static Dictionary<string, Bitmap> _images = new Dictionary<string, Bitmap>();


        public GDIPlusContainer(string masterCssData, ILibInterop libInterop) : base(masterCssData, libInterop) {
        }

        protected override UIntPtr CreateFont(string faceName, int size, int weight, font_style italic, font_decoration decoration, ref font_metrics fm) {
            bool isUnderline = false;// decoration & font_decoration.font_decoration_underline;

            FontStyle fontStyle = italic == font_style.fontStyleItalic ? FontStyle.Italic : FontStyle.Regular;
            if (isUnderline)
                fontStyle |= FontStyle.Underline;

            var font = new FontInfo("Cascadia Code", fontStyle, size, null); 

            _fonts.Add((UIntPtr)font.GetHashCode(), font);

            fm.x_height = font.xHeight;
            fm.ascent = font.Ascent;
            fm.descent = font.Descent;
            fm.height = font.LineHeight;
            fm.draw_spaces = decoration > 0;

            return (UIntPtr)font.GetHashCode();
        }

        protected override void DrawBackground(UIntPtr hdc, string image, background_repeat repeat, ref web_color color, ref position pos, ref border_radiuses borderRadiuses, ref position borderBox, bool isRoot) {
            // TODO: Implement DrawBackground
            // throw new NotImplementedException();
        }

        protected override void DrawBorders(UIntPtr hdc, ref borders borders, ref position draw_pos, bool root) {
            // TODO: Implement DrawBorders
            // throw new NotImplementedException();
        }

        protected override void DrawListMarker(string image, string baseURL, list_style_type marker_type, ref web_color color, ref position pos) {
            // TODO: Implement DrawListMarker
            // throw new NotImplementedException();
        }

        protected override void DrawText(string text, UIntPtr font, ref web_color color, ref position pos) {
            if (_graphics is null) throw new InvalidOperationException("_graphics cannot be null");

            text = text.Replace(' ', (char)160);
            var fontInfo = _fonts[font];

            _graphics.DrawString(text, fontInfo.Font, color.GetBrush(), new Point(pos.x, pos.y));
        }

        protected override string GetDefaultFontName() {
            return DefaultFontName;
        }

        protected override int GetDefaultFontSize() {
            return DefaultFontSize;
        }

        protected override void GetImageSize(string image, ref size size) {
            var bmp = LoadImage(image);
            if (bmp != null) {
                size.width = bmp.Width;
                size.height = bmp.Height;
            }
        }

        protected override int GetTextWidth(string text, UIntPtr font) {
            if (_graphics is null) throw new InvalidOperationException("_graphics cannot be null");
            var fontInfo = _fonts[font];

            //using Bitmap bitmap = new Bitmap(1, 1);
            //var g = Graphics.FromImage(bitmap);
            //g.PageUnit = GraphicsUnit.Pixel;

            var size = _graphics.MeasureString(text, fontInfo.Font);
            return (int)Math.Round(size.Width + 0.25f);
            //return (int)Math.Round(formattedText.WidthIncludingTrailingWhitespace + 0.25f);
        }

        protected override int PTtoPX(int pt) {
            // TODO: This ain't right
            return pt;
        }

        protected override void SetBaseURL(string base_url) {
            // TODO: Impl SetBaseURL
            //throw new NotImplementedException();
        }

        protected override void SetCaption(string caption) {
            // TODO: SetCaption
            //throw new NotImplementedException();
        }

        protected override void SetCursor(string cursor) {
            // TODO: Impelemnt SetCursor
            // throw new NotImplementedException();
        }


        private Bitmap LoadImage(string image) {
            if (_loader is null) throw new InvalidOperationException("_loader cannot be null");

            try {
                Bitmap result;

                if (_images.TryGetValue(image, out result)) {
                    return result;
                }


                var bytes = _loader.GetResourceBytes(image);
                if (bytes != null && bytes.Length > 0) {

                    using (var stream = new MemoryStream(bytes)) {
                        // TODO: Dispose of these.
                        result = new Bitmap(stream);
                        _images.Add(image, result);
                    }
                }

                return result;
            }
            catch {
                return null;
            }
        }

        protected override string ImportCss(string url, string baseurl) {
            return _loader.GetResourceString(url);
        }

    }
}

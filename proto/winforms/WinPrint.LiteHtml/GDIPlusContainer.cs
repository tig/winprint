using System;
using System.Collections.Generic;
using System.Diagnostics;
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

        public string DefaultFontName { get; set; } = "Arial";
        public string DefaultMonospaceFontName { get; set; } = "Consolas";

        public int DefaultFontSize { get; set; } = 10;
        public Graphics Graphics { get => _graphics; set => _graphics = value; }

        private Graphics _graphics;


        static Dictionary<UIntPtr, FontInfo> _fonts = new Dictionary<UIntPtr, FontInfo>();
        static Dictionary<string, Bitmap> _images = new Dictionary<string, Bitmap>();


        public GDIPlusContainer(string masterCssData, ILibInterop libInterop) : base(masterCssData, libInterop) {
        }

        public GDIPlusContainer(string css, IResourceLoader loader) : base(css, LibInterop.Instance) {
            _loader = loader;
        }

        public GDIPlusContainer(string css, Func<string, string> getStringResource, Func<string, byte[]> getBytesResource) : this(css, new ResourceLoader(getStringResource, getBytesResource)) {
        }

        protected override UIntPtr CreateFont(string faceName, int size, int weight, font_style italic, font_decoration decoration, ref font_metrics fm) {
            if (_graphics is null) throw new InvalidOperationException("_graphics cannot be null");

            //Helpers.Logging.TraceMessage($"CreateFont({faceName}, {size}, {italic.ToString()}, {weight}, {decoration.ToString()}");

            bool isUnderline = false;// decoration & font_decoration.font_decoration_underline;
            bool isBold = weight >= 700;

            FontStyle fontStyle = italic == font_style.fontStyleItalic ? FontStyle.Italic : FontStyle.Regular;
            if (isUnderline)
                fontStyle |= FontStyle.Underline;

            if (isBold)
                fontStyle |= FontStyle.Bold;

            FontInfo fi = null;
            // TODO: Find best fit font
            faceName = faceName.Trim(new char[] { ' ', '\"', '\'' });
            var t = faceName.Split(',');
            foreach (var s in t) {
                fi = FontInfo.TryCreateFont(_graphics, s.Trim(new char[] { ' ', '\"', '\'' }), fontStyle, size);
                if (fi != null) break;
            }

            if (fi == null && faceName.Contains("monospace", StringComparison.OrdinalIgnoreCase))
                fi = FontInfo.TryCreateFont(_graphics, DefaultMonospaceFontName, fontStyle, size);

            if (fi == null) 
             fi = FontInfo.TryCreateFont(_graphics, DefaultFontName, fontStyle, size);

            //Helpers.Logging.TraceMessage($"Added FontInfo({fi.Font.FontFamily.ToString()}, {fi.LineHeight}, {fi.Size}, {fi.Font.Style.ToString()}");

            _fonts.Add((UIntPtr)fi.GetHashCode(), fi);

            fm.x_height = fi.xHeight;
            fm.ascent = fi.Ascent;
            fm.descent = fi.Descent;
            fm.height = fi.LineHeight;
            fm.draw_spaces = decoration > 0;

            return (UIntPtr)fi.GetHashCode();
        }

        protected override void DrawBackground(UIntPtr hdc, string image, background_repeat repeat, ref web_color color, ref position pos, ref border_radiuses borderRadiuses, ref position borderBox, bool isRoot) {
            if (pos.width > 0 && pos.height > 0) {
                if (!String.IsNullOrEmpty(image)) {
                    var bitmap = LoadImage(image);
                    if (bitmap != null) {
                        _graphics.DrawImage(bitmap, new Rectangle(pos.x, pos.y, pos.width, pos.height));
                    }
                }
                else {
                    Rectangle rect = new Rectangle(pos.x, pos.y, pos.width, pos.height);
                    _graphics.FillRectangle(color.GetBrush(), rect);

                    //var geometry = new PathGeometry();
                    //PathSegmentCollection path = new PathSegmentCollection();

                    //path.Add(new LineSegment(new Point(rect.Right - br.top_right_x, rect.Top), false));
                    //path.Add(new QuadraticBezierSegment(new Point(rect.Right, rect.Top), new Point(rect.Right, rect.Top + br.top_right_y), false));

                    //path.Add(new LineSegment(new Point(rect.Right, rect.Bottom - br.bottom_right_y), false));
                    //path.Add(new QuadraticBezierSegment(new Point(rect.Right, rect.Bottom), new Point(rect.Right - br.bottom_right_x, rect.Bottom), false));

                    //path.Add(new LineSegment(new Point(rect.Left + br.bottom_left_x, rect.Bottom), false));
                    //path.Add(new QuadraticBezierSegment(new Point(rect.Left, rect.Bottom), new Point(rect.Left, rect.Bottom - br.bottom_left_y), false));

                    //path.Add(new LineSegment(new Point(rect.Left, rect.Top + br.top_left_y), false));
                    //path.Add(new QuadraticBezierSegment(new Point(rect.Left, rect.Top), new Point(rect.Left + br.top_left_x, rect.Top), false));

                    //geometry.Figures.Add(new PathFigure(new Point(rect.Left + br.top_left_x, rect.Top), path, true));

                    //DrawingContext.DrawGeometry(color.GetBrush(), null, geometry);
                }
            }
        }

        protected override void DrawBorders(UIntPtr hdc, ref borders borders, ref position draw_pos, bool root) {
            // Skinny controls can push borders off, in which case we can't create a rect with a negative size.
            if (draw_pos.width < 0) draw_pos.width = 0;
            if (draw_pos.height < 0) draw_pos.height = 0;
            Rectangle rect = new Rectangle(draw_pos.x, draw_pos.y, draw_pos.width, draw_pos.height);
            var br = borders.radius;

            if (borders.top.width > 0) {
                Point p1 = new Point(rect.Left + br.top_left_x, rect.Top);
                Point p2 = new Point(rect.Right - br.top_right_x, rect.Top);
                Point p3 = new Point(rect.Right, rect.Top);
                Point p4 = new Point(rect.Right, rect.Top + br.top_right_y);
                //DrawCurvedPath(p1, p2, p3, p4, ref borders.top.color, borders.top.width);
                //DrawRect(rect.Left, rect.Top, rect.Right, rect.Top, borders.top.color.GetPen(borders.top.width));
                _graphics.DrawLine(borders.top.color.GetPen(borders.top.width), rect.Left, rect.Top, rect.Right, rect.Top);
                //DrawRect(rect.Left + br.top_left_x, rect.Top, rect.Width - br.top_left_x - br.top_right_x, borders.top.color.GetPen(borders.top.width));
            }

            if (borders.right.width > 0) {
                Point p1 = new Point(rect.Right, rect.Top + br.top_right_y);
                Point p2 = new Point(rect.Right, rect.Bottom - br.bottom_right_y);
                Point p3 = new Point(rect.Right, rect.Bottom);
                Point p4 = new Point(rect.Right - br.bottom_right_x, rect.Bottom);
                //DrawCurvedPath(p1, p2, p3, p4, ref borders.right.color, borders.right.width);
                _graphics.DrawLine(borders.right.color.GetPen(borders.right.width), rect.Right, rect.Top, rect.Right, rect.Bottom);
                //DrawRect(p4.X,  draw_pos.x + draw_pos.width - borders.right.width, draw_pos.y, borders.right.width, draw_pos.height, borders.right.color.GetPen(borders.right.width));
            }

            if (borders.bottom.width > 0) {
                Point p1 = new Point(rect.Right - br.bottom_right_x, rect.Bottom);
                Point p2 = new Point(rect.Left + br.bottom_left_x, rect.Bottom);
                Point p3 = new Point(rect.Left, rect.Bottom);
                Point p4 = new Point(rect.Left, rect.Bottom - br.bottom_left_y);
                //DrawCurvedPath(p1, p2, p3, p4, ref borders.bottom.color, borders.bottom.width);
                _graphics.DrawLine(borders.bottom.color.GetPen(borders.bottom.width), rect.Left, rect.Bottom, rect.Right, rect.Bottom);
//                DrawRect(draw_pos.x, draw_pos.y + draw_pos.height - borders.bottom.width, draw_pos.width, borders.bottom.width, borders.bottom.color.GetPen(borders.bottom.width));
            }

            if (borders.left.width > 0) {
                Point p1 = new Point(rect.Left, rect.Bottom - br.bottom_left_y);
                Point p2 = new Point(rect.Left, rect.Top + br.top_left_y);
                Point p3 = new Point(rect.Left, rect.Top);
                Point p4 = new Point(rect.Left + br.top_left_x, rect.Top);
                //DrawCurvedPath(p1, p2, p3, p4, ref borders.left.color, borders.left.width);
                _graphics.DrawLine(borders.left.color.GetPen(borders.left.width), rect.Left, rect.Top, rect.Left, rect.Bottom);
//                DrawRect(draw_pos.x, draw_pos.y, borders.left.width, draw_pos.height, borders.left.color.GetPen(borders.left.width));
            }
        }

        private void DrawRect(int x, int y, int width, int height, Pen pen) {
            var rect = new Rectangle(x, y, width, height);
            Graphics.DrawRectangle(pen, rect);
        }

        protected override void DrawListMarker(string image, string baseURL, list_style_type marker_type, ref web_color color, ref position pos) {
            // TODO: Implement DrawListMarker
            // throw new NotImplementedException();
        }

        protected override void DrawText(string text, UIntPtr font, ref web_color color, ref position pos) {
            if (_graphics is null) throw new InvalidOperationException("_graphics cannot be null");

            text = text.Replace(' ', (char)160);
            var fontInfo = _fonts[font];

            _graphics.DrawString(text, fontInfo.Font, color.GetBrush(), new Point(pos.x, pos.y), StringFormat.GenericTypographic);
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

            text = text.Replace(' ', 'x');

            var size = _graphics.MeasureString(text, fontInfo.Font, (int)Size.Width, StringFormat.GenericTypographic);
            return (int)Math.Round(size.Width);
            //return (int)Math.Round(formattedText.WidthIncludingTrailingWhitespace + 0.25f);
        }

        protected override int PTtoPX(int pt) {
            if (_graphics is null) throw new InvalidOperationException("_graphics cannot be null");
            // BUGBUG: Figure out why the WPF implementaiton just returns pt and Core Graphics returns 1
            //return (int)Math.Round(pt / 72F * _graphics.DpiY);
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

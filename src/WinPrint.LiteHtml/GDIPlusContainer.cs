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
        private IResourceLoader _loader;

        private class ResourceLoader : IResourceLoader {
            private Func<string, string> _getStringResource;
            private Func<string, byte[]> _getBytesResource;

            public ResourceLoader(Func<string, string> getStringResource, Func<string, byte[]> getBytesResource) {
                _getStringResource = getStringResource;
                _getBytesResource = getBytesResource;
            }

            public byte[] GetResourceBytes(string resource) {
                return _getBytesResource(resource);
            }

            public string GetResourceString(string resource) {
                return _getStringResource(resource);
            }
        }

        public string DefaultFontName { get; set; } = "Arial";
        public string DefaultMonospaceFontName { get; set; } = "Consolas";

        public int DefaultFontSize { get; set; } = 10;
        public Graphics Graphics { get => _graphics; set => _graphics = value; }

        private Graphics _graphics;

        public StringFormat StringFormat { get; set; }

        /// <summary>
        /// If true, all colors will be converted to grayscale. 
        /// </summary>
        public bool Grayscale { get; set; }

        /// <summary>
        /// If true, the background color will be printed. Otherwise it will be white.
        /// </summary>
        public bool PrintBackground { get; set; }

        /// <summary>
        /// Darkness factor. 0 = color. 100 = black. Anything inbetween provides a shade of gray (or darker colors).
        /// </summary>
        public int Darkness { get; set; }

        public bool Diagnostics { get; set; }

        private static Dictionary<UIntPtr, FontInfo> _fonts = new Dictionary<UIntPtr, FontInfo>();
        private static Dictionary<string, Bitmap> _images = new Dictionary<string, Bitmap>();


        public GDIPlusContainer(string masterCssData, ILibInterop libInterop) : base(masterCssData, libInterop) {
        }

        public GDIPlusContainer(string css, IResourceLoader loader) : base(css, LibInterop.Instance) {
            _loader = loader;
        }

        public GDIPlusContainer(string css, Func<string, string> getStringResource, Func<string, byte[]> getBytesResource) : this(css, new ResourceLoader(getStringResource, getBytesResource)) {
        }

        public int PageHeight;
        protected override void GetMediaFeatures(ref media_features media) {
            Logging.TraceMessage($"{media.ToString()}");
            media.width = media.device_width = (int)Size.Width;
            media.height = (int)Size.Height;

            // BUGBUG: I don't think litehtml actaully honors device_height
            media.device_height = PageHeight;

            if (Grayscale) {
                media.color = 0;
                media.color_index = 0;
            }
        }

        protected override UIntPtr CreateFont(string faceName, int size, int weight, font_style italic, font_decoration decoration, ref font_metrics fm) {
            Logging.TraceMessage($"{faceName}, {size}, {weight}");
            if (_graphics is null) {
                throw new InvalidOperationException("_graphics cannot be null");
            }

            //Helpers.Logging.TraceMessage($"CreateFont({faceName}, {size}, {italic.ToString()}, {weight}, {decoration.ToString()}");

            var isUnderline = false;// decoration & font_decoration.font_decoration_underline;
            var isBold = weight >= 700;

            var fontStyle = italic == font_style.fontStyleItalic ? FontStyle.Italic : FontStyle.Regular;
            if (isUnderline) {
                fontStyle |= FontStyle.Underline;
            }

            if (isBold) {
                fontStyle |= FontStyle.Bold;
            }

            FontInfo fi = null;
            // TODO: Find best fit font
            faceName = faceName.Trim(new char[] { ' ', '\"', '\'' });
            var t = faceName.Split(',');
            foreach (var s in t) {
                fi = FontInfo.TryCreateFont(_graphics, s.Trim(new char[] { ' ', '\"', '\'' }), fontStyle, size);
                if (fi != null) {
                    break;
                }
            }

            if (fi == null && faceName.Contains("monospace", StringComparison.OrdinalIgnoreCase)) {
                fi = FontInfo.TryCreateFont(_graphics, DefaultMonospaceFontName, fontStyle, size);
            }

            if (fi == null) {
                fi = FontInfo.TryCreateFont(_graphics, DefaultFontName, fontStyle, size);
            }

            //Helpers.Logging.TraceMessage($"Added FontInfo({fi.Font.FontFamily.ToString()}, {fi.LineHeight}, {fi.Size}, {fi.Font.Style.ToString()}");

            // HACK: Used to enable PrismFileContent to determine font used for CODE/PRE
            if (faceName.Contains("winprint", StringComparison.OrdinalIgnoreCase)) {
                codeFontInfo = fi;
            }

            _fonts.Add((UIntPtr)fi.GetHashCode(), fi);

            fm.x_height = fi.xHeight;
            fm.ascent = fi.Ascent;
            fm.descent = fi.Descent;
            fm.height = fi.LineHeight;
            fm.draw_spaces = decoration > 0;

            return (UIntPtr)fi.GetHashCode();
        }

        // HACK: Used to enable PrismFileContent to determine font used for code
        private FontInfo codeFontInfo;
        public FontInfo GetCodeFontInfo() {
            return codeFontInfo;
        }

        protected override void DrawBackground(UIntPtr hdc, string image, background_repeat repeat, ref web_color bgcolor, ref position pos, ref border_radiuses borderRadiuses, ref position borderBox, bool isRoot) {
            //Logging.TraceMessage();

            var color = bgcolor;
            if (Grayscale) {
                color.red = 0xff;
                color.blue = 0xff;
                color.green = 0xff;
                color.alpha = 0x00;
            }

            if (pos.width > 0 && pos.height > 0) {
                if (!string.IsNullOrEmpty(image)) {
                    var bitmap = LoadImage(image);
                    if (bitmap != null) {
                        _graphics.DrawImage(bitmap, new Rectangle(pos.x, pos.y, pos.width, pos.height));
                    }
                }
                else {
                    // TODO: Make this more precise; not for ALL backgrounds, just page background?
                    if (PrintBackground) {
                        var rect = new Rectangle(pos.x, pos.y, pos.width, pos.height);
                        _graphics.FillRectangle(color.GetBrush(), rect);
                    }

                    if (Diagnostics) {
                        DrawRect(pos.x, pos.y, pos.width, pos.height, Pens.Blue);
                    }
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


        /// <summary>
        /// Creates color with corrected brightness.
        /// </summary>
        /// <param name="color">Color to correct.</param>
        /// <param name="correctionFactor">The brightness correction factor. Must be between -1 and 1. 
        /// Negative values produce darker colors.</param>
        /// <returns>
        /// Corrected <see cref="Color"/> structure.
        /// </returns>
        public static Color ChangeColorBrightness(Color color, float correctionFactor) {
            var red = (float)color.R;
            var green = (float)color.G;
            var blue = (float)color.B;

            if (correctionFactor < 0) {
                correctionFactor = 1 + correctionFactor;
                red *= correctionFactor;
                green *= correctionFactor;
                blue *= correctionFactor;
            }
            else {
                red = (255 - red) * correctionFactor + red;
                green = (255 - green) * correctionFactor + green;
                blue = (255 - blue) * correctionFactor + blue;
            }

            return Color.FromArgb(color.A, (int)red, (int)green, (int)blue);
        }

        public web_color ToGrayScaleColor(web_color originalColor) {
            // (0.21*Red + 0.72*Green + 0.07*Blue)
            var grayScale = (byte)((originalColor.red * .21) + (originalColor.green * .72) + (originalColor.blue * .07));
            //            byte grayScale = (byte)((originalColor.red * .3) + (originalColor.green * .59) + (originalColor.blue * .11));
            originalColor.red = originalColor.green = originalColor.blue = grayScale;

            var c = ChangeColorBrightness(Color.FromArgb(originalColor.red, originalColor.green, originalColor.blue), -(Darkness / 100F));
            originalColor.red = c.R;
            originalColor.green = c.G;
            originalColor.blue = c.B;
            return originalColor;
        }

        protected override void DrawBorders(UIntPtr hdc, ref borders borders_ref, ref position draw_pos, bool root) {
            //Logging.TraceMessage();

            var borders = borders_ref;
            if (Grayscale) {
                borders.top.color = ToGrayScaleColor(borders.top.color);
                borders.left.color = ToGrayScaleColor(borders.left.color);
                borders.bottom.color = ToGrayScaleColor(borders.bottom.color);
                borders.right.color = ToGrayScaleColor(borders.right.color);
            }

            // Skinny controls can push borders off, in which case we can't create a rect with a negative size.
            if (draw_pos.width < 0) {
                draw_pos.width = 0;
            }

            if (draw_pos.height < 0) {
                draw_pos.height = 0;
            }

            var rect = new Rectangle(draw_pos.x, draw_pos.y, draw_pos.width, draw_pos.height);
            var br = borders.radius;

            if (borders.top.width > 0) {
                var p1 = new Point(rect.Left + br.top_left_x, rect.Top);
                var p2 = new Point(rect.Right - br.top_right_x, rect.Top);
                var p3 = new Point(rect.Right, rect.Top);
                var p4 = new Point(rect.Right, rect.Top + br.top_right_y);
                //DrawCurvedPath(p1, p2, p3, p4, ref borders.top.color, borders.top.width);
                //DrawRect(rect.Left, rect.Top, rect.Right, rect.Top, borders.top.color.GetPen(borders.top.width));
                _graphics.DrawLine(borders.top.color.GetPen(borders.top.width), rect.Left, rect.Top, rect.Right, rect.Top);
                //DrawRect(rect.Left + br.top_left_x, rect.Top, rect.Width - br.top_left_x - br.top_right_x, borders.top.color.GetPen(borders.top.width));
            }

            if (borders.right.width > 0) {
                var p1 = new Point(rect.Right, rect.Top + br.top_right_y);
                var p2 = new Point(rect.Right, rect.Bottom - br.bottom_right_y);
                var p3 = new Point(rect.Right, rect.Bottom);
                var p4 = new Point(rect.Right - br.bottom_right_x, rect.Bottom);
                //DrawCurvedPath(p1, p2, p3, p4, ref borders.right.color, borders.right.width);
                _graphics.DrawLine(borders.right.color.GetPen(borders.right.width), rect.Right, rect.Top, rect.Right, rect.Bottom);
                //DrawRect(p4.X,  draw_pos.x + draw_pos.width - borders.right.width, draw_pos.y, borders.right.width, draw_pos.height, borders.right.color.GetPen(borders.right.width));
            }

            if (borders.bottom.width > 0) {
                var p1 = new Point(rect.Right - br.bottom_right_x, rect.Bottom);
                var p2 = new Point(rect.Left + br.bottom_left_x, rect.Bottom);
                var p3 = new Point(rect.Left, rect.Bottom);
                var p4 = new Point(rect.Left, rect.Bottom - br.bottom_left_y);
                //DrawCurvedPath(p1, p2, p3, p4, ref borders.bottom.color, borders.bottom.width);
                _graphics.DrawLine(borders.bottom.color.GetPen(borders.bottom.width), rect.Left, rect.Bottom, rect.Right, rect.Bottom);
                //                DrawRect(draw_pos.x, draw_pos.y + draw_pos.height - borders.bottom.width, draw_pos.width, borders.bottom.width, borders.bottom.color.GetPen(borders.bottom.width));
            }

            if (borders.left.width > 0) {
                var p1 = new Point(rect.Left, rect.Bottom - br.bottom_left_y);
                var p2 = new Point(rect.Left, rect.Top + br.top_left_y);
                var p3 = new Point(rect.Left, rect.Top);
                var p4 = new Point(rect.Left + br.top_left_x, rect.Top);
                //DrawCurvedPath(p1, p2, p3, p4, ref borders.left.color, borders.left.width);
                _graphics.DrawLine(borders.left.color.GetPen(borders.left.width), rect.Left, rect.Top, rect.Left, rect.Bottom);
                //                DrawRect(draw_pos.x, draw_pos.y, borders.left.width, draw_pos.height, borders.left.color.GetPen(borders.left.width));
            }
        }

        private void DrawRect(int x, int y, int width, int height, Pen pen) {
            var rect = new Rectangle(x, y, width, height);
            Graphics.DrawRectangle(pen, rect);
        }

        protected override void DrawListMarker(string image, string baseURL, list_style_type marker_type, ref web_color markerColor, ref position pos) {
            Logging.TraceMessage();

            var color = markerColor;
            if (Grayscale) {
                color = ToGrayScaleColor(color);
            }

            if (_graphics is null) {
                throw new InvalidOperationException("_graphics cannot be null");
            }

            _graphics.FillRectangle(color.GetBrush(), pos.x, pos.y, pos.width, pos.height);
        }

        protected override void DrawText(string text, UIntPtr font, ref web_color textColor, ref position pos) {
            //Logging.TraceMessage();
            if (_graphics is null) {
                throw new InvalidOperationException("_graphics cannot be null");
            }

            text = text.Replace(' ', (char)160);
            //text = text.Replace(' ', '_');
            var fontInfo = _fonts[font];

            var color = textColor;
            if (Grayscale) {
                color = ToGrayScaleColor(color);
            }

            Debug.Assert(StringFormat != null);
            _graphics.DrawString(text, fontInfo.Font, color.GetBrush(), new Point(pos.x, pos.y), StringFormat);
            if (Diagnostics) {
                _graphics.DrawLine(Pens.Green, new Point(pos.x, pos.y), new Point(pos.x, pos.y + (fontInfo.LineHeight / 4)));
            }
        }

        protected override string GetDefaultFontName() {
            return DefaultFontName;
        }

        protected override int GetDefaultFontSize() {
            return DefaultFontSize;
        }

        protected override void GetImageSize(string image, ref size size) {
            Logging.TraceMessage();

            var bmp = LoadImage(image);
            if (bmp != null) {
                size.width = bmp.Width;
                size.height = bmp.Height;
            }
        }

        protected override int GetTextWidth(string text, UIntPtr font) {
            //Logging.TraceMessage($"{text}");

            if (_graphics is null) {
                throw new InvalidOperationException("_graphics cannot be null");
            }

            var fontInfo = _fonts[font];

            //using Bitmap bitmap = new Bitmap(1, 1);
            //var g = Graphics.FromImage(bitmap);
            //g.PageUnit = GraphicsUnit.Pixel;

            //text = text.Replace(' ', '_');
            text = text.Replace(' ', (char)160);

            var size = _graphics.MeasureString(text, fontInfo.Font, (int)Size.Width, StringFormat);
            return (int)Math.Round(size.Width);
            //return (int)Math.Round(formattedText.WidthIncludingTrailingWhitespace + 0.25f);
        }

        protected override int PTtoPX(int pt) {
            //Logging.TraceMessage();

            if (_graphics is null) {
                throw new InvalidOperationException("_graphics cannot be null");
            }
            // BUGBUG: Figure out why the WPF implementaiton just returns pt and Core Graphics returns 1
            //return (int)Math.Round(pt / 72F * _graphics.DpiY);
            return pt;

        }

        protected override void SetBaseURL(string base_url) {
            Logging.TraceMessage($"{base_url}");

            // TODO: Impl SetBaseURL
            //throw new NotImplementedException();
        }

        protected override void SetCaption(string caption) {
            Logging.TraceMessage($"{caption}");

            // TODO: SetCaption
            //throw new NotImplementedException();
        }

        protected override void SetCursor(string cursor) {
            Logging.TraceMessage($"{cursor}");

            // TODO: Impelemnt SetCursor
            // throw new NotImplementedException();
        }


        private Bitmap LoadImage(string image) {
            if (_loader is null) {
                throw new InvalidOperationException("_loader cannot be null");
            }

            try {

                if (_images.TryGetValue(image, out var result)) {
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
            Logging.TraceMessage($"{url}");
            return _loader.GetResourceString(url);
        }

    }
}

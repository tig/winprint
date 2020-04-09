using System.Collections.Generic;
using System.Drawing;
using LiteHtmlSharp;

namespace WinPrint.LiteHtml {
    public static class LiteHtmlExtensions {
        private static Dictionary<string, Brush> _brushes = new Dictionary<string, Brush>();
        private static Dictionary<string, Pen> _pens = new Dictionary<string, Pen>();

        public static Brush GetBrush(this web_color color) {
            var key = color.red.ToString() + color.green.ToString() + color.blue.ToString() + color.alpha.ToString();

            if (!_brushes.TryGetValue(key, out var result)) {
                result = new SolidBrush(Color.FromArgb(color.alpha, color.red, color.green, color.blue));
                _brushes.Add(key, result);
            }

            return result;
        }

        public static Pen GetPen(this web_color color, float thickness) {
            var key = color.red.ToString() + color.green.ToString() + color.blue.ToString() + thickness;

            if (!_pens.TryGetValue(key, out var result)) {
                var brush = color.GetBrush();
                result = new Pen(brush, thickness);
                _pens.Add(key, result);
            }

            return result;
        }

    }
}

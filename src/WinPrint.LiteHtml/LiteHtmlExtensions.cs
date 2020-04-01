using System.Collections.Generic;
using System.Drawing;
using LiteHtmlSharp;

namespace WinPrint.LiteHtml {
    public static class LiteHtmlExtensions {
        static Dictionary<string, Brush> _brushes = new Dictionary<string, Brush>();
        static Dictionary<string, Pen> _pens = new Dictionary<string, Pen>();

        public static Brush GetBrush(this web_color color) {
            string key = color.red.ToString() + color.green.ToString() + color.blue.ToString() + color.alpha.ToString();

            Brush result = null;
            if (!_brushes.TryGetValue(key, out result)) {
                result = new SolidBrush(Color.FromArgb(color.alpha, color.red, color.green, color.blue));
                _brushes.Add(key, result);
            }

            return result;
        }

        public static Pen GetPen(this web_color color, float thickness) {
            string key = color.red.ToString() + color.green.ToString() + color.blue.ToString() + thickness;

            Pen result = null;
            if (!_pens.TryGetValue(key, out result)) {
                Brush brush = color.GetBrush();
                result = new Pen(brush, thickness);
                _pens.Add(key, result);
            }

            return result;
        }

    }
}

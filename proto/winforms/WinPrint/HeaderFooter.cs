using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using System.Linq.Dynamic.Core;
using System.Linq.Expressions;
using System.Linq.Dynamic.Core.Exceptions;
using System.Diagnostics;

namespace WinPrint {
    /// <summary>
    /// Knows how to paint a header or footer.
    /// Single line of text? TODO: Might want to suppport wrapping.
    ///  Format: A .NET Interpolated String. Two tabstops.
    ///  $
    ///     Three segement
    ///         Left Aligned       Centered          Right Aligned
    ///     Macros
    ///         Print Date
    ///         File Date
    ///         Page Number
    ///         Num Pages
    ///         File Name
    ///         File Path
    ///         Fully Qualified Path
    ///         Document Title
    ///         Author
    ///         Language
    ///     Options
    ///         Top padding
    ///         Bottom padding
    ///         Right padding
    ///         Left Padding
    ///         top, left, right, bottom border
    ///         border pen style
    ///         border color
    ///         font
    /// 
    /// {FullFilePath}\tModified: {FileDate:F}\t{Page:003}/{NumPages}
    /// \t{Kindel Systems Confidential
    /// 
    /// How to deal with clipping
    /// 1) Order of print - Left, Right, Center (center wins)
    /// 2) Elipsis - different based on macro. E.g. FullFilePath is "Start...FileName" where FileName is truncated last.
    /// 3) Clipped (never overwritten - ugly)
    /// 4) Wrapped (post MLP)
    ///         
    /// </summary>
    public abstract class HeaderFooter {

        private Macros macros;

        public float GetFontHeight() {
            return font.GetHeight(100);
        }

        public string Text { get => text; set => text = value; }
        public Font Font { get => font; set => font = value; }
        public bool LeftBorder { get => leftBorder; set => leftBorder = value; }
        public bool TopBorder { get => topBorder; set => topBorder = value; }
        public bool RightBorder { get => rightBorder; set => rightBorder = value; }
        public bool BottomBorder { get => bottomBorder; set => bottomBorder = value; }
        public Rectangle Bounds { get => bounds; set => bounds = value; }

        private Font font;

        private string text;

        internal Page containingPage;

        internal Rectangle bounds = new Rectangle();

        // Borders
        bool leftBorder, topBorder, rightBorder, bottomBorder;

        public abstract void SetBounds();

        public void Paint(Graphics g) {
            if (g is null) throw new ArgumentNullException(nameof(g));
            if (Text is null) throw new InvalidOperationException($"{nameof(Text)} can't be null");

            GraphicsState state = containingPage.AdjustPrintOrPreview(g);

            Font tempFont;
            if (g.PageUnit == GraphicsUnit.Display) {
                tempFont = (Font)Font.Clone();
            }
            else {
                // Convert font to pixel units if we're in preview
                tempFont = new Font(Font.FontFamily, Font.SizeInPoints / 72F * 100F, Font.Style, GraphicsUnit.Pixel);
            }
            if (leftBorder)
                g.DrawLine(Pens.DarkGray, Bounds.Left, Bounds.Top, Bounds.Left, Bounds.Bottom);

            if (topBorder)
                g.DrawLine(Pens.DarkGray, Bounds.Left, Bounds.Top, Bounds.Right, Bounds.Top);

            if (rightBorder)
                g.DrawLine(Pens.DarkGray, Bounds.Right, Bounds.Top, Bounds.Right, Bounds.Bottom);

            if (bottomBorder)
                g.DrawLine(Pens.DarkGray, Bounds.Left, Bounds.Bottom, Bounds.Right, Bounds.Bottom);

            // Left\tCenter\tRight
            string[] parts = macros.ReplaceMacro(Text).Split("\t");

            // Left
            g.DrawString(parts[0], tempFont, Brushes.Black, Bounds.Left, Bounds.Top, StringFormat.GenericDefault);

            // Center
            if (parts.Length > 1) {
                SizeF size = g.MeasureString(parts[1], tempFont);
                g.DrawString(parts[1], tempFont, Brushes.Black, Bounds.Left + (Bounds.Width/2) - (size.Width/2), Bounds.Top, StringFormat.GenericDefault);
            }

            //Right
            if (parts.Length > 2) {
                SizeF size = g.MeasureString(parts[2], tempFont);
                g.DrawString(parts[2], tempFont, Brushes.Black, Bounds.Right - (int)size.Width, Bounds.Top, StringFormat.GenericDefault);
            }

            tempFont.Dispose();
            g.Restore(state);
        }

        public HeaderFooter(Page containingPage) {
            macros = new Macros(this);
            leftBorder = rightBorder = topBorder = bottomBorder = true;
            this.containingPage = containingPage;
            Font = new Font("Lucida Sans", 8, FontStyle.Italic, GraphicsUnit.Point);
        }
    }

    public class Header : HeaderFooter {

        public Header(Page containingPage) : base(containingPage) {
        }

        public override void SetBounds() {
            bounds.X = containingPage.Bounds.Left + containingPage.Margins.Left;
            bounds.Y = containingPage.Bounds.Top + containingPage.Margins.Top;
            bounds.Width = containingPage.Bounds.Width - containingPage.Margins.Left - containingPage.Margins.Right;
            bounds.Height = (int)GetFontHeight();
        }

        //public new void Paint(Graphics g) {
        //    if (g is null) throw new ArgumentNullException(nameof(g));
        //    GraphicsState state = containingPage.AdjustPrintOrPreview(g);
        //    // Draw borders
        //    base.Paint(g);
        //    g.Restore(state);
        //}
    }
    public class Footer : HeaderFooter {

        public Footer(Page containingPage) : base(containingPage) {
        }


        public override void SetBounds() {
            //if (containingPage is null) throw new InvalidOperationException(nameof(containingPage));

            bounds.X = containingPage.Bounds.Left + containingPage.Margins.Left;
            bounds.Y = containingPage.Bounds.Bottom - containingPage.Margins.Bottom - (int)GetFontHeight();
            bounds.Width = containingPage.Bounds.Width - containingPage.Margins.Left - containingPage.Margins.Right;
            bounds.Height = (int)GetFontHeight();
        }

        //public new void Paint(Graphics g) {
        //    if (g is null) throw new ArgumentNullException(nameof(g));
        //    GraphicsState state = containingPage.AdjustPrintOrPreview(g);
        //    // Draw borders
        //    base.Paint(g);
        //    g.Restore(state);
        //}
    }

    internal class Macros {
        public HeaderFooter headerFooter;
        private string regex;

        // Each Property is exposed as a {Macro}.
        public int Page { get { return headerFooter.containingPage.PageNum; } }
        public int NumPages { get { return headerFooter.containingPage.NumPages; } }
        public string FileExtension { get { return Path.GetExtension(headerFooter.containingPage.Document.File); } }
        public string FileName { get { return Path.GetFileName(headerFooter.containingPage.Document.File); } }
        public string FilePath { get { return Path.GetDirectoryName(FullyQualifiedPath); } }
        public string FullyQualifiedPath { get { return Path.GetFullPath(headerFooter.containingPage.Document.File); } }
        public DateTime DatePrinted { get { return DateTime.Now; } }
        public DateTime DateRevised { get { return File.GetLastWriteTime(headerFooter.containingPage.Document.File); } }
       // TODO: implement - via registry??
        public string FileType { get {
                return "not impl"; } }

        internal Macros(HeaderFooter hf) {
            headerFooter = hf;
        }

        /// <summary>
        /// Replaces macros of the form "{property:format}" using regex and Dynamic Invoke
        /// From https://stackoverflow.com/questions/39874172/dynamic-string-interpolation/39900731#39900731
        /// and  https://haacked.com/archive/2009/01/14/named-formats-redux.aspx/
        /// </summary>
        /// <param name="value"></param>
        /// <returns></returns>
        internal string ReplaceMacro(string value) {
            return Regex.Replace(value, @"(?<start>\{)+(?<property>[\w\.\[\]]+)(?<format>:[^}]+)?(?<end>\})+", match => {
                var p = System.Linq.Expressions.Expression.Parameter(typeof(Macros), "Macros");

                Group startGroup = match.Groups["start"];
                Group propertyGroup = match.Groups["property"];
                Group formatGroup = match.Groups["format"];
                Group endGroup = match.Groups["end"];

                LambdaExpression e;
                try {
                    e = DynamicExpressionParser.ParseLambda(new[] { p }, null, propertyGroup.Value);
                }
                catch (ParseException ex) {
                    // Non-existant Property or other parse error
                    return propertyGroup.Value;
                }
                if (formatGroup.Success)
                    return (string.Format("{0" + formatGroup.Value + "}", e.Compile().DynamicInvoke(this)));
                else
                    return (e.Compile().DynamicInvoke(this) ?? "").ToString();
            });
        }
    }


}

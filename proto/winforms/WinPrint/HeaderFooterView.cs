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
    ///
    /// Format: Left/Centered/Right can be delimited with either tab char (\t) or |
    /// {FullyQualifiedPath}|Modified: {FileDate:F}|{Page:D3}/{NumPages}
    /// {FullyQualifiedPath}\tModified: {FileDate:F}\t{Page:D3}/{NumPages}
    /// 
    ///     Macros
    ///         DatePrinted
    ///         DateRevised
    ///         Page
    ///         NumPages
    ///         FileName
    ///         FilePath
    ///         FullyQualifiedPath
    ///         FileExtension
    ///         FileTYpe
    ///         Title
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
    /// 
    /// TODO: How to deal with clipping
    /// 1) Order of print - Left, Right, Center (center wins)
    /// 2) Elipsis - different based on macro. E.g. FullFilePath is "Start...FileName" where FileName is truncated last.
    /// 3) Clipped (never overwritten - ugly)
    /// 4) Wrapped (post MLP)
    ///         
    /// </summary>
    public abstract class HeaderFooterView : IDisposable {
        private readonly Macros macros;

        public string Text { get; set; }
        public Font Font { get; set; }
        public bool LeftBorder { get; set; }
        public bool TopBorder { get; set; }
        public bool RightBorder { get; set; }
        public bool BottomBorder { get; set; }
        public Rectangle Bounds => CalcBounds();

        public bool Enabled { get; set; }

        internal DocumentViewModel containingDocument;

        public void Dispose() {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        // Protected implementation of Dispose pattern.
        // Flag: Has Dispose already been called?
        bool disposed = false;
        protected virtual void Dispose(bool disposing) {
            if (disposed)
                return;

            if (disposing) {
                if (Font != null) Font.Dispose();
            }
            disposed = true;
        }

        /// <summary>
        /// Calcuate the Header or Footer bounds (position and size on page) based on containing document and font size.
        /// </summary>
        /// <returns></returns>
        internal abstract Rectangle CalcBounds();

        public void Paint(Graphics g, int pageNum) {
            if (!Enabled) return;

            if (g is null) throw new ArgumentNullException(nameof(g));
            if (Text is null) throw new InvalidOperationException($"{nameof(Text)} can't be null");

            GraphicsState state = containingDocument.AdjustPrintOrPreview(g);

            Font tempFont;
            if (g.PageUnit == GraphicsUnit.Display) {
                tempFont = (Font)Font.Clone();
            }
            else {
                // Convert font to pixel units if we're in preview
                tempFont = new Font(Font.FontFamily, Font.SizeInPoints / 72F * 96F, Font.Style, GraphicsUnit.Pixel);
            }
            if (LeftBorder)
                g.DrawLine(Pens.DarkGray, Bounds.Left, Bounds.Top, Bounds.Left, Bounds.Bottom);

            if (TopBorder)
                g.DrawLine(Pens.DarkGray, Bounds.Left, Bounds.Top, Bounds.Right, Bounds.Top);

            if (RightBorder)
                g.DrawLine(Pens.DarkGray, Bounds.Right, Bounds.Top, Bounds.Right, Bounds.Bottom);

            if (BottomBorder)
                g.DrawLine(Pens.DarkGray, Bounds.Left, Bounds.Bottom, Bounds.Right, Bounds.Bottom);

            // Left\tCenter\tRight
            string[] parts = macros.ReplaceMacro(Text, pageNum).Split('\t', '|');

            using StringFormat fmt = new StringFormat(StringFormat.GenericTypographic) {
                LineAlignment = StringAlignment.Near
            };

            // Center goes first - it has priority - ensure it gets drawn completely where
            // Left & Right can be clipped
            SizeF sizeCenter = new SizeF(0, 0);
            if (parts.Length > 1) {
                sizeCenter = g.MeasureString(parts[1], tempFont);
                //g.DrawRectangle(Pens.DarkOrange, Bounds.X, Bounds.Y, Bounds.Width, tempFont.GetHeight(100));
                g.DrawString(parts[1], tempFont, Brushes.Black, Bounds.X + ((Bounds.Width / 2) - (int)(sizeCenter.Width / 2)), Bounds.Y, fmt);
            }

            //g.DrawString(parts[0], tempFont, Brushes.Black, Bounds.Left, Bounds.Top, fmt);

            // Left
            //fmt.Alignment = StringAlignment.Near;
            //fmt.Trimming = StringTrimming.EllipsisPath;
            g.DrawString(parts[0], tempFont, Brushes.Black, Bounds.X, Bounds.Y, fmt);

            //Right
            if (parts.Length > 2) {
                fmt.Alignment = StringAlignment.Near;
                SizeF sizeRight = g.MeasureString(parts[2], tempFont);
                g.DrawString(parts[2], tempFont, Brushes.Black, Bounds.Right - sizeRight.Width, Bounds.Y, fmt);
            }

            tempFont.Dispose();
            g.Restore(state);
        }

        public HeaderFooterView(DocumentViewModel containingDocument) {
            if (containingDocument is null) throw new ArgumentNullException(nameof(containingDocument));
            macros = new Macros(this);
            LeftBorder = RightBorder = TopBorder = BottomBorder = true;
            this.containingDocument = containingDocument;
            Font = (Font)containingDocument.ContentFont.Clone();
        }
    }

    public class HeaderView : HeaderFooterView {

        public HeaderView(DocumentViewModel containingDocument) : base(containingDocument) {
        }

        internal override Rectangle CalcBounds() {
            if (Enabled)
                return new Rectangle(containingDocument.Bounds.Left + containingDocument.Margins.Left,
                            containingDocument.Bounds.Top + containingDocument.Margins.Top,
                            containingDocument.Bounds.Width - containingDocument.Margins.Left - containingDocument.Margins.Right,
                            (int)Font.GetHeight(100));
            else
                return new Rectangle(0, 0, 0, 0);
        }

        //public new void Paint(Graphics g) {
        //    if (g is null) throw new ArgumentNullException(nameof(g));
        //    GraphicsState state = containingDocument.AdjustPrintOrPreview(g);
        //    // Draw borders
        //    base.Paint(g);
        //    g.Restore(state);
        //}
    }
    public class FooterView : HeaderFooterView {

        public FooterView(DocumentViewModel containingDocument) : base(containingDocument) {
        }

        internal override Rectangle CalcBounds() {
            if (Enabled)
                return new Rectangle(containingDocument.Bounds.Left + containingDocument.Margins.Left,
                                containingDocument.Bounds.Bottom - containingDocument.Margins.Bottom - (int)Font.GetHeight(100),
                                containingDocument.Bounds.Width - containingDocument.Margins.Left - containingDocument.Margins.Right,
                                (int)Font.GetHeight(100));
            else            
                return new Rectangle(0, 0, 0, 0);

        }

        //public new void Paint(Graphics g) {
        //    if (g is null) throw new ArgumentNullException(nameof(g));
        //    GraphicsState state = containingDocument.AdjustPrintOrPreview(g);
        //    // Draw borders
        //    base.Paint(g);
        //    g.Restore(state);
        //}
    }

    sealed internal class Macros {
        public HeaderFooterView headerFooter;

        public int NumPages { get { return headerFooter.containingDocument.NumPages; } }
        public string FileExtension { get { return Path.GetExtension(headerFooter.containingDocument.File); } }
        public string FileName { get { return Path.GetFileName(headerFooter.containingDocument.File); } }
        public string FilePath { get { return Path.GetDirectoryName(FullyQualifiedPath); } }
        public string FullyQualifiedPath { get { return Path.GetFullPath(headerFooter.containingDocument.File); } }
        public DateTime DatePrinted { get { return DateTime.Now; } }
        public DateTime DateRevised { get { return File.GetLastWriteTime(headerFooter.containingDocument.File); } }
        public string FileType { get { return headerFooter.containingDocument.Type; } }
        public string Title { get { return headerFooter.containingDocument.Title; } }

        public int Page { get; set; }

        internal Macros(HeaderFooterView hf) {
            headerFooter = hf;
        }

        /// <summary>
        /// Replaces macros of the form "{property:format}" using regex and Dynamic Invoke
        /// From https://stackoverflow.com/questions/39874172/dynamic-string-interpolation/39900731#39900731
        /// and  https://haacked.com/archive/2009/01/14/named-formats-redux.aspx/
        /// </summary>
        /// <param name="value"></param>
        /// <returns></returns>
        internal string ReplaceMacro(string value, int pageNum) {
            return Regex.Replace(value, @"(?<start>\{)+(?<property>[\w\.\[\]]+)(?<format>:[^}]+)?(?<end>\})+", match => {
                var p = System.Linq.Expressions.Expression.Parameter(typeof(Macros), "Macros");

                Group startGroup = match.Groups["start"];
                Group propertyGroup = match.Groups["property"];
                Group formatGroup = match.Groups["format"];
                Group endGroup = match.Groups["end"];

                // TODO: BUGBUG: As written this is not thread-safe. We have to figure out a way
                // of passing pageNum through to the macro parser in a threadsafe way
                Page = pageNum;
                LambdaExpression e;
                try {
                    e = DynamicExpressionParser.ParseLambda(new[] { p }, null, propertyGroup.Value);
                }
                catch (ParseException ex) {
                    // Non-existant Property or other parse error
                    return propertyGroup.Value;
                }
                if (formatGroup.Success) 
                    return string.Format(System.Globalization.CultureInfo.InvariantCulture, "{0" + formatGroup.Value + "}", e.Compile().DynamicInvoke(this));
                else
                    return (e.Compile().DynamicInvoke(this) ?? "").ToString();
            });
        }
    }


}

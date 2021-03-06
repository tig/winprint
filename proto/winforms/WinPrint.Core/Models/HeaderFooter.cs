﻿using System;
using System.Collections.Generic;
using System.Text;
using System.Drawing;
using GalaSoft.MvvmLight;

namespace WinPrint.Core.Models {
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
    /// </summary>
    // TODO: How to deal with clipping
    // 1) Order of print - Left, Right, Center (center wins)
    // 2) Elipsis - different based on macro. E.g. FullFilePath is "Start...FileName" where FileName is truncated last.
    // 3) Clipped (never overwritten - ugly)
    // 4) Wrapped (post MLP)
    public abstract class HeaderFooter : ModelBase {
        private string text;
        private Font font;
        private bool leftBorder;
        private bool topBorder;
        private bool rightBorder;
        private bool bottomBorder;
        private bool enabled;

        /// <summary>
        /// Header text. May contain macros (e.g. {FileName} or {Page}
        /// </summary>
        public string Text { get => text; set => SetField(ref text, value); }

        /// <summary>
        /// Font used for header or footer text
        /// </summary>
        public Font Font { get => font; set => SetField(ref font, value); }

        /// <summary>
        /// Enables or disables printing of left border of heder/footer
        /// </summary>
        public bool LeftBorder { get => leftBorder; set => SetField(ref leftBorder, value); }
        /// <summary>
        /// Enables or disables printing of Top border of heder/footer
        /// </summary>
        public bool TopBorder { get => topBorder; set => SetField(ref topBorder, value); }
        /// <summary>
        /// Enables or disables printing of Right border of heder/footer
        /// </summary>
        public bool RightBorder { get => rightBorder; set => SetField(ref rightBorder, value); }
        /// <summary>
        /// Enables or disables printing of Bottom border of heder/footer
        /// </summary>
        public bool BottomBorder { get => bottomBorder; set => SetField(ref bottomBorder, value); }

        /// <summary>
        /// Enable or disable header/footer
        /// </summary>
        public bool Enabled { get => enabled; set => SetField(ref enabled, value); }

        public HeaderFooter() {
            font = new Font() { Family = "Microsft Sans Serif", Size = 8F, Style = FontStyle.Bold };
        }
    }
    public class Header : HeaderFooter {
        public Header() : base() {
            Text = "|{FullyQualifiedPath}";
        }
    }
    public class Footer : HeaderFooter {
        public Footer() : base() {
            Text = "|{Page}/{NumPages}";
        }
    }
}

﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.IO;
using System.Text;
using System.Windows.Forms;
using WinPrint.Core.Models;

namespace WinPrint {
    /// <summary>
    /// Implements generic text file type support. 
    /// TODO: Wrap with a base-class that enables multiple content types
    /// Base class for WinPrint content types. Each file type may have a Content type
    /// These classes know how to parse and paint the file type's content.
    /// </summary>
    sealed class TextFileContent : IDisposable {
        private readonly SheetViewModel containingSheet;

        // private int linesPerPage;
        private System.Drawing.Font font;

        // All of the lines of the text file, after reflow/line-wrap
        private List<string> lines;

        private float lineHeight;
        private int linesPerPage;
        private float lineNumberWidth;
        private float minCharWidth;

        public TextFileContent(SheetViewModel sheet) {
            containingSheet = sheet;
        }

        public void Dispose() {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        // Protected implementation of Dispose pattern.
        // Flag: Has Dispose already been called?
        bool disposed = false;
        void Dispose(bool disposing) {
            if (disposed)
                return;

            if (disposing) {
                if (font != null) font.Dispose();
                lines = null;
            }
            disposed = true;
        }

        /// <summary>
        /// Get total count of pages. Set any local page-size related values (e.g. linesPerPage)
        /// </summary>
        /// <param name="e"></param>
        /// <returns></returns>
        internal List<Page> GetPages(StreamReader streamToPrint) {
            // Calculate the number of lines per page.
            font = new System.Drawing.Font(containingSheet.Font.Family, 
                containingSheet.Font.Size / 72F * 96F, containingSheet.Font.Style, GraphicsUnit.Pixel); // World?
            lineHeight = font.GetHeight(100);
            linesPerPage = (int)((float)containingSheet.ContentBounds.Height / lineHeight);
            //// BUGBUG: This will be too narrow if font is not fixed-pitch
            //lineNumberWidth = MeasureString(null, $"{((List<string>)DocumentContent).Count}0").Width;
            // TODO: Figure out how to make this right
            lineNumberWidth = MeasureString(null, $"{1234}0").Width;

            List<Page> pages = new List<Page>();
            lines = new List<string>();

            Page page;
            while (NextPage(streamToPrint, out page)) {
                pages.Add(page);
                page.PageNum = pages.Count;
                //Debug.WriteLine($"Added Page {page.PageNum}");
            }
            return pages;
        }

        /// <summary>
        /// Gets next page from stream. Returns false if no more pages
        /// </summary>
        /// <param name="streamToPrint"></param>
        /// <param name="page"></param>
        /// <returns></returns>
        private bool NextPage(StreamReader streamToPrint, out Page page) {
            page = new Page(containingSheet);

            string line = null;

            int charsFitted, linesFilled;
            float contentHeight = containingSheet.ContentBounds.Height;
            // Print each line of the file.
            int startLine = (int)((float)contentHeight / font.GetHeight(100)) * 0;
            int endLine = startLine + linesPerPage;
            //Debug.WriteLine($"NextPage - Height: {contentHeight}, lines: {linesPerPage}");
            //Debug.WriteLine($"startLine - {startLine}, endLine - {endLine}");

            minCharWidth = MeasureString(null, "W").Width;
            int minLineLen = (int)((float)((containingSheet.ContentBounds.Width - lineNumberWidth) / minCharWidth) );

            int curLine = 0;
            while (curLine < endLine && (line = streamToPrint.ReadLine()) != null) {
                float height = MeasureString(null, line, out charsFitted, out linesFilled).Height;
                if (linesFilled > 1) {
                    // Figure out how much bigger and break line apart
                    //Debug.WriteLine("multi-line line");
                    //while (--linesFilled > 0) {
                    // Starting at minLineLen into the line, keep trying until it wraps again
                    int start = 0;
                    int c = minLineLen;
                    for (int i = minLineLen; i < line.Length; i++) {
                        int linesFilled2;
                        height = MeasureString(null, line.Substring(start, c++), out charsFitted, out linesFilled2).Height;
                        if (linesFilled2 > 1) {
                            // It overflowed, so add line
                            lines.Add(line.Substring(start, i - 1));
                            curLine++;
                            start = start + i - 1;
                            c = line.Substring(start, line.Length - start).Length;
                            lines.Add(line.Substring(start, c));
                            curLine++;
                            i = line.Length;
                        }
                    }
                    //}
                }
                else {
                    lines.Add(line);
                    curLine++;
                }
            }
            return curLine != 0;
        }

        private SizeF MeasureString(Graphics g, string text) {
            int charsFitted, linesFilled;
            return MeasureString(g, text, out charsFitted, out linesFilled);
        }

        private SizeF MeasureString(Graphics g, string text, out int charsFitted, out int linesFilled) {
            if (g is null) {
                // define context used for determining glyph metrics.        
                using Bitmap bitmap = new Bitmap(1, 1);
                g = Graphics.FromImage(bitmap);
                //g = Graphics.FromHwnd(PrintPreview.Instance.Handle);
                g.PageUnit = GraphicsUnit.Document;

            }

            // determine width     
            float fontHeight = lineHeight;
            SizeF proposedSize = new SizeF(containingSheet.ContentBounds.Width - lineNumberWidth, lineHeight * linesPerPage);
            SizeF size = g.MeasureString(text, font, proposedSize, StringFormat.GenericTypographic, out charsFitted, out linesFilled);
            return size;
        }

        //private int GetLineHeight(string line) {
        //    //return (int)font.GetHeight(100);
        //    // TODO: TextRenderer is not right appoach because it's not what we use when
        //    // printing to the printer (it's only suppored for displays)
        //    // See if line is longer than margins allow

        //    // Set TextFormatFlags to no padding so strings are drawn together.
        //    TextFormatFlags flags = TextFormatFlags.Default;

        //    // Declare a proposed size with dimensions set to the maximum integer value.
        //    Size proposedSize = new Size(containingDocument.ContentBounds.Width, int.MaxValue);

        //    // Measure each string with its font and NoPadding value and 
        //    return TextRenderer.MeasureText(line, font, proposedSize, flags).Height;
        //}

        internal void Paint(Graphics g, int pageNum) {
            GraphicsState state = containingSheet.AdjustPrintOrPreview(g);

            float leftMargin = containingSheet.ContentBounds.Left;

            int charsFitted, linesFilled;

            // MeasureString(null, "Ay", out charsFitted, out linesFilled).Height;
            float contentHeight = containingSheet.ContentBounds.Height;
            // Print each line of the file.
            int startLine = (int)((float)contentHeight / lineHeight) * (pageNum - 1);
            int endLine = (int)(startLine + ((float)contentHeight / lineHeight));
            int lineOnPage;
            for (lineOnPage = 0; lineOnPage < linesPerPage; lineOnPage++) {
                int lineInDocument = lineOnPage + (linesPerPage * (pageNum - 1));
                PaintLineNumber(g, lineInDocument);

                if (lineInDocument < lines.Count && lineInDocument >= startLine && lineInDocument < endLine) {
                    float xPos = leftMargin + lineNumberWidth;
                    float yPos = containingSheet.ContentBounds.Top + (lineOnPage * lineHeight);
                    g.DrawString(lines[lineInDocument], font, Brushes.Black, xPos, yPos, StringFormat.GenericTypographic);
                    //SizeF proposedSize = new SizeF(containingDocument.ContentBounds.Width - lineNumberWidth, lineHeight * linesPerPage);
                    //g.DrawRectangle(Pens.Green, xPos, yPos, proposedSize.Width, lineHeight);
                    //SizeF s = g.MeasureString(lines[lineInDocument], font, proposedSize, StringFormat.GenericTypographic, out charsFitted, out linesFilled);
                    //g.DrawRectangle(Pens.Red, xPos, yPos, s.Width, s.Height);
                }
            }
            g.Restore(state);
        }

        internal void PaintLineNumber(Graphics g, int lineNumber) {
            if (lineNumberWidth != 0) {
                int lineOnPage = lineNumber % linesPerPage;
                float yPos = containingSheet.ContentBounds.Top + ((lineOnPage) * lineHeight);
                //g.DrawString($"{lineOnPage}", font, Brushes.Black, containingDocument.ContentBounds.Left - (lineNumberWidth), yPos, StringFormat.GenericDefault);
                g.DrawString($"{lineNumber + 1}", font, Brushes.Black, containingSheet.ContentBounds.Left, yPos, StringFormat.GenericDefault);
            }
        }
    }
}

using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using GalaSoft.MvvmLight;

namespace WinPrint.Core.ContentTypes {
    /// <summary>
    /// Implements generic text file type support. 
    /// Base class for WinPrint content types. Each file type may have a Content type
    /// These classes know how to parse and paint the file type's content.
    /// </summary>
    // TOOD: Color code c# kewoards https://www.c-sharpcorner.com/UploadFile/kirtan007/syntax-highlighting-in-richtextbox-using-C-Sharp/
    public class TextFileContent : ContentBase, IDisposable {

        public static string Type = "text/plain";
        public TextFileContent() {
            Font = new WinPrint.Core.Models.Font() { Family = "Lucida Sans Console", Size = 8F, Style = FontStyle.Regular };
        }

        // All of the lines of the text file, after reflow/line-wrap
        private List<string> lines;
        private float lineHeight;
        private int linesPerPage;
        private float lineNumberWidth;
        private float minCharWidth;
        private System.Drawing.Font cachedFont;

        // Publics
        public bool LineNumbers { get => lineNumbers; set => SetField(ref lineNumbers, value); }
        private bool lineNumbers = true;

        public bool LineNumberSeparator { get => lineNumberSeparator; set => SetField(ref lineNumberSeparator, value); }
        private bool lineNumberSeparator = true;

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
                if (cachedFont != null) cachedFont.Dispose();
                lines = null;
            }
            disposed = true;
        }

        /// <summary>
        /// Get total count of pages. Set any local page-size related values (e.g. linesPerPage)
        /// </summary>
        /// <param name="e"></param>
        /// <returns></returns>
        public override int CountPages(StreamReader streamToPrint) {
            // Calculate the number of lines per page.
            cachedFont = new System.Drawing.Font(Font.Family,
                Font.Size / 72F * 96F, Font.Style, GraphicsUnit.Pixel); // World?
            lineHeight = cachedFont.GetHeight(100);
            linesPerPage = (int)(PageSize.Height / lineHeight);
            //// BUGBUG: This will be too narrow if font is not fixed-pitch
            //lineNumberWidth = MeasureString(null, $"{((List<string>)DocumentContent).Count}0").Width;
            // TODO: Figure out how to make this right
            lineNumberWidth = LineNumbers ? MeasureString(null, $"{1234}0").Width : 0;

            //List<Page> pages = new List<Page>();
            lines = new List<string>();

            numPages = 0;
            while (NextPage(streamToPrint)) {
                numPages++;
            }

            return numPages;
        }

        /// <summary>
        /// Gets next page from stream. Returns false if no more pages
        /// </summary>
        /// <param name="streamToPrint"></param>
        /// <param name="page"></param>
        /// <returns></returns>
        // TODO: Deal with PageFeeds
        // TODO: Support different forms of linewrap (truncate/clip, word, line)
        // TODO: Support custom line wrap symbol
        private bool NextPage(StreamReader streamToPrint) {
            //page = new Page(containingSheet);

            string line = null;

            int charsFitted, linesFilled;
            float contentHeight = PageSize.Height;
            // Print each line of the file.
            int startLine = (int)((float)contentHeight / cachedFont.GetHeight(100)) * 0;
            int endLine = startLine + linesPerPage;
            //Debug.WriteLine($"NextPage - Height: {contentHeight}, lines: {linesPerPage}");
            //Debug.WriteLine($"startLine - {startLine}, endLine - {endLine}");

            minCharWidth = MeasureString(null, "W").Width;
            int minLineLen = (int)((float)((PageSize.Width - lineNumberWidth) / minCharWidth));

            int curLine = 0;
            while (curLine < endLine && (line = streamToPrint.ReadLine()) != null) {
                float height = MeasureString(null, line, out charsFitted, out linesFilled).Height;
                // TODO: This only wraps once - needs to recurse
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
            SizeF proposedSize = new SizeF(PageSize.Width - lineNumberWidth, lineHeight * linesPerPage);
            SizeF size = g.MeasureString(text, cachedFont, proposedSize, StringFormat.GenericTypographic, out charsFitted, out linesFilled);
            return size;
        }

        /// <summary>
        /// Paints a single page
        /// </summary>
        /// <param name="g">Graphics with 0,0 being the origin of the Page</param>
        /// <param name="pageNum">Page number to print</param>
        public override void PaintPage(Graphics g, int pageNum) {
            float leftMargin = 0;// containingSheet.GetPageX(pageNum);

            int charsFitted, linesFilled;

            float contentHeight = PageSize.Height;

            PaintLineNumberSeparator(g);

            // Print each line of the file.
            int startLine = (int)((float)contentHeight / lineHeight) * (pageNum - 1);
            int endLine = (int)(startLine + ((float)contentHeight / lineHeight));
            int lineOnPage;
            for (lineOnPage = 0; lineOnPage < linesPerPage; lineOnPage++) {
                int lineInDocument = lineOnPage + (linesPerPage * (pageNum - 1));

                if (lineInDocument < lines.Count && lineInDocument >= startLine && lineInDocument < endLine) {
                    PaintLineNumber(g, pageNum, lineInDocument);
                    float xPos = leftMargin + lineNumberWidth;
                    //float yPos = containingSheet.GetPageY(pageNum) + (lineOnPage * lineHeight);
                    float yPos = lineOnPage * lineHeight;
                    g.DrawString(lines[lineInDocument], cachedFont, Brushes.Black, xPos, yPos, StringFormat.GenericTypographic);
                    //SizeF proposedSize = new SizeF(containingDocument.ContentBounds.Width - lineNumberWidth, lineHeight * linesPerPage);
                    //g.DrawRectangle(Pens.Green, xPos, yPos, proposedSize.Width, lineHeight);
                    //SizeF s = g.MeasureString(lines[lineInDocument], font, proposedSize, StringFormat.GenericTypographic, out charsFitted, out linesFilled);
                    //g.DrawRectangle(Pens.Red, xPos, yPos, s.Width, s.Height);
                }
            }
        }

        private void PaintLineNumberSeparator(Graphics g) {
            if (LineNumbers && LineNumberSeparator && lineNumberWidth != 0) {
                g.DrawLine(Pens.Black, lineNumberWidth-2, 0, lineNumberWidth-2, PageSize.Height);
            }
        }

        // TODO: Allow a different (non-monospace) font for line numbers
        internal void PaintLineNumber(Graphics g, int pageNum, int lineNumber) {
            if (LineNumbers == true && lineNumberWidth != 0) {
                int lineOnPage = lineNumber % linesPerPage;
                int x = LineNumberSeparator ? (int)(lineNumberWidth - 8 - MeasureString(g, $"{lineNumber + 1}").Width) : 0;
                g.DrawString($"{lineNumber + 1}", cachedFont, Brushes.Black, x, lineOnPage * lineHeight, StringFormat.GenericDefault);
            }
        }
    }
}

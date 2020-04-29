using Serilog.Sinks.XUnit;
using System.Drawing;
using WinPrint.Core.ContentTypeEngines;
using WinPrint.Core.Models;
using WinPrint.Core.Services;
using Xunit;
using Xunit.Abstractions;

namespace WinPrint.Core.UnitTests.Cte
{
    public class AnsiCteTests
    {
        private static string CteClassName = typeof(AnsiCte).Name;
        public AnsiCteTests(ITestOutputHelper output)
        {
            ServiceLocator.Current.LogService.Start(GetType().Name, new TestOutputSink(output, new Serilog.Formatting.Display.MessageTemplateTextFormatter("{Message:lj}")), true, true);
        }

        [Fact]
        public void SupportedContentTypesTest()
        {
            AnsiCte cte = new AnsiCte();
            Assert.Equal(2, cte.SupportedContentTypes.Length);
            Assert.Equal("text/plain", cte.SupportedContentTypes[0]);
            Assert.Equal("text/ansi", cte.SupportedContentTypes[1]);
        }

        [Fact]
        public void NewContentTypeEngineTest()
        {
            SheetViewModel svm = new SheetViewModel();
            (svm.ContentEngine, svm.Language) = ContentTypeEngineBase.CreateContentTypeEngine(CteClassName);
            Assert.NotNull(svm.ContentEngine);

            Assert.Equal(CteClassName, svm.ContentEngine.GetType().Name);
            Assert.Equal(string.Empty, svm.Language);
        }

        [Fact]
        public void GetContentTypeTest()
        {
            //
            // Setup FileAssocaitons service
            Settings settings = Settings.CreateDefaultSettings();
            ModelLocator.Current.Settings.CopyPropertiesFrom(settings);

            // obviouslly text
            string path = "foo.txt";
            string type = ContentTypeEngineBase.GetContentTypeOrLanguage(path);
            Assert.Equal("text/plain", type);

            // html
            path = "foo.html";
            type = ContentTypeEngineBase.GetContentTypeOrLanguage(path);
            Assert.Equal("text/html", type);

            path = "foo.htm";
            type = ContentTypeEngineBase.GetContentTypeOrLanguage(path);
            Assert.Equal("text/html", type);

            // Something handled by prismcte
            path = "foo.cs";
            type = ContentTypeEngineBase.GetContentTypeOrLanguage(path);
            Assert.Equal("csharp", type);

            // Defeault
            path = "foo.xxxx";
            type = ContentTypeEngineBase.GetContentTypeOrLanguage(path);
            Assert.Equal("text/plain", type);
        }

        //private SizeF MeasureString(Graphics g, string text)
        //{
        //    int charsFitted, linesFilled;
        //    return MeasureString(g, text, out charsFitted, out linesFilled);
        //}


        /// <summary>
        /// Measures how much width a string will take, given current page settings (including line numbers)
        /// </summary>
        /// <param name="g"></param>
        /// <param name="text"></param>
        /// <param name="charsFitted"></param>
        /// <param name="linesFilled"></param>
        /// <returns></returns>


        [Fact]
        public async void RenderAsyncTest_FixedPitch()
        {
            string shortLine = "This is a line 0123456789";
            string longLine = "This is a line 01234567890";

            Settings settings = Settings.CreateDefaultSettings();
            ModelLocator.Current.Settings.CopyPropertiesFrom(settings);

            SheetViewModel svm = new SheetViewModel();
            (svm.ContentEngine, svm.Language) = ContentTypeEngineBase.CreateContentTypeEngine(CteClassName);
            Assert.NotNull(svm.ContentEngine);
            Assert.Equal(string.Empty, svm.Language);

            svm.ContentEngine.ContentSettings = new ContentSettings();

            // Setup page so only 1 line will fit
            svm.Margins = new System.Drawing.Printing.Margins(0, 0, 0, 0);

            // Setup page so 10 chars can fit across
            using Bitmap bitmap = new Bitmap(1, 1);
            bitmap.SetResolution(96, 96);
            Graphics g = Graphics.FromImage(bitmap);
            g.PageUnit = GraphicsUnit.Display; // Display is 1/100th"
            g.TextRenderingHint = ContentTypeEngineBase.TextRenderingHint;

            // Set a font that's 1" high
            svm.ContentEngine.ContentSettings.Font = new Core.Models.Font() { Family = "Courier New", Size = 72 }; // 72 points is 1" high
            System.Drawing.Font font = new System.Drawing.Font(svm.ContentEngine.ContentSettings.Font.Family,
                svm.ContentEngine.ContentSettings.Font.Size / 72F * 96,
                svm.ContentEngine.ContentSettings.Font.Style, GraphicsUnit.Pixel);

            // determine width     
            // Use page settings including lineNumberWidth
            SizeF proposedSize = new SizeF(10000, font.GetHeight() + (font.GetHeight() / 2));
            SizeF size = g.MeasureString(shortLine, font, proposedSize, ContentTypeEngineBase.StringFormat, out int charsFitted, out int linesFilled);

            ((AnsiCte)svm.ContentEngine).ContentSettings.LineNumbers = false;
            svm.ContentEngine.PageSize = new System.Drawing.SizeF(size.Width, font.GetHeight()); // a line will be about 108 high

            Assert.True(await svm.ContentEngine.SetDocumentAsync(""));
            Assert.Equal(1, await svm.ContentEngine.RenderAsync(new System.Drawing.Printing.PrinterResolution() { X = 96, Y = 96 }, null));

            Assert.True(await svm.ContentEngine.SetDocumentAsync(" "));
            Assert.Equal(1, await svm.ContentEngine.RenderAsync(new System.Drawing.Printing.PrinterResolution() { X = 96, Y = 96 }, null));

            Assert.True(await svm.ContentEngine.SetDocumentAsync("\n"));
            Assert.Equal(2, await svm.ContentEngine.RenderAsync(new System.Drawing.Printing.PrinterResolution() { X = 96, Y = 96 }, null));

            Assert.True(await svm.ContentEngine.SetDocumentAsync("\n\n"));
            Assert.Equal(3, await svm.ContentEngine.RenderAsync(new System.Drawing.Printing.PrinterResolution() { X = 96, Y = 96 }, null));

            Assert.True(await svm.ContentEngine.SetDocumentAsync("\n\n\n"));
            Assert.Equal(4, await svm.ContentEngine.RenderAsync(new System.Drawing.Printing.PrinterResolution() { X = 96, Y = 96 }, null));

            Assert.True(await svm.ContentEngine.SetDocumentAsync(" \n"));
            Assert.Equal(2, await svm.ContentEngine.RenderAsync(new System.Drawing.Printing.PrinterResolution() { X = 96, Y = 96 }, null));

            Assert.True(await svm.ContentEngine.SetDocumentAsync(" \n \n"));
            Assert.Equal(3, await svm.ContentEngine.RenderAsync(new System.Drawing.Printing.PrinterResolution() { X = 96, Y = 96 }, null));

            Assert.True(await svm.ContentEngine.SetDocumentAsync(shortLine));
            Assert.Equal(1, await svm.ContentEngine.RenderAsync(new System.Drawing.Printing.PrinterResolution() { X = 96, Y = 96 }, null));

            Assert.True(await svm.ContentEngine.SetDocumentAsync(shortLine+'_'));
            Assert.Equal(2, await svm.ContentEngine.RenderAsync(new System.Drawing.Printing.PrinterResolution() { X = 96, Y = 96 }, null));

            Assert.True(await svm.ContentEngine.SetDocumentAsync(shortLine + '\n'));
            Assert.Equal(2, await svm.ContentEngine.RenderAsync(new System.Drawing.Printing.PrinterResolution() { X = 96, Y = 96 }, null));

            Assert.True(await svm.ContentEngine.SetDocumentAsync(shortLine.Replace('9', ' ')));
            Assert.Equal(1, await svm.ContentEngine.RenderAsync(new System.Drawing.Printing.PrinterResolution() { X = 96, Y = 96 }, null));

            Assert.True(await svm.ContentEngine.SetDocumentAsync($"{shortLine}\n{shortLine}"));
            Assert.Equal(2, await svm.ContentEngine.RenderAsync(new System.Drawing.Printing.PrinterResolution() { X = 96, Y = 96 }, null));

            Assert.True(await svm.ContentEngine.SetDocumentAsync($"{shortLine}\n{shortLine}\n{shortLine}"));
            Assert.Equal(3, await svm.ContentEngine.RenderAsync(new System.Drawing.Printing.PrinterResolution() { X = 96, Y = 96 }, null));

            Assert.True(await svm.ContentEngine.SetDocumentAsync($"{shortLine}\n{shortLine}\n{shortLine}\n"));
            Assert.Equal(4, await svm.ContentEngine.RenderAsync(new System.Drawing.Printing.PrinterResolution() { X = 96, Y = 96 }, null));

            // Test line wrapping
            // 0123456789
            // 0
            Assert.True(await svm.ContentEngine.SetDocumentAsync($"{longLine}"));
            Assert.Equal(2, await svm.ContentEngine.RenderAsync(new System.Drawing.Printing.PrinterResolution() { X = 96, Y = 96 }, null));

            // 0123456789
            // 0A
            Assert.True(await svm.ContentEngine.SetDocumentAsync($"{longLine}A"));
            Assert.Equal(2, await svm.ContentEngine.RenderAsync(new System.Drawing.Printing.PrinterResolution() { X = 96, Y = 96 }, null));

            // 0123456789
            // 0A01234567
            // 89
            Assert.True(await svm.ContentEngine.SetDocumentAsync($"{longLine}A{longLine}"));
            Assert.Equal(3, await svm.ContentEngine.RenderAsync(new System.Drawing.Printing.PrinterResolution() { X = 96, Y = 96 }, null));
        }

        [Fact]
        public async void RenderAsyncTest_LineWrap()
        {
            string text = "1";
            string ansiText = "[38;2;0;0;207;01m1[39;00m";

            Settings settings = Settings.CreateDefaultSettings();
            ModelLocator.Current.Settings.CopyPropertiesFrom(settings);

            SheetViewModel svm = new SheetViewModel();
            (svm.ContentEngine, svm.Language) = ContentTypeEngineBase.CreateContentTypeEngine(CteClassName);
            Assert.NotNull(svm.ContentEngine);
            Assert.Equal(string.Empty, svm.Language);

            svm.ContentEngine.ContentSettings = new ContentSettings();

            // Setup page so only 1 line will fit
            svm.Margins = new System.Drawing.Printing.Margins(0, 0, 0, 0);

            // Setup page so 10 chars can fit across
            using Bitmap bitmap = new Bitmap(1, 1);
            bitmap.SetResolution(96, 96);
            Graphics g = Graphics.FromImage(bitmap);
            g.PageUnit = GraphicsUnit.Display; // Display is 1/100th"
            g.TextRenderingHint = ContentTypeEngineBase.TextRenderingHint;

            // Set a font that's 1" high
            svm.ContentEngine.ContentSettings.Font = new Core.Models.Font() { Family = "Courier New", Size = 72 }; // 72 points is 1" high
            System.Drawing.Font font = new System.Drawing.Font(svm.ContentEngine.ContentSettings.Font.Family,
                svm.ContentEngine.ContentSettings.Font.Size / 72F * 96,
                svm.ContentEngine.ContentSettings.Font.Style, GraphicsUnit.Pixel);

            // determine width     
            // Use page settings including lineNumberWidth
            SizeF proposedSize = new SizeF(10000, font.GetHeight() + (font.GetHeight() / 2));
            SizeF size = g.MeasureString(text, font, proposedSize, ContentTypeEngineBase.StringFormat, out int charsFitted, out int linesFilled);

            ((AnsiCte)svm.ContentEngine).ContentSettings.LineNumbers = false;
            svm.ContentEngine.PageSize = new System.Drawing.SizeF(size.Width, font.GetHeight()); // a line will be about 108 high

            Assert.True(await svm.ContentEngine.SetDocumentAsync(""));
            Assert.Equal(1, await svm.ContentEngine.RenderAsync(new System.Drawing.Printing.PrinterResolution() { X = 96, Y = 96 }, null));

            Assert.True(await svm.ContentEngine.SetDocumentAsync(text));
            Assert.Equal(1, await svm.ContentEngine.RenderAsync(new System.Drawing.Printing.PrinterResolution() { X = 96, Y = 96 }, null));

            Assert.True(await svm.ContentEngine.SetDocumentAsync(ansiText));
            Assert.Equal(1, await svm.ContentEngine.RenderAsync(new System.Drawing.Printing.PrinterResolution() { X = 96, Y = 96 }, null));

        }

        //[Fact]
        //public async void RenderAsyncTest_VariablePitch()
        //{
        //    string shortLine = "1 01234567890123456789";
        //    string longLine = "2 01234567890123456789A";

        //    Settings settings = Settings.CreateDefaultSettings();
        //    ModelLocator.Current.Settings.CopyPropertiesFrom(settings);

        //    SheetViewModel svm = new SheetViewModel();
        //    (svm.ContentEngine, svm.Language) = ContentTypeEngineBase.CreateContentTypeEngine(CteClassName);
        //    Assert.NotNull(svm.ContentEngine);
        //    Assert.Equal(string.Empty, svm.Language);

        //    svm.ContentEngine.ContentSettings = new ContentSettings();

        //    // Setup page so only 1 line will fit
        //    svm.Margins = new System.Drawing.Printing.Margins(0, 0, 0, 0);

        //    // Setup page so 10 chars can fit across
        //    using Bitmap bitmap = new Bitmap(1, 1);
        //    bitmap.SetResolution(96, 96);
        //    Graphics g = Graphics.FromImage(bitmap);
        //    g.PageUnit = GraphicsUnit.Display; // Display is 1/100th"
        //    g.TextRenderingHint = ContentTypeEngineBase.TextRenderingHint;

        //    // Set a font that's 1" high
        //    svm.ContentEngine.ContentSettings.Font = new Core.Models.Font() { Family = "Arial", Size = 72 }; // 72 points is 1" high
        //    System.Drawing.Font font = new System.Drawing.Font(svm.ContentEngine.ContentSettings.Font.Family,
        //        svm.ContentEngine.ContentSettings.Font.Size / 72F * 96,
        //        svm.ContentEngine.ContentSettings.Font.Style, GraphicsUnit.Pixel);

        //    // determine width     
        //    // Use page settings including lineNumberWidth
        //    SizeF proposedSize = new SizeF(10000, font.GetHeight() + (font.GetHeight() / 2));
        //    SizeF size = g.MeasureString(shortLine, font, proposedSize, ContentTypeEngineBase.StringFormat, out int charsFitted, out int linesFilled);

        //    ((AnsiCte)svm.ContentEngine).ContentSettings.LineNumbers = false;
        //    svm.ContentEngine.PageSize = new System.Drawing.SizeF(size.Width, font.GetHeight()); // a line will be about 108 high

        //    Assert.True(await svm.ContentEngine.SetDocumentAsync(shortLine));
        //    Assert.Equal(1, await svm.ContentEngine.RenderAsync(new System.Drawing.Printing.PrinterResolution() { X = 96, Y = 96 }, null));

        //    Assert.True(await svm.ContentEngine.SetDocumentAsync($"{shortLine}\n{shortLine}"));
        //    Assert.Equal(2, await svm.ContentEngine.RenderAsync(new System.Drawing.Printing.PrinterResolution() { X = 96, Y = 96 }, null));

        //    Assert.True(await svm.ContentEngine.SetDocumentAsync($"{shortLine}\n{shortLine}\n{shortLine}"));
        //    Assert.Equal(3, await svm.ContentEngine.RenderAsync(new System.Drawing.Printing.PrinterResolution() { X = 96, Y = 96 }, null));

        //    // Test line wrapping
        //    // 0123456789
        //    // 0
        //    Assert.True(await svm.ContentEngine.SetDocumentAsync($"{longLine}"));
        //    Assert.Equal(2, await svm.ContentEngine.RenderAsync(new System.Drawing.Printing.PrinterResolution() { X = 96, Y = 96 }, null));

        //    // 0123456789
        //    // 0A
        //    Assert.True(await svm.ContentEngine.SetDocumentAsync($"{longLine}A"));
        //    Assert.Equal(2, await svm.ContentEngine.RenderAsync(new System.Drawing.Printing.PrinterResolution() { X = 96, Y = 96 }, null));

        //    // 0123456789
        //    // 0A01234567
        //    // 89
        //    Assert.True(await svm.ContentEngine.SetDocumentAsync($"{longLine}A{shortLine}"));
        //    Assert.Equal(3, await svm.ContentEngine.RenderAsync(new System.Drawing.Printing.PrinterResolution() { X = 96, Y = 96 }, null));
        //}
    }
    }

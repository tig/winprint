using Serilog.Sinks.XUnit;
using System.Drawing;
using WinPrint.Core.ContentTypeEngines;
using WinPrint.Core.Models;
using WinPrint.Core.Services;
using Xunit;
using Xunit.Abstractions;

namespace WinPrint.Core.UnitTests.Cte
{
    public class TextCteTests
    {
        public TextCteTests(ITestOutputHelper output)
        {
            ServiceLocator.Current.LogService.Start(GetType().Name, new TestOutputSink(output, new Serilog.Formatting.Display.MessageTemplateTextFormatter("{Message:lj}")), true, true);
        }

        // ContentTypeEngineBase tests
        // Using TextCte since CTE is abstract
        [Fact]
        public void GetContentTypeNameTest()
        {
            TextCte cte = new TextCte();
            Assert.Equal("text/plain", cte.ContentTypeEngineName);
        }

        [Fact]
        public async void CreateContentTypeEngineTest()
        {
            SheetViewModel svm = new SheetViewModel
            {
                ContentEngine = await ContentTypeEngineBase.CreateContentTypeEngine("text/plain").ConfigureAwait(true)
            };
            Assert.Equal("text/plain", svm.ContentEngine.ContentTypeEngineName);

            svm.ContentEngine = await ContentTypeEngineBase.CreateContentTypeEngine("text/html").ConfigureAwait(true);
            Assert.NotEqual("text/plain", svm.ContentEngine.ContentTypeEngineName);
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
            string type = ContentTypeEngineBase.GetContentType(path);
            Assert.Equal("text/plain", type);

            // html
            path = "foo.html";
            type = ContentTypeEngineBase.GetContentType(path);
            Assert.Equal("text/html", type);

            path = "foo.htm";
            type = ContentTypeEngineBase.GetContentType(path);
            Assert.Equal("text/html", type);

            // Something handled by prismcte
            path = "foo.cs";
            type = ContentTypeEngineBase.GetContentType(path);
            Assert.Equal("csharp", type);

            // Defeault
            path = "foo.xxxx";
            type = ContentTypeEngineBase.GetContentType(path);
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

            SheetViewModel svm = new SheetViewModel
            {
                ContentEngine = await ContentTypeEngineBase.CreateContentTypeEngine("text/plain").ConfigureAwait(true)
            };
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

            ((TextCte)svm.ContentEngine).ContentSettings.LineNumbers = false;
            svm.ContentEngine.PageSize = new System.Drawing.SizeF(size.Width, font.GetHeight()); // a line will be about 108 high

            Assert.True(await svm.ContentEngine.SetDocumentAsync(shortLine));
            Assert.Equal(1, await svm.ContentEngine.RenderAsync(new System.Drawing.Printing.PrinterResolution() { X = 96, Y = 96 }, null));

            Assert.True(await svm.ContentEngine.SetDocumentAsync($"{shortLine}\n{shortLine}"));
            Assert.Equal(2, await svm.ContentEngine.RenderAsync(new System.Drawing.Printing.PrinterResolution() { X = 96, Y = 96 }, null));

            Assert.True(await svm.ContentEngine.SetDocumentAsync($"{shortLine}\n{shortLine}\n{shortLine}"));
            Assert.Equal(3, await svm.ContentEngine.RenderAsync(new System.Drawing.Printing.PrinterResolution() { X = 96, Y = 96 }, null));

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
        public async void RenderAsyncTest_VariablePitch()
        {
            string shortLine = "1 01234567890123456789";
            string longLine = "2 01234567890123456789A";

            Settings settings = Settings.CreateDefaultSettings();
            ModelLocator.Current.Settings.CopyPropertiesFrom(settings);

            SheetViewModel svm = new SheetViewModel
            {
                ContentEngine = await ContentTypeEngineBase.CreateContentTypeEngine("text/plain").ConfigureAwait(true)
            };
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
            svm.ContentEngine.ContentSettings.Font = new Core.Models.Font() { Family = "Arial", Size = 72 }; // 72 points is 1" high
            System.Drawing.Font font = new System.Drawing.Font(svm.ContentEngine.ContentSettings.Font.Family,
                svm.ContentEngine.ContentSettings.Font.Size / 72F * 96,
                svm.ContentEngine.ContentSettings.Font.Style, GraphicsUnit.Pixel);

            // determine width     
            // Use page settings including lineNumberWidth
            SizeF proposedSize = new SizeF(10000, font.GetHeight() + (font.GetHeight() / 2));
            SizeF size = g.MeasureString(shortLine, font, proposedSize, ContentTypeEngineBase.StringFormat, out int charsFitted, out int linesFilled);

            ((TextCte)svm.ContentEngine).ContentSettings.LineNumbers = false;
            svm.ContentEngine.PageSize = new System.Drawing.SizeF(size.Width, font.GetHeight()); // a line will be about 108 high

            Assert.True(await svm.ContentEngine.SetDocumentAsync(shortLine));
            Assert.Equal(1, await svm.ContentEngine.RenderAsync(new System.Drawing.Printing.PrinterResolution() { X = 96, Y = 96 }, null));

            Assert.True(await svm.ContentEngine.SetDocumentAsync($"{shortLine}\n{shortLine}"));
            Assert.Equal(2, await svm.ContentEngine.RenderAsync(new System.Drawing.Printing.PrinterResolution() { X = 96, Y = 96 }, null));

            Assert.True(await svm.ContentEngine.SetDocumentAsync($"{shortLine}\n{shortLine}\n{shortLine}"));
            Assert.Equal(3, await svm.ContentEngine.RenderAsync(new System.Drawing.Printing.PrinterResolution() { X = 96, Y = 96 }, null));

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
            Assert.True(await svm.ContentEngine.SetDocumentAsync($"{longLine}A{shortLine}"));
            Assert.Equal(3, await svm.ContentEngine.RenderAsync(new System.Drawing.Printing.PrinterResolution() { X = 96, Y = 96 }, null));
        }
    }
}

using System.Drawing;
using System.Drawing.Printing;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using Serilog.Sinks.XUnit;
using WinPrint.Core;
using WinPrint.Core.ContentTypeEngines;
using WinPrint.Core.Models;
using WinPrint.Core.Services;
using Xunit;
using Xunit.Abstractions;

namespace WinPrint.Core.UnitTests.ViewModels
{
    /// <summary>
    /// UX Validation: Tests file loading workflow including ContentEngine/ContentType/Language
    /// assignment, encoding detection, and error handling.
    /// </summary>
    public class FileLoadingTests
    {
        public FileLoadingTests(ITestOutputHelper output)
        {
            ServiceLocator.Current.LogService.Start(GetType().Name, new TestOutputSink(output, new Serilog.Formatting.Display.MessageTemplateTextFormatter("{Message:lj}")), true, true);

            // Ensure settings are loaded for CTE resolution
            ServiceLocator.Current.SettingsService.SettingsFileName = $"WinPrint.{GetType().Name}.json";
            if (File.Exists(ServiceLocator.Current.SettingsService.SettingsFileName))
            {
                File.Delete(ServiceLocator.Current.SettingsService.SettingsFileName);
            }
            var settings = ServiceLocator.Current.SettingsService.ReadSettings();
            ModelLocator.Current.Settings.CopyPropertiesFrom(settings);
        }

        private SheetViewModel CreateConfiguredSvm()
        {
            var svm = new SheetViewModel();
            var sheet = new SheetSettings
            {
                Name = "Test",
                Rows = 1,
                Columns = 1,
                Margins = new Margins(30, 30, 30, 30),
                ContentSettings = new ContentSettings
                {
                    Font = new WinPrint.Core.Models.Font { Family = "Consolas", Size = 10F, Style = FontStyle.Regular }
                },
                Header = new Header { Enabled = false, Text = "" },
                Footer = new Footer { Enabled = false, Text = "" }
            };
            svm.SetSheet(sheet);
            return svm;
        }

        #region LoadFileAsync - Content Engine Assignment

        [Fact]
        public async Task LoadFileAsync_TextFile_SetsContentEngine()
        {
            var svm = CreateConfiguredSvm();
            string tempFile = Path.GetTempFileName();
            File.WriteAllText(tempFile, "Hello World\nLine 2\nLine 3");

            try
            {
                var result = await svm.LoadFileAsync(tempFile);
                Assert.True(result);
                Assert.NotNull(svm.ContentEngine);
            }
            finally
            {
                File.Delete(tempFile);
            }
        }

        [Fact]
        public async Task LoadFileAsync_TextFile_SetsContentType()
        {
            var svm = CreateConfiguredSvm();
            string tempFile = Path.GetTempFileName(); // .tmp extension
            File.WriteAllText(tempFile, "Hello World");

            try
            {
                await svm.LoadFileAsync(tempFile);
                // .tmp should resolve to text/plain
                Assert.Equal("text/plain", svm.ContentType);
            }
            finally
            {
                File.Delete(tempFile);
            }
        }

        [Fact]
        public async Task LoadFileAsync_SetsFilePath()
        {
            var svm = CreateConfiguredSvm();
            string tempFile = Path.GetTempFileName();
            File.WriteAllText(tempFile, "content");

            try
            {
                await svm.LoadFileAsync(tempFile);
                Assert.Equal(tempFile, svm.File);
            }
            finally
            {
                File.Delete(tempFile);
            }
        }

        [Fact]
        public async Task LoadFileAsync_SetsLanguage()
        {
            var svm = CreateConfiguredSvm();
            string tempFile = Path.GetTempFileName();
            File.WriteAllText(tempFile, "content");

            try
            {
                await svm.LoadFileAsync(tempFile);
                // text/plain -> "Plain Text"
                Assert.Equal("Plain Text", svm.Language);
            }
            finally
            {
                File.Delete(tempFile);
            }
        }

        [Fact]
        public async Task LoadFileAsync_ExplicitContentType_OverridesExtension()
        {
            var svm = CreateConfiguredSvm();
            string tempFile = Path.GetTempFileName();
            File.WriteAllText(tempFile, "<html><body>Hello</body></html>");

            try
            {
                await svm.LoadFileAsync(tempFile, "text/html");
                Assert.Equal("text/html", svm.ContentType);
                Assert.IsType<HtmlCte>(svm.ContentEngine);
            }
            finally
            {
                File.Delete(tempFile);
            }
        }

        #endregion

        #region LoadFileAsync - Encoding Detection

        [Fact]
        public async Task LoadFileAsync_UTF8File_DetectsEncoding()
        {
            var svm = CreateConfiguredSvm();
            string tempFile = Path.GetTempFileName();
            File.WriteAllText(tempFile, "Hello UTF-8 café", Encoding.UTF8);

            try
            {
                await svm.LoadFileAsync(tempFile);
                Assert.NotNull(svm.Encoding);
                // UTF-8 should be detected or default
                Assert.Equal(Encoding.UTF8.WebName, svm.Encoding.WebName);
            }
            finally
            {
                File.Delete(tempFile);
            }
        }

        [Fact]
        public async Task LoadFileAsync_EmptyFile_DefaultsToUTF8()
        {
            var svm = CreateConfiguredSvm();
            string tempFile = Path.GetTempFileName();
            // Empty file - no content to detect encoding from
            File.WriteAllText(tempFile, "");

            try
            {
                await svm.LoadFileAsync(tempFile);
                Assert.Equal(Encoding.UTF8, svm.Encoding);
            }
            finally
            {
                File.Delete(tempFile);
            }
        }

        #endregion

        #region LoadFileAsync - Error Handling

        [Fact]
        public async Task LoadFileAsync_NonExistentFile_ThrowsFileNotFoundException()
        {
            var svm = CreateConfiguredSvm();
            string fakePath = Path.Combine(Path.GetTempPath(), "nonexistent_file_12345.txt");

            await Assert.ThrowsAsync<FileNotFoundException>(() => svm.LoadFileAsync(fakePath));
        }

        [Fact]
        public async Task LoadFileAsync_NullDocument_LoadsEmpty()
        {
            var svm = CreateConfiguredSvm();

            // Passing null filePath should load an empty document
            var result = await svm.LoadFileAsync(null);
            Assert.True(result);
            Assert.Equal("", svm.File);
        }

        [Fact]
        public async Task LoadFileAsync_EmptyPath_LoadsEmpty()
        {
            var svm = CreateConfiguredSvm();

            var result = await svm.LoadFileAsync("");
            Assert.True(result);
            Assert.Equal("", svm.File);
        }

        #endregion

        #region LoadFileAsync - State Transitions

        [Fact]
        public async Task LoadFileAsync_SetsLoadingDuringLoad()
        {
            var svm = CreateConfiguredSvm();
            string tempFile = Path.GetTempFileName();
            File.WriteAllText(tempFile, "content");

            bool wasLoading = false;
            svm.Loaded += (s, loading) =>
            {
                if (loading) wasLoading = true;
            };

            try
            {
                await svm.LoadFileAsync(tempFile);
                Assert.True(wasLoading);
                // After load completes, Loading should be false
                Assert.False(svm.Loading);
            }
            finally
            {
                File.Delete(tempFile);
            }
        }

        #endregion

        #region LoadStringAsync

        [Fact]
        public async Task LoadStringAsync_NullDocument_ThrowsArgumentNullException()
        {
            var svm = CreateConfiguredSvm();
            await Assert.ThrowsAsync<System.ArgumentNullException>(() => svm.LoadStringAsync(null, "text/plain"));
        }

        [Fact]
        public async Task LoadStringAsync_EmptyDocument_Succeeds()
        {
            var svm = CreateConfiguredSvm();
            var result = await svm.LoadStringAsync("", "text/plain");
            Assert.True(result);
        }

        [Fact]
        public async Task LoadStringAsync_ValidContent_SetsContentEngine()
        {
            var svm = CreateConfiguredSvm();
            var result = await svm.LoadStringAsync("Hello World\nLine2", "text/plain");
            Assert.True(result);
            Assert.NotNull(svm.ContentEngine);
            Assert.Equal("text/plain", svm.ContentType);
        }

        #endregion
    }
}

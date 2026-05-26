using System.Collections.Generic;
using System.IO;
using Serilog.Sinks.XUnit;
using WinPrint.Core;
using WinPrint.Core.ContentTypeEngines;
using WinPrint.Core.Models;
using WinPrint.Core.Services;
using Xunit;
using Xunit.Abstractions;

namespace WinPrint.Core.UnitTests.Models
{
    /// <summary>
    /// UX Validation: Tests command-line Options model and its mapping to SheetViewModel.
    /// Ensures the MAUI port handles the same CLI arguments.
    /// </summary>
    public class OptionsTests
    {
        public OptionsTests(ITestOutputHelper output)
        {
            ServiceLocator.Current.LogService.Start(GetType().Name, new TestOutputSink(output, new Serilog.Formatting.Display.MessageTemplateTextFormatter("{Message:lj}")), true, true);
        }

        #region Options Model Defaults

        [Fact]
        public void Options_Landscape_DefaultFalse()
        {
            var options = new Options();
            Assert.False(options.Landscape);
        }

        [Fact]
        public void Options_Portrait_DefaultFalse()
        {
            var options = new Options();
            Assert.False(options.Portrait);
        }

        [Fact]
        public void Options_FromPage_DefaultZero()
        {
            var options = new Options();
            Assert.Equal(0, options.FromPage);
        }

        [Fact]
        public void Options_ToPage_DefaultZero()
        {
            var options = new Options();
            Assert.Equal(0, options.ToPage);
        }

        [Fact]
        public void Options_CountPages_DefaultFalse()
        {
            var options = new Options();
            Assert.False(options.CountPages);
        }

        [Fact]
        public void Options_Verbose_DefaultFalse()
        {
            var options = new Options();
            Assert.False(options.Verbose);
        }

        [Fact]
        public void Options_Debug_DefaultFalse()
        {
            var options = new Options();
            Assert.False(options.Debug);
        }

        [Fact]
        public void Options_Gui_DefaultFalse()
        {
            var options = new Options();
            Assert.False(options.Gui);
        }

        #endregion

        #region Options to ViewModel Mapping

        [Fact]
        public void Options_Landscape_MapsToSheetViewModel()
        {
            var svm = new SheetViewModel();
            var sheet = new SheetSettings
            {
                Name = "Test",
                Landscape = false,
                Rows = 1,
                Columns = 1,
                Margins = new System.Drawing.Printing.Margins(30, 30, 30, 30),
                ContentSettings = new ContentSettings(),
                Header = new Header { Enabled = false, Text = "" },
                Footer = new Footer { Enabled = false, Text = "" }
            };
            svm.SetSheet(sheet);

            // Simulate what MainWindow does with --landscape option
            var options = new Options { Landscape = true };
            if (options.Landscape)
            {
                svm.Landscape = true;
            }

            Assert.True(svm.Landscape);
        }

        [Fact]
        public void Options_Portrait_OverridesLandscape()
        {
            var svm = new SheetViewModel();
            var sheet = new SheetSettings
            {
                Name = "Test",
                Landscape = true, // Sheet default is landscape
                Rows = 1,
                Columns = 1,
                Margins = new System.Drawing.Printing.Margins(30, 30, 30, 30),
                ContentSettings = new ContentSettings(),
                Header = new Header { Enabled = false, Text = "" },
                Footer = new Footer { Enabled = false, Text = "" }
            };
            svm.SetSheet(sheet);

            // Simulate what MainWindow does with --portrait option
            var options = new Options { Portrait = true };
            if (options.Portrait)
            {
                svm.Landscape = false;
            }

            Assert.False(svm.Landscape);
        }

        #endregion

        #region Sheet Selection

        [Fact]
        public void FindSheet_ByName_ReturnsCorrectSheet()
        {
            // Setup settings with known sheets
            ServiceLocator.Current.SettingsService.SettingsFileName = $"WinPrint.{GetType().Name}.json";
            if (File.Exists(ServiceLocator.Current.SettingsService.SettingsFileName))
            {
                File.Delete(ServiceLocator.Current.SettingsService.SettingsFileName);
            }

            var settings = Settings.CreateDefaultSettings();
            ModelLocator.Current.Settings.CopyPropertiesFrom(settings);

            var svm = new SheetViewModel();
            var sheet = new SheetSettings
            {
                Name = "Init",
                Rows = 1,
                Columns = 1,
                Margins = new System.Drawing.Printing.Margins(30, 30, 30, 30),
                ContentSettings = new ContentSettings(),
                Header = new Header { Enabled = false, Text = "" },
                Footer = new Footer { Enabled = false, Text = "" }
            };
            svm.SetSheet(sheet);

            // Find by name
            var found = svm.FindSheet("Default 2-Up", out var sheetID);
            Assert.NotNull(found);
            Assert.Equal("Default 2-Up", found.Name);
        }

        [Fact]
        public void FindSheet_InvalidName_ThrowsInvalidOperationException()
        {
            ServiceLocator.Current.SettingsService.SettingsFileName = $"WinPrint.{GetType().Name}.json";
            if (File.Exists(ServiceLocator.Current.SettingsService.SettingsFileName))
            {
                File.Delete(ServiceLocator.Current.SettingsService.SettingsFileName);
            }

            var settings = Settings.CreateDefaultSettings();
            ModelLocator.Current.Settings.CopyPropertiesFrom(settings);

            var svm = new SheetViewModel();
            var sheet = new SheetSettings
            {
                Name = "Init",
                Rows = 1,
                Columns = 1,
                Margins = new System.Drawing.Printing.Margins(30, 30, 30, 30),
                ContentSettings = new ContentSettings(),
                Header = new Header { Enabled = false, Text = "" },
                Footer = new Footer { Enabled = false, Text = "" }
            };
            svm.SetSheet(sheet);

            Assert.Throws<System.InvalidOperationException>(() => svm.FindSheet("NonExistent Sheet", out _));
        }

        [Fact]
        public void FindSheet_Default_ReturnsDefaultSheet()
        {
            ServiceLocator.Current.SettingsService.SettingsFileName = $"WinPrint.{GetType().Name}.json";
            if (File.Exists(ServiceLocator.Current.SettingsService.SettingsFileName))
            {
                File.Delete(ServiceLocator.Current.SettingsService.SettingsFileName);
            }

            var settings = Settings.CreateDefaultSettings();
            ModelLocator.Current.Settings.CopyPropertiesFrom(settings);

            var svm = new SheetViewModel();
            var sheet = new SheetSettings
            {
                Name = "Init",
                Rows = 1,
                Columns = 1,
                Margins = new System.Drawing.Printing.Margins(30, 30, 30, 30),
                ContentSettings = new ContentSettings(),
                Header = new Header { Enabled = false, Text = "" },
                Footer = new Footer { Enabled = false, Text = "" }
            };
            svm.SetSheet(sheet);

            var found = svm.FindSheet("default", out var sheetID);
            Assert.NotNull(found);
            Assert.Equal(settings.DefaultSheet.ToString(), sheetID);
        }

        #endregion

        #region NumFiles

        [Fact]
        public void Options_NumFiles_ReflectsFileCount()
        {
            var options = new Options
            {
                Files = new List<string> { "file1.cs", "file2.cs", "file3.cs" }
            };
            Assert.Equal(3, options.NumFiles);
        }

        #endregion
    }
}

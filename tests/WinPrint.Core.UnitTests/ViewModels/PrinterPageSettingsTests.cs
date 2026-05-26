using System.Drawing;
using System.Drawing.Printing;
using Serilog.Sinks.XUnit;
using WinPrint.Core;
using WinPrint.Core.Models;
using WinPrint.Core.Services;
using Xunit;
using Xunit.Abstractions;

namespace WinPrint.Core.UnitTests.ViewModels
{
    /// <summary>
    /// UX Validation: Tests SetPrinterPageSettings behavior including paper size,
    /// bounds, printable area, hard margins, and landscape dimension swapping.
    /// </summary>
    public class PrinterPageSettingsTests
    {
        public PrinterPageSettingsTests(ITestOutputHelper output)
        {
            ServiceLocator.Current.LogService.Start(GetType().Name, new TestOutputSink(output, new Serilog.Formatting.Display.MessageTemplateTextFormatter("{Message:lj}")), true, true);
        }

        private SheetViewModel CreateConfiguredSvm(bool landscape = false)
        {
            var svm = new SheetViewModel();
            var sheet = new SheetSettings
            {
                Name = "Test",
                Landscape = landscape,
                Rows = 1,
                Columns = 1,
                Padding = 3,
                Margins = new Margins(50, 50, 50, 50),
                ContentSettings = new ContentSettings
                {
                    Font = new WinPrint.Core.Models.Font { Family = "Consolas", Size = 10F, Style = FontStyle.Regular }
                },
                Header = new Header
                {
                    Enabled = true,
                    Text = "Header",
                    Font = new WinPrint.Core.Models.Font { Family = "Calibri", Size = 10F, Style = FontStyle.Bold },
                    VerticalPadding = 10
                },
                Footer = new Footer
                {
                    Enabled = true,
                    Text = "Footer",
                    Font = new WinPrint.Core.Models.Font { Family = "Calibri", Size = 10F, Style = FontStyle.Bold },
                    VerticalPadding = 10
                }
            };
            svm.SetSheet(sheet);
            return svm;
        }

        #region SetPrinterPageSettings - Null argument

        [Fact]
        public void SetPrinterPageSettings_NullPageSettings_ThrowsArgumentNullException()
        {
            var svm = CreateConfiguredSvm();
            Assert.Throws<System.ArgumentNullException>(() => svm.SetPrinterPageSettings(null));
        }

        #endregion

        #region SetPrinterPageSettings - Portrait Mode

        [Fact]
        public void SetPrinterPageSettings_Portrait_SetsBounds()
        {
            var svm = CreateConfiguredSvm(landscape: false);

            // Use default printer page settings
            var ps = new PageSettings();
            svm.SetPrinterPageSettings(ps);

            // Bounds should be set from page settings
            Assert.True(svm.Bounds.Width > 0);
            Assert.True(svm.Bounds.Height > 0);
        }

        [Fact]
        public void SetPrinterPageSettings_Portrait_SetsPaperSize()
        {
            var svm = CreateConfiguredSvm(landscape: false);

            var ps = new PageSettings();
            svm.SetPrinterPageSettings(ps);

            // In portrait, width < height for standard paper
            Assert.True(svm.PaperSize.Width > 0);
            Assert.True(svm.PaperSize.Height > 0);
            Assert.True(svm.PaperSize.Height >= svm.PaperSize.Width);
        }

        [Fact]
        public void SetPrinterPageSettings_Portrait_SetsContentBounds()
        {
            var svm = CreateConfiguredSvm(landscape: false);

            var ps = new PageSettings();
            svm.SetPrinterPageSettings(ps);

            // Content bounds should be smaller than page bounds (margins + header/footer)
            Assert.True(svm.ContentBounds.Width > 0);
            Assert.True(svm.ContentBounds.Height > 0);
            Assert.True(svm.ContentBounds.Width < svm.Bounds.Width);
            Assert.True(svm.ContentBounds.Height < svm.Bounds.Height);
        }

        #endregion

        #region SetPrinterPageSettings - Landscape Mode

        [Fact]
        public void SetPrinterPageSettings_Landscape_SwapsDimensions()
        {
            var svm = CreateConfiguredSvm(landscape: true);

            var ps = new PageSettings();
            ps.Landscape = true;
            svm.SetPrinterPageSettings(ps);

            // In landscape, width should be >= height for standard paper
            Assert.True(svm.PaperSize.Width >= svm.PaperSize.Height);
        }

        [Fact]
        public void SetPrinterPageSettings_Landscape_SetsBounds()
        {
            var svm = CreateConfiguredSvm(landscape: true);

            var ps = new PageSettings();
            ps.Landscape = true;
            svm.SetPrinterPageSettings(ps);

            Assert.True(svm.Bounds.Width > 0);
            Assert.True(svm.Bounds.Height > 0);
        }

        #endregion

        #region SetPrinterPageSettings - Content Bounds

        [Fact]
        public void SetPrinterPageSettings_ContentBounds_RespectsMargins()
        {
            var svm = CreateConfiguredSvm(landscape: false);

            var ps = new PageSettings();
            svm.SetPrinterPageSettings(ps);

            // ContentBounds X should be at left margin
            Assert.Equal(svm.Margins.Left, svm.ContentBounds.X, 0);
        }

        [Fact]
        public void SetPrinterPageSettings_ContentBounds_AccountsForHeaderFooter()
        {
            var svmWithHF = CreateConfiguredSvm(landscape: false);
            var ps = new PageSettings();
            svmWithHF.SetPrinterPageSettings(ps);
            var heightWithHF = svmWithHF.ContentBounds.Height;

            // Create SVM without header/footer
            var svmNoHF = new SheetViewModel();
            var sheet = new SheetSettings
            {
                Name = "Test",
                Landscape = false,
                Rows = 1,
                Columns = 1,
                Margins = new Margins(50, 50, 50, 50),
                ContentSettings = new ContentSettings
                {
                    Font = new WinPrint.Core.Models.Font { Family = "Consolas", Size = 10F, Style = FontStyle.Regular }
                },
                Header = new Header { Enabled = false, Text = "" },
                Footer = new Footer { Enabled = false, Text = "" }
            };
            svmNoHF.SetSheet(sheet);
            svmNoHF.SetPrinterPageSettings(ps);
            var heightWithoutHF = svmNoHF.ContentBounds.Height;

            // Content area should be larger without header/footer
            Assert.True(heightWithoutHF > heightWithHF);
        }

        #endregion

        #region SetPrinterPageSettings - Events

        [Fact]
        public void SetPrinterPageSettings_FiresPageSettingsSetEvent()
        {
            var svm = CreateConfiguredSvm();

            bool eventFired = false;
            svm.PageSettingsSet += (s, e) => eventFired = true;

            var ps = new PageSettings();
            svm.SetPrinterPageSettings(ps);

            Assert.True(eventFired);
        }

        #endregion

        #region SetPrinterPageSettings - PrinterResolution

        [Fact]
        public void SetPrinterPageSettings_SetsPrinterResolution()
        {
            var svm = CreateConfiguredSvm();

            var ps = new PageSettings();
            svm.SetPrinterPageSettings(ps);

            Assert.NotNull(svm.PrinterResolution);
        }

        #endregion

        #region CheckPrintOutsideHardMargins

        [Fact]
        public void CheckPrintOutsideHardMargins_LargeMarginsReturnTrue()
        {
            var svm = CreateConfiguredSvm();

            var ps = new PageSettings();
            svm.SetPrinterPageSettings(ps);

            // Default margins (50/100ths inch = 0.5") should be within printable area
            var result = svm.CheckPrintOutsideHardMargins();
            Assert.True(result);
        }

        #endregion
    }
}

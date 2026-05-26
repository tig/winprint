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
    /// UX Validation: Tests Header/Footer ViewModel bounds calculation, border propagation,
    /// enabled/disabled state, and property change notifications.
    /// </summary>
    public class HeaderFooterViewModelTests
    {
        public HeaderFooterViewModelTests(ITestOutputHelper output)
        {
            ServiceLocator.Current.LogService.Start(GetType().Name, new TestOutputSink(output, new Serilog.Formatting.Display.MessageTemplateTextFormatter("{Message:lj}")), true, true);
        }

        private SheetViewModel CreateConfiguredSvm()
        {
            var svm = new SheetViewModel();
            var sheet = new SheetSettings
            {
                Name = "Test",
                Landscape = false,
                Rows = 1,
                Columns = 1,
                Padding = 3,
                PageSeparator = false,
                Margins = new Margins(50, 50, 50, 50),
                ContentSettings = new ContentSettings
                {
                    Font = new WinPrint.Core.Models.Font { Family = "Consolas", Size = 10F, Style = FontStyle.Regular }
                },
                Header = new Header
                {
                    Enabled = true,
                    Text = "Left|Center|Right",
                    BottomBorder = true,
                    Font = new WinPrint.Core.Models.Font { Family = "Calibri", Size = 10F, Style = FontStyle.Bold },
                    VerticalPadding = 10
                },
                Footer = new Footer
                {
                    Enabled = true,
                    Text = "Footer Left|Footer Center|Footer Right",
                    TopBorder = true,
                    Font = new WinPrint.Core.Models.Font { Family = "Calibri", Size = 10F, Style = FontStyle.Bold },
                    VerticalPadding = 10
                }
            };
            svm.SetSheet(sheet);

            // Set bounds to simulate a letter-size page (850x1100 hundredths of inch)
            svm.Bounds = new Rectangle(0, 0, 850, 1100);

            return svm;
        }

        #region Header CalcBounds

        [Fact]
        public void Header_CalcBounds_ReturnsNonZeroWhenEnabled()
        {
            var svm = CreateConfiguredSvm();
            var bounds = svm.Header.Bounds;

            Assert.True(bounds.Width > 0);
            Assert.True(bounds.Height > 0);
        }

        [Fact]
        public void Header_CalcBounds_RespectsMargins()
        {
            var svm = CreateConfiguredSvm();
            var bounds = svm.Header.Bounds;

            // Header X should start at left margin
            Assert.Equal(svm.Bounds.Left + svm.Margins.Left, bounds.Left);
            // Header width should be page width minus both margins
            Assert.Equal(svm.Bounds.Width - svm.Margins.Left - svm.Margins.Right, bounds.Width);
        }

        [Fact]
        public void Header_CalcBounds_StartsAtTopMargin()
        {
            var svm = CreateConfiguredSvm();
            var bounds = svm.Header.Bounds;

            Assert.Equal(svm.Bounds.Top + svm.Margins.Top, bounds.Top);
        }

        [Fact]
        public void Header_CalcBounds_ZeroWhenDisabled()
        {
            var svm = CreateConfiguredSvm();
            svm.Header.Enabled = false;

            var bounds = svm.Header.Bounds;
            Assert.Equal(0, bounds.Width);
            Assert.Equal(0, bounds.Height);
        }

        #endregion

        #region Footer CalcBounds

        [Fact]
        public void Footer_CalcBounds_ReturnsNonZeroWhenEnabled()
        {
            var svm = CreateConfiguredSvm();
            var bounds = svm.Footer.Bounds;

            Assert.True(bounds.Width > 0);
            Assert.True(bounds.Height > 0);
        }

        [Fact]
        public void Footer_CalcBounds_RespectsMargins()
        {
            var svm = CreateConfiguredSvm();
            var bounds = svm.Footer.Bounds;

            Assert.Equal(svm.Bounds.Left + svm.Margins.Left, bounds.Left);
            Assert.Equal(svm.Bounds.Width - svm.Margins.Left - svm.Margins.Right, bounds.Width);
        }

        [Fact]
        public void Footer_CalcBounds_PositionedAtBottom()
        {
            var svm = CreateConfiguredSvm();
            var bounds = svm.Footer.Bounds;

            // Footer bottom should align with page bottom minus bottom margin
            var expectedBottom = svm.Bounds.Bottom - svm.Margins.Bottom;
            Assert.Equal(expectedBottom, bounds.Bottom, 1); // 1 decimal precision
        }

        [Fact]
        public void Footer_CalcBounds_ZeroWhenDisabled()
        {
            var svm = CreateConfiguredSvm();
            svm.Footer.Enabled = false;

            var bounds = svm.Footer.Bounds;
            Assert.Equal(0, bounds.Width);
            Assert.Equal(0, bounds.Height);
        }

        #endregion

        #region Border Properties

        [Fact]
        public void Header_BorderProperties_PropagateFromModel()
        {
            var svm = new SheetViewModel();
            var sheet = new SheetSettings
            {
                Name = "Test",
                Rows = 1,
                Columns = 1,
                Margins = new Margins(30, 30, 30, 30),
                ContentSettings = new ContentSettings(),
                Header = new Header
                {
                    Enabled = true,
                    Text = "Test",
                    LeftBorder = true,
                    TopBorder = true,
                    RightBorder = true,
                    BottomBorder = true,
                    Font = new WinPrint.Core.Models.Font { Family = "Calibri", Size = 10F, Style = FontStyle.Bold },
                    VerticalPadding = 10
                },
                Footer = new Footer { Enabled = false, Text = "" }
            };
            svm.SetSheet(sheet);

            Assert.True(svm.Header.LeftBorder);
            Assert.True(svm.Header.TopBorder);
            Assert.True(svm.Header.RightBorder);
            Assert.True(svm.Header.BottomBorder);
        }

        [Fact]
        public void Footer_BorderProperties_PropagateFromModel()
        {
            var svm = new SheetViewModel();
            var sheet = new SheetSettings
            {
                Name = "Test",
                Rows = 1,
                Columns = 1,
                Margins = new Margins(30, 30, 30, 30),
                ContentSettings = new ContentSettings(),
                Header = new Header { Enabled = false, Text = "" },
                Footer = new Footer
                {
                    Enabled = true,
                    Text = "Test",
                    LeftBorder = true,
                    TopBorder = true,
                    RightBorder = true,
                    BottomBorder = true,
                    Font = new WinPrint.Core.Models.Font { Family = "Calibri", Size = 10F, Style = FontStyle.Bold },
                    VerticalPadding = 10
                }
            };
            svm.SetSheet(sheet);

            Assert.True(svm.Footer.LeftBorder);
            Assert.True(svm.Footer.TopBorder);
            Assert.True(svm.Footer.RightBorder);
            Assert.True(svm.Footer.BottomBorder);
        }

        #endregion

        #region Property Change from Model

        [Fact]
        public void Header_TextChange_PropagatesFromModel()
        {
            var svm = new SheetViewModel();
            var sheet = new SheetSettings
            {
                Name = "Test",
                Rows = 1,
                Columns = 1,
                Margins = new Margins(30, 30, 30, 30),
                ContentSettings = new ContentSettings(),
                Header = new Header
                {
                    Enabled = true,
                    Text = "Original",
                    Font = new WinPrint.Core.Models.Font { Family = "Calibri", Size = 10F, Style = FontStyle.Bold },
                    VerticalPadding = 10
                },
                Footer = new Footer { Enabled = false, Text = "" }
            };
            svm.SetSheet(sheet);

            Assert.Equal("Original", svm.Header.Text);

            sheet.Header.Text = "Updated";
            Assert.Equal("Updated", svm.Header.Text);
        }

        [Fact]
        public void Header_EnabledChange_FiresSettingsChanged()
        {
            var svm = new SheetViewModel();
            var sheet = new SheetSettings
            {
                Name = "Test",
                Rows = 1,
                Columns = 1,
                Margins = new Margins(30, 30, 30, 30),
                ContentSettings = new ContentSettings(),
                Header = new Header
                {
                    Enabled = true,
                    Text = "Test",
                    Font = new WinPrint.Core.Models.Font { Family = "Calibri", Size = 10F, Style = FontStyle.Bold },
                    VerticalPadding = 10
                },
                Footer = new Footer { Enabled = false, Text = "" }
            };
            svm.SetSheet(sheet);

            bool settingsChangedFired = false;
            svm.SettingsChanged += (s, e) => settingsChangedFired = true;

            sheet.Header.Enabled = false;
            Assert.True(settingsChangedFired);
        }

        [Fact]
        public void Header_FontChange_TriggersReflow()
        {
            var svm = new SheetViewModel();
            var sheet = new SheetSettings
            {
                Name = "Test",
                Rows = 1,
                Columns = 1,
                Margins = new Margins(30, 30, 30, 30),
                ContentSettings = new ContentSettings(),
                Header = new Header
                {
                    Enabled = true,
                    Text = "Test",
                    Font = new WinPrint.Core.Models.Font { Family = "Calibri", Size = 10F, Style = FontStyle.Bold },
                    VerticalPadding = 10
                },
                Footer = new Footer { Enabled = false, Text = "" }
            };
            svm.SetSheet(sheet);

            bool? reflowValue = null;
            svm.SettingsChanged += (s, e) => reflowValue = e.Reflow;

            sheet.Header.Font = new WinPrint.Core.Models.Font { Family = "Arial", Size = 14F, Style = FontStyle.Regular };
            Assert.True(reflowValue);
        }

        #endregion

        #region Text Parsing (Left|Center|Right)

        [Fact]
        public void Header_Text_SupportsTabSeparatedParts()
        {
            var svm = CreateConfiguredSvm();

            // Verify text is stored correctly with separators
            Assert.Equal("Left|Center|Right", svm.Header.Text);
        }

        [Fact]
        public void Footer_Text_SupportsTabSeparatedParts()
        {
            var svm = CreateConfiguredSvm();
            Assert.Equal("Footer Left|Footer Center|Footer Right", svm.Footer.Text);
        }

        #endregion

        #region VerticalPadding

        [Fact]
        public void Header_VerticalPadding_PropagatesFromModel()
        {
            var svm = CreateConfiguredSvm();
            Assert.Equal(10, svm.Header.VerticalPadding);
        }

        [Fact]
        public void Footer_VerticalPadding_PropagatesFromModel()
        {
            var svm = CreateConfiguredSvm();
            Assert.Equal(10, svm.Footer.VerticalPadding);
        }

        [Fact]
        public void Header_VerticalPaddingChange_TriggersReflow()
        {
            var svm = new SheetViewModel();
            var sheet = new SheetSettings
            {
                Name = "Test",
                Rows = 1,
                Columns = 1,
                Margins = new Margins(30, 30, 30, 30),
                ContentSettings = new ContentSettings(),
                Header = new Header
                {
                    Enabled = true,
                    Text = "Test",
                    Font = new WinPrint.Core.Models.Font { Family = "Calibri", Size = 10F, Style = FontStyle.Bold },
                    VerticalPadding = 10
                },
                Footer = new Footer { Enabled = false, Text = "" }
            };
            svm.SetSheet(sheet);

            bool? reflowValue = null;
            svm.SettingsChanged += (s, e) => reflowValue = e.Reflow;

            sheet.Header.VerticalPadding = 20;
            Assert.True(reflowValue);
        }

        #endregion
    }
}

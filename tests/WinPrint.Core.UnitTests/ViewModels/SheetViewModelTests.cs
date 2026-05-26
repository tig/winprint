using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Drawing.Printing;
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
    /// UX Validation: Tests SheetViewModel lifecycle (SetSheet, Reset, Ready/Loading states)
    /// and property change notification contracts that the UI relies on.
    /// </summary>
    public class SheetViewModelTests
    {
        public SheetViewModelTests(ITestOutputHelper output)
        {
            ServiceLocator.Current.LogService.Start(GetType().Name, new TestOutputSink(output, new Serilog.Formatting.Display.MessageTemplateTextFormatter("{Message:lj}")), true, true);
        }

        private SheetSettings CreateTestSheet()
        {
            return new SheetSettings
            {
                Name = "Test Sheet",
                Landscape = true,
                Rows = 2,
                Columns = 2,
                Padding = 5,
                PageSeparator = true,
                Margins = new Margins(50, 50, 50, 50),
                ContentSettings = new ContentSettings
                {
                    Font = new WinPrint.Core.Models.Font { Family = "Consolas", Size = 10F, Style = FontStyle.Regular },
                    LineNumbers = true,
                    LineNumberSeparator = false
                },
                Header = new Header
                {
                    Enabled = true,
                    Text = "Header|Center|Right",
                    TopBorder = false,
                    BottomBorder = true,
                    LeftBorder = false,
                    RightBorder = false,
                    Font = new WinPrint.Core.Models.Font { Family = "Calibri", Size = 10F, Style = FontStyle.Bold },
                    VerticalPadding = 10
                },
                Footer = new Footer
                {
                    Enabled = true,
                    Text = "Footer|Page {Page}|{NumPages}",
                    TopBorder = true,
                    BottomBorder = false,
                    LeftBorder = false,
                    RightBorder = false,
                    Font = new WinPrint.Core.Models.Font { Family = "Calibri", Size = 10F, Style = FontStyle.Bold },
                    VerticalPadding = 10
                }
            };
        }

        #region SetSheet Property Propagation

        [Fact]
        public void SetSheet_PropagatesLandscape()
        {
            var svm = new SheetViewModel();
            var sheet = CreateTestSheet();
            svm.SetSheet(sheet);

            Assert.Equal(sheet.Landscape, svm.Landscape);
        }

        [Fact]
        public void SetSheet_PropagatesMargins()
        {
            var svm = new SheetViewModel();
            var sheet = CreateTestSheet();
            svm.SetSheet(sheet);

            Assert.Equal(sheet.Margins.Left, svm.Margins.Left);
            Assert.Equal(sheet.Margins.Right, svm.Margins.Right);
            Assert.Equal(sheet.Margins.Top, svm.Margins.Top);
            Assert.Equal(sheet.Margins.Bottom, svm.Margins.Bottom);
        }

        [Fact]
        public void SetSheet_PropagatesRowsAndColumns()
        {
            var svm = new SheetViewModel();
            var sheet = CreateTestSheet();
            svm.SetSheet(sheet);

            Assert.Equal(sheet.Rows, svm.Rows);
            Assert.Equal(sheet.Columns, svm.Columns);
        }

        [Fact]
        public void SetSheet_PropagatesPadding()
        {
            var svm = new SheetViewModel();
            var sheet = CreateTestSheet();
            svm.SetSheet(sheet);

            Assert.Equal(sheet.Padding, svm.Padding);
        }

        [Fact]
        public void SetSheet_PropagatesPageSeparator()
        {
            var svm = new SheetViewModel();
            var sheet = CreateTestSheet();
            svm.SetSheet(sheet);

            Assert.Equal(sheet.PageSeparator, svm.PageSeparator);
        }

        [Fact]
        public void SetSheet_PropagatesContentSettings()
        {
            var svm = new SheetViewModel();
            var sheet = CreateTestSheet();
            svm.SetSheet(sheet);

            Assert.NotNull(svm.ContentSettings);
            Assert.Equal(sheet.ContentSettings.Font.Family, svm.ContentSettings.Font.Family);
            Assert.Equal(sheet.ContentSettings.LineNumbers, svm.ContentSettings.LineNumbers);
        }

        [Fact]
        public void SetSheet_CreatesHeaderViewModel()
        {
            var svm = new SheetViewModel();
            var sheet = CreateTestSheet();
            svm.SetSheet(sheet);

            Assert.NotNull(svm.Header);
            Assert.Equal(sheet.Header.Text, svm.Header.Text);
            Assert.Equal(sheet.Header.Enabled, svm.Header.Enabled);
            Assert.Equal(sheet.Header.BottomBorder, svm.Header.BottomBorder);
        }

        [Fact]
        public void SetSheet_CreatesFooterViewModel()
        {
            var svm = new SheetViewModel();
            var sheet = CreateTestSheet();
            svm.SetSheet(sheet);

            Assert.NotNull(svm.Footer);
            Assert.Equal(sheet.Footer.Text, svm.Footer.Text);
            Assert.Equal(sheet.Footer.Enabled, svm.Footer.Enabled);
            Assert.Equal(sheet.Footer.TopBorder, svm.Footer.TopBorder);
        }

        [Fact]
        public void SetSheet_NullSheet_ReturnsEarlyWithoutChange()
        {
            var svm = new SheetViewModel();
            var sheet = CreateTestSheet();
            svm.SetSheet(sheet);

            // SetSheet(null) returns early — no change, no exception
            svm.SetSheet(null);
            Assert.True(svm.Landscape); // Still the value from CreateTestSheet
        }

        #endregion

        #region Reset

        [Fact]
        public void Reset_SetsReadyToFalse()
        {
            var svm = new SheetViewModel();
            var sheet = CreateTestSheet();
            svm.SetSheet(sheet);

            // Simulate ready state
            // After reset, Ready should be false
            svm.Reset();
            Assert.False(svm.Ready);
        }

        [Fact]
        public void Reset_ClearsContentEngine()
        {
            var svm = new SheetViewModel();
            var sheet = CreateTestSheet();
            svm.SetSheet(sheet);

            svm.ContentEngine = new TextCte();
            svm.Reset();

            Assert.Null(svm.ContentEngine);
        }

        [Fact]
        public void Reset_SetsNumSheetsToZero()
        {
            var svm = new SheetViewModel();
            var sheet = CreateTestSheet();
            svm.SetSheet(sheet);

            svm.Reset();
            Assert.Equal(0, svm.NumSheets);
        }

        #endregion

        #region Ready/Loading State Transitions

        [Fact]
        public void Loading_FiresLoadedEvent()
        {
            var svm = new SheetViewModel();
            var sheet = CreateTestSheet();
            svm.SetSheet(sheet);

            bool loadedFired = false;
            bool loadedValue = false;
            svm.Loaded += (s, loading) =>
            {
                loadedFired = true;
                loadedValue = loading;
            };

            svm.Loading = true;
            Assert.True(loadedFired);
            Assert.True(loadedValue);

            loadedFired = false;
            svm.Loading = false;
            Assert.True(loadedFired);
            Assert.False(loadedValue);
        }

        [Fact]
        public void Ready_FiresReadyChangedEvent()
        {
            var svm = new SheetViewModel();
            var sheet = CreateTestSheet();
            svm.SetSheet(sheet);

            bool readyFired = false;
            bool readyValue = false;
            svm.ReadyChanged += (s, ready) =>
            {
                readyFired = true;
                readyValue = ready;
            };

            svm.Ready = true;
            Assert.True(readyFired);
            Assert.True(readyValue);

            readyFired = false;
            svm.Ready = false;
            Assert.True(readyFired);
            Assert.False(readyValue);
        }

        [Fact]
        public void Loading_SameValue_DoesNotFireEvent()
        {
            var svm = new SheetViewModel();
            var sheet = CreateTestSheet();
            svm.SetSheet(sheet);

            svm.Loading = true;

            bool firedAgain = false;
            svm.Loaded += (s, l) => firedAgain = true;
            svm.Loading = true; // same value

            Assert.False(firedAgain);
        }

        #endregion

        #region Property Change Notifications

        [Fact]
        public void PropertyChanged_FiresForLandscape()
        {
            var svm = new SheetViewModel();
            var changedProps = new List<string>();
            svm.PropertyChanged += (s, e) => changedProps.Add(e.PropertyName);

            svm.Landscape = true;
            Assert.Contains("Landscape", changedProps);
        }

        [Fact]
        public void PropertyChanged_FiresForMargins()
        {
            var svm = new SheetViewModel();
            var changedProps = new List<string>();
            svm.PropertyChanged += (s, e) => changedProps.Add(e.PropertyName);

            svm.Margins = new Margins(100, 100, 100, 100);
            Assert.Contains("Margins", changedProps);
        }

        [Fact]
        public void PropertyChanged_FiresForRows()
        {
            var svm = new SheetViewModel();
            var changedProps = new List<string>();
            svm.PropertyChanged += (s, e) => changedProps.Add(e.PropertyName);

            svm.Rows = 3;
            Assert.Contains("Rows", changedProps);
        }

        [Fact]
        public void PropertyChanged_FiresForColumns()
        {
            var svm = new SheetViewModel();
            var changedProps = new List<string>();
            svm.PropertyChanged += (s, e) => changedProps.Add(e.PropertyName);

            svm.Columns = 4;
            Assert.Contains("Columns", changedProps);
        }

        [Fact]
        public void PropertyChanged_FiresForPadding()
        {
            var svm = new SheetViewModel();
            var changedProps = new List<string>();
            svm.PropertyChanged += (s, e) => changedProps.Add(e.PropertyName);

            svm.Padding = 10;
            Assert.Contains("Padding", changedProps);
        }

        [Fact]
        public void PropertyChanged_FiresForPageSeparator()
        {
            var svm = new SheetViewModel();
            var changedProps = new List<string>();
            svm.PropertyChanged += (s, e) => changedProps.Add(e.PropertyName);

            svm.PageSeparator = true;
            Assert.Contains("PageSeparator", changedProps);
        }

        [Fact]
        public void PropertyChanged_FiresForFile()
        {
            var svm = new SheetViewModel();
            var changedProps = new List<string>();
            svm.PropertyChanged += (s, e) => changedProps.Add(e.PropertyName);

            svm.File = "test.cs";
            Assert.Contains("File", changedProps);
        }

        [Fact]
        public void PropertyChanged_FiresForContentEngine()
        {
            var svm = new SheetViewModel();
            var changedProps = new List<string>();
            svm.PropertyChanged += (s, e) => changedProps.Add(e.PropertyName);

            svm.ContentEngine = new TextCte();
            Assert.Contains("ContentEngine", changedProps);
        }

        [Fact]
        public void PropertyChanged_FiresForContentType()
        {
            var svm = new SheetViewModel();
            var changedProps = new List<string>();
            svm.PropertyChanged += (s, e) => changedProps.Add(e.PropertyName);

            svm.ContentType = "text/x-csharp";
            Assert.Contains("ContentType", changedProps);
        }

        [Fact]
        public void PropertyChanged_FiresForLanguage()
        {
            var svm = new SheetViewModel();
            var changedProps = new List<string>();
            svm.PropertyChanged += (s, e) => changedProps.Add(e.PropertyName);

            svm.Language = "C#";
            Assert.Contains("Language", changedProps);
        }

        [Fact]
        public void PropertyChanged_FiresForContentSettings()
        {
            var svm = new SheetViewModel();
            var changedProps = new List<string>();
            svm.PropertyChanged += (s, e) => changedProps.Add(e.PropertyName);

            svm.ContentSettings = new ContentSettings();
            Assert.Contains("ContentSettings", changedProps);
        }

        [Fact]
        public void PropertyChanged_FiresForHeader()
        {
            var svm = new SheetViewModel();
            var sheet = CreateTestSheet();
            svm.SetSheet(sheet);

            // Header changes fire via SettingsChanged event, not PropertyChanged
            var settingsChangedFired = false;
            svm.SettingsChanged += (s, e) => settingsChangedFired = true;

            // Modify the header through the view model's internal header
            sheet.Header.Text = "Different";

            Assert.True(settingsChangedFired);
        }

        [Fact]
        public void PropertyChanged_FiresForFooter()
        {
            var svm = new SheetViewModel();
            var sheet = CreateTestSheet();
            svm.SetSheet(sheet);

            // Footer changes fire via SettingsChanged event, not PropertyChanged
            var settingsChangedFired = false;
            svm.SettingsChanged += (s, e) => settingsChangedFired = true;

            // Modify the footer through the sheet model
            sheet.Footer.Text = "Different";

            Assert.True(settingsChangedFired);
        }

        [Fact]
        public void PropertyChanged_DoesNotFireForSameValue()
        {
            var svm = new SheetViewModel();
            svm.Rows = 2;

            var changedProps = new List<string>();
            svm.PropertyChanged += (s, e) => changedProps.Add(e.PropertyName);

            svm.Rows = 2; // same value
            Assert.DoesNotContain("Rows", changedProps);
        }

        #endregion

        #region SettingsChanged Event

        [Fact]
        public void SettingsChanged_FiresOnSheetPropertyChange()
        {
            var svm = new SheetViewModel();
            var sheet = CreateTestSheet();
            svm.SetSheet(sheet);

            bool settingsChangedFired = false;
            svm.SettingsChanged += (s, e) => settingsChangedFired = true;

            // Changing the sheet's Rows should trigger SettingsChanged
            sheet.Rows = 3;
            Assert.True(settingsChangedFired);
        }

        [Fact]
        public void SettingsChanged_ReflowTrueForStructuralChanges()
        {
            var svm = new SheetViewModel();
            var sheet = CreateTestSheet();
            svm.SetSheet(sheet);

            bool? reflowValue = null;
            svm.SettingsChanged += (s, e) => reflowValue = e.Reflow;

            // Margins change should trigger reflow
            sheet.Margins = new Margins(100, 100, 100, 100);
            Assert.True(reflowValue);
        }

        #endregion

        #region NumSheets Calculation

        [Fact]
        public void NumSheets_ZeroWhenNoContentEngine()
        {
            var svm = new SheetViewModel();
            var sheet = CreateTestSheet();
            svm.SetSheet(sheet);

            Assert.Equal(0, svm.NumSheets);
        }

        [Fact]
        public void NumSheets_ZeroWhenRowsZero()
        {
            var svm = new SheetViewModel();
            svm.Rows = 0;
            svm.Columns = 2;
            svm.ContentEngine = new TextCte();

            Assert.Equal(0, svm.NumSheets);
        }

        [Fact]
        public void NumSheets_ZeroWhenColumnsZero()
        {
            var svm = new SheetViewModel();
            svm.Rows = 2;
            svm.Columns = 0;
            svm.ContentEngine = new TextCte();

            Assert.Equal(0, svm.NumSheets);
        }

        #endregion

        #region SetSheet Replaces Previous Sheet

        [Fact]
        public void SetSheet_CalledTwice_UsesNewSheetValues()
        {
            var svm = new SheetViewModel();

            var sheet1 = CreateTestSheet();
            sheet1.Rows = 1;
            sheet1.Columns = 1;
            svm.SetSheet(sheet1);

            var sheet2 = CreateTestSheet();
            sheet2.Rows = 3;
            sheet2.Columns = 3;
            svm.SetSheet(sheet2);

            Assert.Equal(3, svm.Rows);
            Assert.Equal(3, svm.Columns);
        }

        #endregion
    }
}

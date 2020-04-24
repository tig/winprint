using Serilog.Sinks.XUnit;
using System.Drawing;
using WinPrint.Core.ContentTypeEngines;
using WinPrint.Core.Models;
using WinPrint.Core.Services;
using Xunit;
using Xunit.Abstractions;

namespace WinPrint.Core.UnitTests.Cte
{
    public class PygmentsConverterServiceTests
    {
        private static string CteClassName = typeof(TextCte).Name;
        public PygmentsConverterServiceTests(ITestOutputHelper output)
        {
            ServiceLocator.Current.LogService.Start(GetType().Name, new TestOutputSink(output, new Serilog.Formatting.Display.MessageTemplateTextFormatter("{Message:lj}")), true, true);
        }

        // ContentTypeEngineBase tests
        // Using TextCte since CTE is abstract
        [Fact]
        public void SupportedContentTypesTest()
        {
            var p = ServiceLocator.Current.PygmentsConverterService;
            Assert.NotNull(p);

            var input = $@"using system;";
            var output = PygmentsConverterService.Convert("using system;");
            Assert.Equal(input, output);
        }
    }
}
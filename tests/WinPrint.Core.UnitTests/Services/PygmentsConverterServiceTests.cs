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

#if CI_BUILD
#else
        [Fact]
        public async void ConvertAsyncTest()
        {
            var ps = new PygmentsConverterService();
            var input = $@"using system;";
            // "using system;" | out-file using.cs
            // pygmentize -O 16m,style=friendly .\using.cs | out-file using.ans
            var expectedOutput = "\u001b[38;2;0;112;32;01musing\u001b[39;00m\u001b[38;2;187;187;187m \u001b[39m\u001b[38;2;14;132;181;01msystem\u001b[39;00m;";
            var output = await ps.ConvertAsync(input, "friendly", "c#");
            Assert.Equal(expectedOutput, output);
        }
#endif
    }
}
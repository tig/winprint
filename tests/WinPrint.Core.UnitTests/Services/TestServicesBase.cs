using Serilog.Sinks.XUnit;
using System.Text.Json;
using System.Text.Json.Serialization;
using WinPrint.Core.Services;
using Xunit.Abstractions;

namespace WinPrint.Core.UnitTests.Services
{
    public class TestServicesBase
    {
        public JsonSerializerOptions jsonOptions;
        public TestServicesBase(ITestOutputHelper output)
        {
            ServiceLocator.Current.LogService.Start(GetType().Name, new TestOutputSink(output, new Serilog.Formatting.Display.MessageTemplateTextFormatter("{Message:lj}")), true, true);
            jsonOptions = new JsonSerializerOptions
            {
                WriteIndented = true,
                AllowTrailingCommas = true,
                PropertyNameCaseInsensitive = true,
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                ReadCommentHandling = JsonCommentHandling.Skip
            };
            jsonOptions.Converters.Add(new JsonStringEnumConverter(JsonNamingPolicy.CamelCase));
        }
    }
}
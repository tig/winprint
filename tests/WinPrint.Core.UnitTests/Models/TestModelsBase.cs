using System.Text.Json;
using System.Text.Json.Serialization;
using Serilog.Formatting.Display;
using Serilog.Sinks.XUnit;
using WinPrint.Core.Services;
using Xunit.Abstractions;

namespace WinPrint.Core.UnitTests.Models;

public class TestModelsBase
{
    public JsonSerializerOptions jsonOptions;

    public TestModelsBase(ITestOutputHelper output)
    {
        ServiceLocator.Current.LogService.Start(GetType().Name,
            new TestOutputSink(output, new MessageTemplateTextFormatter("{Message:lj}")), true, true);

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

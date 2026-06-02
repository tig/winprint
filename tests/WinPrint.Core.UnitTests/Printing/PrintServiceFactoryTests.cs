using WinPrint.Core.Abstractions;
using WinPrint.Core.Printing;
using Xunit;

namespace WinPrint.Core.UnitTests.Printing;

public class PrintServiceFactoryTests
{
    [Fact]
    public void Create_OnWindows_ReturnsWindowsBackendWithDefaultMeasurementContext()
    {
        IPrintService service = PrintServiceFactory.Create();

        if (OperatingSystem.IsWindows())
        {
            Assert.IsType<WindowsPrintService>(service);
            // Windows reflows with the System.Drawing default context, so none is supplied here.
            Assert.Null(service.CreateMeasurementContext());
        }
        else
        {
            Assert.IsType<UnixPrintService>(service);
            Assert.NotNull(service.CreateMeasurementContext());
        }
    }
}

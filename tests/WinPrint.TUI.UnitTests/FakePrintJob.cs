using WinPrint.Core.Abstractions;

namespace WinPrint.TUI.UnitTests;

public sealed class FakePrintJob : IPrintJob
{
    public FakePrintJob(PrintPageSetup pageSetup, string documentName)
    {
        PageSetup = pageSetup;
        DocumentName = documentName;
    }

    public PrintPageSetup PageSetup { get; }

    public string DocumentName { get; }

    public int BeginCalls { get; private set; }

    public List<int> PrintedPages { get; } = [];

    public void Begin()
    {
        BeginCalls++;
    }

    public void PrintPage(int pageNumber, Action<IGraphicsContext, int> renderPage)
    {
        PrintedPages.Add(pageNumber);
    }

    public Task<PrintJobResult> EndAsync(CancellationToken cancellationToken = default)
    {
        return Task.FromResult(PrintJobResult.Succeeded(PrintedPages.Count));
    }

    public void Dispose()
    {
    }
}

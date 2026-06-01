namespace WinPrint.Core.Abstractions;

public interface IPrintJob : IDisposable
{
    void Begin();
    void PrintPage(int pageNumber, Action<IGraphicsContext, int> renderPage);

    /// <summary>
    ///     Completes the job: renders/submits all queued pages and returns the outcome. Asynchronous
    ///     so backends that hand off to an external spooler (lpr/CUPS) or a UI print controller can
    ///     await completion and report failures, rather than disposing before rendering finishes.
    /// </summary>
    Task<PrintJobResult> EndAsync(CancellationToken cancellationToken = default);
}

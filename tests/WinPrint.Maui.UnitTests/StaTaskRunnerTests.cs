using WinPrint.Maui.Services;
using Xunit;

namespace WinPrint.Maui.UnitTests;

/// <summary>
///     Contract tests for <see cref="StaTaskRunner" />, the dependency-free background-thread runner
///     that <c>WindowsSkiaPrintJob.EndAsync</c> relies on. These guard the two review concerns the
///     job's spool path can't be unit-tested for directly (it needs WPF + a printer): the returned
///     Task must always complete — even when the work throws — and it must observe cancellation.
/// </summary>
public class StaTaskRunnerTests
{
    private static readonly TimeSpan Timeout = TimeSpan.FromSeconds(5);

    [Fact]
    public async Task RunAsync_ReturnsWorkResult()
    {
        int result = await StaTaskRunner.RunAsync(() => 42, _ => -1).WaitAsync(Timeout);

        Assert.Equal(42, result);
    }

    [Fact]
    public async Task RunAsync_WhenWorkThrows_CompletesWithOnErrorResult_AndDoesNotHang()
    {
        // The whole point: an unexpected exception on the worker thread must not leave the awaiter
        // hanging forever — it must resolve via the onError mapping. WaitAsync fails the test on hang.
        int result = await StaTaskRunner
            .RunAsync<int>(() => throw new InvalidOperationException("boom"), _ => 99)
            .WaitAsync(Timeout);

        Assert.Equal(99, result);
    }

    [Fact]
    public async Task RunAsync_WhenOnErrorAlsoThrows_FaultsTaskInsteadOfHanging()
    {
        Task<int> task = StaTaskRunner.RunAsync<int>(
            () => throw new InvalidOperationException("boom"),
            _ => throw new FormatException("onError blew up"));

        await Assert.ThrowsAsync<FormatException>(() => task.WaitAsync(Timeout));
    }

    [Fact]
    public async Task RunAsync_WithPreCancelledToken_CancelsWithoutRunningWork()
    {
        using var cts = new CancellationTokenSource();
        cts.Cancel();
        bool ran = false;

        Task<int> task = StaTaskRunner.RunAsync(() => { ran = true; return 1; }, _ => 0, cts.Token);

        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => task.WaitAsync(Timeout));
        Assert.False(ran);
    }

    [Fact]
    public async Task RunAsync_WhenWorkObservesCancellation_Cancels()
    {
        using var cts = new CancellationTokenSource();

        Task<int> task = StaTaskRunner.RunAsync<int>(() =>
        {
            cts.Cancel();
            cts.Token.ThrowIfCancellationRequested();
            return 1;
        }, _ => -1, cts.Token);

        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => task.WaitAsync(Timeout));
    }
}

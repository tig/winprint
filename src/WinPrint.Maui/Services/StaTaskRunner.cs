namespace WinPrint.Maui.Services;

/// <summary>
///     Runs a synchronous unit of work on a dedicated background thread and exposes it as a
///     <see cref="Task{TResult}" />, <b>guaranteeing the task always completes</b> — with the work's
///     result, a mapped error, a fault, or cancellation — so awaiters can never hang. On Windows the
///     thread is STA because <c>WindowsSkiaPrintJob</c>'s spool path uses WPF imaging /
///     <c>XpsDocumentWriter</c>, which require it; elsewhere it is a plain background thread.
///     <para>
///         Kept dependency-free (BCL only) so the print job's completion/cancellation contract can be
///         unit-tested cross-platform without WPF or a printer (see <c>StaTaskRunnerTests</c>).
///     </para>
/// </summary>
internal static class StaTaskRunner
{
    /// <summary>
    ///     Invokes <paramref name="work" /> on a background (STA on Windows) thread.
    ///     <list type="bullet">
    ///         <item>Returns the value from <paramref name="work" /> on success.</item>
    ///         <item>
    ///             Maps an exception thrown by <paramref name="work" /> through
    ///             <paramref name="onError" /> into a result (so expected failures surface as data, not a
    ///             faulted task); if <paramref name="onError" /> itself throws, the task faults rather
    ///             than hanging.
    ///         </item>
    ///         <item>
    ///             Cancels the task if <paramref name="cancellationToken" /> is already cancelled (work
    ///             never runs) or if <paramref name="work" /> observes the token and throws
    ///             <see cref="OperationCanceledException" />.
    ///         </item>
    ///     </list>
    /// </summary>
    public static Task<T> RunAsync<T>(Func<T> work, Func<Exception, T> onError,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(work);
        ArgumentNullException.ThrowIfNull(onError);

        // RunContinuationsAsynchronously so awaiter continuations never run inline on this worker
        // thread (which is STA on Windows and dedicated to spooling).
        var tcs = new TaskCompletionSource<T>(TaskCreationOptions.RunContinuationsAsynchronously);

        if (cancellationToken.IsCancellationRequested)
        {
            tcs.SetCanceled(cancellationToken);
            return tcs.Task;
        }

        var thread = new Thread(() =>
        {
            try
            {
                cancellationToken.ThrowIfCancellationRequested();
                tcs.TrySetResult(work());
            }
            catch (OperationCanceledException oce)
            {
                tcs.TrySetCanceled(oce.CancellationToken);
            }
            catch (Exception ex)
            {
                // Map known failures to a result; if the mapping itself throws, fault the task so the
                // awaiter still completes rather than waiting forever.
                try
                {
                    tcs.TrySetResult(onError(ex));
                }
                catch (Exception mappingFailure)
                {
                    tcs.TrySetException(mappingFailure);
                }
            }
        })
        {
            IsBackground = true,
            Name = "WinPrint STA Worker",
        };

        // STA is required by WPF on Windows; SetApartmentState throws off-Windows, so guard it (the
        // off-Windows path only runs in cross-platform unit tests, where MTA is fine).
        if (OperatingSystem.IsWindows())
        {
            thread.SetApartmentState(ApartmentState.STA);
        }

        thread.Start();
        return tcs.Task;
    }
}

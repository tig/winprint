using WinPrint.Core.ContentTypeEngines;

namespace WinPrint.Core.UnitTests.TestSupport;

/// <summary>
///     An <see cref="IMermaidRenderer" /> whose render blocks until the test releases it — used to
///     hold a <c>RenderAsync</c> mid-await (as a slow mermaid.ink call would) so tests can prove that
///     painting concurrently with a re-render is safe.
/// </summary>
public sealed class GatedMermaidRenderer(byte[]? bytes) : IMermaidRenderer
{
    private readonly TaskCompletionSource _gate = new(TaskCreationOptions.RunContinuationsAsynchronously);
    private readonly TaskCompletionSource _entered = new(TaskCreationOptions.RunContinuationsAsynchronously);

    /// <summary>Completes when a render has entered and is blocked on the gate.</summary>
    public Task Entered => _entered.Task;

    public void Release()
    {
        _gate.TrySetResult();
    }

    public async Task<byte[]?> RenderAsync(string diagram)
    {
        _entered.TrySetResult();
        await _gate.Task;
        return bytes;
    }
}

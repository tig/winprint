using WinPrint.Core.ContentTypeEngines;

namespace WinPrint.Core.UnitTests.TestSupport;

/// <summary>
///     <see cref="IMermaidRenderer" /> double that awaits a real delay before returning, forcing
///     <c>MarkdownCte.RenderAsync</c> to yield mid-build the way a network-backed renderer does.
///     Concurrency tests use this to open the window in which a second render can start while the
///     first is still building.
/// </summary>
public sealed class DelayingMermaidRenderer(byte[]? bytes, int delayMs) : IMermaidRenderer
{
    public async Task<byte[]?> RenderAsync(string diagram)
    {
        await Task.Delay(delayMs);
        return bytes;
    }
}

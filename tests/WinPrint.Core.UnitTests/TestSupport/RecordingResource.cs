using WinPrint.Core.Abstractions;

namespace WinPrint.Core.UnitTests.TestSupport;

/// <summary>A no-op brush/pen used by <see cref="RecordingGraphicsContext" />.</summary>
internal sealed class RecordingResource : IGraphicsBrush, IGraphicsPen
{
    public void Dispose()
    {
    }
}

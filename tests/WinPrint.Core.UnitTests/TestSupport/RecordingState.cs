using WinPrint.Core.Abstractions;

namespace WinPrint.Core.UnitTests.TestSupport;

/// <summary>A no-op graphics state used by <see cref="RecordingGraphicsContext" />.</summary>
internal sealed class RecordingState : IGraphicsState
{
}

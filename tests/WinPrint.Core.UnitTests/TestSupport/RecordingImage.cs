using WinPrint.Core.Abstractions;

namespace WinPrint.Core.UnitTests.TestSupport;

/// <summary>
///     A platform-neutral <see cref="IGraphicsImage" /> stub returned by
///     <see cref="RecordingGraphicsContext.LoadImage" />. Carries deterministic intrinsic dimensions
///     so image layout (aspect-preserving fit) is exactly predictable in tests.
/// </summary>
public sealed class RecordingImage : IGraphicsImage
{
    public RecordingImage(float width, float height)
    {
        Width = width;
        Height = height;
    }

    public float Width { get; }
    public float Height { get; }

    public void Dispose()
    {
    }
}

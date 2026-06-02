using WinPrint.Core.Abstractions;

namespace WinPrint.Core.Printing.Skia;

/// <summary>
///     Captures the SkiaSharp canvas save-count so <see cref="SkiaGraphicsContext.Restore" /> can
///     unwind transforms and clips back to the matching <see cref="SkiaGraphicsContext.Save" />.
/// </summary>
internal sealed class SkiaState : IGraphicsState
{
    public SkiaState(int saveCount)
    {
        SaveCount = saveCount;
    }

    public int SaveCount { get; }
}

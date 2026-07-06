namespace WinPrint.Core.Abstractions;

/// <summary>
///     A decoded raster image bound to a specific <see cref="IGraphicsContext" /> backend, obtained
///     from <see cref="IGraphicsContext.LoadImage" /> and painted with
///     <see cref="IGraphicsContext.DrawImage" />. <see cref="Width" />/<see cref="Height" /> are the
///     image's intrinsic pixel dimensions, used by callers to compute an aspect-preserving draw size.
///     Instances own native resources and must be disposed.
/// </summary>
public interface IGraphicsImage : IDisposable
{
    /// <summary>Intrinsic pixel width of the decoded image.</summary>
    float Width { get; }

    /// <summary>Intrinsic pixel height of the decoded image.</summary>
    float Height { get; }
}

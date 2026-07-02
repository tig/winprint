using System.Drawing;
using WinPrint.Core.Abstractions;
using Xunit;

namespace WinPrint.Core.UnitTests.Abstractions;

/// <summary>
///     Windows-only tests for the GDI+ <see cref="SystemDrawingGraphicsContext" /> image path. They no-op
///     on non-Windows (GDI+/System.Drawing.Common P/Invokes <c>gdiplus</c>, absent on Linux/macOS) and
///     run for real on the windows-latest CI leg.
/// </summary>
public class SystemDrawingImageTests
{
    // A minimal valid 1x1 PNG.
    private const string OnePixelPngBase64 =
        "iVBORw0KGgoAAAANSUhEUgAAAAEAAAABCAYAAAAfFcSJAAAAC0lEQVR42mNgYAAAAAMAASsJTYQAAAAASUVORK5CYII=";

    [Fact]
    public void LoadImage_ReturnsImageThatSurvivesSourceStreamDisposal()
    {
        if (!OperatingSystem.IsWindows())
        {
            return; // GDI+ is Windows-only; verified on CI.
        }

        byte[] png = Convert.FromBase64String(OnePixelPngBase64);
        using var surface = new Bitmap(8, 8);
        using var graphics = Graphics.FromImage(surface);
        var ctx = new SystemDrawingGraphicsContext(graphics);

        IGraphicsImage? image;
        using (var stream = new MemoryStream(png))
        {
            image = ctx.LoadImage(stream);
        }

        // The source stream is now disposed. Image.FromStream keeps a reference to its stream, so a
        // non-cloned image would throw here when GDI+ lazily reads pixels; the clone must own its data.
        Assert.NotNull(image);
        Assert.True(image!.Width > 0);
        ctx.DrawImage(image, 0, 0, image.Width, image.Height);
        image.Dispose();
    }
}

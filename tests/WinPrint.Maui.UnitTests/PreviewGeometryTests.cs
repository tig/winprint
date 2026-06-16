using WinPrint.Maui.Views;
using Xunit;

namespace WinPrint.Maui.UnitTests;

/// <summary>
///     Unit tests for <see cref="PreviewGeometry" /> — the pure page-fit and pan-clamp math behind the
///     MAUI <see cref="PrintPreviewDrawable" /> preview. These run cross-platform (and in CI) because the
///     geometry is deliberately free of MAUI types; the source file is linked into this assembly.
/// </summary>
public class PreviewGeometryTests
{
    // US Letter portrait aspect (width ÷ height), in hundredths of an inch: 850 / 1100.
    private const float LetterAspect = 850f / 1100f;

    [Fact]
    public void ComputePageLayout_WideView_FitsToHeightAndCenters()
    {
        // View is wider (relative to the page) than the page aspect → the page is height-constrained.
        (float x, float y, float w, float h) = PreviewGeometry.ComputePageLayout(1000f, 800f, LetterAspect, 1f);

        Assert.Equal(800f * 0.9f, h, 3);              // height = 90% of view height
        Assert.Equal(h * LetterAspect, w, 3);         // width follows the aspect
        Assert.Equal((1000f - w) / 2f, x, 3);         // centered horizontally
        Assert.Equal((800f - h) / 2f, y, 3);          // centered vertically (zoom ≤ 1)
    }

    [Fact]
    public void ComputePageLayout_NarrowView_FitsToWidthAndCenters()
    {
        // View is narrower than the page aspect → the page is width-constrained.
        (float x, float y, float w, float h) = PreviewGeometry.ComputePageLayout(400f, 1000f, LetterAspect, 1f);

        Assert.Equal(400f * 0.9f, w, 3);              // width = 90% of view width
        Assert.Equal(w / LetterAspect, h, 3);         // height follows the aspect
        Assert.Equal((400f - w) / 2f, x, 3);
        Assert.Equal((1000f - h) / 2f, y, 3);
    }

    [Fact]
    public void ComputePageLayout_ZoomScalesPageLinearly()
    {
        (float _, float _, float w1, float h1) = PreviewGeometry.ComputePageLayout(1000f, 800f, LetterAspect, 1f);
        (float _, float _, float w2, float h2) = PreviewGeometry.ComputePageLayout(1000f, 800f, LetterAspect, 2f);

        Assert.Equal(w1 * 2f, w2, 3);
        Assert.Equal(h1 * 2f, h2, 3);
    }

    [Fact]
    public void ComputePageLayout_ZoomedIn_PinsToTopInsteadOfCentering()
    {
        (float _, float y, float _, float _) = PreviewGeometry.ComputePageLayout(1000f, 800f, LetterAspect, 2f);

        // At zoom > 1 the page is pinned near the top (2% inset), not vertically centered.
        Assert.Equal(800f * 0.02f, y, 3);
    }

    [Fact]
    public void ComputePageLayout_AtUnityZoom_CentersVertically()
    {
        (float _, float y, float _, float h) = PreviewGeometry.ComputePageLayout(1000f, 800f, LetterAspect, 1f);

        Assert.Equal((800f - h) / 2f, y, 3);
    }

    [Fact]
    public void ClampPanOffset_PageFitsWithinView_NoPanAllowed()
    {
        // Page (500) fits in the view (1000), centered at basePos = 250. Pan must be pinned to 0.
        float panned = PreviewGeometry.ClampPanOffset(desiredPan: 123f, basePos: 250f, pageExtent: 500f, viewExtent: 1000f);

        Assert.Equal(0f, panned, 3);
    }

    [Fact]
    public void ClampPanOffset_PageOverflows_ClampsToEdges()
    {
        // Page (1500) overflows the view (1000); centered base puts it at -250.
        // Allowed pan is [-250, +250]: +250 aligns the left edge, -250 aligns the right edge.
        const float basePos = -250f, pageExtent = 1500f, viewExtent = 1000f;

        Assert.Equal(250f, PreviewGeometry.ClampPanOffset(10_000f, basePos, pageExtent, viewExtent), 3);
        Assert.Equal(-250f, PreviewGeometry.ClampPanOffset(-10_000f, basePos, pageExtent, viewExtent), 3);
        Assert.Equal(100f, PreviewGeometry.ClampPanOffset(100f, basePos, pageExtent, viewExtent), 3); // within range, unchanged
    }

    [Fact]
    public void ClampPanOffset_PageOverflows_PinningLeftEdgeAlignsToZero()
    {
        // basePos + maxPan should land the page's leading edge at the view's leading edge (0).
        const float basePos = -250f;
        float maxPan = PreviewGeometry.ClampPanOffset(float.MaxValue, basePos, 1500f, 1000f);

        Assert.Equal(0f, basePos + maxPan, 3);
    }
}

namespace WinPrint.Maui.Views;

/// <summary>
///     Pure, dependency-free preview geometry for <see cref="PrintPreviewDrawable" />: fitting a page of
///     a given aspect ratio into the view at a zoom level, and clamping pan offsets to the page edges.
///     Deliberately free of MAUI types (<c>ICanvas</c>, <c>RectF</c>, view models) so it can be
///     unit-tested cross-platform — the MAUI app itself only builds for the maccatalyst/windows heads.
/// </summary>
internal static class PreviewGeometry
{
    /// <summary>
    ///     Computes the page rectangle (position + size, in view units) for a page of aspect ratio
    ///     <paramref name="pageAspect" /> (width ÷ height) fitted into a <paramref name="viewWidth" /> ×
    ///     <paramref name="viewHeight" /> view at <paramref name="zoom" />. The page fills 90% of the view
    ///     before zoom; it is centered at zoom ≤ 1 and pinned near the top (2% inset) at zoom &gt; 1.
    /// </summary>
    internal static (float X, float Y, float Width, float Height) ComputePageLayout(
        float viewWidth, float viewHeight, float pageAspect, float zoom)
    {
        float availWidth = viewWidth * 0.9f;
        float availHeight = viewHeight * 0.9f;

        float pageWidth, pageHeight;
        if (availWidth / availHeight > pageAspect)
        {
            pageHeight = availHeight * zoom;
            pageWidth = pageHeight * pageAspect;
        }
        else
        {
            pageWidth = availWidth * zoom;
            pageHeight = pageWidth / pageAspect;
        }

        float x = (viewWidth - pageWidth) / 2;
        float y = zoom <= 1.0f
            ? (viewHeight - pageHeight) / 2
            : viewHeight * 0.02f;

        return (x, y, pageWidth, pageHeight);
    }

    /// <summary>
    ///     Clamps a desired pan offset so the page edge never pans past the view edge on its overflowing
    ///     axis, and stays put (no pan) on an axis where the page fits.
    /// </summary>
    internal static float ClampPanOffset(float desiredPan, float basePos, float pageExtent, float viewExtent)
    {
        float lo = Math.Min(basePos, viewExtent - pageExtent) - basePos;
        float hi = Math.Max(basePos, 0f) - basePos;
        return Math.Clamp(desiredPan, lo, hi);
    }
}

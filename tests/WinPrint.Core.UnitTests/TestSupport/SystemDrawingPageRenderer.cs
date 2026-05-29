using System;
using System.Drawing;
using System.Drawing.Imaging;
using WinPrint.Core.Abstractions;
using WinPrint.Core.ContentTypeEngines;

namespace WinPrint.Core.UnitTests.TestSupport;

/// <summary>
///     Renders a Content Type Engine page to an in-memory <see cref="Bitmap" /> using the production
///     <see cref="SystemDrawingGraphicsContext" /> (real GDI+), mirroring the print-preview setup
///     (<see cref="GraphicsUnit.Display" />, i.e. 1/100"). This gives regression tests a faithful,
///     headless rendering of the real measurement/draw path — unlike <see cref="RecordingGraphicsContext" />,
///     which is fixed-pitch and cannot surface GDI+ measurement issues (e.g. per-token padding drift).
///
///     Because it uses System.Drawing, it only runs on Windows; on non-Windows hosts the GDI+ calls
///     throw (an environment limitation, consistent with the other System.Drawing-based tests).
/// </summary>
public static class SystemDrawingPageRenderer
{
    /// <summary>
    ///     Paints <paramref name="pageNum" /> of an already-reflowed <paramref name="cte" /> onto a white
    ///     bitmap of the given pixel size. The caller owns (and must dispose) the returned bitmap.
    /// </summary>
    public static Bitmap RenderPage (ContentTypeEngineBase cte, int pageNum, int pixelWidth, int pixelHeight,
        int dpi = 96)
    {
        var bitmap = new Bitmap (pixelWidth, pixelHeight);
        bitmap.SetResolution (dpi, dpi);
        using (Graphics g = Graphics.FromImage (bitmap))
        {
            g.Clear (Color.White);
            g.PageUnit = GraphicsUnit.Display; // 1/100" — same as the WinForms preview
            var context = new SystemDrawingGraphicsContext (g);
            cte.PaintPage (context, pageNum);
        }

        return bitmap;
    }

    /// <summary>
    ///     Returns the x of the right-most "inked" (non-white) pixel in the bitmap, or -1 if the bitmap is
    ///     blank. Useful for asserting that drawn content does not drift wider than expected.
    /// </summary>
    public static int RightmostInkColumn (Bitmap bitmap, int whiteThreshold = 750)
    {
        for (int x = bitmap.Width - 1; x >= 0; x--)
        {
            for (int y = 0; y < bitmap.Height; y++)
            {
                Color c = bitmap.GetPixel (x, y);
                if (c.R + c.G + c.B < whiteThreshold)
                {
                    return x;
                }
            }
        }

        return -1;
    }

    /// <summary>Saves the bitmap as a PNG (handy for eyeballing a failing regression locally).</summary>
    public static void SavePng (Bitmap bitmap, string path)
    {
        if (bitmap is null)
        {
            throw new ArgumentNullException (nameof (bitmap));
        }

        bitmap.Save (path, ImageFormat.Png);
    }
}

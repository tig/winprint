// Copyright Kindel, LLC - http://www.kindel.com
// Published under the MIT License at https://github.com/tig/winprint

using System.Net.Http;
using System.Runtime.InteropServices;
using System.Text;
using Serilog;
using TheArtOfDev.HtmlRenderer.Adapters.Entities;
using TheArtOfDev.HtmlRenderer.Core;
using TheArtOfDev.HtmlRenderer.Core.Entities;
using WinPrint.Core.Abstractions;
using WinPrint.Core.ContentTypeEngines.Html;
using WinPrint.Core.Models;
using WinPrint.Core.Services;

namespace WinPrint.Core.ContentTypeEngines;

/// <summary>
///     Implements <c>text/html</c> by laying out and painting HTML/CSS through the pure-managed
///     HtmlRenderer engine, rendered onto WinPrint's cross-platform <see cref="IGraphicsContext" /> via
///     <see cref="WinPrintHtmlAdapter" />. Images (local files relative to
///     <see cref="ContentTypeEngineBase.SourceFileName" />, <c>data:</c> URIs, and <c>http(s)</c> URLs)
///     are resolved through the engine's image-load hook. Pagination splits the laid-out document by
///     <c>PageSize.Height</c>.
/// </summary>
public class HtmlCte : ContentTypeEngineBase, IDisposable
{
    private static readonly string[] s_supportedContentTypes = ["text/html"];
    private static readonly HttpClient s_http = new() { Timeout = TimeSpan.FromSeconds(10) };

    private MhtmlArchive? _archive;
    private bool _disposed;
    private int _dpiY = 96;
    private int _pageCount;

    public override string[] SupportedContentTypes => s_supportedContentTypes;

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    public static HtmlCte Create()
    {
        var content = new HtmlCte();
        content.CopyPropertiesFrom(ModelLocator.Current.Settings.HtmlContentTypeEngineSettings);
        return content;
    }

    private void Dispose(bool disposing)
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;
    }

    public override async Task<bool> SetDocumentAsync(string doc)
    {
        Document = doc;
        // .mhtml/.mht web archives are MIME multipart, not HTML — unpack the root HTML part and its
        // embedded resources so they render offline.
        _archive = doc is not null && MhtmlArchive.LooksLikeMhtml(doc) ? MhtmlArchive.Parse(doc) : null;
        return await Task.FromResult(true);
    }

    public override async Task<int> RenderAsync(PrintResolution? printerResolution,
        EventHandler<string>? reflowProgress)
    {
        LogService.TraceMessage();
        if (Document is null)
        {
            throw new InvalidOperationException("Document can't be null for RenderAsync");
        }

        ArgumentNullException.ThrowIfNull(printerResolution);
        int dpiX = printerResolution.X;
        int dpiY = printerResolution.Y;
        if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows) || dpiX < 0 || dpiY < 0)
        {
            dpiX = dpiY = 96;
        }

        _dpiY = dpiY;
        if (PageSize.Height < 1f)
        {
            throw new InvalidOperationException($"Page height ({PageSize.Height:F2}) is too small.");
        }

        IGraphicsContext g = ResolveMeasurementContext(dpiX, dpiY, out IDisposable? owner);
        try
        {
            (HtmlContainerInt container, _, double height) = LayoutHtml(g, dpiY);
            container.Dispose();
            _pageCount = height <= 0 ? 1 : Math.Max(1, (int)Math.Ceiling(height / PageSize.Height));
            Log.Debug("Rendered {pages} HTML pages from {height:F0}px of content.", _pageCount, height);
            return await Task.FromResult(_pageCount);
        }
        finally
        {
            owner?.Dispose();
        }
    }

    public override void PaintPage(IGraphicsContext g, int pageNum)
    {
        LogService.TraceMessage($"{pageNum}");
        if (Document is null)
        {
            return;
        }

        g.SetTextRenderingMode(GraphicsTextRenderingMode);

        // Re-layout against the paint context so fonts and decoded images bind to the paint backend.
        (HtmlContainerInt container, WinPrintHtmlAdapter adapter, _) = LayoutHtml(g, _dpiY);
        try
        {
            // Negative scrolls the laid-out content up so page N's slice maps to the top of the surface.
            container.ScrollOffset = new RPoint(0, -((pageNum - 1) * PageSize.Height));
            var clip = RRect.FromLTRB(0, 0, PageSize.Width, PageSize.Height);
            using var gfx = new WinPrintHtmlGraphics(adapter, g, clip);
            gfx.PushClip(clip);
            container.PerformPaint(gfx);
            gfx.PopClip();
        }
        finally
        {
            container.Dispose();
        }
    }

    private (HtmlContainerInt container, WinPrintHtmlAdapter adapter, double height) LayoutHtml(IGraphicsContext g,
        int dpiY)
    {
        var adapter = new WinPrintHtmlAdapter { Graphics = g, DpiY = dpiY };
        var container = new HtmlContainerInt(adapter)
        {
            AvoidAsyncImagesLoading = true,
            AvoidImagesLateLoading = true,
            MaxSize = new RSize(PageSize.Width, 0)
        };
        container.ImageLoad += (_, e) => OnImageLoad(adapter, e);
        container.StylesheetLoad += (_, e) => OnStylesheetLoad(e);
        container.RenderError += (_, e) => Log.Warning("HtmlCte render error: {type} {message}", e.Type, e.Message);
        container.SetHtml(_archive?.Html ?? Document ?? string.Empty);

        var layoutGfx = new WinPrintHtmlGraphics(adapter, g, RRect.FromLTRB(0, 0, PageSize.Width, 1_000_000));
        container.PerformLayout(layoutGfx);
        layoutGfx.Dispose();
        return (container, adapter, container.ActualSize.Height);
    }

    private void OnImageLoad(WinPrintHtmlAdapter adapter, HtmlImageLoadEventArgs e)
    {
        e.Handled = true;
        byte[]? bytes = _archive?.Resolve(e.Src) ?? LoadResourceBytes(e.Src);
        if (bytes is not { Length: > 0 })
        {
            return;
        }

        using var ms = new MemoryStream(bytes);
        IGraphicsImage? image = adapter.Graphics.LoadImage(ms);
        if (image is not null)
        {
            e.Callback(new WinPrintHtmlImage(image));
        }
    }

    private void OnStylesheetLoad(HtmlStylesheetLoadEventArgs e)
    {
        // Resolve external stylesheets (<link rel="stylesheet">) from the MHTML archive if present,
        // else the same way as images: local files relative to SourceFileName, data: URIs, and http(s).
        byte[]? bytes = _archive?.Resolve(e.Src) ?? LoadResourceBytes(e.Src);
        if (bytes is { Length: > 0 })
        {
            e.SetStyleSheet = Encoding.UTF8.GetString(bytes);
        }
    }

    private byte[]? LoadResourceBytes(string? url)
    {
        if (string.IsNullOrWhiteSpace(url))
        {
            return null;
        }

        byte[]? dataUri = TryDecodeDataUri(url);
        if (dataUri is not null)
        {
            return dataUri;
        }

        try
        {
            if (url.StartsWith("http://", StringComparison.OrdinalIgnoreCase) ||
                url.StartsWith("https://", StringComparison.OrdinalIgnoreCase))
            {
                return s_http.GetByteArrayAsync(url).GetAwaiter().GetResult();
            }

            string path = ResolveLocalPath(url);
            return File.Exists(path) ? File.ReadAllBytes(path) : null;
        }
        catch (Exception ex)
        {
            Log.Debug(ex, "HtmlCte: failed to load image {url}", url);
            return null;
        }
    }

    private string ResolveLocalPath(string url)
    {
        string path = url;
        if (url.StartsWith("file://", StringComparison.OrdinalIgnoreCase) &&
            Uri.TryCreate(url, UriKind.Absolute, out Uri? fileUri))
        {
            return fileUri.LocalPath;
        }

        path = Uri.UnescapeDataString(path);
        if (Path.IsPathRooted(path))
        {
            return path;
        }

        string? dir = string.IsNullOrEmpty(SourceFileName) ? null : Path.GetDirectoryName(SourceFileName);
        return string.IsNullOrEmpty(dir) ? path : Path.Combine(dir, path);
    }

    private static byte[]? TryDecodeDataUri(string url)
    {
        if (!url.StartsWith("data:", StringComparison.OrdinalIgnoreCase))
        {
            return null;
        }

        int comma = url.IndexOf(',');
        if (comma < 0)
        {
            return null;
        }

        string meta = url[5..comma];
        string data = url[(comma + 1)..];
        try
        {
            return meta.Contains("base64", StringComparison.OrdinalIgnoreCase)
                ? Convert.FromBase64String(data)
                : Encoding.UTF8.GetBytes(Uri.UnescapeDataString(data));
        }
        catch (Exception)
        {
            return null;
        }
    }
}

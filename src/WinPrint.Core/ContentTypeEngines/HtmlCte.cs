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
///     Implements <c>text/html</c> (and <c>.mhtml</c>/<c>.mht</c> web archives) by laying out and painting
///     HTML/CSS through the pure-managed HtmlRenderer engine, rendered onto WinPrint's cross-platform
///     <see cref="IGraphicsContext" /> via <see cref="WinPrintHtmlAdapter" />. The document is laid out once
///     (in <see cref="RenderAsync" />) and that layout is reused for every page; images are decoded per
///     paint backend on demand. Local files (relative to <see cref="ContentTypeEngineBase.SourceFileName" />)
///     and <c>data:</c> URIs always load; <c>http(s)</c> resources require <see cref="AllowRemoteResources" />.
/// </summary>
public class HtmlCte : ContentTypeEngineBase, IDisposable
{
    private static readonly string[] s_supportedContentTypes = ["text/html"];
    private static readonly HttpClient s_http = new() { Timeout = TimeSpan.FromSeconds(10) };

    private readonly Dictionary<string, byte[]?> _resourceCache = new(StringComparer.Ordinal);
    private WinPrintHtmlAdapter? _adapter;
    private MhtmlArchive? _archive;
    private HtmlContainerInt? _container;
    private bool _disposed;
    private int _dpiY = 96;
    private int _pageCount;

    /// <summary>
    ///     When false (the default), <c>http(s)</c> resources referenced by the HTML are not fetched —
    ///     avoiding SSRF-style outbound requests when printing untrusted documents. Local (document-relative)
    ///     files, <c>data:</c> URIs, and MHTML-embedded resources always load.
    /// </summary>
    public bool AllowRemoteResources { get; set; }

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

    public override void CopyPropertiesFrom(ModelBase? source)
    {
        base.CopyPropertiesFrom(source);
        if (source is HtmlCte src)
        {
            AllowRemoteResources = src.AllowRemoteResources;
        }
    }

    private void Dispose(bool disposing)
    {
        if (_disposed)
        {
            return;
        }

        if (disposing)
        {
            _container?.Dispose();
            _container = null;
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

        _resourceCache.Clear();
        _container?.Dispose();

        // Lay the document out exactly once; PaintPage reuses this layout for every page.
        IGraphicsContext g = ResolveMeasurementContext(dpiX, dpiY, out IDisposable? owner);
        try
        {
            _adapter = new WinPrintHtmlAdapter { Graphics = g, DpiY = dpiY };
            _container = new HtmlContainerInt(_adapter)
            {
                AvoidAsyncImagesLoading = true,
                AvoidImagesLateLoading = true,
                MaxSize = new RSize(PageSize.Width, 0)
            };
            _container.ImageLoad += (_, e) => OnImageLoad(e);
            _container.StylesheetLoad += (_, e) => OnStylesheetLoad(e);
            _container.RenderError += (_, e) =>
                Log.Warning("HtmlCte render error: {type} {message}", e.Type, e.Message);
            _container.SetHtml(_archive?.Html ?? Document ?? string.Empty);

            using (var layoutGfx = new WinPrintHtmlGraphics(_adapter, g, RRect.FromLTRB(0, 0, PageSize.Width, 1e6)))
            {
                _container.PerformLayout(layoutGfx);
            }

            double height = _container.ActualSize.Height;
            _pageCount = height <= 0 ? 1 : Math.Max(1, (int)Math.Ceiling(height / PageSize.Height));
            Log.Debug("Rendered {pages} HTML pages from {height:F0} (1/100\") of content.", _pageCount, height);
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
        if (_container is null || _adapter is null)
        {
            return;
        }

        g.SetTextRenderingMode(GraphicsTextRenderingMode);

        // Reuse the single layout; only the paint surface and scroll position change per page.
        _adapter.Graphics = g;
        _adapter.DpiY = _dpiY;
        // Negative scrolls the laid-out content up so page N's slice maps to the top of the surface.
        _container.ScrollOffset = new RPoint(0, -((pageNum - 1) * PageSize.Height));

        var clip = RRect.FromLTRB(0, 0, PageSize.Width, PageSize.Height);
        using var gfx = new WinPrintHtmlGraphics(_adapter, g, clip);
        _container.PerformPaint(gfx);
    }

    private void OnImageLoad(HtmlImageLoadEventArgs e)
    {
        e.Handled = true;
        byte[]? bytes = ResolveResource(e.Src);
        WinPrintHtmlImage? image = bytes is { Length: > 0 } ? _adapter?.CreateImage(bytes) : null;
        if (image is not null)
        {
            e.Callback(image);
        }
    }

    private void OnStylesheetLoad(HtmlStylesheetLoadEventArgs e)
    {
        // Resolve external stylesheets (<link rel="stylesheet">) the same way as images.
        byte[]? bytes = ResolveResource(e.Src);
        if (bytes is { Length: > 0 })
        {
            e.SetStyleSheet = Encoding.UTF8.GetString(bytes);
        }
    }

    private byte[]? ResolveResource(string? src)
    {
        // MHTML archive first, then a per-render cache so resources load at most once across pages.
        byte[]? fromArchive = _archive?.Resolve(src);
        if (fromArchive is not null)
        {
            return fromArchive;
        }

        if (string.IsNullOrEmpty(src))
        {
            return null;
        }

        if (!_resourceCache.TryGetValue(src, out byte[]? bytes))
        {
            bytes = LoadResourceBytes(src);
            _resourceCache[src] = bytes;
        }

        return bytes;
    }

    private byte[]? LoadResourceBytes(string url)
    {
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
                if (!AllowRemoteResources)
                {
                    Log.Debug("HtmlCte: skipping remote resource {url} (AllowRemoteResources is false).", url);
                    return null;
                }

                return s_http.GetByteArrayAsync(url).ConfigureAwait(false).GetAwaiter().GetResult();
            }

            string path = ResolveLocalPath(url);
            return File.Exists(path) ? File.ReadAllBytes(path) : null;
        }
        catch (Exception ex)
        {
            Log.Debug(ex, "HtmlCte: failed to load resource {url}", url);
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

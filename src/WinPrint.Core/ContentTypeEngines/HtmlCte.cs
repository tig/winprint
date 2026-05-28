using System;
#if WINDOWS
using System.Drawing.Printing;
#endif
using System.Threading.Tasks;
using WinPrint.Core.Abstractions;
using WinPrint.Core.Models;
using WinPrint.Core.Services;

namespace WinPrint.Core.ContentTypeEngines;

/// <summary>
///     Implements generic HTML file type support. Previously used LiteHtml for rendering.
///     The LiteHtmlSharp and WinPrint.LiteHtml dependencies have been removed; this class is a stub
///     that can be re-implemented when the dependencies are restored.
/// </summary>
public class HtmlCte : ContentTypeEngineBase, IDisposable
{
    private static readonly string[] _supportedContentTypes = ["text/html"];

    private bool _disposed;

    public override string[] SupportedContentTypes => _supportedContentTypes;

    public void Dispose ()
    {
        Dispose (true);
        GC.SuppressFinalize (this);
    }

    public static HtmlCte Create ()
    {
        var content = new HtmlCte ();
        content.CopyPropertiesFrom (ModelLocator.Current.Settings.HtmlContentTypeEngineSettings);
        return content;
    }

    private void Dispose (bool disposing)
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;
    }

    public override async Task<bool> SetDocumentAsync (string doc)
    {
        Document = doc;
        return await Task.FromResult (true);
    }

    public override async Task<int> RenderAsync (PrintResolution? printerResolution,
        EventHandler<string>? reflowProgress)
    {
        LogService.TraceMessage ("HtmlCte is a stub - LiteHtmlSharp dependency removed.");
        return await Task.FromResult (0);
    }

    public override void PaintPage (IGraphicsContext g, int pageNum)
    {
        // Stub: LiteHtmlSharp dependency removed
    }
}

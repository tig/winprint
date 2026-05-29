// Copyright Kindel, LLC - http://www.kindel.com
// Published under the MIT License at https://github.com/tig/winprint

#if WINDOWS
using System.Drawing.Printing;
#endif
using WinPrint.Core.Abstractions;
using WinPrint.Core.Models;
using WinPrint.Core.Services;

namespace WinPrint.Core.ContentTypeEngines;

/// <summary>
///     Implements text/plain and text/ansi file type support. Previously used libvt100 to parse/render
///     ANSI formatted text. The libvt100 submodule has been removed; this class is a stub that can be
///     re-implemented when the dependency is restored.
///     NOTE: https://invisible-island.net/xterm/ctlseqs/ctlseqs.html
/// </summary>
public class AnsiCte : ContentTypeEngineBase, IDisposable
{
    private static readonly string[] s_supportedContentTypes = ["text/plain", "text/ansi"];

    private bool _disposed;

    public override string[] SupportedContentTypes => s_supportedContentTypes;

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    public static AnsiCte Create()
    {
        var engine = new AnsiCte();
        engine.CopyPropertiesFrom(ModelLocator.Current.Settings.AnsiContentTypeEngineSettings);
        return engine;
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
        return await Task.FromResult(true);
    }

    public override async Task<int> RenderAsync(PrintResolution? printerResolution,
        EventHandler<string>? reflowProgress)
    {
        LogService.TraceMessage("AnsiCte is a stub - libvt100 submodule removed.");
        return await Task.FromResult(0);
    }

    public override void PaintPage(IGraphicsContext g, int pageNum)
    {
        // Stub: libvt100 submodule removed
    }
}

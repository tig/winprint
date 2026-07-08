// Copyright Kindel, LLC - http://www.kindel.com
// Published under the MIT License at https://github.com/tig/winprint

using System.Net.Http;
using System.Text;
using Serilog;

namespace WinPrint.Core.ContentTypeEngines;

/// <summary>
///     The default <see cref="IMermaidRenderer" />: renders diagrams to PNG via a
///     mermaid.ink-compatible HTTP service (<c>GET {service}/img/{url-safe-base64-of-source}?type=png</c>,
///     the encoding documented at https://mermaid.js.org/ecosystem/tutorials.html). Note the diagram
///     source is sent to the service; <see cref="MarkdownCte" /> only uses this renderer when
///     <see cref="MarkdownCte.RenderMermaidDiagrams" /> is explicitly enabled. Returns null on any
///     failure so the caller can fall back to rendering the source as a code block.
/// </summary>
public sealed class MermaidInkRenderer : IMermaidRenderer
{
    private static readonly HttpClient s_http = new() { Timeout = TimeSpan.FromSeconds(10) };
    private readonly string _serviceUrl;

    public MermaidInkRenderer(string serviceUrl)
    {
        _serviceUrl = serviceUrl.TrimEnd('/');
    }

    public async Task<byte[]?> RenderAsync(string diagram)
    {
        string encoded = Convert.ToBase64String(Encoding.UTF8.GetBytes(diagram))
            .Replace('+', '-')
            .Replace('/', '_');
        try
        {
            return await s_http.GetByteArrayAsync($"{_serviceUrl}/img/{encoded}?type=png");
        }
        catch (Exception ex)
        {
            Log.Debug(ex, "MermaidInkRenderer: failed to render diagram via {service}", _serviceUrl);
            return null;
        }
    }
}

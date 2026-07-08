// Copyright Kindel, LLC - http://www.kindel.com
// Published under the MIT License at https://github.com/tig/winprint

using System.Collections.Concurrent;
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
///     Successful renders are memoized process-wide (keyed by service + source): the same diagram is
///     re-rendered on every reflow (sheet switch, font change — the engine's image cache is
///     per-render), and without the memo each reflow repeats the network round-trip, stalling the
///     preview and inviting service rate limiting. Failures are not memoized, so a diagram that
///     failed once (offline, throttled) is retried on the next reflow.
/// </summary>
public sealed class MermaidInkRenderer : IMermaidRenderer
{
    private const int MemoCapacity = 100;
    private static readonly HttpClient s_http = new() { Timeout = TimeSpan.FromSeconds(10) };
    private static readonly ConcurrentDictionary<string, byte[]> s_memo = new(StringComparer.Ordinal);
    private readonly string _serviceUrl;

    public MermaidInkRenderer(string serviceUrl)
    {
        _serviceUrl = serviceUrl.TrimEnd('/');
    }

    public async Task<byte[]?> RenderAsync(string diagram)
    {
        string memoKey = $"{_serviceUrl}\n{diagram}";
        if (s_memo.TryGetValue(memoKey, out byte[]? memoized))
        {
            return memoized;
        }

        string encoded = Convert.ToBase64String(Encoding.UTF8.GetBytes(diagram))
            .Replace('+', '-')
            .Replace('/', '_');
        try
        {
            byte[] bytes = await s_http.GetByteArrayAsync($"{_serviceUrl}/img/{encoded}?type=png");
            if (s_memo.Count < MemoCapacity)
            {
                s_memo[memoKey] = bytes;
            }

            return bytes;
        }
        catch (Exception ex)
        {
            Log.Debug(ex, "MermaidInkRenderer: failed to render diagram via {service}", _serviceUrl);
            return null;
        }
    }
}

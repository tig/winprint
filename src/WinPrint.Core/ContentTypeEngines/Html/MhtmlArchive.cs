// Copyright Kindel, LLC - http://www.kindel.com
// Published under the MIT License at https://github.com/tig/winprint

using System.Text;

namespace WinPrint.Core.ContentTypeEngines.Html;

/// <summary>
///     A minimal MHTML ("MIME HTML" / saved-web-archive, <c>.mhtml</c>/<c>.mht</c>) reader. Parses the
///     multipart/related MIME container into its root <see cref="Html" /> document and a map of embedded
///     <see cref="Resources" /> (keyed by both <c>Content-Location</c> URL and <c>cid:&lt;Content-ID&gt;</c>),
///     so images and stylesheets render offline from the archive instead of the network.
/// </summary>
internal sealed class MhtmlArchive
{
    private MhtmlArchive(string html, Dictionary<string, byte[]> resources)
    {
        Html = html;
        Resources = resources;
    }

    public string Html { get; }

    public IReadOnlyDictionary<string, byte[]> Resources { get; }

    /// <summary>Cheap heuristic: does this document look like an MHTML MIME archive?</summary>
    public static bool LooksLikeMhtml(string doc)
    {
        if (string.IsNullOrEmpty(doc))
        {
            return false;
        }

        string head = doc.Length > 4000 ? doc[..4000] : doc;
        return head.Contains("MIME-Version:", StringComparison.OrdinalIgnoreCase) &&
               head.Contains("multipart/related", StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>Parses the archive, or returns null if it isn't valid MHTML / has no HTML part.</summary>
    public static MhtmlArchive? Parse(string doc)
    {
        (Dictionary<string, string> topHeaders, string topBody) = SplitHeadersBody(doc);
        string? boundary = ExtractBoundary(topHeaders.GetValueOrDefault("content-type", string.Empty));
        if (boundary is null)
        {
            return null;
        }

        string? html = null;
        var resources = new Dictionary<string, byte[]>(StringComparer.OrdinalIgnoreCase);

        foreach (string rawPart in SplitParts(topBody, boundary))
        {
            (Dictionary<string, string> headers, string body) = SplitHeadersBody(rawPart);
            if (headers.Count == 0 && body.Length == 0)
            {
                continue;
            }

            string contentType = headers.GetValueOrDefault("content-type", string.Empty);
            string encoding = headers.GetValueOrDefault("content-transfer-encoding", string.Empty);
            byte[] bytes = Decode(body, encoding);

            if (html is null && contentType.Contains("text/html", StringComparison.OrdinalIgnoreCase))
            {
                html = GetString(bytes, contentType);
            }

            string location = headers.GetValueOrDefault("content-location", string.Empty).Trim();
            if (location.Length > 0)
            {
                resources[location] = bytes;
            }

            string contentId = headers.GetValueOrDefault("content-id", string.Empty).Trim().Trim('<', '>');
            if (contentId.Length > 0)
            {
                resources["cid:" + contentId] = bytes;
            }
        }

        return html is null ? null : new MhtmlArchive(html, resources);
    }

    /// <summary>Resolves a resource by an HTML reference (a <c>cid:</c> URI or a Content-Location URL).</summary>
    public byte[]? Resolve(string? src)
    {
        if (string.IsNullOrEmpty(src))
        {
            return null;
        }

        if (Resources.TryGetValue(src, out byte[]? exact))
        {
            return exact;
        }

        if (src.StartsWith("cid:", StringComparison.OrdinalIgnoreCase) &&
            Resources.TryGetValue("cid:" + src[4..].Trim().Trim('<', '>'), out byte[]? byCid))
        {
            return byCid;
        }

        return null;
    }

    private static (Dictionary<string, string> Headers, string Body) SplitHeadersBody(string text)
    {
        var headers = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        // Headers are separated from the body by the first blank line.
        int split = IndexOfBlankLine(text, out int bodyStart);
        string headerBlock = split < 0 ? text : text[..split];
        string body = split < 0 ? string.Empty : text[bodyStart..];

        string? name = null;
        var value = new StringBuilder();
        foreach (string rawLine in headerBlock.Split('\n'))
        {
            string line = rawLine.TrimEnd('\r');
            if (line.Length == 0)
            {
                continue;
            }

            if ((line[0] == ' ' || line[0] == '\t') && name is not null)
            {
                value.Append(' ').Append(line.Trim()); // folded continuation
                continue;
            }

            int colon = line.IndexOf(':');
            if (colon <= 0)
            {
                continue;
            }

            if (name is not null)
            {
                headers[name] = value.ToString().Trim();
            }

            name = line[..colon].Trim().ToLowerInvariant();
            value.Clear();
            value.Append(line[(colon + 1)..]);
        }

        if (name is not null)
        {
            headers[name] = value.ToString().Trim();
        }

        return (headers, body);
    }

    private static int IndexOfBlankLine(string text, out int bodyStart)
    {
        int crlf = text.IndexOf("\r\n\r\n", StringComparison.Ordinal);
        int lf = text.IndexOf("\n\n", StringComparison.Ordinal);
        if (crlf >= 0 && (lf < 0 || crlf <= lf))
        {
            bodyStart = crlf + 4;
            return crlf;
        }

        if (lf >= 0)
        {
            bodyStart = lf + 2;
            return lf;
        }

        bodyStart = text.Length;
        return -1;
    }

    private static IEnumerable<string> SplitParts(string body, string boundary)
    {
        string delimiter = "--" + boundary;
        string[] segments = body.Split(delimiter);
        // segments[0] is the preamble; the final segment is the "--" close or epilogue.
        for (int i = 1; i < segments.Length; i++)
        {
            string seg = segments[i];
            if (seg.StartsWith("--", StringComparison.Ordinal))
            {
                yield break; // closing boundary reached
            }

            yield return seg.TrimStart('\r', '\n');
        }
    }

    private static string? ExtractBoundary(string contentType)
    {
        int idx = contentType.IndexOf("boundary", StringComparison.OrdinalIgnoreCase);
        if (idx < 0)
        {
            return null;
        }

        int eq = contentType.IndexOf('=', idx);
        if (eq < 0)
        {
            return null;
        }

        string rest = contentType[(eq + 1)..].Trim();
        if (rest.StartsWith('"'))
        {
            int end = rest.IndexOf('"', 1);
            return end > 0 ? rest[1..end] : null;
        }

        int stop = rest.IndexOfAny([';', '\r', '\n', ' ']);
        return stop > 0 ? rest[..stop] : rest;
    }

    private static byte[] Decode(string body, string encoding)
    {
        if (encoding.Contains("base64", StringComparison.OrdinalIgnoreCase))
        {
            try
            {
                return Convert.FromBase64String(RemoveWhitespace(body));
            }
            catch (Exception)
            {
                return [];
            }
        }

        if (encoding.Contains("quoted-printable", StringComparison.OrdinalIgnoreCase))
        {
            return DecodeQuotedPrintable(body);
        }

        return Encoding.UTF8.GetBytes(body); // 7bit/8bit/binary
    }

    private static byte[] DecodeQuotedPrintable(string body)
    {
        var bytes = new List<byte>(body.Length);
        for (int i = 0; i < body.Length; i++)
        {
            char c = body[i];
            if (c == '=' && i + 1 < body.Length)
            {
                char n1 = body[i + 1];
                if (n1 == '\r' || n1 == '\n')
                {
                    // Soft line break: drop "=", CR?, LF.
                    i += n1 == '\r' && i + 2 < body.Length && body[i + 2] == '\n' ? 2 : 1;
                    continue;
                }

                if (i + 2 < body.Length && Uri.IsHexDigit(n1) && Uri.IsHexDigit(body[i + 2]))
                {
                    bytes.Add((byte)((Uri.FromHex(n1) << 4) | Uri.FromHex(body[i + 2])));
                    i += 2;
                    continue;
                }
            }

            bytes.Add((byte)c);
        }

        return [.. bytes];
    }

    private static string RemoveWhitespace(string s)
    {
        var sb = new StringBuilder(s.Length);
        foreach (char c in s)
        {
            if (!char.IsWhiteSpace(c))
            {
                sb.Append(c);
            }
        }

        return sb.ToString();
    }

    private static string GetString(byte[] bytes, string contentType)
    {
        Encoding encoding = Encoding.UTF8;
        int idx = contentType.IndexOf("charset", StringComparison.OrdinalIgnoreCase);
        if (idx >= 0)
        {
            int eq = contentType.IndexOf('=', idx);
            if (eq >= 0)
            {
                string charset = contentType[(eq + 1)..].Trim().Trim('"');
                int stop = charset.IndexOfAny([';', ' ']);
                if (stop > 0)
                {
                    charset = charset[..stop];
                }

                try
                {
                    encoding = Encoding.GetEncoding(charset);
                }
                catch (Exception)
                {
                    encoding = Encoding.UTF8;
                }
            }
        }

        return encoding.GetString(bytes);
    }
}

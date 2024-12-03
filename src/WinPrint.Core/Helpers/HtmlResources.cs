using System;
using System.IO;
using WinPrint.Core.Services;

namespace WinPrint.LiteHtml;

public class HtmlResources(string filePath) {
    //private HttpClient _httpClient = null;
    private readonly string? _lastUrl = null;

    public byte[] GetResourceBytes(string resource) {
        LogService.TraceMessage($"{resource}");
        var data = Array.Empty<byte>();
        if (string.IsNullOrWhiteSpace(resource)) {
            return data;
        }

        try {
            data = File.ReadAllBytes($"{Path.GetDirectoryName(filePath)}\\{resource}");
        }
        catch (Exception e) {
            LogService.TraceMessage($"GetResourceBytes({resource}) - {e.Message}");
        }

        return data;
    }

    public string GetResourceString(string resource) {
        LogService.TraceMessage($"{resource}");
        var data = string.Empty;
        if (string.IsNullOrWhiteSpace(resource)) {
            return data;
        }

        try {
            if (resource.StartsWith("file:")) {
                var urlBuilder = new UriBuilder(resource);
                using var reader = new StreamReader(urlBuilder.Path);
                data = reader.ReadToEnd();
            }

            // TODO: Implement loading external html resources
            //var url = GetUrlForRequest(resource);
            //data = _httpClient.GetStringAsync(url).Result;
            return data;
        }
        catch (Exception e) {
            LogService.TraceMessage($"GetResourceString({resource}) - {e.Message}");
            return data;
        }
    }
}

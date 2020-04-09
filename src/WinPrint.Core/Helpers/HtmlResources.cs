using System;
using System.IO;
using System.Net.Http;
using WinPrint.Core.Services;

namespace WinPrint.LiteHtml {
    public class HtmlResources {
        //private HttpClient _httpClient = null;
        private string _lastUrl = null;
        private string filePath;

        public HtmlResources(string filePath) {
            this.filePath = filePath;
        }

        public byte[] GetResourceBytes(string resource) {
            LogService.TraceMessage($"{resource}");
            var data = new byte[0];
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
                else {
                    // TODO: Implement loading external html resources
                    //var url = GetUrlForRequest(resource);
                    //data = _httpClient.GetStringAsync(url).Result;
                }
                return data;
            }
            catch (Exception e) {
                LogService.TraceMessage($"GetResourceString({resource}) - {e.Message}");
                return data;
            }
        }

        private string GetUrlForRequest(string resource) {
            LogService.TraceMessage($"{resource}");

            try {
                UriBuilder urlBuilder;

                if (resource.StartsWith("file:")) {
                    urlBuilder = new UriBuilder {
                        Scheme = "file",
                        Host = "",
                        Path = resource
                    };
                }
                else if (resource.StartsWith("//") || resource.StartsWith("http:") || resource.StartsWith("https:")) {
                    urlBuilder = new UriBuilder(resource.TrimStart(new char[] { '/' }));
                }
                else {
                    urlBuilder = new UriBuilder(_lastUrl) {
                        Path = resource
                    };
                }
                var requestUrl = urlBuilder.ToString();
                return requestUrl;
            }
            catch {
                LogService.TraceMessage($"GetUrlForReqeust({resource}) returning null.");
                return null;
            }
        }

    }
}

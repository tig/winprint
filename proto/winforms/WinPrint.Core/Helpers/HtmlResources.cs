using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Text;
using WinPrint.Core;

namespace WinPrint.LiteHtml {
    public class HtmlResources {
        private HttpClient _httpClient;
        private string _lastUrl;
        private string filePath;

        public HtmlResources(string filePath) {
            this.filePath = filePath;
        }

        public byte[] GetResourceBytes(string resource) {
            byte[] data = new byte[0];
            if (string.IsNullOrWhiteSpace(resource)) {
                return data;
            }

            try {
                data = File.ReadAllBytes($"{Path.GetDirectoryName(filePath)}\\{resource}");
            }
            catch (Exception e) {
                Core.Helpers.Logging.TraceMessage($"GetResourceBytes({resource}) - {e.Message}");
            }
            return data;
        }

        public string GetResourceString(string resource) {
            string data = string.Empty;
            if (string.IsNullOrWhiteSpace(resource)) {
                return data;
            }
            try {
                if (resource.StartsWith("file:")) {
                    UriBuilder urlBuilder = new UriBuilder(resource);
                    using StreamReader reader = new StreamReader(urlBuilder.Path);
                    data = reader.ReadToEnd();
                }
                else {
                    var url = GetUrlForRequest(resource);
                    data = _httpClient.GetStringAsync(url).Result;
                }
                return data;
            }
            catch (Exception e) {
                Core.Helpers.Logging.TraceMessage($"GetResourceString({resource}) - {e.Message}");
                return data;
            }
        }

        private string GetUrlForRequest(string resource) {
            try {
                UriBuilder urlBuilder;

                if (resource.StartsWith("file:")) {
                    urlBuilder = new UriBuilder();
                    urlBuilder.Scheme = "file";
                    urlBuilder.Host = "";
                    urlBuilder.Path = resource;
                }
                else if (resource.StartsWith("//") || resource.StartsWith("http:") || resource.StartsWith("https:")) {
                    urlBuilder = new UriBuilder(resource.TrimStart(new char[] { '/' }));
                }
                else {
                    urlBuilder = new UriBuilder(_lastUrl);
                    urlBuilder.Path = resource;
                }
                var requestUrl = urlBuilder.ToString();
                return requestUrl;
            }
            catch {
                WinPrint.Core.Helpers.Logging.TraceMessage($"GetUrlForReqeust({resource}) returning null.");
                return null;
            }
        }

    }
}

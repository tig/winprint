using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using Octokit;
using Serilog;

namespace WinPrint.Core.Services {
    public class UpdateService {

        private const string versionUrl = "https://tig.github.io/winprint/assets/version.txt";

        // FileVersionInfo.GetVersionInfo(Assembly.GetAssembly(typeof(LogService)).Location).FileVersion;
        public event EventHandler<Version> GotLatestVersion;
        protected void OnGotLatestVersion(Version v) => GotLatestVersion?.Invoke(this, v);

        public String ErrorMessage { get; private set; }
        public Version LatestStableVersion { get; private set; }

        public object DownloadUri { get; set; }

        public async Task GetLatestStableVersionAsync() {
            DownloadUri = "https://github.com/tig/winprint/releases";
            using (var client = new WebClient()) {
                try {
                    //string contents =
                    //    await client.DownloadStringTaskAsync(versionUrl).ConfigureAwait(true);

                    //string[] parts = contents.Split('.');

                    //string version = string.Join(".", parts);

                    //if (version != null)
                    //    LatestStableVersion = new Version(version);
                    //else
                    //    ErrorMessage = "Could not parse version data.";

                    var github = new GitHubClient(new Octokit.ProductHeaderValue("tig-winprint"));
                    var release = await github.Repository.Release.GetLatest("tig", "winprint");
                    Log.Debug(
                        "The latest release is tagged at {t} and is named {n}. Download Url: {u}",
                        release.TagName,
                        release.Name,
                        release.Assets[0].BrowserDownloadUrl);

                    var v = release.TagName;
                    // Remove leading "v" (v2.0.0.1000)
                    if (v.StartsWith('v'))
                        v = v.Substring(1, v.Length - 1);
                    string[] parts = v.Split('.');

                    string version = string.Join(".", parts);

                    if (version != null)
                        LatestStableVersion = new Version(version);
                    else
                        ErrorMessage = "Could not parse version data.";

                    DownloadUri = release.Assets[0].BrowserDownloadUrl;
                }
                catch (Exception e) {
                    ErrorMessage = $"({versionUrl}) {e.Message}";
                }
            }

            OnGotLatestVersion(LatestStableVersion);
        }

        public async Task GetLatestRelease() {
            var client = new GitHubClient(new Octokit.ProductHeaderValue("tig-winprint"));
            var release = await client.Repository.Release.GetLatest("tig", "winprint");
            Log.Debug(
                "The latest release is tagged at {t} and is named {n}. Download Url: {u}",
                release.TagName,
                release.Name,
                release.Assets[0].BrowserDownloadUrl);
        }
    }
}

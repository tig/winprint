using System;
using System.Net;
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

        public string DownloadUri { get; set; }

        public async Task GetLatestStableVersionAsync() {
            DownloadUri = "https://github.com/tig/winprint/releases";
            using (var client = new WebClient()) {
                try {
                    var github = new GitHubClient(new Octokit.ProductHeaderValue("tig-winprint"));
                    var release = await github.Repository.Release.GetLatest("tig", "winprint");
                    Log.Debug(
                        "The latest release is tagged at {t} and is named {n}. Download Url: {u}",
                        release.TagName,
                        release.Name,
                        release.Assets[0].BrowserDownloadUrl);

                    var v = release.TagName;
                    // Remove leading "v" (v2.0.0.1000.alpha)
                    if (v.StartsWith('v'))
                        v = v.Substring(1, v.Length - 1);

                    string[] parts = v.Split('.');

                    // Get 4 elements which excludes any .alpha or .beta
                    string version = string.Join(".", parts, 0, 4);

                    if (version != null) {
                        LatestStableVersion = new Version(version);
                        DownloadUri = release.Assets[0].BrowserDownloadUrl;
                    }
                    else
                        ErrorMessage = "Could not parse version data.";

                }
                catch (Exception e) {
                    ErrorMessage = $"({versionUrl}) {e.Message}";
                    ServiceLocator.Current.TelemetryService.TrackException(e);
                }
            }

            OnGotLatestVersion(LatestStableVersion);
        }
    }
}

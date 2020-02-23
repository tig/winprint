using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Net;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace WinPrint.Core.Services {
    public class UpdateService {

        // FileVersionInfo.GetVersionInfo(Assembly.GetAssembly(typeof(LogService)).Location).FileVersion;
        public event EventHandler<Version> GotLatestVersion;
        protected void OnGotLatestVersion(Version v) => GotLatestVersion?.Invoke(this, v);

        public String ErrorMessage { get; private set; }
        public Version LatestStableRelease { get; private set; }

        public string Url { get; set; }

        public async Task GetLatestStableVersionAsync() {
            using (var client = new WebClient()) {
                try {
                    string contents =
                        await client.DownloadStringTaskAsync("https://tig.github.io/winprint/assets/version.txt").ConfigureAwait(true);

                    string[] parts = contents.Split('.');

                    string version = string.Join(".", parts);

                    if (version != null)
                        LatestStableRelease = new Version(version);
                    else
                        ErrorMessage = "Could not parse version data.";
                }
                catch (Exception e) {
                    ErrorMessage = $"({Url}) {e.Message}";
                }
            }

            OnGotLatestVersion(LatestStableRelease);
        }
    }
}

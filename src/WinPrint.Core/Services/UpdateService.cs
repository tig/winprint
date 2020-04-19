using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Octokit;
using Serilog;

namespace WinPrint.Core.Services {
    /// <summary>
    /// Implements version checks, updated version downloads, and installs.
    /// </summary>
    public class UpdateService {

        /// <summary>
        /// Fired whenever a check for latest version has completed. 
        /// </summary>
        public event EventHandler<Version> GotLatestVersion;
        protected void OnGotLatestVersion(Version latestVersion) {
            GotLatestVersion?.Invoke(this, latestVersion);
        }

        /// <summary>
        /// Fired when a download kicked off by StartUpgrade completes
        /// </summary>
        public event EventHandler<string> DownloadComplete;
        protected void OnDownloadComplete(string path) {
            DownloadComplete?.Invoke(this, path);
        }

        public event EventHandler<DownloadProgressChangedEventArgs> DownloadProgressChanged;
        protected void OnDownloadProgressChanged(DownloadProgressChangedEventArgs e) {
            DownloadProgressChanged?.Invoke(this, e);
        }

        /// <summary>
        /// Any error messages from failed update checks or downloads
        /// </summary>
        public string ErrorMessage { get; private set; }

        /// <summary>
        /// Provides the current version number
        /// </summary>
        public static Version CurrentVersion => new Version(FileVersionInfo.GetVersionInfo(Assembly.GetAssembly(typeof(UpdateService)).Location).ProductVersion);

        /// <summary>
        /// Contains the version number of the latest version found online (only valid after GotLatestVersion)
        /// </summary>
        public Version LatestVersion { get; private set; }


        /// <summary>
        /// Uri to the release notes page (only valid after GotLatestVersion)
        /// </summary>
        public System.Uri ReleasePageUri { get; set; }

        /// <summary>
        /// Uri to the installer file (only valid after GotLatestVersion)
        /// </summary>
        public System.Uri InstallerUri { get; set; }

        private string _tempFilename;

        public UpdateService() {
            LatestVersion = new Version(0, 0);
        }

        /// <summary>
        /// Compares current version ot latest online version.
        /// > 0 - Current version is newer
        /// = 0 - Same version
        /// < 0 - A newer version available</summary>
        /// <returns></returns>
        public int CompareVersions() {
            return CurrentVersion.CompareTo(LatestVersion);
        }

        /// <summary>
        /// Checks for updated version online. 
        /// </summary>
        /// <returns></returns>
        public async Task<Version> GetLatestVersionAsync(CancellationToken token) {
            InstallerUri = new Uri("https://github.com/tig/winprint/releases");
            using var client = new WebClient();
            try {
                var github = new GitHubClient(new Octokit.ProductHeaderValue("tig-winprint"));
                var allReleases = await github.Repository.Release.GetAll("tig", "winprint").ConfigureAwait(false);

                //#if DEBUG
                //                Debug.WriteLine("Pausing 2s to simulate version check taking a long time...");
                //                Thread.Sleep(2000);
                //                Debug.WriteLine("Done pausing.");
                //#endif
                token.ThrowIfCancellationRequested();

                // Get all releases and pre-releases
#if DEBUG
                var releases = allReleases.Where(r => r.Prerelease).OrderByDescending(r => new Version(r.TagName.Replace('v', ' '))).ToArray();
#else
                    var releases = allReleases.Where(r => !r.Prerelease).OrderByDescending(r => new Version(r.TagName.Replace('v', ' '))).ToArray();
#endif
                //Log.Debug("Releases {releases}", JsonSerializer.Serialize(releases, options: new JsonSerializerOptions() { WriteIndented = true }));
                if (releases.Length > 0) {
                    Log.Debug("The latest release is tagged at {TagName} and is named {Name}. Download Url: {BrowserDownloadUrl}",
                        releases[0].TagName, releases[0].Name, releases[0].Assets[0].BrowserDownloadUrl);

                    LatestVersion = new Version(releases[0].TagName.Replace('v', ' '));
                    ReleasePageUri = new Uri(releases[0].HtmlUrl);
                    InstallerUri = new Uri(releases[0].Assets[0].BrowserDownloadUrl);
                }
                else {
                    ErrorMessage = "No release found.";
                }
            }
            catch (Exception e) {
                ErrorMessage = $"({ReleasePageUri}) {e.Message}";
                ServiceLocator.Current.TelemetryService.TrackException(e);
            }

            OnGotLatestVersion(LatestVersion);
            return LatestVersion;
        }

        /// <summary>
        /// Starts an upgrade. Must be called after GotLatestVersion has been fired.
        /// </summary>
        public async Task<string> StartUpgradeAsync() {
            Debug.WriteLine("StartUpgradeAsync");
            // Download file
            _tempFilename = Path.GetTempFileName() + ".msi";
            //Log.Information($"{this.GetType().Name}: Downloading {InstallerUri.AbsoluteUri} to {_tempFilename}...");

            var client = new WebClient();
            client.DownloadDataCompleted += Client_DownloadDataCompleted;
            client.DownloadProgressChanged += Client_DownloadProgressChanged;
            File.WriteAllBytes(_tempFilename, await client.DownloadDataTaskAsync(InstallerUri).ConfigureAwait(false));
            return _tempFilename;
        }

        private void Client_DownloadProgressChanged(object sender, DownloadProgressChangedEventArgs e) {
            //Log.Debug($"{this.GetType().Name}: Download progress {e.ProgressPercentage}%");
            //if (e.ProgressPercentage % 33 == 0) {
            //    Log.Information($"{this.GetType().Name}: Download progress {e.ProgressPercentage}%");
            //}
            OnDownloadProgressChanged(e);
        }

        private void Client_DownloadDataCompleted(object sender, DownloadDataCompletedEventArgs e) {
            //try {
            //    // If the request was not canceled and did not throw
            //    // an exception, display the resource.
            //    if (!e.Cancelled && e.Error == null) {
            //        //File.WriteAllBytes(_tempFilename, (byte[])e.Result);
            //    }
            //}
            //finally {

            //}
            ////Log.Information($"{this.GetType().Name}: Download complete");
            ////Log.Information($"{this.GetType().Name}: Exiting and running installer ({_tempFilename})...");


            OnDownloadComplete(_tempFilename);
        }
    }
}

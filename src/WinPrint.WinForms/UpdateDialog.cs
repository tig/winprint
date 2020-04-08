using System;
using System.Diagnostics;
using System.Windows.Forms;
using Serilog;
using WinPrint.Core.Services;

namespace WinPrint.WinForms {
    public partial class UpdateDialog : Form {
        public UpdateDialog() {
            InitializeComponent();
            StartPosition = FormStartPosition.CenterParent;
            this.labelNewVersion.Text = $"A newer version ({ServiceLocator.Current.UpdateService.LatestVersion}) is available.";
            this.linkReleasePage.Links[0].LinkData = ServiceLocator.Current.UpdateService.ReleasePageUri.AbsoluteUri;
        }

        private void downloadButton_Click(object sender, EventArgs args) {
            ServiceLocator.Current.UpdateService.StartUpgradeAsync().ConfigureAwait(false);
        }

        private void linkReleasePage_LinkClicked(object sender, LinkLabelLinkClickedEventArgs args) {
            Log.Debug($"Browsing to release page: {(string)linkReleasePage.Links[0].LinkData}");

            ServiceLocator.Current.TelemetryService.TrackEvent("Release Page Click");

            Process proc = null;
            try {
                var psi = new ProcessStartInfo {
                    UseShellExecute = true,   // This is important
                    FileName = (string)linkReleasePage.Links[0].LinkData
                };
                proc = Process.Start(psi);
            }
            catch (Exception e) {
                // TODO: Better error message (output of stderr?)
                ServiceLocator.Current.TelemetryService.TrackException(e, false);

                Log.Error(e, $"Couldn't browse to {(string)linkReleasePage.Links[0].LinkData}.");
            }
            finally {
                proc?.Dispose();
            }
        }
    }
}

using System;
using System.Diagnostics;
using System.Windows.Forms;
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

        private void linkReleasePage_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e) {
            Process.Start((string)linkReleasePage.Links[0].LinkData);
        }
    }
}

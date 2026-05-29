using System;
using System.Diagnostics;
using System.Windows.Forms;
using Serilog;
using WinPrint.Core.Services;

namespace WinPrint.WinForms;

public partial class UpdateDialog : Form
{
    public UpdateDialog()
    {
        InitializeComponent();
        StartPosition = FormStartPosition.CenterParent;
        labelNewVersion.Text = $"A newer version ({ServiceLocator.Current.UpdateService.LatestVersion}) is available.";
        LinkLabel.Link releaseLink = linkReleasePage.Links[0] ??
                                     throw new InvalidOperationException("Release link was not initialized.");
        releaseLink.LinkData = ServiceLocator.Current.UpdateService.ReleasePageUri?.AbsoluteUri ??
                               throw new InvalidOperationException("Release page URI was not initialized.");
    }

    private void downloadButton_Click(object? sender, EventArgs args)
    {
        ServiceLocator.Current.UpdateService.StartUpgradeAsync().ConfigureAwait(false);
    }

    private void linkReleasePage_LinkClicked(object? sender, LinkLabelLinkClickedEventArgs args)
    {
        string releasePage = linkReleasePage.Links[0]?.LinkData as string ??
                             throw new InvalidOperationException("Release link data was not initialized.");
        Log.Debug($"Browsing to release page: {releasePage}");

        ServiceLocator.Current.TelemetryService.TrackEvent("Release Page Click");

        Process? proc = null;
        try
        {
            var psi = new ProcessStartInfo
            {
                UseShellExecute = true, // This is important
                FileName = releasePage
            };
            proc = Process.Start(psi);
        }
        catch (Exception e)
        {
            // TODO: Better error message (output of stderr?)
            ServiceLocator.Current.TelemetryService.TrackException(e);

            Log.Error(e, $"Couldn't browse to {releasePage}.");
        }
        finally
        {
            proc?.Dispose();
        }
    }
}

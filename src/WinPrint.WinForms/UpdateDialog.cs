using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Drawing;
using System.Text;
using System.Windows.Forms;
using Serilog;
using WinPrint.Core.Services;

namespace WinPrint.WinForms {
    public partial class UpdateDialog : Form {
        public UpdateDialog() {
            InitializeComponent();
        }

        private void downloadButton_Click(object sender, EventArgs args) {

            string url = ServiceLocator.Current.UpdateService.DownloadUri;
            Log.Debug($"Browsing to download: {url}");
            Process proc = null;
            try {
                ProcessStartInfo psi = new ProcessStartInfo();
                psi.UseShellExecute = true;   // This is important
                psi.FileName = url;
                proc = Process.Start(psi);
            }
            catch (Exception e) {
                // TODO: Better error message (output of stderr?)
                Log.Error(e, $"Couldn't browse to {url}.");
            }
            finally {
                proc?.Dispose();
            }
            //Log.Debug("Exiting app.");
            //Application.Exit();
        }
    }
}

//---------------------------------------------------------------------
// <copyright file="SetupWizard.xaml.cs" company="Outercurve Foundation">
//   Copyright (c) 2004, Outercurve Foundation.
//   This software is released under Microsoft Reciprocal License (MS-RL).
//   The license and further copyright text can be found in the file
//   LICENSE.TXT at the root directory of the distribution.
// </copyright>
// <summary>
// Sample embedded UI for the Deployment Tools Foundation project.
// </summary>
//---------------------------------------------------------------------

namespace WinPrintInstaller {
    using System;
    using System.Diagnostics;
    using System.Threading;
    using System.Windows;
    using System.Windows.Navigation;
    using Microsoft.Deployment.WindowsInstaller;
    using Microsoft.Win32;

    /// <summary>
    /// Interaction logic for SetupWizard.xaml
    /// </summary>
    public partial class SetupWizard : Window {
        private ManualResetEvent installStartEvent;
        private InstallProgressCounter progressCounter;
        private bool canceled;
        public Session Session;
        private string productVersion;

        public SetupWizard(ManualResetEvent installStartEvent) {
            this.installStartEvent = installStartEvent;
            progressCounter = new InstallProgressCounter(0.5);
        }

        public MessageResult ProcessMessage(InstallMessage messageType, Record messageRecord,
            MessageButtons buttons, MessageIcon icon, MessageDefaultButton defaultButton) {
            try {
                WixSharp.CommonTasks.UACRevealer.Exit();

                progressCounter.ProcessMessage(messageType, messageRecord);
                progressBar.Value = progressBar.Minimum +
                    progressCounter.Progress * (progressBar.Maximum - progressBar.Minimum);
                //this.progressLabel.Content = "" + (int)Math.Round(100 * this.progressCounter.Progress) + "%";

                var message = string.Format("{0}: {1}", messageType, messageRecord);
                switch (messageType) {
                    case InstallMessage.Error:
                    case InstallMessage.Warning:
                        LogMessage(message);
                        break;

                    case InstallMessage.Info:
#if DEBUG
                        LogMessage(message);
#else
                        if (messageRecord.ToString().Contains("INSTALL. Return value 1."))
                            this.messagesTextBox.Text = $"winprint {productVersion} successfully installed.";
#endif
                        break;
                }

                if (canceled) {
                    canceled = false;
                    return MessageResult.Cancel;
                }
            }
            catch (Exception ex) {
                LogMessage(ex.ToString());
                LogMessage(ex.StackTrace);
            }

            return MessageResult.OK;
        }

        private void LogMessage(string message) {
            messagesTextBox.Text += Environment.NewLine + message;
            messagesTextBox.ScrollToEnd();
        }

        internal void EnableExit() {
            progressBar.Visibility = Visibility.Hidden;
            //this.progressLabel.Visibility = Visibility.Hidden;
            cancelButton.Visibility = Visibility.Hidden;
            telemetryCheck.Visibility = Visibility.Hidden;
            exitButton.Visibility = Visibility.Visible;
        }

        private void installButton_Click(object sender, RoutedEventArgs e) {
            Session["TELEMETRY"] = (bool)telemetryCheck.IsChecked ? "1" : "0";
            WixSharp.CommonTasks.UACRevealer.Enter();
            telemetryCheck.Visibility = Visibility.Hidden;
            installButton.Visibility = Visibility.Hidden;
            progressBar.Visibility = Visibility.Visible;
            //this.progressLabel.Visibility = Visibility.Visible;
            installStartEvent.Set();
        }

        private void exitButton_Click(object sender, RoutedEventArgs e) {
            Close();
        }

        private void cancelButton_Click(object sender, RoutedEventArgs e) {
            if (installButton.Visibility == Visibility.Visible) {
                Close();
            }
            else {
                canceled = true;
                cancelButton.IsEnabled = false;
            }
        }

        private void messagesTextBox_Initialized(object sender, EventArgs e) {
            messagesTextBox.Text = Properties.Resources.License;
        }

        private void hyperlink_RequestNavigate(object sender, RequestNavigateEventArgs e) {
            Process.Start(new ProcessStartInfo(e.Uri.AbsoluteUri));
            e.Handled = true;
        }

        private void Window_Initialized(object sender, EventArgs e) {
            productVersion = Session["ProductVersion"];


        }
        private bool IsDotNetCore31Installed() {
            try {
                RegistryKey localMachine64 = RegistryKey.OpenBaseKey(Microsoft.Win32.RegistryHive.LocalMachine, RegistryView.Registry64);
                RegistryKey lKey = localMachine64.OpenSubKey(@"SOFTWARE\dotnet\Setup\InstalledVersions\x64\sharedhost\", false);
                Version installed = new Version((string)lKey.GetValue("Version"));
                Version required = new Version(Session["RequiredDotNetCoreVersion"]);
                return installed.CompareTo(required) > 0;
            }
            catch {
                return false;
            }
        }

        private void Window_Loaded(object sender, RoutedEventArgs e) {
            if (IsDotNetCore31Installed()) {
            }
            else {

                // Configure message box
                string message = $"winprint requires .NET Core {Session["RequiredDotNetCoreVersion"]} to run.\n\nClick OK to download and install.";
                string caption = this.Title;
                MessageBoxButton buttons = MessageBoxButton.OK;
                MessageBoxImage icon = MessageBoxImage.Information;
                MessageBoxResult defaultResult = MessageBoxResult.OK;
                // Show message box
                MessageBoxResult result = MessageBox.Show(this, message, caption, buttons, icon, defaultResult);
                Process.Start("https://dotnet.microsoft.com/download/dotnet-core/current/runtime");
            }
        }
    }
}

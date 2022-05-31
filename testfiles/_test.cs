using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using WinPrint.Core.Services;

// This is a very long comment to test wrapping of comments in source code. It starts at left column and it goes on and on...
using test; // This is a long comment that starts with a statement and extends way to the right in order to test wrapping.

// private string file = "..\\..\\..\\..\\..\\..\\tests\\formfeeds.txt";
// private string file = "..\\..\\..\\..\\..\\..\\tests\\TEST.TXT";
    // HAS TAB BEFORE private string file = "..\\..\\..\\..\\..\\..\\tests\\long html doc as text.TXT";

namespace WinPrint {
    public partial class MainWindow : Form {

        // The Windows printer document
        private PrintDocument printDoc = new PrintDocument();

        // Winprint Print Preview control
        Application.UseSystemConsole = _useSystemConsole;
        Application.Init ();
        Application.HeightAsBuffer = _heightAsBuffer;

        // Set this here because not initialized until driver is loaded
        _baseColorScheme = Colors.Base;

        StringBuilder aboutMessage = new StringBuilder ();
        aboutMessage.AppendLine (@"");
        aboutMessage.AppendLine (@"UI Catalog is a comprehensive sample library for Terminal.Gui");
        aboutMessage.AppendLine (@"");
        aboutMessage.AppendLine (@"  _______                  _             _   _____       _ ");
        aboutMessage.appendline (@" |__   __|                (_)           | | / ____|     (_)");
        aboutMessage.           (@"    | | ___ _ __ _ __ ___  _ _ __   __ _| || |  __ _   _ _ ");
        aboutMessage.1234567890 (@"    | |/ _ \ '__| '_ ` _ \| | '_ \ / _` | || | |_ | | | | |");
        aboutMessage.WWWWWWWWWW (@"    | |  __/ |  | | | | | | | | | | (_| | || |__| | |_| | |");
        aboutMessage........... (@"    |_|\___|_|  |_| |_| |_|_|_| |_|\__,_|_(_)_____|\__,_|_|");
        aboutMessage.AppendLine (@"");
        aboutMessage.AppendLine ($"Using Terminal.Gui Version: {FileVersionInfo.GetVersionInfo (typeof (Terminal.Gui.Application).Assembly.Location).FileVersion}");
        aboutMessage.AppendLine (@"");

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Globalization", "CA1303:Do not pass literals as localized parameters", Justification = "<Pending>")]
        public MainWindow() {
            InitializeComponent();

            Icon = Resources.printersCB;

            if (LicenseManager.UsageMode != LicenseUsageMode.Designtime) {
                this.panelRight.Controls.Remove(this.dummyButton);
                this.panelRight.Controls.Add(this.printPreview);
                printersCB.Enabled = false;
                paperSizesCB.Enabled = false;
            }
        }

        bool disposed = false;
        // Protected implementation of Dispose pattern.
        protected override void Dispose(bool disposing) {
        }
    }
}

// Last line
using System;
using System.Diagnostics;
using Microsoft.Deployment.WindowsInstaller;
using WixSharp;

//#error "DON'T FORGET to install NuGet package 'WixSharp' and remove this `#error` statement."
// NuGet console: Install-Package WixSharp
// NuGet Manager UI: browse tab

namespace WinPrint.WixSharp {
    class Install {
        public static readonly Guid UpgradeCode = Guid.Parse("{0002A500-0000-0000-C000-000000000046}");
        public static readonly Guid ProductCode = Guid.Parse("{0002A501-0000-0000-C000-000000000046}");

        static void Main() {
            const string sourceBaseDir = @"..\..\release";
            const string outDir = @"..\..\install";
            string versionFile = $"{sourceBaseDir}\\WinPrint.Core.dll";
            Debug.WriteLine($"version path: {versionFile}", versionFile);
            var info = FileVersionInfo.GetVersionInfo(versionFile);
            var companyName = info.CompanyName;
            var productName = info.ProductName;
            var productVersion = info.ProductVersion;


           // Feature Feature = new Feature(new Id("Feature_Run"));

            var project = new Project(productName, 
                new EnvironmentVariable("PATH", "[INSTALLDIR]") { Part = EnvVarPart.last }) {
 
                Dirs = new[] {
                    new Dir($"%ProgramFiles%\\{companyName}\\{productName}", 
                        new Files(@"*.dll"),
                        new Files(@"*.deps.json"),
                        new Files(@"*.runtimeconfig.json"),
                        new File(new Id("winprint_exe"), @"winprint.exe"),
                        new File(new Id("winprintgui_exe"), @"winprintgui.exe", 
                            new FileShortcut("WinPrint", "INSTALLDIR") { AttributesDefinition = "Advertise=yes"} ),
                        new ExeFileShortcut("Uninstall WinPrint", "[System64Folder]msiexec.exe", "/x [ProductCode]")),
                    new Dir($"%AppData%\\{companyName}\\{productName}"),
                    new Dir($"%ProgramMenu%\\{companyName}\\{productName}",
                        new ExeFileShortcut("WinPrint", "[INSTALLDIR]winprintgui.exe", arguments: ""))
                        //new ExeFileShortcut("WinPrint Config Directory", 
                        //    $"[%AppData%\\{companyName}\\{productName}".ToDirID() +" ]", ""),
                        //new ExeFileShortcut("Uninstall WinPrint", "[System64Folder]msiexec.exe", "/x [ProductCode]"))
                 },

            //Binaries = new[] {
            //},

            //Actions = new[] {
            //    new InstalledFileAction("winprintgui_exe", "")
            //    {
            //        Step = Step.InstallFinalize,
            //        When = When.After,
            //        Return = Return.asyncNoWait,
            //        Execute = Execute.immediate,
            //        Impersonate = true,
            //        //Condition = Feature.BeingInstall(),
            //    }
            //},

            Properties = new[]{
                    //setting property to be used in install condition
                    new Property("ALLUSERS", "1"),
                }
            };

            // See Core.Models.
            project.GUID = ProductCode;
            project.UpgradeCode = UpgradeCode;
            project.SourceBaseDir = sourceBaseDir;
            project.OutDir = outDir;
            project.Version = Version.Parse(info.ProductVersion); //new Version("2.0.1.10040"); 

            project.MajorUpgrade = new MajorUpgrade {
                Schedule = UpgradeSchedule.afterInstallInitialize,
                AllowSameVersionUpgrades = true,
                DowngradeErrorMessage = "A later version of [ProductName] is already installed. Setup will now exit."
            };

            project.Platform = Platform.x64;

            //project.LicenceFile = "license.rtf";

            project.ControlPanelInfo.Comments = "WinPrint by Charlie Kindel";
            project.ControlPanelInfo.Readme = "https://github.com/tig/winprint";
            project.ControlPanelInfo.HelpLink = "https://github.com/tig/winprint";
            project.ControlPanelInfo.UrlInfoAbout = "https://github.com/tig/winprint";
            project.ControlPanelInfo.UrlUpdateInfo = "https://github.com/tig/winprint";
            //project.ControlPanelInfo.ProductIcon = "app_icon.ico";
            project.ControlPanelInfo.Contact = "Charlie Kindel (charlie@kindel.com)";
            project.ControlPanelInfo.Manufacturer = companyName;
            project.ControlPanelInfo.InstallLocation = "[INSTALLDIR]";
            project.ControlPanelInfo.NoModify = true;
            //project.ControlPanelInfo.NoRepair = true,
            //project.ControlPanelInfo.NoRemove = true,
            //project.ControlPanelInfo.SystemComponent = true, //if set will not be shown in Control Panel

            project.PreserveTempFiles = true;

            //project.UI = WUI.WixUI_ProgressOnly;

            project.BuildMsi();
        }
    }


    //public class CustonActions {
    //    [CustomAction]
    //    public static ActionResult StartWinPrintGUI(Session session) {
    //        Record record = new Record();
    //        record.FormatString = $"{session["INSTALLDIR"]}winprintgui.exe";
    //        session.Message(InstallMessage.Info, record);
    //        System.Diagnostics.Process.Start($"{session["INSTALLDIR"]}winprintgui.exe");
    //        return ActionResult.Success;
    //    }
    //}
}

using System;
using System.Diagnostics;
using WixSharp;

namespace WinPrintInstaller {
    internal class Install {
        public static readonly Guid UpgradeCode = new Guid("{0002A500-0000-0000-C000-000000000046}");
        public static readonly Guid ProductCode = new Guid("{0002A501-0000-0000-C000-000000000046}");

        private static void Main() {
            const string sourceBaseDir = @"..\..\release";
            const string outDir = @"..\..\install";
            var versionFile = $"{sourceBaseDir}\\WinPrint.Core.dll";
            Debug.WriteLine($"version path: {versionFile}");
            var info = FileVersionInfo.GetVersionInfo(versionFile);
            var feature = new Feature(new Id("winprint"));

            var project = new Project(info.ProductName, new EnvironmentVariable("PATH", "[INSTALLDIR]") { Part = EnvVarPart.last }) {

                RegValues = new[] {
                    new RegValue(feature, RegistryHive.LocalMachine, $@"Software\{info.CompanyName}\{info.ProductName}", "Telemetry", "[TELEMETRY]") {
                        Win64 = true,
                        // https://github.com/oleg-shilo/wixsharp/issues/818#issuecomment-597058371
                        AttributesDefinition = "Type=integer"
                    }
                },

                Dirs = new[] {
                    new Dir(feature, $"%ProgramFiles%\\{info.CompanyName}\\{info.ProductName}",
                        new File(@"pygmentize.exe"),
                        new Files(@"*.dll"),
                        new Files(@"*.deps.json"),
                        new Files(@"*.runtimeconfig.json"),
                        new File(new Id("winprintgui_exe"), @"winprintgui.exe",
                            new FileShortcut("winprint", "INSTALLDIR") { AttributesDefinition = "Advertise=yes" }),
                        new ExeFileShortcut("Uninstall winprint", "[System64Folder]msiexec.exe", "/x [ProductCode]")),
                    new Dir(feature, $"%AppData%\\{info.CompanyName}\\{info.ProductName}"),
                    new Dir(feature, $"%ProgramMenu%\\{info.CompanyName}\\{info.ProductName}",
                        new ExeFileShortcut("WinPrint", "[INSTALLDIR]winprintgui.exe", arguments: ""))
                },

                Properties = new[]{
                    //setting property to be used in install condition
                    new Property("ALLUSERS", "1"),
                    new Property("TELEMETRY", "1"),
                    new Property("RequiredDotNetCoreVersion", "3.1")
                }
            };

            // See Core.Models.
            project.GUID = ProductCode;
            project.UpgradeCode = UpgradeCode;
            project.SourceBaseDir = sourceBaseDir;
            project.OutDir = outDir;

            project.Version = new Version(info.ProductVersion);
            project.MajorUpgrade = new MajorUpgrade {
                Schedule = UpgradeSchedule.afterInstallInitialize,
                AllowSameVersionUpgrades = true,
                DowngradeErrorMessage = "A later version of [ProductName] is already installed. Setup will now exit."
            };
            project.Platform = Platform.x64;

            project.ControlPanelInfo.Comments = $"winprint by Charlie Kindel";
            project.ControlPanelInfo.Readme = "https://tig.github.io/winprint";
            project.ControlPanelInfo.HelpLink = "https://tig.github.io/winprint";
            project.ControlPanelInfo.UrlInfoAbout = "https://tig.github.io/winprint";
            project.ControlPanelInfo.UrlUpdateInfo = "https://tig.github.io/winprint";
            project.ControlPanelInfo.Manufacturer = info.CompanyName;
            project.ControlPanelInfo.InstallLocation = "[INSTALLDIR]";
            project.ControlPanelInfo.NoModify = true;

            project.PreserveTempFiles = true;

            project.EmbeddedUI = new EmbeddedAssembly(System.Reflection.Assembly.GetExecutingAssembly().Location);
            project.PreserveTempFiles = true;

            project.CAConfigFile = "CustomAction.config";

            project.BuildMsi();

            //var bootstrapper = new Bundle($"{info.ProductName}", new PackageGroupRef("NetFx462Web"), 
            //    new MsiPackage(project.BuildMsi()) { 
            //        Id = project.Id,
            //        DisplayInternalUI = true,
            //    } );
            //bootstrapper.Manufacturer = info.CompanyName;
            //bootstrapper.AboutUrl = project.ControlPanelInfo.Readme;
            //bootstrapper.IconFile = @"..\..\src\WinPrint.WinForms\winprint.ico";
            //bootstrapper.Version = project.Version;
            //bootstrapper.UpgradeCode = new Guid("{0002A502-0000-0000-C000-000000000046}");
            ////bootstrapper.Application.LogoFile = @"..\..\src\WinPrint.WinForms\winprint.png";
            ////bootstrapper.Application.LicensePath = "license.rtf";
            //bootstrapper.Build($@"{outDir}\{info.ProductName} setup.exe");
        }
    }
}

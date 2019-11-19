using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using System.Windows.Forms;
using CommandLine;
using GalaSoft.MvvmLight.Ioc;
using WinPrint.Core.Models;

namespace WinPrint {
    static class Program {

        [DllImport("kernel32.dll")]
        static extern bool AttachConsole(int dwProcessId);
        private const int ATTACH_PARENT_PROCESS = -1;
        /// <summary>
        ///  The main entry point for the application.
        /// </summary>
        [STAThread]
        static void Main(string[] args) {
            if (args.Length > 0) {

                // BUGBUG: This is a hack. See
                // https://devblogs.microsoft.com/oldnewthing/20090101-00/?p=19643 
                // redirect console output to parent process;
                // must be before any calls to Console.WriteLine()
                AttachConsole(ATTACH_PARENT_PROCESS);

                var parser = new Parser(with => with.EnableDashDash = true);
                var result = parser.ParseArguments<Options>(args);

                //result.WithNotParsed<Options>(o => {
                //    Debug.WriteLine(o.ToString());

                //    if (o.GetType() == typeof(CommandLine.Error)) {
                //        Console.WriteLine("Errro");
                //    }

                //});

                result.WithParsed<Options>(o => {
                    // copy Files
                    ModelLocator.Current.Options.Files = o.Files.ToList() ;
                    ModelLocator.Current.Options.Landscape = o.Landscape;
                    ModelLocator.Current.Options.Printer = o.Printer;
                    ModelLocator.Current.Options.PaperSize = o.PaperSize;
                    ModelLocator.Current.Options.Gui = o.Gui;
                    ModelLocator.Current.Options.Exit = o.Exit;
                });
                parser.Dispose();
            }

            Application.SetHighDpiMode(HighDpiMode.SystemAware);
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);


#pragma warning disable CA2000 // Dispose objects before losing scope
            Application.Run(new MainWindow());
#pragma warning restore CA2000 // Dispose objects before losing scope
        }
    }
}

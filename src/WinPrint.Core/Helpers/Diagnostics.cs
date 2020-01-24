using System.Runtime.InteropServices;

namespace WinPrint.Core.Helpers {
    class Diagnostics {

        [DllImport("libgdiplus", ExactSpelling = true)]
        internal static extern string GetLibgdiplusVersion();

        ///// <summary>
        ///// Gets the version of libgdiplus. 
        ///// Solution found here: https://github.com/dotnet/corefx/issues/37846
        ///// </summary>
        ///// <returns></returns>
        //public static string GetlibgdiplusVersion() {
        //    try {
        //        using (var process = Process.Start(new ProcessStartInfo {
        //            FileName = "dpkg-query",
        //            Arguments = "--showformat '${Version}' --show libgdiplus",
        //            RedirectStandardOutput = true,
        //            UseShellExecute = false
        //        })) {
        //            process.WaitForExit();
        //            return process.StandardOutput.ReadToEnd();
        //        }
        //    }
        //    catch (Exception ex) {
        //        return $"Unable to determine libgdiplus version using `dpkg-query`. exception: {ex}";
        //    }
        //}

    }

}


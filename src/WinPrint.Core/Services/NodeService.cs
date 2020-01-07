using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Serilog;

namespace WinPrint.Core.Services {
    public class NodeService {

        internal NodeService Create() {
            return new NodeService();
        }

        /// <summary>
        /// Gets the directory node is installed in. Assumes node is installed.
        /// </summary>
        /// <returns></returns>
        private async Task<string> GetNodeDirectory() {
            if (!string.IsNullOrEmpty(nodeDir)) 
                return nodeDir;

            string path = "";
            Process proc = null;
            try {
                ProcessStartInfo psi = new ProcessStartInfo();
                psi.UseShellExecute = false;   // This is important
                psi.CreateNoWindow = true;     // This is what hides the command window.
                psi.FileName = @"where.exe";
                psi.Arguments = "node";
                psi.RedirectStandardInput = true;
                psi.RedirectStandardOutput = true;
                proc = Process.Start(psi);
                //StreamWriter sw = node.StandardInput;
                //sw.WriteLine("");
                //sw.Close();
                path = await proc.StandardOutput.ReadLineAsync();
                Log.Debug(LogService.GetTraceMsg(), path);
            }
            catch (Exception e) {
                // TODO: Better error message (output of stderr?)
                Log.Error(e, "Failed to find node.js using where.exe.");
            }
            finally {
                proc?.Dispose();
            }
            return nodeDir = Path.GetDirectoryName(path);
        }


        /// <summary>
        /// Gets the directory node is installed in. Assumes node is installed.
        /// </summary>
        /// <returns></returns>
        public async Task<string> GetModulesDirectory() {
            string path = "";
            Process proc = null;
            try {
                ProcessStartInfo psi = new ProcessStartInfo();
                psi.UseShellExecute = false;   // This is important
                psi.CreateNoWindow = true;     // This is what hides the command window.
                psi.FileName = @"node";
                psi.Arguments = $"\"{await GetNodeDirectory()}\\node_modules\\npm\\bin\\npm-cli.js\" root";
                psi.RedirectStandardInput = true;
                psi.RedirectStandardOutput = true;
                proc = Process.Start(psi);
                //StreamWriter sw = node.StandardInput;
                //sw.WriteLine("");
                //sw.Close();
                path = await proc.StandardOutput.ReadLineAsync() + "\\";
            }
            catch (Exception e) {
                // TODO: Better error message (output of stderr?)
                Log.Error(e, "Failed to get node_modules location.");
            }
            finally {
                proc?.Dispose();
            }
            return Path.GetDirectoryName(path);
        }

        private StringBuilder stdIn = new StringBuilder();
        private StringBuilder stdOut = new StringBuilder();
        private StringBuilder stdErr = new StringBuilder();
        private Process nodeProc = null;
        private string nodeDir = null;

        private string version;

        public string Version { get => version; set => version = value; }

        /// <summary>
        /// Sees if node.js is installed. If it's not been installed, returns false.
        /// sets Version.
        /// </summary>
        /// <returns></returns>
        public async Task<bool> IsInstalled() {
            bool installed = false;
            Process proc = null;
            ProcessStartInfo psi = new ProcessStartInfo();
            try {
                psi.UseShellExecute = false;   // This is important
                psi.CreateNoWindow = true;     // This is what hides the command window.
                psi.FileName = @"node";
                psi.Arguments = $"\"{await GetNodeDirectory()}\\node_modules\\npm\\bin\\npm-cli.js\" version";
                psi.RedirectStandardInput = true;
                psi.RedirectStandardOutput = true;
                psi.RedirectStandardError = true;

                Log.Debug("Starting Process: {f}, {a}", psi.FileName, psi.Arguments);
                proc = Process.Start(psi);
                StreamWriter sw = proc.StandardInput;
                Log.Debug("Sending: {cmd}", stdIn.ToString());
                await sw.WriteLineAsync(stdIn.ToString());
                // This terminates the session
                sw.Close();

                Log.Debug("Reading stdOut...");
                while (!proc.StandardOutput.EndOfStream) {
                    var outputLine = await proc.StandardOutput.ReadLineAsync();
                    stdOut.AppendLine(outputLine);
                }

                Log.Debug($"Reading stdErr...");
                while (!proc.StandardError.EndOfStream) {
                    var outputLine = await proc.StandardError.ReadLineAsync();
                    stdErr.AppendLine(outputLine);
                }

                // Process output
                if (stdOut.Length > 0) {
                    // node returns data in Javascript format (no quotes around property names)
                    // System.Text.Json does not support this. So a little regex to just find the 
                    // npm: "x.y.z" version #. We don't use the version # for anything bug diagnostics
                    // so this could just be a `installed = stdOut.Contains("npm:")'.
                    Version = Regex.Match(stdOut.ToString(), @"npm:\W'(.*)',").Groups[1].ToString();
                    Log.Debug("Node.js found. File: {file} {args}", psi.FileName, psi.Arguments);
                    installed = true;
                }

                // TODO: Implement better error handling of stdErr
            }
            catch (Exception e) {
                Log.Debug(e, "File: {file}, Args: {args}", psi.FileName, psi.Arguments);
                // Node not installed. 
                // TODO: Install node
            }
            finally {
                proc?.Dispose();
                Log.Debug("stdOutput: {s}", stdOut);
                Log.Debug("stdError: {s}", stdErr);
            }

            Log.Debug("Node.js is {installed}, Version: {version}", installed ? "installed" : "not installed", Version);
            return installed;
        }

        /// <summary>
        /// Installs prismjs.
        /// </summary>
        /// <returns></returns>
        private async Task<bool> InstallPrismJS() {
            bool installed = false;
            Process proc = null;
            var nodeDir = await GetNodeDirectory(); ;
            try {
                ProcessStartInfo psi = new ProcessStartInfo();
                psi.UseShellExecute = false;   // This is important
                psi.CreateNoWindow = true;     // This is what hides the command window.
                psi.FileName = @"node";
                psi.Arguments = $"\"{nodeDir}\\node_modules\\npm\\bin\\npm-cli.js\" install prismjs";
                psi.RedirectStandardInput = true;
                psi.RedirectStandardOutput = true;
                psi.RedirectStandardError = true;

                LogService.TraceMessage($"Starting Process: {psi.FileName} {psi.Arguments}");
                nodeProc = Process.Start(psi);
                StreamWriter sw = nodeProc.StandardInput;
                LogService.TraceMessage($"Process started: {proc.ProcessName}");
                LogService.TraceMessage($"Sending: {stdIn.ToString()}");
                await sw.WriteLineAsync(stdIn.ToString());
                sw.Close();

                LogService.TraceMessage($"Reading stdOut:");
                while (!nodeProc.StandardOutput.EndOfStream) {
                    var outputLine = await proc.StandardOutput.ReadLineAsync();
                    stdOut.AppendLine(outputLine);
                }

                LogService.TraceMessage($"Reading stdErr:");
                while (!nodeProc.StandardError.EndOfStream) {
                    var outputLine = await proc.StandardError.ReadLineAsync();
                    stdErr.AppendLine(outputLine);
                }

                LogService.TraceMessage($"EOF");
            }
            catch (Exception e) {
                LogService.TraceMessage(e.Message);
                // Node not installed. 
                // TODO: Install node
                nodeProc?.Dispose();
                nodeProc = null;
            }
            finally {
                LogService.TraceMessage($"stdOutput: {stdOut.ToString()}");
                LogService.TraceMessage($"stdError: {stdErr.ToString()}");
            }
            return installed;
        }
    }
}

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

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
            }
            catch (Exception e) {
                LogService.TraceMessage(e.Message);
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
                LogService.TraceMessage(e.Message);
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
            try {
                ProcessStartInfo psi = new ProcessStartInfo();
                psi.UseShellExecute = false;   // This is important
                psi.CreateNoWindow = true;     // This is what hides the command window.
                psi.FileName = @"nodex";
                psi.Arguments = $"\"{await GetNodeDirectory()}\\node_modules\\npm\\bin\\npm-cli.js\" version";
                psi.RedirectStandardInput = true;
                psi.RedirectStandardOutput = true;
                psi.RedirectStandardError = true;

                LogService.TraceMessage($"Starting Process: {psi.FileName} {psi.Arguments}");
                proc = Process.Start(psi);
                StreamWriter sw = proc.StandardInput;
                LogService.TraceMessage($"Process started: {proc.ProcessName}");
                LogService.TraceMessage($"Sending: {stdIn.ToString()}");
                await sw.WriteLineAsync(stdIn.ToString());
                // This terminates the session
                sw.Close();

                LogService.TraceMessage($"Reading stdOut:");
                while (!proc.StandardOutput.EndOfStream) {
                    var outputLine = await proc.StandardOutput.ReadLineAsync();
                    stdOut.AppendLine(outputLine);
                }

                LogService.TraceMessage($"Reading stdErr:");
                while (!proc.StandardError.EndOfStream) {
                    var outputLine = await proc.StandardError.ReadLineAsync();
                    stdErr.AppendLine(outputLine);
                }

                // Process output
                if (stdOut.Length > 0) {
                    version = Regex.Match(stdOut.ToString(), @"npm:\W'(.*)',").Groups[1].ToString();
                    installed = true;
                }
            }
            catch (Exception e) {

                LogService.TraceMessage(e.Message);
                // Node not installed. 
                // TODO: Install node
            }
            finally {
                proc?.Dispose();
                LogService.TraceMessage($"stdOutput: {stdOut.ToString()}");
                LogService.TraceMessage($"stdError: {stdErr.ToString()}");
            }

            LogService.TraceMessage($"Node.js installed: {installed}, Version: {version}");
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

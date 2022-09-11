using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using LiteHtmlSharp;
using Microsoft.VisualBasic;
using Serilog;

namespace WinPrint.Core.Services {
    /// <summary>
    /// Converts (syntax highlights) a document using the Pygments 
    /// Syntax highlighter (https://pygments.org/)
    /// </summary>
    public class PygmentsConverterService {
        internal PygmentsConverterService Create() {
            return new PygmentsConverterService();
        }

        public PygmentsConverterService() {

        }

        static bool _pygmentsInstalled = false;
        static string _scriptsPath = string.Empty;


        /// <summary>
        /// Check that Python 3.x is installed and that pygmentize.exe works
        /// </summary>
        /// <returns>true if pygmentize.exe is working, false otherwise + message</returns>
        public (bool installed, string message) CheckInstall() {

            if (_pygmentsInstalled) {
                return (true, "Pygments is is functional.");
            }
            string message = String.Empty;

            Process proc = new Process();

            proc.StartInfo.RedirectStandardInput = true;
            proc.StartInfo.RedirectStandardOutput = true;
            proc.StartInfo.RedirectStandardError = true;
            proc.StartInfo.UseShellExecute = false;
            proc.StartInfo.CreateNoWindow = true;
            proc.EnableRaisingEvents = false;

            // Test for Python 3.x
            bool python = false;
            try {
                Log.Information("Source code formatting: Verifying Python is installed");
                proc.StartInfo.FileName = "python3";
                proc.StartInfo.Arguments = $"-V";
                Log.Debug("Pygments: Starting process {proc} {args}", proc.StartInfo.FileName, proc.StartInfo.Arguments);
                proc.Start();

                if (proc.WaitForExit(5000)) {
                    string output = proc.StandardOutput.ReadLine();
                    if (output !=null && output.StartsWith("Python ")) {
                        python = true;
                        Log.Debug("Pygments: Python is installed: {output}", output);
                    }
                    else {
                        message = $"Python does not appear to be installed.\n{proc.StartInfo.FileName} failed to start: {output}";
                    }
                }
                else {
                    message = $"Could not launch {proc.StartInfo.FileName}; timeout.";
                }
            }
            catch (System.Exception ex) { // Console and WinForms are different
                message = $"{proc.StartInfo.FileName} {proc.StartInfo.Arguments} failed:\n{ex.Message}";
            }

            if (!python) {
                message = $"Python 3.x must be installed (on the PATH) for source code formatting.\n\n{message}";
                Log.Debug("Pygments: {message}", message);
                return (_pygmentsInstalled, message);
            }

            // Get python scripts folder
            //  python -c 'import os,sysconfig;print(sysconfig.get_path("scripts",f"{os.name}_user"))'
            // from https://stackoverflow.com/questions/62162970/programmatically-determine-pip-user-install-location-scripts-directory/62167797#62167797
            try {
                Log.Information("Source code formatting: Verifying Python scripts location");
                proc.StartInfo.FileName = "pythonx";
                proc.StartInfo.Arguments = "-c \"import os,sysconfig;print(sysconfig.get_path('scripts',f'{os.name}_user'))\"";
                Log.Debug("Pygments: Starting process {proc} {args}", proc.StartInfo.FileName, proc.StartInfo.Arguments);
                proc.Start();

                if (proc.WaitForExit(5000)) {
                    _scriptsPath = proc.StandardOutput.ReadLine();
                    if (!string.IsNullOrEmpty(_scriptsPath) && _scriptsPath.ToLowerInvariant().EndsWith("scripts")) {
                        Log.Debug("Pygments: Python scripts folder: {scriptsPath}", _scriptsPath);
                        message = _scriptsPath;
                    }
                    else {
                        _scriptsPath = proc.StandardError.ReadToEnd();

                        Log.Debug("Pygments: Python error: {scriptsPath}", _scriptsPath);
                        message = $"Could not find the Python scripts folder.\n{proc.StartInfo.FileName} failed to start: {_scriptsPath}";
                    }
                }
                else {
                    message = $"Could not launch {proc.StartInfo.FileName}; timeout.";
                }
            }
            catch (System.Exception ex) { // Console and WinForms are different
                message = $"{proc.StartInfo.FileName} {proc.StartInfo.Arguments} failed:\n{ex.Message}";
            }

            if (string.IsNullOrEmpty(_scriptsPath)) {
                message = $"Could not find Pygments. Source code formatting will not work.\n\n{message}";
                Log.Debug("Pygments: {message}", message);
                return (_pygmentsInstalled, message);
            }

            // Test pygmentize.exe - Which is installed in the app's Program File dir
            try {
                Log.Information("Source code formatting: Verifying Pygments is installed");
                proc.StartInfo.FileName = @$"{_scriptsPath}\pygmentize.exe";
                proc.StartInfo.Arguments = $"-V";
                Log.Debug("Pygments: Starting process {proc} {args}", proc.StartInfo.FileName, proc.StartInfo.Arguments);
                proc.Start();

                if (proc.WaitForExit(5000)) {
                    string output = proc.StandardOutput.ReadLine();
                    if (output != null && output.StartsWith("Pygments version ")) {
                        _pygmentsInstalled = true;
                        message = $"Pygments is functional: {output}";
                        Log.Debug("Pygments: {output}", message);
                        return (_pygmentsInstalled, message);
                    }
                    else {
                        message = $"Pygments is not installed. {proc.StartInfo.FileName} failed to start: {(output == null ? "no output" : output)}";
                        // Try to install it
                        (_pygmentsInstalled, message) = InstallPygments();
                    }
                }
                else {
                    message = $"Could not launch {proc.StartInfo.FileName}; timeout.";
                }
            }
            catch (System.Exception ex) { // Console and WinForms are different
                message = $"{proc.StartInfo.FileName} {proc.StartInfo.Arguments} failed:\n{ex.Message}";
                Log.Debug("Pygments: {message}", message);
                // Try to install it
                (_pygmentsInstalled, message) = InstallPygments();
            }

            if (!_pygmentsInstalled) {
                message = $"Pygments error. Source code formatting will not function.\n\n{message}";
            }
            Log.Debug("Pygments: {message}", message);
            return (_pygmentsInstalled, message);
        }


        /// <summary>
        /// Installs pygments via pip install
        /// </summary>
        /// <returns>true if pygmentize.exe is working, false otherwise + message</returns>
        private (bool installed, string message) InstallPygments() {
            string message = String.Empty;

            Process proc = new Process();

            proc.StartInfo.RedirectStandardInput = true;
            proc.StartInfo.RedirectStandardOutput = true;
            proc.StartInfo.RedirectStandardError = true;
            proc.StartInfo.UseShellExecute = false;
            proc.StartInfo.CreateNoWindow = true;
            proc.EnableRaisingEvents = false;

            // Install Pygments via pip install
            bool pygments = false;
            try {
                Log.Information("Source code formatting: Installing Pygments");

                proc.StartInfo.FileName = "pip";
                proc.StartInfo.Arguments = $"install Pygments --upgrade";
                Log.Debug("Pygments: Attempting to install Pygments via: {cmd} {args}", proc.StartInfo.FileName, proc.StartInfo.Arguments);
                proc.Start();

                // TODO: 30 secs is a long time without status updates 
                if (proc.WaitForExit(30000)) {
                    string output = proc.StandardOutput.ReadToEnd();
                    if (output != null && 
                        (output.Contains("Successfully installed Pygments") ||
                        output.Contains("Requirement already satisfied"))) {
                        pygments = true;
                        Log.Debug("Pygments: Pygments is installed: {output}", output);
                        message = "Pygments is installed";
                    }
                    else {
                        message = $"Error installing Pygments.\n{proc.StartInfo.FileName} failed to start: {output}";
                    }
                }
                else {
                    message = $"Could not launch {proc.StartInfo.FileName}; timeout.";
                }
            }
            catch (System.Exception ex) { // Console and WinForms are different
                message = $"{proc.StartInfo.FileName} {proc.StartInfo.Arguments} failed:\n{ex.Message}";
            }

            if (!pygments) {
                message = $"Pygments: Pygments must be installed for source code formatting to work.\n\n{message}";
                Log.Debug("{message}", message);
                return (pygments, message);
            }

            return (pygments, message);
        }

        private Process _proc;
        private TaskCompletionSource<bool> _eventHandled;

        public async Task<string> ConvertAsync(string document, string style, string language) {
            LogService.TraceMessage();

            if (_proc != null || _eventHandled != null) {
                throw new InvalidOperationException("Pygments: ConvertAsync already in progress.");
            }

            if (string.IsNullOrEmpty(_scriptsPath)) {
                throw new InvalidOperationException("Pygments: Pygments is not configured; script path is not set.");
            }

            string file = Path.GetTempFileName();
            _proc = new Process();
            _proc.StartInfo.FileName = @$"{_scriptsPath}\pygmentize.exe";
            _proc.StartInfo.Arguments = $"-P outencoding=utf-8 -f 16m -O style=\"{(string.IsNullOrEmpty(style) ? "default" : style)}\" -l \"{language}\" -o \"{file}.an\" \"{file}\"";
            _proc.StartInfo.RedirectStandardInput = true;
            _proc.StartInfo.RedirectStandardOutput = true;
            _proc.StartInfo.RedirectStandardError = true;
            _proc.StartInfo.UseShellExecute = false;
            _proc.StartInfo.CreateNoWindow = true;
            _proc.EnableRaisingEvents = true;
            _proc.Exited += Proc_Exited;

            _eventHandled = new TaskCompletionSource<bool>();

            try {
                Log.Debug("Pygments: Writing temp file {file}", file);
                await File.WriteAllTextAsync(file, document, Encoding.UTF8).ConfigureAwait(true);
                Log.Debug("Pygments: Starting {pyg} {args}", _proc.StartInfo.FileName, _proc.StartInfo.Arguments);
                _proc.Start();

                Log.Debug("Waiting for pygments to exit");
                await Task.WhenAny(_eventHandled.Task, Task.Delay(10000)).ConfigureAwait(true);

                if (_proc.ExitCode != 0) {
                    var stdErr = _proc.StandardError.ReadToEnd();
                    Log.Debug("Pygments: StandardError: {stdErr}", stdErr);
                    var stdOut = _proc.StandardOutput.ReadToEnd();
                    Log.Debug("Pygments: StandardOutput: {stdOut}", stdOut);
                    if (stdErr.StartsWith("Usage:")) {
                        stdErr = "Invalid command line.";
                    }
                    document = $"Pygments: pygmentize.exe error (exit code: {_proc.ExitCode}): {stdErr}";
                    Log.Debug("{document}", document);
                    throw new InvalidOperationException(document);
                }
                else {
                    if (!string.IsNullOrEmpty($"{file}.an") && File.Exists($"{file}.an")) {
                        Log.Debug("Pygments: Reading {file}", $"{file}.an");
                        document = await File.ReadAllTextAsync($"{file}.an", Encoding.UTF8).ConfigureAwait(true);

                        // HACK: Because of this bug: https://github.com/pygments/pygments/issues/1435
                        if (document[^1] == '\n')
                            document = document.Remove(document.Length - 1, 1);
                    }
                    else {
                        // TODO: This should really throw an exception
                        var stdErr = _proc.StandardError.ReadToEnd();
                        document = $"Pygments failed to create converter file: {stdErr}";
                        Log.Debug("Pygments: {document}", document);
                        throw new InvalidOperationException(document);
                    }
                }
            }
            catch (System.ComponentModel.Win32Exception e) {
                // TODO: Better error message (output of stderr?)
                document = $"Could not format document:\n{_proc.StartInfo.FileName} {_proc.StartInfo.Arguments} failed:\n{e.Message}";
                Log.Debug(e, "{document}", document);
                throw new InvalidOperationException(document);
            }
            finally {
                // Clean up
                if (!string.IsNullOrEmpty(file) && File.Exists(file)) {
                    File.Delete(file);
                }
                if (!string.IsNullOrEmpty($"{file}.an") && File.Exists($"{file}.an")) {
                    File.Delete($"{file}.an");
                }
                _proc.Exited -= Proc_Exited;
                _proc?.Dispose();
                _proc = null;
                _eventHandled = null;
            }

            return document;
        }

        private void Proc_Exited(object sender, EventArgs e) {
            Log.Debug("Pygments: pygmatize exited: Time: {exitTime}, ExitCode: {exitCode}, ElapsedTime: {elapsedTime}ms", _proc.ExitTime, _proc.ExitCode, Math.Round((_proc.ExitTime - _proc.StartTime).TotalMilliseconds));

            _eventHandled.TrySetResult(true);
        }
    }
}

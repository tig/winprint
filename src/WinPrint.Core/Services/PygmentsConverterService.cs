using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using IronPython.Hosting;
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

        private Process _proc;
        private TaskCompletionSource<bool> _eventHandled;

        public async Task<string> ConvertAsync(string document, string style, string language) {
            if (_proc != null) {
                throw new InvalidOperationException("ConvertAsync already in progress.");
            }
            if (_eventHandled != null) {
                throw new InvalidOperationException("ConvertAsync already in progress.");
            }

            string file = Path.GetTempFileName();
            _proc = new Process();
            _proc.StartInfo.FileName = @"pygmentize.exe";
            _proc.StartInfo.Arguments = $"-P outencoding=utf-8 -f 16m -O style={(string.IsNullOrEmpty(style) ? "default" : style)} -l {language} -o {file}.an {file}";
            _proc.StartInfo.RedirectStandardInput = true;
            _proc.StartInfo.RedirectStandardOutput = true;
            _proc.StartInfo.RedirectStandardError = true;
            _proc.StartInfo.UseShellExecute = false;
            _proc.StartInfo.CreateNoWindow = true;
            _proc.EnableRaisingEvents = true;
            _proc.Exited += Proc_Exited;

            _eventHandled = new TaskCompletionSource<bool>();

            try {
                Log.Debug("Writing temp file {file}", _proc.StartInfo.FileName);
                await File.WriteAllTextAsync(file, document, Encoding.UTF8).ConfigureAwait(true);
                Log.Debug("Starting {pyg} {args}", _proc.StartInfo.FileName, _proc.StartInfo.Arguments);
                _proc.Start();
            }
            catch (Exception e) {
                // TODO: Better error message (output of stderr?)
                Log.Information("Failed to pygmentize: {Message}", e.Message);
                document = $"{ _proc.StartInfo.FileName} { _proc.StartInfo.Arguments} failed:\n{e.Message}:\n";
            }
            finally {
                Log.Debug("Waiting for pygments to exit");
                await Task.WhenAny(_eventHandled.Task, Task.Delay(10000)).ConfigureAwait(true);

                if (_proc.ExitCode != 0) {
                    var stdErr = _proc.StandardError.ReadToEnd();
                    Log.Information("Failed to pygmentize: {Message}", stdErr);
                    // TODO: This should really throw an exception
                    document = $"Failed to pygmentize: {stdErr}";
                }
                else {

                    if (!string.IsNullOrEmpty($"{file}.an") && File.Exists($"{file}.an")) {
                        Log.Debug("Reading {file}", $"{file}.an");
                        document = await File.ReadAllTextAsync($"{file}.an", Encoding.UTF8).ConfigureAwait(true);

                        // HACK: Because of this bug: https://github.com/pygments/pygments/issues/1435
                        if (document[^1] == '\n')
                            document = document.Remove(document.Length - 1, 1);
                    }
                }

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
            Log.Debug("pygmatize exited: Time: {exitTime}, ExitCode: {exitCode}, ElapsedTime: {elapsedTime}ms", _proc.ExitTime, _proc.ExitCode, Math.Round((_proc.ExitTime - _proc.StartTime).TotalMilliseconds));

            _eventHandled.TrySetResult(true);
        }
    }
}

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Threading;
using WfComponent.Utils;

namespace ShotgunMetagenome.External
{
    // Singletonパターン?
    internal sealed class RequestCommand
    {

        // CovGAS WSL name
        public static readonly string wslname = "kali-linux-metagenome";　// wsl name
        public static readonly string wsluser = "user";

        private static RequestCommand instance = new RequestCommand();
        public static RequestCommand GetInstance() => instance;

        public int Pid;

        public Process process;
        private RequestCommand()
        {
            // initialization

        }
        public static string WslCommand = Environment.Is64BitProcess ?
                                                                    ConstantValues.x64WSL :
                                                                    ConstantValues.x86WSL;

        internal void CommandCancel()
        {
            if (this.process == null || this.process.HasExited) return;
            try
            {
                this.process.Kill();
                Thread.Sleep(500);

                this.process.Dispose();
            }
            catch
            {
                // todo nothing ....>
            }
        }


        internal bool ExecuteWSLCommand(string command, string arguments, ref string stdout, ref string stderr, string workdir = null, IProgress<string> progress = null)
        {
            /// var args = "bash -c \" "
            var args = " --distribution " + wslname   // 利用するWSL登録名
                             + " --user " + wsluser   // 使うWSLには ユーザ名が user で作成している。
                             + " -- "
                             + "source ~/.profile; "
                             + command
                             + " "
                             + arguments
                             + "";
            return ExecuteWinCommand(WslCommand, args, ref stdout, ref stderr, workdir);
        }

        // 全てのコマンドを集約
        internal bool ExecuteWinCommand(string command, string arguments, ref string stdout, ref string stderr, string workdir = null, IProgress<string> progress = null)
        {
            if(progress == null)
                progress = new Progress<string>(s => System.Diagnostics.Debug.WriteLine(s));

            var message = string.Empty;
            var logfile = FileUtils.GetUniqDateLogFile();
            FileUtils.WriteFileFromString(logfile,
                                                        "Execute  command" + Environment.NewLine +
                                                        " bin:  " + command + Environment.NewLine +
                                                        " args: " + arguments + Environment.NewLine,
                                                        ref message);

            workdir = workdir ?? Path.GetTempPath();
            var executelog = new List<string>();
            var isError = false;
            // command = "\"" + command + "\"";
            var startInfo = new ProcessStartInfo(@command, arguments)
            {
                ErrorDialog = true,
                WorkingDirectory = workdir,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                // StandardOutputEncoding = Encoding.UTF8,
                // StandardErrorEncoding = Encoding.UTF8,
                UseShellExecute = false,
                CreateNoWindow = true
            };
            var resstdout = new StringBuilder();
            var resstderr = new StringBuilder();
            var output = string.Empty;
            try
            {
                using (process = Process.Start(startInfo))
                {
                    Pid = process.Id;
                    process.BeginOutputReadLine();
                    process.BeginErrorReadLine();
                    process.OutputDataReceived += (sender, e) => { if (e.Data != null) { resstdout.Append(e.Data); } }; // 標準出力に書き込まれた文字列を取り出す
                    process.ErrorDataReceived += (sender, e) => { if (e.Data != null) { resstderr.Append(e.Data); } }; // 標準エラー出力に書き込まれた文字列を取り出す
                    process.OutputDataReceived += (sender, e) => { if (e.Data != null) { progress.Report(e.Data); } };
                    process.OutputDataReceived += (sender, e) => { if (e.Data != null) { progress.Report(e.Data); } };
                    process.WaitForExit();  // 取り敢えず延々とまつ
                    process.CancelOutputRead();
                    process.CancelErrorRead();
                }
            }
            catch (Exception e)   // SegmentationError とか Process-kill 
            {
                executelog.Add("-----   " + e.Message);
                executelog.Add("-----   " + e.StackTrace);
                isError = true;
            }

            stderr = resstderr.ToString();
            stdout = resstdout.ToString();
            executelog.Add(stdout);
            executelog.Add(stderr);
            if (isError) executelog.Add(ConstantValues.ErrorMessage);
            stdout += "\n" + string.Join("\n", executelog);
            // System.Diagnostics.Debug.WriteLine(stdout);
            // System.Diagnostics.Debug.WriteLine(stderr);

            FileUtils.WriteFile(logfile, executelog, ref message, true);
            return isError;
        }

        // Windowsコマンド　投げっぱなし版 (IGV起動とかに使う)
        public static string ExecCommandLeave(string command, string arguments)
        {
            var message = string.Empty; // init.

            // tmp directory
            var workdir = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
            var startInfo = new ProcessStartInfo(command, arguments)
            {
                ErrorDialog = true,
                WorkingDirectory = workdir,
                RedirectStandardOutput = false,
                RedirectStandardError = false,
                UseShellExecute = false,
                CreateNoWindow = true
            };
            try
            {
                using (var process = Process.Start(startInfo))
                {
                    // process.BeginOutputReadLine();
                    // process.BeginErrorReadLine();
                    // process.CancelOutputRead();
                    // process.CancelErrorRead();
                }
            }
            catch (Exception e)
            {
                message += "-----   Exception Message\n" + e.Message + "\n";
                message += "-----   Exception StackTrace\n" + e.StackTrace + "\n";
            }

            return message;   // 何も無ければ string.empty
        }

    }
}
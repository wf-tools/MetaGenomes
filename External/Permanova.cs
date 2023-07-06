using ShotgunMetagenome.Proc.Flow;
using ShotgunMetagenome.Several;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.IO;
using System.Linq;
using WfComponent.External;
using WfComponent.External.Properties;
using WfComponent.Utils;
using static WfComponent.Utils.FileUtils;

namespace ShotgunMetagenome.External
{
    public class Permanova : BaseProcess
    {
        // python permanova
        public static string binaryName = "python.exe";
        public static string permanovaScript = "do_permanova.py";
        public static string[] permutations = new string[] {"200", "500", "1000", "2000", "5000", "8000", "10000" };
        public static string defaultPermutations = permutations[2];

        private PermanovaOptions op;
        private RequestCommand proc;

        public Permanova(PermanovaOptions options) : base(options)
        {
            if (progress == null)
                progress = new Progress<string>(s => System.Diagnostics.Debug.WriteLine(s));

            this.op = options;
            progress.Report("permanova (python tool): ");

            if (!BinaryExist(binaryName))
                Message = "not found " + binaryName;

            var script = FindFile(
                Path.Combine(AnywayUtils.currentDir, "bin"),
                permanovaScript);  // python script
            if(script == null || !script.Any()) 
                Message = "not found " + permanovaScript;
            else 
                this.op.useTool = script.First();

            progress.Report(Message);
            isSuccess = string.IsNullOrEmpty(Message);
        }

        public override string StartProcess()
        {
            if (!isSuccess)
            {
                Message = "permanova Process is initialisation Error";
                return IFlow.ErrorEndMessage;
            }

            var args = new List<string>();
            args.Add(this.op.useTool);   // do_permanova.py

            args.Add("--file");   //
            args.Add(GetDoubleQuotationPath(op.matrixDataFile ));

            args.Add("--grouping_file");   //
            args.Add(GetDoubleQuotationPath(op.groupFile));

            args.Add("--permutations");   //
            args.Add(GetDoubleQuotationPath(op.permutations));

            args.Add("--out");   //
            args.Add(GetDoubleQuotationPath(op.outPath));

            SetArguments(string.Join(" ", args));  // command arguments

            var workDir = Path.GetDirectoryName(op.matrixDataFile);
            proc = RequestCommand.GetInstance();
            var commandRes = proc.ExecuteWinCommand(binaryPath, arguments, ref stdout, ref stderr, workDir);
            winProcessId = proc.Pid; // to kill process ?? 


            var message = string.Empty;
            // if (commandRes == false) StdError に出力があるから false になる。
            if (FileSize(op.matrixDataFile, ref message) < 10)
            {
                progress.Report("Permanova command.  return:  " + commandRes);
                isSuccess = false;
                return IFlow.ErrorEndMessage;
            }


            isSuccess = true;  // ここまでくれば。
            return IFlow.NormalEndMessage;
        }

        public override string StopProcess()
        {
            if (this.proc == null) return string.Empty;

            this.proc.CommandCancel();
            return ConstantValues.CanceledMessage;

        }

    }


    public class PermanovaOptions : BaseOptions
    {
         
        [Required()] // 試行回数
        public string permutations;

        [Required()] // matrix data file
        public string matrixDataFile;

        [Required()] // 1 vs 1 group... 
        public string groupFile;

        [Required()] // out text path (tabler)
        public string outPath;

        public string useTool;
    }
}

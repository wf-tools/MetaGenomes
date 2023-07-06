using ShotgunMetagenome.Proc.Flow;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.IO;
using WfComponent.External;
using WfComponent.External.Properties;
using WfComponent.Utils;
using static WfComponent.Utils.FileUtils;

namespace ShotgunMetagenome.External
{
    public class Kalign : BaseProcess
    {
        //  tool 
        public static string binaryName = "kalign.exe";
        public static string samtools = "samtools.exe";

        private KalignOptions op;
        private RequestCommand proc;


        public Kalign(KalignOptions options) : base(options)
        {
            if (progress == null)
                progress = new Progress<string>(s => System.Diagnostics.Debug.WriteLine(s));

            this.op = options;
            if (!BinaryExist(binaryName))
                progress.Report("not found " + binaryName + Environment.NewLine
                                        + Message);

            progress.Report("binary :" + binaryName);
            progress.Report("input pair-fasta " + op.pairseqPath);
            progress.Report("output align  " + op.outAlignPath);

            // 必須のパラメータをチェック
            if (string.IsNullOrEmpty(binaryPath) ||
                string.IsNullOrEmpty(op.pairseqPath) ||
                string.IsNullOrEmpty(op.outAlignPath))
                Message += "required parameter is not found, Please check parameters";

            progress.Report(Message);
            isSuccess = string.IsNullOrEmpty(Message);
        }  // init end.

        public override string StartProcess()
        {
            if (!isSuccess)
            {
                Message = "Process is initialisation Error";
                return IFlow.ErrorEndMessage;
            }

            var args = new List<string>();
            args.Add("-i ");  // input file.
            args.Add(GetDoubleQuotationPath(op.pairseqPath));

            args.Add("-o ");  // output file.
            args.Add(GetDoubleQuotationPath(op.outAlignPath));

            args.Add("-f fasta ");   // format
            SetArguments(string.Join(" ", args));  // command arguments

            var workDir = Path.GetDirectoryName(op.outAlignPath);
            if (!Directory.Exists(workDir))
                Directory.CreateDirectory(workDir);

            proc = RequestCommand.GetInstance();
            var commandRes = proc.ExecuteWinCommand(binaryPath, arguments, ref stdout, ref stderr, workDir);
            winProcessId = proc.Pid; // to kill process ?? 


            var message = string.Empty;
            if (FileSize(op.outAlignPath, ref message) < 10)
            {
                message += "command fail, " + op.outAlignPath + " is not created...." ;
                progress.Report("return:  " + commandRes);
                isSuccess = false;
                return IFlow.ErrorEndMessage;
            }

            isSuccess = true;
            return IFlow.NormalEndMessage;
        }

        public override string StopProcess()
        {
            if (this.proc == null) return string.Empty;

            this.proc.CommandCancel();
            return ConstantValues.CanceledMessage;

        }
    }


    public class KalignOptions : BaseOptions
    {
        [Required()]
        public string pairseqPath;

        [Required()]
        public string outAlignPath;


    }
}

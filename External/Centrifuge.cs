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
    public class Centrifuge : BaseProcess
    {
        //  taxonomy assignment
        public static string binaryName = "centrifuge-class.exe";
        private CentrifugeOptions op;
        private RequestCommand proc;

        public Centrifuge(CentrifugeOptions options) : base(options)
        {
            if (progress == null)
                progress = new Progress<string>(s => System.Diagnostics.Debug.WriteLine(s));

            this.op = options;

            progress.Report("binary :" + binaryName);
            progress.Report("db " + op.dbName);
            progress.Report("fwd " + op.fwdFastq);
            progress.Report("rev " + op.revFastq);
            progress.Report("un  " + op.unpaired);
            progress.Report("out " + op.outReportPath);

            if (!BinaryExist(binaryName))
                progress.Report("not found " + binaryName);

            // 必須のパラメータをチェック
            if (string.IsNullOrEmpty(binaryPath) ||
                string.IsNullOrEmpty(op.dbName) ||
                string.IsNullOrEmpty(op.outReportPath))  
                Message += "required parameter is not found, Please check parameters";


            progress.Report(Message);
            isSuccess = string.IsNullOrEmpty(Message);
        }

        // centrifuge-class 
        public override string StartProcess()
        {

            if (!isSuccess)
            {
                Message = "Centrifuge Process is initialisation Error";
                return IFlow.ErrorEndMessage;
            }

            // create command args.
            var args = new List<string>();
            args.Add("-x ");   // db
            args.Add(GetDoubleQuotationPath(op.dbName));
            args.Add("--threads " + op.threads);   // cpu

            if (!string.IsNullOrEmpty(op.fwdFastq) && op.fwdFastq.Length > 1)
                args.Add("-1 " + GetDoubleQuotationPath(op.fwdFastq));   // Foward
            if (!string.IsNullOrEmpty(op.revFastq) && op.revFastq.Length > 1)
                args.Add("-2 " + GetDoubleQuotationPath(op.revFastq));   // reverse
            if (!string.IsNullOrEmpty(op.unpaired) && op.unpaired.Length > 1)
                args.Add("-U " + GetDoubleQuotationPath(op.unpaired));   // unpair

            // report file.
            args.Add("-S ");   //  classification  map
            args.Add(GetDoubleQuotationPath(op.outClassificationPath));
            args.Add("--report ");   // report
            args.Add(GetDoubleQuotationPath(op.outReportPath));

            // args.Add("--met-file  ");   // meta report
            // args.Add(GetDoubleQuotationPath(
            // Path.ChangeExtension(op.outClassificationPath, ".debug")));


            SetArguments(string.Join(" ", args));  // command arguments


            var workDir = Path.GetDirectoryName(op.outReportPath);
            proc = RequestCommand.GetInstance();
            var commandRes = proc.ExecuteWinCommand(binaryPath, arguments, ref stdout, ref stderr, workDir);
            winProcessId = proc.Pid; // to kill process ?? 


            var message = string.Empty;
            // if (commandRes == false) StdError に出力があるから false になる。
            if (FileSize(op.outReportPath, ref message) < 10)
            {
                message += "Centrifuge command was not create taxonomy-report , ";
                progress.Report("error, Centrifuge command.  return:  " + commandRes);
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

    public class CentrifugeOptions : BaseOptions
    {
        [Required()]
        public string dbName;

        [Required()]
        public string outReportPath;  
        public string outClassificationPath; // for recentrifuge
        public string fwdFastq;
        public string revFastq;
        public string unpaired;

        public string threads;
    }
}

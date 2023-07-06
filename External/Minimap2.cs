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
    public class Minimap2 : BaseProcess
    {
        //  mapping tool 
        public static string binaryName = "minimap2.exe";
        private Minimap2Options op;
        private RequestCommand proc;

        public Minimap2(Minimap2Options options) : base(options)
        {
            if (progress == null)
                progress = new Progress<string>(s => System.Diagnostics.Debug.WriteLine(s));

            this.op = options;
            if (!BinaryExist(binaryName))
                progress.Report("not found " + binaryName + Environment.NewLine
                                        + Message);



            progress.Report("binary :" + binaryName);
            progress.Report("db " + op.dbName);
            progress.Report("fwd " + op.fwdFastq);
            progress.Report("rev " + op.revFastq);
            progress.Report("out " + op.outSamPath);


            // 必須のパラメータをチェック
            if (string.IsNullOrEmpty(binaryPath) ||
                string.IsNullOrEmpty(op.dbName) ||
                string.IsNullOrEmpty(op.outSamPath))
                Message += "required parameter is not found, Please check parameters";


            progress.Report(Message);
            isSuccess = string.IsNullOrEmpty(Message);
        }

        public override string StartProcess()
        {

            if (!isSuccess)
            {
                Message = "Mapping Process is initialisation Error";
                return IFlow.ErrorEndMessage;
            }

            var preset = op.isLongRead ? "map-ont" : "sr"; // 通常はIllumina=sr
            var splitOption = op.isLargeReference ? "--split-prefix=./" : "";

            // create command args.
            // minimap2 --split-prefix=./  -t 4 -ax sr %REF1% %QRY1% %QRY2% -o aln1-minimap.sam
            var args = new List<string>();
            args.Add(splitOption);   // おまじない（配列数が大きいReferenceはSAMにヘッダがなくなる）

            args.Add("-t " + op.threads);   // cpu
            args.Add("-ax " + preset);   // query type
            args.Add(GetDoubleQuotationPath(op.dbName)); // reference

            args.Add(GetDoubleQuotationPath(op.fwdFastq));
            if(! string.IsNullOrEmpty(op.revFastq))
                args.Add(GetDoubleQuotationPath(op.revFastq));

            args.Add("-o ");
            args.Add(GetDoubleQuotationPath(op.outSamPath));
            SetArguments(string.Join(" ", args));  // command arguments
            
            var workDir = Path.GetDirectoryName(op.outSamPath);
            if(! Directory.Exists(workDir))
                Directory.CreateDirectory(workDir); 

            proc = RequestCommand.GetInstance();
            var commandRes = proc.ExecuteWinCommand(binaryPath, arguments, ref stdout, ref stderr, workDir);
            winProcessId = proc.Pid; // to kill process ?? 


            var message = string.Empty;
            if (FileSize(op.outSamPath, ref message) < 10)
            {
                message += "mapping command fail, ";
                progress.Report("error, minimap2 command.  return:  " + commandRes);
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

    public class Minimap2Options : BaseOptions 
    {
        [Required()]
        public string dbName;  // reference fasta. 

        [Required()]
        public string outSamPath;

        public bool isLongRead = false;

        public string fwdFastq;
        public string revFastq;

        public bool isLargeReference = false;
        public string threads = ProcessUtils.CpuCore(); // default
    }

}

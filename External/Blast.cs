using ShotgunMetagenome.Proc.Flow;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.IO;
using WfComponent.External;
using WfComponent.External.Properties;
using WfComponent.Utils;
using static WfComponent.Utils.FileUtils;
using static ShotgunMetagenome.Several.AnywayUtils;

namespace ShotgunMetagenome.External
{
    public class Blast : BaseProcess
    {
        //  tool 
        public static string binaryName = "blastx.exe";

        private BlastOptions op;
        private RequestCommand proc;

        public static string blastdbDir = "blastdb";
        public static string blastdbName = "cog";

        public Blast(BlastOptions options) : base(options)
        {
            if (progress == null)
                progress = new Progress<string>(s => System.Diagnostics.Debug.WriteLine(s));

            this.op = options;
            if (!BinaryExist(binaryName))
                progress.Report("not found " + binaryName + Environment.NewLine
                                        + Message);


            progress.Report("binary :" + binaryName);
            progress.Report("blast-db " + op.referendeDb);
            progress.Report("query " + op.queryFasta);

            // 必須のパラメータをチェック
            if (string.IsNullOrEmpty(binaryPath) ||
                string.IsNullOrEmpty(op.referendeDb) ||
                string.IsNullOrEmpty(op.outBlastResult) ||
                string.IsNullOrEmpty(op.queryFasta))
                Message += "required parameter is not found, Please check parameters";

            progress.Report(Message);
            isSuccess = string.IsNullOrEmpty(Message);
        }  // init end.

        public override string StartProcess()
        {
            if (!isSuccess)
            {
                Message = "bcftools(create vcf and consensus) Process is initialisation Error";
                return IFlow.ErrorEndMessage;
            }

            // db name 
            var db = Path.Combine(currentDir, dataDir, blastdbDir, blastdbName);
            if(! File.Exists(db + ".psq"))   // blastdb の代表ファイル
            {
                Message += "not found cog database, " + db;
                return IFlow.ErrorEndMessage;
            }

            var args = new List<string>();
            args.Add("-query");  // 
            args.Add(GetDoubleQuotationPath(op.queryFasta));

            args.Add("-db");  // 
            args.Add(GetDoubleQuotationPath(db));

            args.Add("-out");  // 
            args.Add(GetDoubleQuotationPath(op.outBlastResult));

            args.Add("-evalue");  // 
            args.Add(GetDoubleQuotationPath(op.evalue));

            args.Add("-num_threads " + op.threads);   // cpu

            args.Add("-outfmt 6");  // 
            args.Add("-max_target_seqs ");  // 
            args.Add(op.maxTargetSeq);

            SetArguments(string.Join(" ", args));  // command arguments

            var workDir = Path.GetDirectoryName(op.outBlastResult);
            if (!Directory.Exists(workDir))
                Directory.CreateDirectory(workDir);

            proc = RequestCommand.GetInstance();
            var commandRes = proc.ExecuteWinCommand(binaryPath, arguments, ref stdout, ref stderr, workDir);
            winProcessId = proc.Pid; // to kill process ?? 


            var message = string.Empty;
            if (FileSize(op.outBlastResult, ref message) < 10)
            {
                message += "command fail, ";
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


    public class BlastOptions : BaseOptions
    {
        public string referendeDb = Blast.blastdbName;

        [Required()]
        public string queryFasta;

        [Required()]
        public string outBlastResult;


        public string  threads = ProcessUtils.CpuCore(); // default

        public string evalue = "2e-10";
        public string maxTargetSeq = "30";

        
    }
}

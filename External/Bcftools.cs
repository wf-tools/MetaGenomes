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
    public class Bcftools : BaseProcess
    {
        //  tool 
        public static string binaryName = "bcftools.exe";
        public static string samtools = "samtools.exe";
            
        private BcftoolsOptions op;
        private RequestCommand proc;

        private string sortBam;
        private string preBam;
        private string outVcf;
        private string outBcf;
        private string outFilterVcf;

        public Bcftools(BcftoolsOptions options) : base(options)
        {
            if (progress == null)
                progress = new Progress<string>(s => System.Diagnostics.Debug.WriteLine(s));

            this.op = options;
            if (!BinaryExist(binaryName))
                progress.Report("not found " + binaryName + Environment.NewLine
                                        + Message);

            progress.Report("binary :" + binaryName);
            progress.Report("db " + op.reference);
            progress.Report("fwd " + op.samFile);

            // 必須のパラメータをチェック
            if (string.IsNullOrEmpty(binaryPath) ||
                string.IsNullOrEmpty(op.reference) ||
                string.IsNullOrEmpty(op.samFile))
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

            this.sortBam = op.samFile + ".sort.bam";
            this.preBam = op.samFile + ".pre.bam";
            this.outVcf = op.samFile + ".vcf.gz";
            this.outBcf = op.samFile + ".bcf";
            this.outFilterVcf = op.samFile + ".flt.vcf.gz";


            var bcftoolsPath = binaryPath;
            var cmdRes = string.Empty;

            cmdRes = SamtoolsSortCmd(); // samtools sort...
            if (cmdRes == IFlow.ErrorEndMessage)
                return IFlow.ErrorEndMessage;


            this.binaryPath = bcftoolsPath;
            cmdRes = BcftoolsMpileupCmd();
            if (cmdRes == IFlow.ErrorEndMessage)
                return IFlow.ErrorEndMessage;

            cmdRes = BcftoolsCallCmd();
            if (cmdRes == IFlow.ErrorEndMessage)
                return IFlow.ErrorEndMessage;

            cmdRes = BcftoolsNormCmd();
            if (cmdRes == IFlow.ErrorEndMessage)
                return IFlow.ErrorEndMessage;

            cmdRes = BcftoolsFilterCmd();
            if (cmdRes == IFlow.ErrorEndMessage)
                return IFlow.ErrorEndMessage;

            cmdRes = BcftoolsIndexCmd();
            if (cmdRes == IFlow.ErrorEndMessage)
                return IFlow.ErrorEndMessage;

            cmdRes = BcftoolsConsensusCmd();
            if (cmdRes == IFlow.ErrorEndMessage)
                return IFlow.ErrorEndMessage;

            isSuccess = true;
            return IFlow.NormalEndMessage;
        }

        private string SamtoolsSortCmd() // pre command.
        {
            var samtoolsPath = Path.Combine(
                                            Path.GetDirectoryName(binaryPath),
                                            samtools);
            if (!File.Exists(samtoolsPath))
            {
                progress.Report("not found samtools, " + Path.GetDirectoryName(binaryPath));
                return IFlow.ErrorEndMessage;
            }

            this.binaryPath = samtoolsPath;

            var args = new List<string>();
            args.Add("sort -O bam ");
            args.Add("-o ");  // out
            args.Add(GetDoubleQuotationPath(this.sortBam));
            args.Add("--threads " + op.threads);   // cpu
            args.Add(GetDoubleQuotationPath(op.samFile));

            SetArguments(string.Join(" ", args));  // command arguments
            return DoCommand(op.samFile);
        }

        private string BcftoolsMpileupCmd()  // 1st bcftools, create vcf from sam-file.
        {
            // bcftools mpileup -Ou -f %REF2% -o %SAM3%.pre.bam %SAM3%
            var args = new List<string>();
            args.Add("mpileup -Ou");
            args.Add("-f ");   // reference
            args.Add(GetDoubleQuotationPath(op.reference));
            args.Add("-o ");  // out
            args.Add(GetDoubleQuotationPath(this.preBam));
            args.Add(GetDoubleQuotationPath(op.samFile));

            SetArguments(string.Join(" ", args));  // command arguments
            return DoCommand(this.preBam);
        }

        private string BcftoolsCallCmd()  // 2nd bcftools, create vcf call.
        {
            var args = new List<string>();
            args.Add("call  -mv -Oz");
            args.Add("-o ");  // out
            args.Add(GetDoubleQuotationPath(this.outVcf));
            args.Add(GetDoubleQuotationPath(this.preBam));

            SetArguments(string.Join(" ", args));  // command arguments
            return DoCommand(this.outVcf);
        }

        private string BcftoolsNormCmd() // 3rd bcftools, vcf Normalize
        {
            var args = new List<string>();
            args.Add("norm -Ob");
            args.Add("-f ");   // reference
            args.Add(GetDoubleQuotationPath(op.reference));
            args.Add("-o ");  // out
            args.Add(GetDoubleQuotationPath(this.outBcf));
            args.Add(GetDoubleQuotationPath(this.outVcf)); // target.

            SetArguments(string.Join(" ", args));  // command arguments
            return DoCommand(this.outBcf);
        }

        private string BcftoolsFilterCmd()  // 4th bcftools, vcf filter.
        {
            var args = new List<string>();
            args.Add("filter --IndelGap 5  -Oz");
            args.Add("-o ");  // out
            args.Add(GetDoubleQuotationPath(this.outFilterVcf));
            args.Add(GetDoubleQuotationPath(this.outBcf)); // target.

            SetArguments(string.Join(" ", args));  // command arguments
            return DoCommand(this.outFilterVcf);
        }

        private string BcftoolsIndexCmd()  // 5th bctools, index 
        {
            var args = new List<string>();
            args.Add("index");
            args.Add(GetDoubleQuotationPath(this.outFilterVcf)); // target.

            SetArguments(string.Join(" ", args));  // command arguments
            return DoCommand(this.outFilterVcf);
        }

        private string BcftoolsConsensusCmd()  // 6th bcftools, consensus call.
        {
            var args = new List<string>();
            args.Add("consensus ");
            args.Add("-f ");  // out
            args.Add(GetDoubleQuotationPath(op.reference));
            args.Add("-o ");  // out
            args.Add(GetDoubleQuotationPath(op.outConsensus));
            args.Add(GetDoubleQuotationPath(this.outFilterVcf)); // target.

            SetArguments(string.Join(" ", args));  // command arguments
            return DoCommand(op.outConsensus);
        }


        private string DoCommand(string probe)
        {
            var workDir = Path.GetDirectoryName(op.outConsensus);
            if (!Directory.Exists(workDir))
                Directory.CreateDirectory(workDir);

            proc = RequestCommand.GetInstance();
            var commandRes = proc.ExecuteWinCommand(binaryPath, arguments, ref stdout, ref stderr, workDir);
            winProcessId = proc.Pid; // to kill process ?? 


            var message = string.Empty;
            if (FileSize(probe, ref message) < 10)
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


    public class BcftoolsOptions : BaseOptions
    {
        [Required()]
        public string samFile;

        [Required()]
        public string reference;

        public string outConsensus;

        public string threads = ProcessUtils.CpuCore(); // default
    }
}

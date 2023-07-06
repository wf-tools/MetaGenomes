using ShotgunMetagenome.Proc.Flow;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.IO;
using WfComponent.External;
using WfComponent.External.Properties;
using WfComponent.Utils;
using static WfComponent.Utils.FileUtils;

namespace ShotgunMetagenome.External
{

    public class Kraken2 : BaseProcess
    {
        // pangolin tool
        public static string binaryName = "kraken2";
        private Kraken2Option op;
        private RequestCommand proc;
        public Kraken2(Kraken2Option options) : base(options)
        {
            this.op = options;
            // 必須のパラメータをチェック
            if (string.IsNullOrEmpty(op.dbName) ||
                string.IsNullOrEmpty(op.outTaxnomyPath) ||
                string.IsNullOrEmpty(op.fwdFastq) ||
                string.IsNullOrEmpty(op.outReportPath))
                Message += "required parameter is not found, Please check parameters";


            progress.Report(Message);
            isSuccess = string.IsNullOrEmpty(Message);
        }

        public override string StartProcess()
        {
            if (!isSuccess)
            {
                Message = "Kraken2 Process is initialisation Error";
                return IFlow.ErrorEndMessage;
            }

            var message = string.Empty;
            // create command string.　
            // wsl -d %DST% --user user -- source ~/.profile; 
            // kraken2 --db $DBNAME seqs.fa


            //  kraken2--db minikraken_8GB_20200312_k2db
            // --threads 4                 #number of threads
            // --report SAMPLE.kreport2    #kraken-style report (REQUIRED FOR BRACKEN)
            // --paired SAMPLE_1.fq SAMPLE_2.fq > SAMPLE.kraken2

            // fastq は Trimmomatic の結果なので gz していない。
            var queryFastqs = string.IsNullOrEmpty(op.revFastq)
                                        ? FileUtils.GetDoubleQuotationPath(WindowsPath2LinuxPath(op.fwdFastq ))     // single-end.
                                        : "--paired " + WindowsPath2LinuxPath(FileUtils.GetDoubleQuotationPath(op.fwdFastq ))+ " " 
                                                          + WindowsPath2LinuxPath(FileUtils.GetDoubleQuotationPath(op.revFastq ));

            var cpus = string.IsNullOrEmpty(op.threads)
                                        ? ProcessUtils.DefaultCpuCore().ToString()
                                        : op.threads;

            this.binaryPath = RequestCommand.WslCommand;


            // create command args.
            var args = new List<string>();
            args.Add("-d " + RequestCommand.wslname + "  --user user -- ");
            args.Add(" source ~/.profile;");
            args.Add("kraken2");
            args.Add("--db ");   // db
            args.Add(GetDoubleQuotationPath(
                            WindowsPath2LinuxPath(op.dbName)));
            args.Add("--threads " + cpus);   // cpu
            args.Add("--report ");   // kraken report
            args.Add(GetDoubleQuotationPath(
                            WindowsPath2LinuxPath(op.outReportPath)));
            args.Add("--output ");   // kraken taxonomy
            args.Add(GetDoubleQuotationPath(
                            WindowsPath2LinuxPath(op.outTaxnomyPath)));
            args.Add(queryFastqs);  // fastq query.


            SetArguments(string.Join(" ", args));  // command arguments

            var workDir = Path.GetDirectoryName(op.fwdFastq);
            proc = RequestCommand.GetInstance();
            var commandRes = proc.ExecuteWinCommand(binaryPath, arguments, ref stdout, ref stderr, workDir);
            winProcessId = proc.Pid; // to kill process ?? 

            // if (commandRes == false) StdError に出力があるから false になる。
            if (FileSize(op.outReportPath, ref message) < 100)
            {
                message += "kraken2 command was not create taxonomy, ";
                progress.Report("error, kraken2 command.  return:" + commandRes );
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

    public class Kraken2Option : BaseOptions
    {
        [Required()]
        public string dbName;
        [Required()]
        public string outTaxnomyPath;
        [Required()]
        public string outReportPath;  // for braken, recentrifuge

        [Required()]
        public string fwdFastq;
        public string revFastq;

        public string threads;

    }

}

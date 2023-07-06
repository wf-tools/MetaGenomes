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

    public class Bracken : BaseProcess
    {
        // pangolin tool
        public static string binaryName = "braken";
        private BrakenOption op;
        private RequestCommand proc;
        public Bracken(BrakenOption options) : base(options)
        {
            this.op = options;

            // 必須のパラメータをチェック
            if (string.IsNullOrEmpty(op.dbName) ||
                string.IsNullOrEmpty(op.inKrakenReportPath) ||
                string.IsNullOrEmpty(op.outKrakenReportPath) ||
                string.IsNullOrEmpty(op.outBrackenReportPath))
                Message += "required parameter is not found, Please check parameters";


            progress.Report(Message);
            isSuccess = string.IsNullOrEmpty(Message);
        }

        public override string StartProcess()
        {
            if (!isSuccess)
            {
                Message = "Braken Process is initialisation Error";
                return ConstantValues.ErrorMessage;
            }

            var message = string.Empty;
            // create command string.　
            // wsl -d %DST% --user user -- source ~/.profile; 
            //bracken - d $KRKN2DB - i krk2.report - o krk2.bracken - r 100 - l $CLASSIFICATION_LEVEL - t $THRESHOLD

            // wsl
            this.binaryPath = RequestCommand.WslCommand;

            // create command args.
            var args = new List<string>();
            args.Add("-d " + RequestCommand.wslname + "  --user user -- ");
            args.Add(" source ~/.profile;");
            args.Add("bracken");
            args.Add("-d ");   // bracken database, use kraken command.
            args.Add(GetDoubleQuotationPath(
                WindowsPath2LinuxPath(op.dbName)));
            args.Add("-r " + op.read_len);
            args.Add("-l " + op.level);
            args.Add("-t " + op.threshold);

            args.Add("-i ");   // input kraken report
            args.Add(GetDoubleQuotationPath(
                            WindowsPath2LinuxPath(op.inKrakenReportPath)));


            args.Add("-o ");   // out new kraken taxonomy report
            args.Add(GetDoubleQuotationPath(
                            WindowsPath2LinuxPath(op.outKrakenReportPath)));
            args.Add("-w ");   // bracken report
            args.Add(GetDoubleQuotationPath(
                            WindowsPath2LinuxPath(op.outBrackenReportPath)));

            SetArguments(string.Join(" ", args));  // blast arguments

            var workDir = Path.GetDirectoryName(op.inKrakenReportPath);
            proc = RequestCommand.GetInstance();
            var commandRes = proc.ExecuteWinCommand(binaryPath, arguments, ref stdout, ref stderr, workDir);
            winProcessId = proc.Pid; // to kill process ?? 

            // if (commandRes == false) StdError に出力があるから false になる。
            if (FileSize(op.outBrackenReportPath, ref message) < 100)
            {
                message += "bracken command was not create taxonomy, ";
                progress.Report("error, bracken command.  return:" + commandRes);
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

    public class BrakenOption : BaseOptions
    {
        [Required()]
        public string dbName;
        [Required()]
        public string inKrakenReportPath;
        [Required()]
        public string outBrackenReportPath;
        [Required()]
        public string outKrakenReportPath;  // for  recentrifuge

        public string read_len = "100";   // read length to get all classifications for (default: 100);
        public string level = "S";  // level to estimate abundance at [options: D,P,C,O,F,G,S,S1,etc] (default: S)
        public string threshold = "0";   //  number of reads required PRIOR to abundance estimation to perform reestimation (default: 0)

    }


}

using ShotgunMetagenome.Proc.Flow;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.IO;
using System.Linq;
using WfComponent.External;
using WfComponent.External.Properties;
using WfComponent.Utils;
using static ShotgunMetagenome.Several.AnywayUtils;
using static WfComponent.Utils.FileUtils;

namespace ShotgunMetagenome.External
{

    public class Recentrifuge : BaseProcess
    {
        // Recentrifuge tool
        public static string binaryName = "python.exe";
        public static string reportsDir = "reports";
        public static string toolName = "rcf";
        public static string taxonomy = "nodes.dmp";
        private RecentrifugeOption op;
        private RequestCommand proc;

        public string outHtml;
        public Recentrifuge(RecentrifugeOption option) :base(option)
        {
            this.op = option;
            // 必須のパラメータをチェック
            if (string.IsNullOrEmpty(op.outPrefix) ||
                (op.inReports == null | !op.inReports.Any()))
                Message += "required parameter is not found, Please check parameters";

            if (!BinaryExist(binaryName))
                Message += "required program is not found, Please check installed environment";

            progress.Report(Message);
            isSuccess = string.IsNullOrEmpty(Message);
        }

        public override string StartProcess()
        {
            if (!isSuccess)
            {
                Message = "recentrifuge Process is initialisation Error, ";
                return IFlow.ErrorEndMessage;
            }

            // search rcf
            var rcfSeach = Directory.GetFiles(
                                            Path.GetDirectoryName(binaryPath),
                                            toolName,
                                            SearchOption.AllDirectories);
            if (rcfSeach == null || !rcfSeach.Any())
            {
                Message = "Error, recentrifuge script is not found. " + toolName;
                return IFlow.ErrorEndMessage;
            }

            var txnSearch = Directory.GetFiles(
                                            Path.Combine(currentDir, dataDir),
                                            taxonomy,
                                            SearchOption.AllDirectories);

            if (txnSearch == null || !txnSearch.Any())
            {
                Message = "Error, ncbi taxonomy files are not found. " + taxonomy;
                return IFlow.ErrorEndMessage;
            }

            // 出力先
            var reportOut = Path.Combine(op.outDir, reportsDir);
            if (! Directory.Exists(op.outDir))
                Directory.CreateDirectory(op.outDir);
            if (! Directory.Exists(reportOut))
                Directory.CreateDirectory(reportOut);

            var outHtml = Path.Combine(op.outDir, op.outPrefix);

            var reports = op.inReports.Select(s => s.Replace(op.outDir + "\\",  ""));


            // create command args.
            var args = new List<string>();
            args.Add(GetDoubleQuotationPath(rcfSeach.First()));        // rcf is python script...
            args.Add("-x 9606 "); // excluding human
            args.Add("-n");
            args.Add(GetDoubleQuotationPath(Path.GetDirectoryName(txnSearch.First())));
            args.Add("-o");
            args.Add(GetDoubleQuotationPath(outHtml));

            // foreach (var rep in op.inReports)
            foreach (var rep in reports)
            {
                args.Add("-f");   //  Centrifuge output files
                args.Add(GetDoubleQuotationPath(rep));
            }

            // binaryPath = "export LC_CTYPE = en_US.UTF-8;" + binaryPath;
            SetArguments(string.Join(" ", args));  // command arguments

            progress.Report("work directory is " + op.outDir);
            
            proc = RequestCommand.GetInstance();
            var commandRes = proc.ExecuteWinCommand(binaryPath, arguments, ref stdout, ref stderr, op.outDir);

            progress.Report(stdout);
            progress.Report(stderr);

            winProcessId = proc.Pid; // to kill process ?? 


            outHtml = Path.Combine(
                                        op.outDir,
                                        op.outPrefix + ".rcf.html");

            progress.Report("out put html  : " + outHtml);
            if (!File.Exists(outHtml))
            {
                this.logMessage += "Recentrifuge command was not create taxonomy, ";
                progress.Report("error, Recentrifuge command.  return: " + commandRes);
                isSuccess = false;
                return IFlow.ErrorEndMessage;
            }

            isSuccess = true;
            progress.Report("Recentrifuge command end. ");

            // 正常終了
            return IFlow.NormalEndMessage;
        }

        public override string StopProcess()
        {
            if (this.proc == null) return string.Empty;

            this.proc.CommandCancel();
            return ConstantValues.CanceledMessage;
        }

    }

    public class RecentrifugeOption : BaseOptions
    {
        [Required()]
        public string outDir;
        [Required()]
        public string outPrefix;
        [Required()]
        public IEnumerable<string> inReports;

        public string addOutType;

    }


}

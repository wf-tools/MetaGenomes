using ShotgunMetagenome.Proc.Flow;
using ShotgunMetagenome.Several;
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

    public class RecentrifugeWsl : BaseProcess
    {
        // tools
        public static string binaryName = "rcf ";
        private RecentrifugeOption op;
        private RequestCommand proc;
        public RecentrifugeWsl(RecentrifugeOption options) : base(options)
        {
            this.op = options;
            // 必須のパラメータをチェック
            if (string.IsNullOrEmpty(op.outPrefix) ||
                (op.inReports == null | ! op.inReports.Any()))
                Message += "required parameter is not found, Please check parameters";


            progress.Report(Message);
            isSuccess = string.IsNullOrEmpty(Message);
        }

        public override string StartProcess()
        {
            if (!isSuccess)
            {
                Message = "Recentrifuge Process is initialisation Error";
                return ConstantValues.ErrorMessage;
            }

            var message = string.Empty;
            // create command string.　
            // wsl -d %DST% --user user -- source ~/.profile; 

            // usage: rcf[-h][-V][-n PATH][--format GENERIC_FORMAT]
            //   (-f FILE | -g FILE | -l FILE | -r FILE | -k FILE)[-o FILE]
            //   [-e OUTPUT_TYPE][-p][--nohtml][-a | -c CONTROLS_NUMBER]
            //   [-s SCORING][-y NUMBER][-m INT][-x TAXID][-i TAXID][-z NUMBER]
            //   [-w INT][-u SUMMARY_BEHAVIOR][-t][--nokollapse][-d][--strain]
            //   [--sequential]

            // rcf - k S1.krk - k S2.krk - k S3.krk

            //  nodes option.
            var ncbiTaxonmy = string.Empty;
            var datDir = Path.Combine(
                                        AnywayUtils.currentDir,
                                        "data");
            var nodespath = FindFile(datDir, "nodes.dmp");
            if (nodespath != null && nodespath.Any() && !string.IsNullOrEmpty(nodespath.First())) {
                progress.Report("found ncbi taxnomy, " + nodespath.First());
                ncbiTaxonmy = Path.GetDirectoryName(nodespath.First());
            }
            else
            {
                progress.Report ("not found ncbi-taxonomy, nodes.dmp. Please check data folder.");
                return IFlow.ErrorEndMessage;
            }

            var reports = op.inReports.Select(s => s.Replace(op.outDir, "."));

            // wsl 
            this.binaryPath = RequestCommand.WslCommand;

            // create command args.
            var args = new List<string>();
            args.Add("-d " + RequestCommand.wslname + "  --user user -- ");
            args.Add(" source ~/.profile;");
            args.Add("rcf");
            args.Add("-n ");   // (nodes.dmp and names.dmp from NCBI)
            args.Add(GetDoubleQuotationPath(
                            WindowsPath2LinuxPath(ncbiTaxonmy)));

            foreach(var krk in reports)
            {
                args.Add("-k ");   //  Kraken output files
                args.Add(GetDoubleQuotationPath(
                                WindowsPath2LinuxPath(krk)));
            }

            args.Add("-o ");   // html 
            args.Add(op.outPrefix); 
            SetArguments(string.Join(" ", args));  // blast arguments

            proc = RequestCommand.GetInstance();
            var commandRes = proc.ExecuteWinCommand(binaryPath, arguments, ref stdout, ref stderr, op.outDir);
            winProcessId = proc.Pid; // to kill process ?? 

            var outHtml = Path.Combine(
                                        op.outDir,
                                        op.outPrefix + ".rcf.html");

            progress.Report("out put html  : " + outHtml);
            if (! File.Exists(outHtml))
            {
                message += "Recentrifuge command was not create taxonomy, ";
                progress.Report("error, Recentrifuge command.  return:" + commandRes);
                isSuccess = false;
                return IFlow.ErrorEndMessage;
            }

            isSuccess = true;
            progress.Report("Recentrifuge command end. ");
            return IFlow.NormalEndMessage;
        }

        public override string StopProcess()
        {
            if (this.proc == null) return string.Empty;

            this.proc.CommandCancel();
            return ConstantValues.CanceledMessage;
        }

    }

    public class RecentrifugeWslOption : BaseOptions
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

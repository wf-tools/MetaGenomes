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
    public class PcoaR : BaseProcess
    {
        // Rscript nMDS
        public static string binaryName = "Rscript.exe";
        public static string pcoaName =  "pcoa-plot.R";
        public static string nmdsName = "nmds-plot.R";

        private PcoaROptions op;
        private RequestCommand proc;
        private string rscript;

        public PcoaR(PcoaROptions options) : base(options)
        {
            if (progress == null)
                progress = new Progress<string>(s => System.Diagnostics.Debug.WriteLine(s));

            this.op = options;
            progress.Report("PCoA (R): ");

            if (!BinaryExist(binaryName))
                progress.Report("not found " + binaryName);

            var scripts = FindFile(
                                    Path.Combine(AnywayUtils.currentDir, "bin"),
                                    pcoaName);
            if (!scripts.Any() || string.IsNullOrEmpty(scripts.First()))
                Message += "not found " + op.metric + Environment.NewLine;
            else
                this.rscript = scripts.First();

            progress.Report(Message);
            isSuccess = string.IsNullOrEmpty(Message);

        }

        public override string StartProcess()
        {
            if (!isSuccess)
            {
                Message = "PCoA Process is initialisation Error";
                return IFlow.ErrorEndMessage;
            }

            var datFile = op.matrixDataFile.Replace("\\", "/");
            var outImg = op.outImage.Replace("\\", "/");

            // create command args.
            var args = new List<string>();
            args.Add(rscript);   // pcoa-plot.R
            args.Add(GetDoubleQuotationPath(datFile));
            args.Add(GetDoubleQuotationPath(outImg));

            SetArguments(string.Join(" ", args));  // command arguments


            var workDir = Path.GetDirectoryName(op.matrixDataFile);
            proc = RequestCommand.GetInstance();
            var commandRes = proc.ExecuteWinCommand(binaryPath, arguments, ref stdout, ref stderr, workDir);
            winProcessId = proc.Pid; // to kill process ?? 


            var message = string.Empty;
            // if (commandRes == false) StdError に出力があるから false になる。
            if (FileSize(op.matrixDataFile, ref message) < 10)
            {
                progress.Report("PCoA (Rscript) command.  return:  " + commandRes);
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

    public class PcoaROptions : BaseOptions
    {
        [Required()]
        public string outImage;
        [Required()] // matrix data file
        public string matrixDataFile;

        public string title;
        public string metric;  // PCoA, nMDS, 

    }
}

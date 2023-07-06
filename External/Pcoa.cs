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
    public class Pcoa : BaseProcess
    {
        // python PCoA
        public static string binaryName = "python.exe";
        public static string pcoaScript = "do_pcoa.py";
        public static string nmdsScript = "do_nmds.py";
        public static string pcoaHtml = "pcoa_plot.html";
        public static string nmdsHtml = "nmds_plot.html";

        public static string[] pcoaDistanceMetric = new string[] { "Jaccard", "BrayCurtis", "JSD" };
        public static string[] nmdsDistanceMetric = new string[] { "Pearson", "Bray-Curtis", "Morishita-Horn" };

        public static string defaultPcoaDistanceMetric = pcoaDistanceMetric.First();
        public static string defaultNmdsDistanceMetric = nmdsDistanceMetric.First();
        public static string defaultDistanceMetric = pcoaDistanceMetric[1]; // BrayCurtis  // 共通？

        private string useTools;
        private PcoaOptions op;
        private RequestCommand proc;

        public Pcoa(PcoaOptions options) : base(options)
        {
            if (progress == null)
                progress = new Progress<string>(s => System.Diagnostics.Debug.WriteLine(s));

            this.op = options;
            progress.Report( op.useTools + " (python): ");

            if (!BinaryExist(binaryName))
                progress.Report("not found " + binaryName);

            // 必須のパラメータをチェック
            if (string.IsNullOrEmpty(binaryPath))
                Message += "not found  python binary," + Environment.NewLine;

            if (string.IsNullOrEmpty(op.matrixDataFile))
                Message += "not set report file ," + Environment.NewLine;

            var scripts = FindFile(
                                        Path.Combine(AnywayUtils.currentDir, "bin"),
                                        op.useTools);

            if ( !scripts.Any() || string.IsNullOrEmpty(scripts.First()))
                Message += "not found " + useTools + Environment.NewLine;
            else
                this.useTools = scripts.First(); 

            // TODO distance-method check??

            progress.Report(Message);
            isSuccess = string.IsNullOrEmpty(Message);
        }


        public override string StartProcess()
        {

            if (!isSuccess)
            {
                Message = "nMDS Process is initialisation Error";
                return IFlow.ErrorEndMessage;
            }

            var distance = string.IsNullOrEmpty(op.distanceMetric) 
                                        ? defaultDistanceMetric
                                        : op.distanceMetric;
            progress.Report(useTools +  "  distance " + distance);

            var colors = (op.colors == null || !op.colors.Any()) 
                                ?  new string[] { "r", "g", "b", "c", "m", "y", "k" }
                                : op.colors;

            // create command args.
            var args = new List<string>();
            args.Add(useTools);   // do_pcoa.py || do_nmds.py

            args.Add("--file");   //
            args.Add(GetDoubleQuotationPath(op.matrixDataFile));

            args.Add("--distance_metric");   //
            args.Add(distance);   //

            args.Add("--biplot ");   // create png
            args.Add("--out_image");   //
            args.Add(GetDoubleQuotationPath(op.outImage));

            args.Add("--colors"); // plot color...
            args.Add(GetDoubleQuotationPath(
                                string.Join(",", colors)));

            args.Add("--grouping_file"); // plot group-file
            args.Add(GetDoubleQuotationPath(op.groupFile));

            args.Add("--title ");  // PCoA rank title
            args.Add(GetDoubleQuotationPath(op.title));

            SetArguments(string.Join(" ", args));  // command arguments


            var workDir = Path.GetDirectoryName(op.matrixDataFile);
            proc = RequestCommand.GetInstance();
            var commandRes = proc.ExecuteWinCommand(binaryPath, arguments, ref stdout, ref stderr, workDir);
            winProcessId = proc.Pid; // to kill process ?? 


            var message = string.Empty;
            // if (commandRes == false) StdError に出力があるから false になる。
            if (FileSize(op.matrixDataFile, ref message) < 10)
            {
                progress.Report("nMDS command.  return:  " + commandRes);
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



    public class PcoaOptions : BaseOptions
    {
        [Required()]
        public string useTools;

        [Required()] 
        public string outImage;

        [Required()] // matrix data file
        public string matrixDataFile;
        public string distanceMetric;

        public string title;
        public string groupFile;
        public IEnumerable<string> colors;
    }
}

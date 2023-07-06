using ShotgunMetagenome.Proc.Flow;
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
    // gtf reagion count.
    public class FeatureCounts : BaseProcess
    {
        // result count file.
        public string outFeatureCounts;
        public static string outFeatureCountsFooter = ".cnt";
        public IEnumerable<string> acclist;

        //  count tool 
        public static string binaryName = "featureCounts.exe";
        private featureCountsOptions op;
        private RequestCommand proc;

        public FeatureCounts(featureCountsOptions options) : base(options)
        {
            if (progress == null)
                progress = new Progress<string>(s => System.Diagnostics.Debug.WriteLine(s));

            this.op = options;
            if (!BinaryExist(binaryName))
                progress.Report("not found " + binaryName + Environment.NewLine
                                        + Message);

            progress.Report("binary :" + binaryName);
            progress.Report("sam " + op.samFile);
            progress.Report("gff  " + op.gffFile);


            progress.Report(Message);
            isSuccess = string.IsNullOrEmpty(Message);
        }

        public override string StartProcess()
        {

            if (!isSuccess)
            {
                Message = "Process is initialisation Error";
                return IFlow.ErrorEndMessage;
            }

            this.outFeatureCounts = op.samFile + outFeatureCountsFooter;
            var pairOption = op.isPair ? "-p --countReadPairs" : "";
            // create command args.
            // featureCounts -O -M -p --countReadPairs --fraction -Q 1 -T %CPU% -t CDS -g ID -a %GFF% -o %SAM3%.cnt %SAM3%
            var args = new List<string>();
            args.Add("-O -M --fraction -Q 1 ");
            // -O  1 つの領域が複数の feature id で定義されている場合がある。featureCounts は、このような領域にマッピングされるリードを集計していない。-O を付けることで、このような複数の feature で定義されている領域にマッピングされるリードも集計する。
            // -M  featureCounts のデフォルトでは、複数箇所にマッピングされたリードを集計していない。-M を付けることで BAM の NH タグの情報を参照にして、multi - loci mapped リードも集計する。
            // --fraction - O または - M を使用するとき、同じリードが複数回計上される。この--fraction を付けることで、全体で 1 リードとして計上する。例えば、あるリードが n 箇所にマッピングされた場合、それぞれの箇所で 1 / n ずつ計上する。
            // -C  リードペアが異なる染色体にマッピングされていたり、あるいは異なるストランドにマッピングされている場合は集計対象としない。

            args.Add(pairOption);
            // Specify that input data contain paired-end reads

            args.Add("-T " + op.threads);   // cpu
            args.Add("-t gene");   // count term
            args.Add("-g ID");   // count term
            args.Add("-a ");
            args.Add(GetDoubleQuotationPath(op.gffFile));
            args.Add("-o ");
            args.Add(GetDoubleQuotationPath(outFeatureCounts));

            args.Add(GetDoubleQuotationPath(op.samFile)); // read file
            SetArguments(string.Join(" ", args));  // command arguments

            var workDir = Path.GetDirectoryName(op.samFile);
            if (!Directory.Exists(workDir))
                Directory.CreateDirectory(workDir);

            proc = RequestCommand.GetInstance();
            var commandRes = proc.ExecuteWinCommand(binaryPath, arguments, ref stdout, ref stderr, workDir);
            winProcessId = proc.Pid; // to kill process ?? 


            var message = string.Empty;
            if (FileSize(outFeatureCounts, ref message) < 10)
            {
                message += "mapping command fail, ";
                progress.Report("error, FeatureCounts command.  return:  " + commandRes);
                isSuccess = false;
                return IFlow.ErrorEndMessage;
            }

            // grep ?
            if (ValidCounts(outFeatureCounts) == IFlow.ErrorEndMessage)
            {
                isSuccess = false;
                return IFlow.ErrorEndMessage;
            }

            // 全て正常終了しているはず
            isSuccess = true;
            return IFlow.NormalEndMessage;
        }

        public override string StopProcess()
        {
            if (this.proc == null) return string.Empty;

            this.proc.CommandCancel();
            return ConstantValues.CanceledMessage;

        }

        public string ValidCounts(string cntFilePath)
        {
            var message = string.Empty;
            var lines = FileUtils.ReadFile(cntFilePath, ref message);
            if (!string.IsNullOrEmpty(message)) 
            { 
                progress.Report(message);
                return IFlow.ErrorEndMessage;
            }
            if (lines ==null || lines.Length < 1) 
            {
                progress.Report("read file is empty....: " + cntFilePath);
                return IFlow.ErrorEndMessage;
            }

            var writeLine = lines.Where(s => ! s.EndsWith("0")).ToList();
            File.Delete(cntFilePath); // いったん削除

            FileUtils.WriteFile(cntFilePath, writeLine, ref message);
            if (!string.IsNullOrEmpty(message))
            {
                progress.Report(message);
                return IFlow.ErrorEndMessage;
            }

            // 読み込んだついでに取得するACCリスト
            this.acclist = writeLine.Where(s => s.Split("\t").Count() > 1)
                                              .Select(s => s.Split("\t")[1] )
                                              .Where(s => !string.IsNullOrEmpty(s))
                                              .ToList();

            return IFlow.NormalEndMessage;
        }

    }


    public class featureCountsOptions : BaseOptions
    {

        [Required()]
        public string samFile;

        [Required()]
        public string gffFile;

        public bool isPair = false;
        public string threads = ProcessUtils.CpuCore(); // default


    }

}

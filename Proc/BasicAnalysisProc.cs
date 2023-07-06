using ShotgunMetagenome.External;
using ShotgunMetagenome.Proc.Flow;
using ShotgunMetagenome.Proc.Properties;
using ShotgunMetagenome.Several;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using static WfComponent.Utils.FileUtils;

namespace ShotgunMetagenome.Proc
{
    public partial class BasicAnalysisProc : CommonProc
    {
        // 取り敢えず解析用パラメータ。
        AnalysisProperties _p;
        public static string recentrifugeName = "metagenome";
        public static string centrifugeReportFooter = ".report";
        public static string centrifugeClassReportFooter = "-class.report";

        public BasicAnalysisProc(AnalysisProperties parameter) : base(parameter)
        {
            this._p = parameter;
            progress.Report("-- analysis flow start ....");

        }

        // flow-start <- BaseFlow.task
        private List<string> allCentrifugeReports;
        private List<FastqPair> allFastqPairs;
        public override string StartFlow()
        {
            this.allCentrifugeReports = new List<string>(); // centrifuge は 各fastqに対して行う
            this.allFastqPairs = new List<FastqPair>();  // fastq-pairs
            var res = IFlow.ErrorEndMessage;

            foreach(var group in _p.groups)
            {
                res = FstAnalysis(group.Name, group.FilePaths);
                if (res != IFlow.NormalEndMessage)
                    progress.Report("basic analysis is error, " + group.Name);
            }

            if (! allCentrifugeReports.Any()) // no-results Centrifuger 
            {
                progress.Report("no create assignment of bacterial (Centrifuge)");
                return IFlow.ErrorEndMessage;
            }

            res = Recentrifuge(recentrifugeName, allCentrifugeReports, tmpDir);
            if( res != IFlow.NormalEndMessage)
            {
                progress.Report("error, Recentrifuge is fail....");
                return res;
            }

            var isDatDel = true;
            // plot PCoA,nMDS
            res = CreatePlot();
            if (res != IFlow.NormalEndMessage) 
            { 
                progress.Report("PCoA/nMDS plot create is fail....");
                isDatDel = false;
            }
            // PERMANOVA
            res = Permanova();
            if (res != IFlow.NormalEndMessage)
            {
                progress.Report("Permanova calc is fail....");
                isDatDel = false;
            }
                


            // tmp data delete... is normal end.
            if( isDatDel )
                FileUtils.Delete(this.tmpDir);

            /// return IFlow.NormalEndMessage;
            return res;
        }


        // per 1-group flow.... centrifuge-class
        private string FstAnalysis(string groupName, IEnumerable<string> filePaths)
        {
            // QC -> fastq marge.
            var cpFastqs = PreSettings(groupName, filePaths);   // 


            // ----- Trisomatic term ------ //
            var centrifugeFastqs = new List<FastqPair>();

            var fastqPairs = GetIlluminaPairs(cpFastqs, ref message);
            foreach (var pair in fastqPairs)
            {
                var fastqPair = new FastqPair()
                {
                    GropName = groupName,
                    FwdFastq = pair.Key,
                    RevFastq = pair.Value
                };
                // Trisomatic
                this.ExecuteMethod = Path.GetFileNameWithoutExtension(pair.Key) +  " FastQC ";
                var result = FastQcTrim(fastqPair, resultsOut);   // tmp/yyyymmdd/group/
                if (result != IFlow.NormalEndMessage)
                {
                    progress.Report("QC command error, skip (pair) " + Path.GetFileName(pair.Key));
                    continue;  // QC-error.
                }
                // QC 正常終了を centrifuge コマンドへ。
                centrifugeFastqs.Add(fastqPair);
            }

            // ----- Centirifuge term ------ //
            var centrifugeReports = Centrifuge(groupName, centrifugeFastqs);
            if (!centrifugeReports.Any())
            {
                LogReport("not found report file, Please check Centrifuge results.");
                return IFlow.ErrorEndMessage;
            }
            allCentrifugeReports.AddRange(centrifugeReports);  // for plot-png report
            allFastqPairs.AddRange(centrifugeFastqs);
            return IFlow.NormalEndMessage;
        }



        // return Centrifuge-report file path.
        public IEnumerable<string>  Centrifuge(string groupName, IEnumerable<FastqPair> centrifugeFastqs)
        {

            var groupOut = Path.Combine(resultsOut, groupName);
            if (!Directory.Exists(groupOut))
                Directory.CreateDirectory(groupOut);

            var classReports = new List<string>();  // report-file paths for reCentrifuge

            foreach(var pair in centrifugeFastqs)
            {
                this.ExecuteMethod = Path.GetFileNameWithoutExtension(pair.FwdFastqQc) + "  " +
                                                 System.Reflection.MethodBase.GetCurrentMethod().Name;
                var outName = // groupName + "-" + 
                                        Path.GetFileNameWithoutExtension(pair.FwdFastqQc);
                var fwd = string.Empty;
                var rev = string.Empty;
                var unp = string.Empty;
                if (string.IsNullOrEmpty(pair.RevFastqQc))
                {
                    unp = pair.FwdFastqQc;
                }
                else
                {
                    fwd = pair.FwdFastqQc;
                    rev = pair.RevFastqQc;
                }

                message = string.Empty;
                var centrifugeReport = Path.Combine(
                                                    resultsOut,
                                                    outName + centrifugeReportFooter);

                var classReport = Path.Combine(
                                            resultsOut,
                                            outName + centrifugeClassReportFooter);

                this.specificProcess = new Centrifuge(
                                                   new CentrifugeOptions()
                                                   {
                                                       progress = progress,
                                                       dbName = _p.useDb,
                                                       fwdFastq = fwd,
                                                       revFastq = rev,
                                                       unpaired =  unp,

                                                       outReportPath = centrifugeReport,
                                                       outClassificationPath = classReport,
                                                       threads = _p.useThreads
                                                   });

                var res = specificProcess.StartProcess();  // cancel 出来ない
                if (IFlow.ErrorEndMessage.Equals(res, StringComparison.OrdinalIgnoreCase) ||
                    !File.Exists(centrifugeReport))
                {
                    progress.Report("error, centrifuge process return code:" + res);
                    progress.Report(specificProcess.GetMessage());
                    /// return IFlow.ErrorEndMessage;
                    continue;
                }
                progress.Report("out report: " + classReport);
                classReports.Add(classReport);
            }

            return classReports;
        }

        // Recentrifuge  
        private string Recentrifuge(string groupName,  IEnumerable<string> reportFilePaths, string htmlOutDir )
        {

            progress.Report("use reports : " + Environment.NewLine + 
                                    String.Join(Environment.NewLine, reportFilePaths));
            this.ExecuteMethod = groupName + " " + System.Reflection.MethodBase.GetCurrentMethod().Name;

            // 一時出力先
            htmlOutDir = htmlOutDir == null
                            ? _p.outDirectory
                            : htmlOutDir;

            // var relativePaths = reportFilePaths.Select(s => s.Replace(htmlOutDir, "."));


            this.specificProcess = new Recentrifuge(
                                    new RecentrifugeOption()
                                    {
                                        outPrefix = groupName,
                                        inReports = reportFilePaths,
                                        outDir = htmlOutDir,
                                        progress = progress,
                                    });

            var res = specificProcess.StartProcess();  // cancel 出来ない
            if (IFlow.ErrorEndMessage.Equals(res, StringComparison.OrdinalIgnoreCase))
            {
                progress.Report("error, Recentrifuge process return code:" + res);
                progress.Report(specificProcess.GetMessage());
                return IFlow.ErrorEndMessage;
            }

            // 正常終了したらwork-dir から out-dir へ コピー
            var recentrifugeRes = FindFile(htmlOutDir, recentrifugeName);
            foreach(var file in recentrifugeRes)
            {
                message = string.Empty;
                var copyTo = Path.Combine(_p.outDirectory, Path.GetFileName(file));
                progress.Report(copyTo);
                if (FileCopy(file, copyTo, ref message))
                    progress.Report(message);
            }


            return IFlow.NormalEndMessage;
        }


    }
}

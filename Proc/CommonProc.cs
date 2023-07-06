using ShotgunMetagenome.Proc.Flow;
using ShotgunMetagenome.Proc.Properties;

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using WfComponent.External;
using WfComponent.External.Properties;
using static ShotgunMetagenome.Several.AnywayUtils;

namespace ShotgunMetagenome.Proc
{
    public abstract class CommonProc : BaseFlow
    {

        // 一時作業フォルダ  2Byte 文字が入らないように。
        protected string startDateTime;
        protected string tmpDir;
        protected string resultsOut;
        protected string cpFastqs;
        protected List<string> resultOuts;
        public string message = string.Empty;
        public static readonly string fastqc = "fastqc";
        public static readonly string fastq = "fastq";


        // constractor...
        protected CommonProc(BaseProperties _p) : base(_p.progress)
        {
            // viewmodel で作成する。各コンストラクタで代入・・・
            // this.uniqDate = DateTime.Now.ToString("yyyyMMdd-HHmmss");

            this.startDateTime = string.IsNullOrEmpty(_p.startDateTime)
                                                ? DateTime.Now.ToString("yyyyMMdd-HHmmss")
                                                : _p.startDateTime;

            this.tmpDir = Path.Combine(
                                    GetTmpDir(),
                                    this.startDateTime);

            if (!Directory.Exists(tmpDir))
                Directory.CreateDirectory(tmpDir);


            // Data, PCoA, nMDS

            this.resultOuts = new List<string>();
        }

        // 解析時のフォルダとか作ります。
        protected IEnumerable<string> PreSettings(string basename, IEnumerable<string> filePaths)
        {
            // group-dir
            resultsOut = Path.Combine(tmpDir, basename);
            if (!Directory.Exists(resultsOut))
                Directory.CreateDirectory(resultsOut);
            LogReport("create tmp directory " + resultsOut);

            // cp-fastqc after
            cpFastqs = Path.Combine(resultsOut, fastq);
            if (!Directory.Exists(cpFastqs))
                Directory.CreateDirectory(cpFastqs);
            LogReport("create directory " + cpFastqs);


            var cpFastqPaths = new List<string>();
            var message = string.Empty;
            foreach (var file in filePaths)
            {
                // copy to tmp-dir,  tmp-dir is not include space, 2byte-char
                var toCopy = Path.Combine(cpFastqs, Path.GetFileName(file));
                WfComponent.Utils.FileUtils.FileCopy(file, toCopy, ref message);
                if (!string.IsNullOrEmpty(message))
                    progress.Report(message);
                else
                    cpFastqPaths.Add(toCopy);
            }
            // 終了時に移動する
            this.resultOuts.Add(resultsOut);

            return cpFastqPaths;
        }


        // Illumina  key -> seq-name(s)
        protected IDictionary<string, FastqPair> FastqPairs(IEnumerable<string> fastqs)
        {
            var fastqPairs = new Dictionary<string, FastqPair>();

            Array.Sort(fastqs.ToArray());   // sort して、Fwd/Rev を確定する。
            foreach (var fastq in fastqs)
            {
                var fastqBaseName = WfComponent.Utils.FileUtils.GetMiseqFastqBaseName(fastq);
                if (fastqPairs.ContainsKey(fastqBaseName))
                    if (string.IsNullOrEmpty(fastqPairs[fastqBaseName].RevFastq))
                        fastqPairs[fastqBaseName].RevFastq = fastq;
                    else    // 2019.10.01  分割する文字列を2つ以上持ち、Split 結果が同じになる。後から見つかったやつはSKIP
                        LogReport("duplicate sequence base name!! " + fastq + "\n this sequence is skip!!");
                else
                    fastqPairs.Add(fastqBaseName, new FastqPair { FwdFastq = fastq });
            }

            // basename　-> FastqFullpath
            return fastqPairs;
        }

        // Trisomatic 
        public string FastQCMinPhredScore = "13";
        public string FastQCWindowSize = "50";
        public string FastQCMinLength = "100";

        protected string FastQcTrim(FastqPair seqs, string fastqcOutDir)
        {
            LogReport("FastqQC start ...");
            // 1-sample ごとにQC処理を行う。sample-basename 毎に作成されているはず。
            if (! Directory.Exists (fastqcOutDir))
                Directory.CreateDirectory(fastqcOutDir); 


            var outFastq1 = Path.Combine(
                                        fastqcOutDir,
                                        WfComponent.Utils.FileUtils.GetFileBaseName(seqs.FwdFastq) + ".fastq");

            var outFastq2 = string.IsNullOrEmpty(seqs.RevFastq) 
                                        ? string.Empty 
                                        : Path.Combine(
                                            fastqcOutDir,
                                            WfComponent.Utils.FileUtils.GetFileBaseName(seqs.RevFastq) + ".fastq");

            this.specificProcess = new Trimmomatic(
                new TrimmomaticOptions()
                {
                    fastqPath1 = seqs.FwdFastq,
                    fastqPath2 = seqs.RevFastq,
                    outFastq1 = outFastq1,
                    outFastq2 = outFastq2,
                    threads = WfComponent.Utils.ProcessUtils.CpuCore(),
                    minPhreadScore = FastQCMinPhredScore,
                    windowSize = FastQCWindowSize,
                    minLength = FastQCMinLength
                }
            );
            // 必須項目チェック
            if (specificProcess != null && !specificProcess.IsProcessSuccess())
            {
                LogReport("FastQC error, " + specificProcess.GetMessage());
                return IFlow.ErrorEndMessage;
            }


            specificProcess.StartProcess();  // Process が終わるまで。
            if (specificProcess != null && specificProcess.IsProcessSuccess())   // FastQC - OK
            {
                if (File.Exists(outFastq1) && WfComponent.Utils.FileUtils.FileSize(outFastq1, ref message) > 100f)
                {
                    // QC fastq の保存
                    seqs.FwdFastqQc = outFastq1;
                    seqs.RevFastqQc = outFastq2;
                    LogReport("FastQC end, " + Path.GetFileName(fastqcOutDir));

                    // Trimmomatic log delete
                    var log = Path.Combine(
                                    Path.GetDirectoryName(seqs.FwdFastq),
                                    "Trimmomatic.log");
                    if (File.Exists(log)) File.Delete(log);

                }
                else
                {
                    LogReport("not FastQC result, " + Path.GetFileName(fastqcOutDir));
                    LogReport(specificProcess.GetMessage());
                    return IFlow.ErrorEndMessage;
                }
            }

            return IFlow.NormalEndMessage;
        }



    }
    // MiSeq Pairend 対応。
    public class FastqPair
    {
        public string GropName { get; set; }
        public string FwdFastq { get; set; }
        public string RevFastq { get; set; }
        public string FwdFastqQc { get; set; }
        public string RevFastqQc { get; set; }
    }

}

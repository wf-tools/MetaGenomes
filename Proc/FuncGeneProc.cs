using ShotgunMetagenome.External;
using ShotgunMetagenome.Proc.Flow;
using ShotgunMetagenome.Proc.Properties;
using ShotgunMetagenome.Several;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using static WfComponent.Utils.FileUtils;

namespace ShotgunMetagenome.Proc
{
    public partial class FuncGeneProc : CommonProc
    {

        public static readonly string gffAnnotGene = "gene"; // gff の位置対象 名
        public static readonly char alignGap = '-';
        public static readonly string blastQueryFooter = "-gene.fasta";

        public static readonly string mappingReferenceFooter = ".fasta";
        private readonly string gffFooter = ".gff";
        private string gffPath;

        public static readonly string outFuncGenes = "FuncGenes.csv";
        private string cogBlastReferenceName = "cog";
        private string cogBlastReference;

        private IEnumerable<KeyValuePair<string,string>> cogProt2cogNm;
        private IDictionary<string, string> cogNm2disc;

        FuncGeneAnalysisProperties _p;
        public FuncGeneProc(FuncGeneAnalysisProperties parameter) : base(parameter)
        {
            this._p = parameter;
            this.group2sams = new Dictionary<string, IEnumerable<string>>();

            gffPath = Path.ChangeExtension(_p.MappingReferene, gffFooter);

            // view では Fotter が省かれている
            _p.MappingReferene += mappingReferenceFooter;

            if ( !File.Exists(gffPath))   // viewで制限しているので通常はありえない。
            {
                progress.Report("Error, not found gff-file. Please setup gff: " + gffPath);
                message += "Error, not found gff-file. Please setup gff: " + gffPath;
                // this.cancellationTokenSource.Cancel();  
                return;
            }

            var cogBlxFile = cogBlastReferenceName + ".psq"; // blast reference の 代表ファイル
            var cogPath = FindFile(
                                        Path.Combine(AnywayUtils.currentDir, "data"),
                                        cogBlastReferenceName);

            if(cogPath == null || !cogPath.Any())
            {
                LogReport("cog reference is not found.... Please check data folder. " + AnywayUtils.currentDir);
                message += "fatal error, The required file could not be found.";
                return;
            }

            // blastx reference path
            cogBlastReference = Path.Combine(
                                                Path.GetDirectoryName(cogPath.First()),
                                                cogBlastReferenceName);


            // cog fasta -> cog-nm  // cog-20.cog-ProteinID-COGID.csv
            var cogProteinNmCsv = FindFile(
                                                    Path.Combine(AnywayUtils.currentDir, "data"),
                                                    cogBlastReferenceName, 
                                                    ".csv");

            if (cogProteinNmCsv == null || !cogProteinNmCsv.Any())
            {
                LogReport("cog protein csv is not found.... Please check data folder. " + AnywayUtils.currentDir);
                LogReport("seach name eg.)  cog-20.cog-ProteinID-COGID.csv");
                message += "fatal error, The required file could not be found.";
                return;
            }

            var csvLines = ReadFile(cogProteinNmCsv.First(), ref message);
            LogReport(message);
            if (csvLines == null || !csvLines.Any())
            {
                progress.Report("fatal error, none read csv, " + cogProteinNmCsv.First());
                return;
            }

            // 1つのFasta-ProteinID に 複数のCogID がある。。。
            this.cogProt2cogNm = csvLines.Select(s => new KeyValuePair<string, string>(
                                                                                s.Split(",").First().Trim(),
                                                                                s.Split(",").Last().Trim()));




            LogReport("read end " + cogProteinNmCsv.First());

            // cog-20.def.tab
            var cogNamesPath = FindFile(
                                            Path.Combine(AnywayUtils.currentDir, "data"),
                                            cogBlastReferenceName,
                                            ".tab");
            if (cogNamesPath == null || !cogNamesPath.Any())
            {
                LogReport("cog names tabler is not found.... Please check data folder. " + AnywayUtils.currentDir);
                LogReport("seach name eg.)   cog-20.def.tab");
                message += "fatal error, The required file could not be found.";
                return;
            }

            var cogNames = ReadFile(cogNamesPath.First(), ref message);
            cogNm2disc = cogNames.ToDictionary(s => s.Split("\t").First(),
                                                                      s => s.Split("\t").ElementAt(2));
            LogReport("read end " + cogNamesPath.First());
        }


        // func gene flow start....
        public override string StartFlow()
        {
            LogReport("-- analysis flow start ....");
            var res = IFlow.ErrorEndMessage;

            // 
            foreach (var group in _p.groups)
            {
                // 1st mapping
                res = FstMapping(group.Name, group.FilePaths);
                if (res != IFlow.NormalEndMessage)
                    LogReport("basic analysis is error, " + group.Name);
            }

            // 2nd Consensus seq, single thread ?  TODO parallel each?
            // 最終的な表に対応する　1sam＝1clm 
            var allSam2cogNm = new List<IDictionary<string, IDictionary<string, int>>>();
            foreach (var group2sam in group2sams)
            {
                // groupName -> sam-paths.
                var sam2cogs = GetConsensusHits(group2sam);
                if (sam2cogs == null || !sam2cogs.Any())
                    LogReport("  no create consensus, blast-hit,");


                // sam -> cog : int.
                var sam2cogNm = new Dictionary<string, IDictionary<string, int>>();
                foreach(var cogs in sam2cogs)
                    sam2cogNm.Add(Path.GetFileNameWithoutExtension( cogs.Key ),
                                              GetCogCounts(this.cogProt2cogNm, cogs.Value, progress)
                                                                 .ToDictionary(s => s.Key, s => s.Value));

                allSam2cogNm.Add(sam2cogNm);  // 空でもsam が必要なので空でも追加
            }

            if (!allSam2cogNm.Any())
            {
                LogReport("no-data results. ");
                return res;   // error end.
            }

            // create csv
            var outlines = new List<string>();
            var samnames = string.Join("\t", allSam2cogNm.SelectMany(s => s.Keys))
                                              .Replace(Environment.NewLine, string.Empty);

            // out puts csv-line header..
            outlines.Add("\t" + samnames);
            LogReport(samnames);

            var hitCogs = allSam2cogNm.SelectMany(s => s.SelectMany(s => s.Value.Keys))
                                                        .Distinct();

            foreach (var cog in hitCogs)
            {
                var out1cog = cog + "\t";
                foreach (var sam in allSam2cogNm)
                {
                    // 1-sam -> cog:nm
                    foreach(var cog2nm in sam)
                    {
                        if (cog2nm.Value.ContainsKey(cog))
                        {
                            out1cog += cog2nm.Value[cog]  + "\t";
                        }
                        else
                        {
                            out1cog += "n/a\t";
                        }
                    }
                }
                // out1cog += Environment.NewLine;
                out1cog += cogNm2disc[cog].Replace(Environment.NewLine, string.Empty);
                outlines.Add(out1cog);
            }

            // tab だけど csv ファイルとして出力。Excelで開ける。
            var outTebFilePath = Path.Combine(
                                                    _p.outDirectory,
                                                    outFuncGenes);
            WriteFile(outTebFilePath, outlines, ref message);
            LogReport(message);

            // ここまで何もなければ
            return IFlow.NormalEndMessage;
        }

        // Trisomatic と Mapping, FeatureCounts
        IDictionary<string, IEnumerable<string>> group2sams;
        private string FstMapping(string groupName, IEnumerable<string> filePaths)
        {
            // QC -> fastq marge.
            var cpFastqs = PreSettings(groupName, filePaths);   // 
            var fastqPairs = GetIlluminaPairs(cpFastqs, ref message);

            // mapping fastqs
            var mappingFastqs = new List<FastqPair>();
            foreach (var pair in fastqPairs)  //  pair or single
            {
                var fastqPair = new FastqPair()
                {
                    GropName = groupName,
                    FwdFastq = pair.Key,
                    RevFastq = pair.Value
                };

                // Trisomatic
                this.ExecuteMethod = Path.GetFileNameWithoutExtension(pair.Key) + " FastQC ";
                var result = FastQcTrim(fastqPair, resultsOut);   // tmp/yyyymmdd/group/
                if (result != IFlow.NormalEndMessage)
                {
                    progress.Report("QC command error, skip (pair) " + Path.GetFileName(pair.Key));
                    continue;  // QC-error.
                }
                // QC 正常終了を centrifuge コマンドへ。
                mappingFastqs.Add(fastqPair);
            }

            var mappingResults = Mapping( mappingFastqs);
            if (mappingResults == null || !mappingResults.Any())
            {
                LogReport("no-results mapping process, Please check mapping command logs.");
                return IFlow.ErrorEndMessage;
            }

            // sam file 保持
            group2sams.Add(groupName, mappingResults);
            return IFlow.NormalEndMessage;
        }

        // マッピングコマンドの発行    // とりまマッピング(minimap2)
        private IEnumerable<string> Mapping( IEnumerable<FastqPair> fastqs)
        {
            this.ExecuteMethod = System.Reflection.MethodBase.GetCurrentMethod().Name;
            var groupWorkOut = Path.Combine(resultsOut, "results");
            if (!Directory.Exists(groupWorkOut))
                Directory.CreateDirectory(groupWorkOut);

            var mappingResults = new List<string>();
            foreach (var pair in fastqs)
            {
                this.ExecuteMethod = Path.GetFileNameWithoutExtension(pair.FwdFastqQc) + "  " +
                                                 System.Reflection.MethodBase.GetCurrentMethod().Name;
                var outSam = // groupName + "-" + 
                                        Path.Combine(groupWorkOut,
                                        Path.GetFileNameWithoutExtension(pair.FwdFastqQc) + ".sam");

                string fwd;
                string rev;
                bool isPair;
                if (string.IsNullOrEmpty(pair.RevFastqQc))
                {
                    fwd = pair.FwdFastqQc;
                    rev = string.Empty;
                    isPair = false;
                }
                else
                {
                    fwd = pair.FwdFastqQc;
                    rev = pair.RevFastqQc;
                    isPair = true;
                }
                
                this.specificProcess = new Minimap2(
                                new Minimap2Options()
                                {
                                    dbName = _p.MappingReferene,
                                    fwdFastq = fwd,
                                    revFastq = rev,
                                    outSamPath = outSam,
                                    isLargeReference = _p.isFullReference,
                                });

                var res = specificProcess.StartProcess();  // cancel 出来ない
                if (IFlow.ErrorEndMessage.Equals(res, StringComparison.OrdinalIgnoreCase) ||
                    !File.Exists(outSam))
                {
                    progress.Report("error, mapping process return code:" + res);
                    progress.Report(specificProcess.GetMessage());
                    /// return IFlow.ErrorEndMessage;
                    continue;
                }
                // mapping end report...
                progress.Report("mapping command end,  " + outSam);
                mappingResults.Add(outSam);

                // gene にマッピングされている箇所があるなら対象。FeatureCounts実行
                res = GetFeatureCounts(outSam, isPair);
                if (IFlow.ErrorEndMessage.Equals(res, StringComparison.OrdinalIgnoreCase))
                {
                    progress.Report("error, sam-term count (FeatureCounts) process return code:" + res);
                    progress.Report(specificProcess.GetMessage());
                    progress.Report("fatal error....!");
                    /// return IFlow.ErrorEndMessage;
                    return null;
                }

            }

            return mappingResults;
        }

        //　FeatureCounts の実行
        private string GetFeatureCounts(string outsam, bool isPair)
        {
            this.ExecuteMethod = System.Reflection.MethodBase.GetCurrentMethod().Name;
            var fc  = new FeatureCounts(
                                        new featureCountsOptions()
                                        {
                                            gffFile = this.gffPath,
                                            samFile = outsam,
                                            isPair = isPair,
                                        });
            this.specificProcess = fc;
            var res = specificProcess.StartProcess();  // cancel 出来ない

            if (IFlow.ErrorEndMessage.Equals(res, StringComparison.OrdinalIgnoreCase) ||
                !File.Exists(fc.outFeatureCounts))
            {
                progress.Report("mapping term-count(FeatureCounts) process return code:" + res);
                progress.Report(specificProcess.GetMessage());
                return IFlow.ErrorEndMessage;
            }
            // 何もなければ。
            return IFlow.NormalEndMessage;
        }


        // マッピング結果からコンセンサスファイルを作成する
        // 対象は gff-gene にマッピングされているGenomeが対象となる。
        private IDictionary<string, IEnumerable<string>> GetConsensusHits(KeyValuePair<string, IEnumerable<string>>group2sampaths)
        {
            this.ExecuteMethod = System.Reflection.MethodBase.GetCurrentMethod().Name;

            // return 
            var sam2cogs = new Dictionary<string, IEnumerable<string>>();
            foreach (var sam in group2sampaths.Value)
            {
                // sam file の加工
                // FeatureCounts の結果からSAM file を限定化
                var cntFile = sam + FeatureCounts.outFeatureCountsFooter;
                var defacementSam = GetDefacementSam(sam, cntFile);
                var defacementRefer = GetDefacementReference( _p.MappingReferene, cntFile);

                if (defacementSam == null || defacementRefer == null)
                {
                    LogReport("fatal error....");
                    return sam2cogs;  // 基本的に有り得無い
                }


                var outCons = Path.ChangeExtension(defacementSam, ".fasta");
                // bcftools create consensus.
                var cons = new Bcftools(
                                    new BcftoolsOptions()
                                    {
                                        reference = defacementRefer,
                                        samFile = defacementSam,
                                        outConsensus = outCons
                                    });

                var res = cons.StartProcess();  // cancel 出来ない
                if (IFlow.ErrorEndMessage.Equals(res, StringComparison.OrdinalIgnoreCase) ||
                    !File.Exists(outCons))
                {
                    LogReport("error, create consensus-fasta process return code:" + res);
                    LogReport(specificProcess.GetMessage());
                    LogReport("fatal error....");
                    return sam2cogs;  
                }

                // samfile -> consensus
                if (!File.Exists(defacementSam))  continue;

                // counsensus -> align
                var aligns = GetAlignConsensus(_p.MappingReferene, outCons, Path.GetFileNameWithoutExtension(sam), progress);

                // align -> blast query
                var query = Align2BlastQuery(aligns, gffPath, Path.GetFileNameWithoutExtension(sam), progress);

                // vs Cog BLASTX   // query -> tophit-reference  　もし cog-hits 計算値が 1/n-hit の場合はここで計算すること。
                var blastres = GetBlastxTopResults(this.cogBlastReference,  query );

                if(blastres == null || !blastres.Any())
                {
                    LogReport("error, blast process is fail,  ");
                    return sam2cogs;   // 空。
                }


                sam2cogs.Add(sam, blastres.Select(s => s.Value));
                // sam 単位の処理
            }

            // 何もなければ。
            // return IFlow.NormalEndMessage;
            return sam2cogs;
        }

        // return sam-file path
        public static string GetDefacementSam(string sam, string featureCnts, IProgress<string> progress = null)
        {
            if (progress == null)
                progress = new Progress<string>(s => System.Diagnostics.Debug.WriteLine(s));

            var message = string.Empty;
            var defacement = Path.Combine(
                                            Path.GetDirectoryName(sam),
                                            Path.GetFileNameWithoutExtension(sam) + "-fc.sam");

            try {

                // count file
                var cntfile = ReadFile(featureCnts, ref message);
                if (!string.IsNullOrEmpty(message))
                {
                    progress.Report(message);
                    return null;
                }


                var reflist = cntfile.Where(s => !s.StartsWith("#"))
                                            .Select(s => s.Split("\t").ElementAt(1))
                                            .Distinct();

                // dictionary の方がKey検索が早い・・・はず。
                Dictionary<string, int> diclist = reflist.ToDictionary(n => n, n => n.Length);

                using (StreamWriter sw = new StreamWriter(defacement))
                using (StreamReader streamReader = new StreamReader(sam))
                {
                    while (streamReader.Peek() >= 0)
                    {
                        // ReadLine
                        string line = streamReader.ReadLine();
                        if (line.StartsWith("@"))
                        {
                            if (line.StartsWith("@SQ"))
                            {
                                if (diclist.ContainsKey(line.Split("\t").ElementAt(1).Split(":").Last()))
                                    sw.WriteLine(line);
                                continue;
                            }
                            else
                            {
                                sw.WriteLine(line);
                                continue;
                            }
                        }

                        if (diclist.ContainsKey(line.Split("\t").ElementAt(2)))
                        {
                            sw.WriteLine(line);
                            continue;
                        }

                    }
                }
            }
            catch
            {
                progress.Report("file read/write error, " + sam);
                return null;
            }

            return defacement;
        }

        // Reference-fasta をスリム化
        public static string GetDefacementReference(string referemce, string featureCnts, IProgress<string> progress = null)
        {
            if (progress == null)
                progress = new Progress<string>(s => System.Diagnostics.Debug.WriteLine(s));

            var message = string.Empty;
            var defacement = Path.Combine(
                                            Path.GetDirectoryName(featureCnts),
                                            Path.GetFileNameWithoutExtension(featureCnts) + "-deface.fasta");

            try
            {

                // count file
                var cntfile = ReadFile(featureCnts, ref message);
                if (!string.IsNullOrEmpty(message))
                {
                    progress.Report(message);
                    return null;
                }


                var reflist = cntfile.Where(s => !s.StartsWith("#"))
                                            .Select(s => s.Split("\t").ElementAt(1))
                                            .Distinct();

                // dictionary の方がKey検索が早い・・・はず。
                Dictionary<string, int> diclist = reflist.ToDictionary(n => n, n => n.Length);

                bool isRead = false;
                using (StreamWriter sw = new StreamWriter(defacement))
                using (StreamReader streamReader = new StreamReader(referemce))
                {
                    while (streamReader.Peek() >= 0)
                    {
                        // ReadLine
                        string line = streamReader.ReadLine();
                        if (line.StartsWith(">"))
                            if (diclist.ContainsKey(line.Split(" ").First().Replace(">", "")))
                                isRead = true;
                            else
                                isRead = false;

                        //                             
                        if (isRead)
                            sw.WriteLine(line);

                    }

                }
            }
            catch
            {
                progress.Report("file read/write erorro, " + defacement);
                return null;
            }

            return defacement;
        }

        public static string conesnsusNameFooter = "-consensus";
        public static IEnumerable<string> GetAlignConsensus(string reference, string consensus, string basename, IProgress<string> progress = null)
        {
            if (progress == null)
                progress = new Progress<string>(s => System.Diagnostics.Debug.WriteLine(s));

            var alignCons = new List<string>();

            var referenceSeqs = WfComponent.Utils.Fasta.FastaFile2Dic(reference)
                                            .ToDictionary(x => x.Key, x => x.Value);
            var consSeqs = WfComponent.Utils.Fasta.FastaFile2Dic(consensus);
            if(consSeqs == null || !consSeqs.Any())
            {
                progress.Report("error, no consensus sequences found.....");
                return alignCons;
            }


            // foreach(var cons in consSeqs)
            ParallelOptions pt = new ParallelOptions();
            // pt.MaxDegreeOfParallelism = int.Parse( WfComponent.Utils.ProcessUtils.CpuCore());
            // pt.MaxDegreeOfParallelism = 2;
            var cpu = int.Parse(WfComponent.Utils.ProcessUtils.CpuCore());
            pt.MaxDegreeOfParallelism = cpu  > 2 ? cpu -2 : 2;
                                               
            Parallel.ForEach(consSeqs, pt,  
            cons =>
            {
                var consSeqname = cons.Key.Trim();
                var refNucs = referenceSeqs[consSeqname];

                if (refNucs == null)
                {  // 通常はありえない。
                    progress.Report("not found referene sequencre... " + consSeqname);
                    // continue;
                }
                progress.Report("create align " + consSeqname);
                var referenceConsensus = ">" + consSeqname + Environment.NewLine
                                                        + refNucs + Environment.NewLine
                                                        + ">" + consSeqname + conesnsusNameFooter + Environment.NewLine
                                                        + cons.Value + Environment.NewLine;

                var message = string.Empty;
                var outPairFasta = Path.Combine(
                                                Path.GetDirectoryName(consensus),
                                                basename + "-" + consSeqname + "-pair.fasta");
                progress.Report("pair fasta: " + outPairFasta);
                WriteFileFromString(outPairFasta, referenceConsensus, ref message);
                progress.Report(message);


                if (!File.Exists(outPairFasta))
                {
                    progress.Report("no created pair-consensus file. " + outPairFasta);
                    //continue;   // error end? 基本的に有り得ない。
                }


                var outAlign = Path.ChangeExtension(outPairFasta, ".aln");
                // bcftools create consensus.
                var align = new Kalign(
                                    new KalignOptions()
                                    {
                                        pairseqPath = outPairFasta,
                                        outAlignPath = outAlign
                                    });
                progress.Report("align command start... ");
                var res = align.StartProcess();  // cancel 出来ない
                if (IFlow.ErrorEndMessage.Equals(res, StringComparison.OrdinalIgnoreCase) ||
                    !File.Exists(outAlign))
                {
                    progress.Report("align command error, " + res);
                    progress.Report(align.Message);
                    // continue; // TODO error end??
                }
                progress.Report("created align: " + outAlign);
                alignCons.Add(outAlign);
            });  // Parallel

            return alignCons;  // aligned files ....
        }


        // 1sam 由来の コンセンサス配列から作成した Align ファイルから BLASTX query を作成する
        public static string Align2BlastQuery(IEnumerable<string> alignFstas, string gffAnot, string basename, IProgress<string> progress = null)
        {
            if (progress == null)
                progress = new Progress<string>(s => System.Diagnostics.Debug.WriteLine(s));

            var message = string.Empty;
            var queryFasta = new List<string>();
            var gtt = ReadFile(gffAnot, ref message);
            progress.Report(message);

            foreach (var alignFsta in alignFstas)
            {

                if(!File.Exists(alignFsta))
                {
                    progress.Report("not found align file..." + alignFsta);
                    continue;
                }

                var align = WfComponent.Utils.Fasta.FastaFile2Dic(alignFsta);
                if(align == null || ! align.Any())
                {
                    progress.Report("not found align file lines..." + alignFsta);
                    continue;
                }

                // 作成したconsensus ではないAccession ＝ Reference Accession
                var refer = align.Where(s => !s.Key.EndsWith(conesnsusNameFooter)).First();
                var cons = align.Where(s => s.Key.EndsWith(conesnsusNameFooter)).First();
                var referleng = refer.Value.Length;
                var consleng = cons.Value.Length; //gff に length超えるものがある。

                // gff gene を対象とする。　TODO protein_coding; も含める？
                var gttGenes = gtt.Where(s => refer.Key.Equals(s.Split("\t").First()))
                                          .Where(s => gffAnnotGene.Equals(s.Split("\t").ElementAt(2)));

                
                foreach (var line in gttGenes)
                {
                    var tabler = line.Split("\t");
                    if (tabler.Length < 5) continue; // gff は8カラムあるはず。

                    if (Int32.TryParse(tabler.ElementAt(3), out int _start) &&
                        Int32.TryParse(tabler.ElementAt(4), out int _end))
                    {

                        // debug.
                        if (_start < 2)
                            progress.Report(line);

                        _start -= 1;
                        _end -= 1;    // 0-orign.
                        var len = _end - _start + 1;
                        var sublen = (_start + len >= referleng) ? referleng - 1: len;   // gff position 超過  全長
                        var refPregap = refer.Value.Substring(0, sublen)
                                                    .Where(s => s == alignGap)
                                                    .Count();   // alignment で reference の方にGapがある場合

                        sublen = (_start + len >= referleng) ? referleng - _start : len;
                        var refgap = refer.Value.Substring(_start, sublen)
                                                    .Where(s => s == alignGap)
                                                    .Count();   // alignment で reference の方にGapがある場合

                        var consStart = (_start + refPregap >= consleng ) ? -1 : _start + refPregap;
                        if (consStart < 0) continue;

                        sublen = (consStart + len + refgap) > consleng ? consleng - consStart : len + refgap;
                        // var consNuc = cons.Value.Substring(_start + refPregap, len + refgap);
                        var consNuc = cons.Value.Substring(consStart, sublen);
                        queryFasta.Add(">" + tabler.First() + "|" + _start.ToString() + "-" + _end.ToString());
                        queryFasta.Add(consNuc);

                    }

                }  // gtt gene end,
            }

            var blastQuery = Path.Combine(
                                        Path.GetDirectoryName(alignFstas.First()),
                                        basename+ blastQueryFooter);

            if (File.Exists(blastQuery)) File.Delete(blastQuery);
            WriteFile(blastQuery, queryFasta, ref message);
            progress.Report(message);


            if (FileSize(blastQuery, ref message) < 100)
                return string.Empty;

            return blastQuery;
        }

        // Cog blastx 
        public static readonly string blastxFooter = ".blx";
        public double cutoffEvalue = 2e-10;
        public double cutoffIdent = 95.0;  // blast top-hit  cutoff value
        public IEnumerable<KeyValuePair<string, string>> GetBlastxTopResults(string blastRefer, string query)
        {
            if (progress == null)
                progress = new Progress<string>(s => System.Diagnostics.Debug.WriteLine(s));

            var outblx = Path.Combine(
                                Path.GetDirectoryName(query),
                                Path.GetFileNameWithoutExtension(query) + blastxFooter);

            this.specificProcess = new Blastx(
                    new BlastOptions()
                    {
                        referendeDb = blastRefer,
                        queryFasta = query,
                        outBlastResult = outblx,
                        evalue = cutoffEvalue.ToString(),
                        progress = progress
                    });

            var res = specificProcess.StartProcess();
            if (IFlow.ErrorEndMessage.Equals(res, StringComparison.OrdinalIgnoreCase) ||
                !File.Exists(outblx))
            {
                progress.Report("blast command error, " + res);
                progress.Report(this.message);
                return null;
            }

            return GetBlastTophit(outblx, cutoffEvalue, cutoffIdent, progress);
        }


        //Fields: query id, subject id, % identity, alignment length, mismatches, gap opens, q. start, q. end, s. start, s. end, evalue, bit score
        public static readonly int BlxQueryClm = 0;
        public static readonly int BlxReferClm = 1;
        public static readonly int BlxIdentClm = 2;
        public static readonly int BlxEvaltClm = 10;
        public static readonly int BlxScorClm = 11;
        // out format 6 blast result, get top1 
        public static IEnumerable<KeyValuePair<string, string>> GetBlastTophit(string blastResult, double cutoffEvalue, double cutoffIdentity, IProgress<string> progress)
        {

            if (progress == null)
                progress = new Progress<string>(s => System.Diagnostics.Debug.WriteLine(s));


            var message = string.Empty;
            var resTophits = new List<KeyValuePair<string, string>>();      

            var blxLines = ReadFile(blastResult, ref message);
            progress.Report(message);
            if(blxLines == null || blxLines.Length == 0)
            {
                progress.Report("no blastx results, skip " + blastResult);
                return resTophits;  // 空リスト
            }
            
            var querys = blxLines.GroupBy(s => s.Split("\t").First());
            foreach(var que in querys)
            {


                var topEval = que.First().Split("\t").ElementAt(BlxEvaltClm);
                var topScore = que.First().Split("\t").ElementAt(BlxScorClm);
                var tops = que.Where(s => double.Parse( s.Split("\t").ElementAt(BlxEvaltClm)) < cutoffEvalue
                                                    && double.Parse(s.Split("\t").ElementAt(BlxIdentClm))  > cutoffIdentity
                                                    && s.Split("\t").ElementAt(BlxEvaltClm) == topEval 
                                                    && s.Split("\t").ElementAt(BlxScorClm) == topScore);

                foreach(var top in tops)
                {
                    var _tabler = top.Split("\t");
                    resTophits.Add(
                                    new KeyValuePair<string, string>(
                                        _tabler.ElementAt(BlxQueryClm),
                                        _tabler.ElementAt(BlxReferClm))
                                    );
                }

            }

            return resTophits;
        }
        
        // Cog fasta name -> cogNm + counts 
        public static string pattern = @"(.)(\d)$";
        public static IEnumerable<KeyValuePair<string, int>> GetCogCounts(IEnumerable<KeyValuePair<string, string>> cogProt2cogNm, IEnumerable<string>cogFastaIds, IProgress<string> progress = null)
        {
            if (progress == null)
                progress = new Progress<string>(s => System.Diagnostics.Debug.WriteLine(s));

            var cogCounts = new Dictionary<string, int>();

            foreach(var pid in cogFastaIds)
            {
                if (string.IsNullOrEmpty(pid )) continue;

                var much = Regex.Match(pid, pattern);
                if(!much.Success)
                {
                    progress.Report("caution !!  COG Protein-ID has not branch number?  "+ pid);
                    continue;
                }

                var cogPid = pid.Substring(0, much.Index) + "." + much.Value.Last();

                var coghits = cogProt2cogNm.Where(s => s.Key.Equals(cogPid));
                if (coghits.Count() > 0)
                {
                    foreach(var cog in coghits)
                    {

                        if (cogCounts.ContainsKey(cog.Value))
                            cogCounts[cog.Value] += 1;
                        else
                            cogCounts.Add(cog.Value, 1);
                    }
                }
                else
                {
                    progress.Report("error, not found cog-protein id " + cogPid);
                }
            }


            return cogCounts;
        }

    }
}



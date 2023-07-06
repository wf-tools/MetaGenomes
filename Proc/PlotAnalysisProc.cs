using ShotgunMetagenome.External;
using ShotgunMetagenome.Proc.Flow;
using ShotgunMetagenome.Several;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using static WfComponent.Utils.FileUtils;

namespace ShotgunMetagenome.Proc
{

    // Second : group analysis.
    public partial class BasicAnalysisProc
    {

        public static string outMatrixDatDir = "data";
        public static string outPcoaDatDir = "PCoA";
        public static string outNmdsDatDir = "nMDS";

        public static string groupFile = "group.txt";

        private string CreatePlot()
        {
            if (_p.groups.Count() < 3)
            {
                progress.Report("sample group less than 3,  plot-flow... ");
                // return IFlow.NormalEndMessage;
            }

            // １．PcoA 
            var res = CreatePcoa();
            if (res != IFlow.NormalEndMessage)
                return res;  // error end...


            // ここまでくれば正常
            return IFlow.NormalEndMessage;
        }

        // PCoA の作成
        private string CreatePcoa() 
        {
            this.ExecuteMethod = System.Reflection.MethodBase.GetCurrentMethod().Name;

            // all-centrifuge -> recentrifuge 
            var datas = FindFile(tmpDir, recentrifugeName, "csv");

            var csvPath = datas.Any() ? datas.First() : string.Empty;
            if (string.IsNullOrEmpty(csvPath)) 
            {
                progress.Report("not found reCentrifuge csv file... ");
                return IFlow.ErrorEndMessage;  // empty
            }

            var datDir = Path.Combine(tmpDir, outMatrixDatDir);
            if (!Directory.Exists(datDir))
                Directory.CreateDirectory(datDir);

            var groupFile = OutGroupFile( datDir);
            if(string.IsNullOrEmpty(groupFile))
                return groupFile;   // string.empty 

            var pcoaOutDir = Path.Combine(
                                            Path.GetDirectoryName(csvPath),
                                            outPcoaDatDir);

            var nmdsOutDir = Path.Combine(
                                            Path.GetDirectoryName(csvPath),
                                            outNmdsDatDir);

            if (! Directory.Exists(pcoaOutDir))
                Directory.CreateDirectory(pcoaOutDir);

            if (!Directory.Exists(nmdsOutDir))
                Directory.CreateDirectory(nmdsOutDir);

            // reCentrifuge csv -> rank-matrix
            var rankTableDic = ExtractRank.OutRank(csvPath, datDir);
            foreach (var rank in ExtractRank.ranks)
            {
                if (rank == ExtractRank.ranks.Last())  // kingdum 除く
                    continue;

                this.ExecuteMethod = rank + " " + 
                                System.Reflection.MethodBase.GetCurrentMethod().Name;

                if (rankTableDic.ContainsKey(rank))  // rank 別に作成されたはず
                {
                    var rankTable = rankTableDic[rank];

                    var pcoaOutImage = 
                        Path.Combine(
                            pcoaOutDir,
                            Path.GetFileNameWithoutExtension(rankTable.path) +  ".png");

                    var nmdsOutImage =
                        Path.Combine(
                            nmdsOutDir,
                            Path.GetFileNameWithoutExtension(rankTable.path) + ".png");

                    var proc = new Pcoa(
                            new PcoaOptions()
                            {
                                useTools = Pcoa.pcoaScript,  // 使う距離関数が違うだけ
                                title = rank + " /PcoA." + _p.usePcoaDistance,
                                outImage = pcoaOutImage,
                                matrixDataFile = rankTable.path,
                                distanceMetric = _p.usePcoaDistance,
                                groupFile = groupFile,
                                colors = _p.plotColors
                            });
                    var res = proc.StartProcess();
                    progress.Report(rank + "  PCoA plot " + res);


                    var nmds = new Pcoa(
                            new PcoaOptions()
                            {
                                useTools = Pcoa.nmdsScript,  // 使う距離関数が違うだけ
                                title = rank + " /nMDS." + _p.useNmdsDistance,
                                outImage = nmdsOutImage,
                                matrixDataFile = rankTable.path,
                                distanceMetric = _p.useNmdsDistance,
                                groupFile = groupFile,
                                colors = _p.plotColors
                            });
                    res = nmds.StartProcess();
                    progress.Report(rank + "  nMDS plot " + res);
                }
            }



            // 全て終わったら
            var pcoaHtmlPaths = FindFile(
                                        Path.Combine(AnywayUtils.currentDir, "data"),
                                        Pcoa.pcoaHtml);
            var nmdsHtmlPaths = FindFile(
                                        Path.Combine(AnywayUtils.currentDir, "data"),
                                        Pcoa.nmdsHtml);
            var pcoaCopyTo = Path.Combine(_p.outDirectory, Pcoa.pcoaHtml);
            var nmdsCopyTo = Path.Combine(_p.outDirectory, Pcoa.nmdsHtml);
            if (pcoaHtmlPaths != null && pcoaHtmlPaths.Any())
            {
                if (File.Exists(pcoaCopyTo)) File.Delete(pcoaCopyTo);
                File.Copy(pcoaHtmlPaths.First(), pcoaCopyTo);
            }
            if (nmdsHtmlPaths != null && nmdsHtmlPaths.Any())
            {
                if (File.Exists(nmdsCopyTo)) File.Delete(nmdsCopyTo);
                File.Copy(nmdsHtmlPaths.First(), nmdsCopyTo);
            }

            DirectoryCopy(datDir, Path.Combine(_p.outDirectory, outMatrixDatDir));
            DirectoryCopy(pcoaOutDir, Path.Combine(_p.outDirectory, outPcoaDatDir));
            DirectoryCopy(nmdsOutDir, Path.Combine(_p.outDirectory, outNmdsDatDir));
            return IFlow.NormalEndMessage;
        }

        // plot で使うGroup を指定するファイル
        private string OutGroupFile(string outDir)
        {
            var outPath = Path.Combine(outDir, groupFile);
            var lines = new List<string>();
            foreach(var pair in allFastqPairs)
            {
                lines.Add(GetFileBaseName(pair.FwdFastqQc)
                                + "\t"
                                + pair.GropName);
            }

            var message = string.Empty;
            WriteFile(outPath, lines, ref message);

            if (!string.IsNullOrEmpty(message)) 
            { 
                _p.progress.Report(message);
                outPath = string.Empty;   // write error で実行終了？
            }

            return outPath;
        }
    }
}

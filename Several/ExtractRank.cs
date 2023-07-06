using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static WfComponent.Utils.FileUtils;

namespace ShotgunMetagenome.Several
{
    public static class ExtractRank
    {

        public static string[] ranks = new string[]{
                                                    "species",
                                                    "genus",
                                                    "family",
                                                    "order",
                                                    "class",
                                                    "phylum",
                                                    "kingdom"};

        public static string tab = "\t";
        public static string delimiter = ",";
        public static string tsvFooter = ".tsv";

        public static int clmGroup = 3;
        public static string distinguish = "EXCLUSIVE";

        // 指定したRank を抽出する return: 
        public static IDictionary<string, RankMatrixs> OutRank(string inCsv, string outDir, IProgress<string> progress = null)
        {
            if (progress == null)
                progress = new Progress<string>(s => System.Diagnostics.Debug.WriteLine(s));

            if (!File.Exists(inCsv))   // ファイルが無い場合
                return null;

            var message = string.Empty;
            var lines = ReadFile(inCsv, ref message); // File.ReadAllLines(inCsv);
            if (!string.IsNullOrEmpty(message))
            {
                progress.Report("file read error.... in OutRank");
                progress.Report(message);
                return null;
            }

            // rank -> matrix-file-path
            var rankMatrixDic = new Dictionary<string, RankMatrixs>();
            var samples = GetSampleCount(   // ヘッダからサンプルの名前を取得
                                    lines.First().Split(delimiter));

            foreach (var rank in ranks)
            {
                if (rank == ranks.Last()) continue;  // kingdum 除く
                var rankTable = GetRankCounts(lines, samples, rank);

                if (rankTable.Count() < 3)
                {
                    progress.Report("no-create table.... in OutRank" + rank);
                    continue;
                }

                // ファイルへ書き込み。
                var outTablePath = Path.Combine(
                                                outDir,
                                                Path.GetFileNameWithoutExtension(inCsv) + "-" + rank + tsvFooter);

                WriteFile(outTablePath, rankTable, ref message);
                if (!string.IsNullOrEmpty(message))
                {
                    progress.Report("file write error.... Rank : " + rank);
                    progress.Report(message);
                    // return IFlow.ErrorEndMessage;
                    continue;
                }

                progress.Report("create " + rank + " table: " + outTablePath);
                // ファイルが出来たら登録。
                rankMatrixDic.Add(rank, new RankMatrixs() {
                                                            rank = rank,
                                                            path = outTablePath,
                                                            matrix = rankTable
                }); // 辞書に追加
            }
            
            return rankMatrixDic;  // Dictionary  rank->RankMatrixs.class
        }

        public static IEnumerable<string> GetSampleCount(IEnumerable<string> cdvHeader)
        {
            var sampleNames = new List<string>();
            bool isFst = true;
            foreach (var name in cdvHeader)
            {
                if (isFst) // 最初のSamples をスキップ
                {
                    isFst = false;
                    continue;
                }

                if (name.Contains(distinguish))
                    break;

                var sampleId = Path.GetFileNameWithoutExtension(name).Replace("-class", "");
                if (!sampleNames.Contains(sampleId))
                {
                    System.Diagnostics.Debug.WriteLine("add sample name " + sampleId);
                    sampleNames.Add(sampleId);
                }
            }

            return sampleNames;  // 順番も大事。
        }

        private static int GetRankClm(string line2)
        {
            var rankClm = line2.Split(delimiter).ToList().IndexOf("Rank");
            return rankClm;
        }

        private static int GetNameClm(string line2)
        {
            var nameClm = line2.Split(delimiter).ToList().IndexOf("Name");
            return nameClm;
        }

        // private static int idClm = 0;
        public static IEnumerable<int> GetSampleCountClm( int sampleCnt)
        {
            var clmList = new List<int>();
            for (int clm = 0; clm < sampleCnt; clm++)
                clmList.Add(1 + clm * clmGroup);

            return clmList;
        }

        public static IEnumerable<string> GetRankCounts(IEnumerable<string> csvLines, IEnumerable<string> sampleNames, string rank)
        {

            var table = new List<string>();
            table.Add("Id" + tab + string.Join(tab, sampleNames));  // teble header 

            // Cloumn-no. 
            var sampleClms = GetSampleCountClm(sampleNames.Count());

            int rankClm = GetRankClm(csvLines.Take(2).Last());  // csv の 2行目にある。
            int taxNameClm = GetNameClm(csvLines.Take(2).Last());  // csv の 2行目にある。
            foreach (var line in csvLines)
            {
                var csv = line.Split(delimiter);
                if (csv[rankClm] == rank)
                {
                    var clmIdx = -1;
                    var sampleLiner = new List<string>();
                    sampleLiner.Add(csv[taxNameClm] + "(" + csv.First() + ")");

                    // 当該Rank の サンプル Count を取得する。
                    foreach (var item in csv)
                    {
                        clmIdx += 1;
                        if (sampleClms.Contains(clmIdx))
                            sampleLiner.Add(item);
                    }
                    table.Add(string.Join(tab, sampleLiner));
                }
            }

            return table;  // 多変数解析用テーブル
        }

    }

    public class RankMatrixs
    {
        public string rank;
        public string path;
        public IEnumerable<string> matrix;
    }

}

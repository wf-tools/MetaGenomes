using ShotgunMetagenome.External;
using ShotgunMetagenome.Models;
using ShotgunMetagenome.Proc.Flow;
using ShotgunMetagenome.Several;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using static ShotgunMetagenome.Several.ExtractRank;
using static WfComponent.Utils.FileUtils;

namespace ShotgunMetagenome.Proc
{
    // third : group analysis.
    public partial class BasicAnalysisProc
    {

        public static int defPermutations = 200;
        public static string permanovaHtml = "permanova.html";
        public static string permanovaTxtFooter = "-permanova.txt";
        private static string pvalueTableKey = "[PVALUES]";
        private static string fvalueTableKey = "[FVALUES]";

        private string Permanova()
        {
            if (_p.groups.Count() < 2)
            {
                progress.Report("sample group less than 2, skip permanova... end term.");
                return IFlow.NormalEndMessage;
            }

            this.ExecuteMethod = System.Reflection.MethodBase.GetCurrentMethod().Name;

            // メッセンジャー
            var message = string.Empty;

            // metagenome.rcf.data-species.tsv が出力されているか？
            var rcfdata = FindFile(tmpDir, recentrifugeName, "csv");
            if (rcfdata == null || !rcfdata.Any())
            {
                progress.Report("no-create recentrifuge resutl ? (not found csv) , skip permanova... end term.");
                return IFlow.NormalEndMessage;
            }

            // rcf.data.csv 読み込み
            var rcfLines = ReadFile(rcfdata.First(), ref message);
            if (!string.IsNullOrEmpty(message))
                progress.Report(message);

            if (!rcfLines.Any())
            {
                progress.Report("not read rcf csv) , skip permanova... end term.");
                return IFlow.NormalEndMessage;
            }

            var rcfSampleNames = GetSampleCount(rcfLines.First().Split(delimiter));
            var spTabLines = GetRankCounts(rcfLines, rcfSampleNames, ranks.First());


            var anovaOutDir = Path.Combine(
                                            tmpDir,
                                            outMatrixDatDir);   // data/


            var vsGroup = new List<KeyValuePair<SampleGroup, SampleGroup>>();
            foreach (var g1 in _p.groups)
            {
                foreach (var g2 in _p.groups)
                {
                    if (g1 == g2) continue;

                    var paired = new KeyValuePair<SampleGroup, SampleGroup>(g1, g2);
                    if (vsGroup.Contains(paired) ||
                        vsGroup.Contains(new KeyValuePair<SampleGroup, SampleGroup>(g2, g1))) continue;

                    vsGroup.Add(paired);    // group の 総当たり戦。
                }
            }

            var permanovaResults = new List<GroupDic>();
            foreach (var pairGroup in vsGroup)
            {
                // グループペアの表を作る。
                var table = GetVsTable(spTabLines, pairGroup);
                if (!table.Any())  // 基本あり得ない。
                {
                    progress.Report("not create table.." + pairGroup.Key.Name + "  " + pairGroup.Value.Name);
                    continue;
                }
                // 出力先ファイル
                var outTable = Path.Combine(
                                            anovaOutDir,
                                            pairGroup.Key.Name + "-" + pairGroup.Value.Name + ".tsv");

                var outGroup = Path.Combine(
                                            anovaOutDir,
                                            pairGroup.Key.Name + "-" + pairGroup.Value.Name + "-group.tsv");


                if (File.Exists(outTable)) File.Delete(outTable);
                if (File.Exists(outGroup)) File.Delete(outGroup);

                WriteFile(outTable, table, ref message);
                if (!string.IsNullOrEmpty(message))
                {
                    progress.Report("write table error, " + outTable);
                    continue;
                }

                var sampleGroupList = GetSampleGroup(pairGroup);
                if (sampleGroupList.Count() < 2)
                {
                    progress.Report("not permanova execute, sample requires 3+ .");
                    continue;
                }

                WriteFile(outGroup, sampleGroupList, ref message);
                if (!string.IsNullOrEmpty(message))
                {
                    progress.Report("write sample-group list error, " + outTable);
                    continue;
                }

                var outTextPath = DoPermanova(outTable, outGroup);

                var resdic = GetPermanovaValues(pairGroup.Key.Name, pairGroup.Value.Name, outTextPath);
                if (resdic != null)
                {
                    resdic.tableFile = outTable;
                    resdic.groupFile = outGroup;

                    permanovaResults.Add(resdic); // from pyrhon program // permanova F-value P-value
                }
            }

            // 
            var outParmanova = Path.Combine(
                                                anovaOutDir,
                                                permanovaHtml);
            if (File.Exists(outParmanova))
                File.Delete(outParmanova);

            message = OutMatrix(outParmanova, permanovaResults);
            if (!string.IsNullOrEmpty(message))
                return IFlow.ErrorEndMessage;
            
            // user out directory 
            var userOut = Path.Combine(
                                    _p.outDirectory,
                                    permanovaHtml);
            if(File.Exists(userOut))
                File.Delete(userOut);

            if (File.Exists(outParmanova))
                File.Copy(outParmanova, userOut);


            // 使ったテーブルとかの保存。
            foreach(var dic in permanovaResults)
            {
                var dataOut = Path.Combine(
                                                _p.outDirectory,
                                                "data",
                                                Path.GetFileName(dic.tableFile));
                if (File.Exists(dataOut))   
                    File.Delete(dataOut);
                if (File.Exists(dic.tableFile))
                    File.Copy(dic.tableFile, dataOut);


                var groupOut = Path.Combine(
                                _p.outDirectory,
                                "data",
                                Path.GetFileName(dic.groupFile));
                if (File.Exists(groupOut))
                    File.Delete(groupOut);
                if (File.Exists(dic.groupFile))
                    File.Copy(dic.groupFile, groupOut);
            }

            return message;
        }

        // 1vs1 group table
        public IEnumerable<string> GetVsTable(IEnumerable<string> speciesTsv, KeyValuePair<SampleGroup, SampleGroup> vsGroup)
        {
            // Group in fastq name = sample name
            // var samples = vsGroup.Key.FilePaths.Concat(vsGroup.Value.FilePaths)
            //                         .Select(s => GetFileBaseName(s))
            //                         .ToArray();
            var samples = new List<string>();
            foreach (var pair in allFastqPairs)
            {
                if (pair.GropName == vsGroup.Key.Name ||
                    pair.GropName == vsGroup.Value.Name)

                    samples.Add(GetFileBaseName(pair.FwdFastqQc));
            }

            // 当該fastq 
            var outClmno = GetSampleClms(speciesTsv.First(), samples.ToArray());

            var outLines = new List<string>();
            foreach (var line in speciesTsv)
            {
                var oneLine = new List<string>();
                var tsvClm = line.Split(tab);
                foreach (var clm in outClmno)
                {
                    oneLine.Add(tsvClm[clm]);
                }
                outLines.Add(string.Join(tab, oneLine)); // 1行分。。。
            }

            return outLines;  // tabler lines.
        }

        // 当該Fastq の カラム番号を取得
        private IEnumerable<int> GetSampleClms(string header, string[] names)
        {

            var clmno = 0;
            var clmnos = new List<int>();
            clmnos.Add(clmno);  // ID header
            foreach (var clmName in header.Split(tab))
            {
                if (names.Contains(clmName))
                    clmnos.Add(clmno);

                clmno++;
            }
            return clmnos;
        }

        // oneway sample-group 
        private IEnumerable<string> GetSampleGroup(KeyValuePair<SampleGroup, SampleGroup> vsGroup)
        {

            var nameGroupLine = new List<string>();
            foreach (var pair in allFastqPairs)
            {
                if (pair.GropName == vsGroup.Key.Name ||
                    pair.GropName == vsGroup.Value.Name)

                    nameGroupLine.Add(GetFileBaseName(pair.FwdFastqQc)
                                + tab
                                + pair.GropName);
            }

            return nameGroupLine;
        }

        private string DoPermanova(string tablePath, string groupingPath)
        {
            this.ExecuteMethod = System.Reflection.MethodBase.GetCurrentMethod().Name;

            var outPath = Path.Combine(
                                    Path.GetDirectoryName(tablePath),
                                    Path.GetFileNameWithoutExtension(tablePath) + permanovaTxtFooter);

            var permutations = string.IsNullOrEmpty(_p.usePermanovaPermutations)
                                            ? External.Permanova.defaultPermutations
                                            : _p.usePermanovaPermutations;

            var proc = new Permanova(
                                new PermanovaOptions()
                                {
                                    progress = this.progress,
                                    matrixDataFile = tablePath,
                                    groupFile = groupingPath,
                                    permutations = permutations,
                                    outPath = outPath
                                });

            var res = proc.StartProcess();
            progress.Report("permanova " + res  + "    Permutations=" + permutations); // 回数も載せる

            return outPath;
        }

        private GroupDic GetPermanovaValues(string group1, string group2, string resultsPath)
        {
            if (!File.Exists(resultsPath))
            {
                progress.Report("not found permanova result file...." + resultsPath);
                return null;
            }  // permanova script error-end.

            var message = string.Empty;
            var reslines = ReadFile(resultsPath, ref message);
            if( !string.IsNullOrEmpty(message))
            {
                progress.Report("read error, no permanova results, " + resultsPath);
                progress.Report(message);
                return null;
            }

            var permanovaFandP = reslines.Last().Split(tab);
            return new GroupDic()
            {
                group1 = group1,
                group2 = group2,
                fValue = permanovaFandP.First(),
                pValue = permanovaFandP.Last()
            };
        }

        public string OutMatrix(string outPath, List<GroupDic> dicmtrix)
        {
            var html = GetTemplateHtml();
            if (html == null) return IFlow.ErrorEndMessage;  // テンプレートなし　エラー

            var pvalueLine = GetResultsMatrix(dicmtrix, true);
            var fvalueLine = GetResultsMatrix(dicmtrix, false);

            if (pvalueLine.Count() < 2 || fvalueLine.Count() < 2)
                return IFlow.ErrorEndMessage;

            var outHtml = new List<string>();
            foreach(var line in html)
            {
                if(line == pvalueTableKey)
                {
                    foreach(var item in GetHtmlTable(pvalueLine))
                        outHtml.Add(item);
                    continue;
                }
                if (line == fvalueTableKey)
                {
                    foreach (var item in GetHtmlTable(fvalueLine))
                        outHtml.Add(item);
                    continue;
                }
                outHtml.Add(line);
            }

            var message = string.Empty;
            WriteFile(outPath, outHtml, ref message);
            if(string.IsNullOrEmpty(message))
                progress.Report("permanova html write error...  " + outPath);

            return message;
        }

        private IEnumerable<string> GetTemplateHtml()
        {
            var templateHtml = FindFile(
                            Path.Combine(AnywayUtils.currentDir, "data"),
                            permanovaHtml);
            if (templateHtml == null || !templateHtml.Any())
            {
                progress.Report("not found template-htm, " + permanovaHtml);
                progress.Report("no create permanova results teble. Please check data-directory.");
                return null;
            }

            var message = string.Empty;
            var htmlLine = ReadFile(templateHtml.First(), ref message);
            if (!string.IsNullOrEmpty(message))
            {
                progress.Report("error, read template... " + permanovaHtml);
                return null;
            }

            return htmlLine;
        }

        public static IEnumerable<string> GetHtmlTable(IEnumerable<string> line)
        {
            var tableLine = new List<string>();
            tableLine.Add("<thead>");
            tableLine.Add("<tr>");
            foreach(var row in line.First().Split(tab))
            {
                tableLine.Add("<th>" + row.Trim() + "</th>");
            }
            tableLine.Add("</tr>");
            tableLine.Add("</thead>");

            tableLine.Add("<tbody>");
            // line.ToList().RemoveAt(0);

            foreach (var row in line.Skip(1))  // 2行目以降
            {
                var isFst = true;
                tableLine.Add("<tr>");
                foreach (var clm in row.Split(tab))
                {
                    if (isFst)
                    {
                        tableLine.Add("<th>" + clm.Trim() + "</th>");
                        isFst = false;
                        continue;
                    }
                    tableLine.Add("<td>" + clm.Trim() + "</td>");
                }
                tableLine.Add("</tr>");
            }
            tableLine.Add("</tbody>");

            return tableLine;
        }

        public static IEnumerable<string> GetResultsMatrix(List<GroupDic> dic, bool isPvalue)
        {
            // var samples = new List<string>();
            // var g1 = dic.Select(s => s.group1);
            // var g2 = dic.Select(s => s.group2);
            // samples.Concat(g1);
            // samples.Concat(g2);

            var samples = (dic.Select(s => s.group1).ToList().Concat(dic.Select(s => s.group2).ToList()));
            samples = samples.Distinct();
            samples.ToList().Sort();

            var reslist = new List<string>();
            reslist.Add(string.Join(tab, new List<string> { "id" }.Concat(samples)));  // header

            foreach(var r in samples)
            {
                var rowline = new List<string>();
                rowline.Add(r); // row header.
                foreach (var c in samples)
                {
                    if (r == c)
                    {
                        rowline.Add("-");
                        continue;
                    }
                       
                    var groupdic = dic.Where(s => s.IsPair(r, c));
                    if (groupdic != null && groupdic.Any())
                    {
                        rowline.Add(
                            isPvalue ? groupdic.First().pValue : groupdic.First().fValue);
                    }
                    else
                    {
                        rowline.Add("n/a");
                    }
                }
                reslist.Add(string.Join(tab, rowline));
            }

            return reslist;
        }


    }

    public class GroupDic
    {
        public string group1;
        public string group2;
        public string fValue;
        public string pValue;
        public string tableFile;
        public string groupFile;

        public bool IsPair(string r, string c)
        {
            if (string.IsNullOrEmpty(group1) ||
                string.IsNullOrEmpty(group2) ||
                string.IsNullOrEmpty(r) ||
                string.IsNullOrEmpty(c))
                return false;

            if (group1 == r && group2 == c)
                return true;

            if (group1 == c && group2 == r)
                return true;

            return false;
        }

    }
}

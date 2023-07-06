using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;

namespace ShotgunMetagenome.Several
{
    public static  class AnywayUtils
    {

        public static string currentDir =>
            System.IO.Path.GetDirectoryName(System.Diagnostics.Process.GetCurrentProcess().MainModule.FileName);
        // System.AppDomain.CurrentDomain.BaseDirectory.TrimEnd('\\')

        public static readonly string settingFileName = "setting.txt";
        public static readonly string tmpLine = "tmp=";
        private static string tmpDir =>
                             Path.Combine( @"C:\ProgramData", "metagenome");

        public static string GetTmpDir()
        {
            var settingFile = Path.Combine( currentDir, dataDir, settingFileName);
            if (!File.Exists(settingFile))
                return tmpDir;   // default settings

            var message = string.Empty;
            var lines = WfComponent.Utils.FileUtils.ReadFile(settingFile, ref message);
            if (!string.IsNullOrEmpty(message) || !lines.Any())
                return tmpDir;

            var tmpline = lines.Where(s => s.StartsWith(tmpLine));
            if(! tmpline.Any())
                return tmpDir;

            var tmplineDir = tmpline.Last().Split("=").Last();
            if (Directory.Exists(tmplineDir))   // TODO space,2byte-char
                return tmplineDir;

            return tmpDir;
        }

        // データを保持しているディレクトリ
        public static readonly string dataDir = "data";

        // database directory name(固定)
        public static readonly string databaseDir = "database";
        public static readonly string databaseTxt = "database.txt";

        // centrifuge database.
        public static readonly string centrifugeDbFooter = ".1.cf";

        // mapping references dir
        public static readonly string referenceDir = "references";
        public static readonly string referenceFooter = ".fasta";

        // Centrifuge
        // http://ccb.jhu.edu/software/centrifuge/manual.shtml

        // centrifuge database file....
        public static IEnumerable<string> GetDatabase(ref string message)
        {
            // current directory database
            var currentSearchPath = Path.Combine(currentDir, dataDir);
            var currentDatabase = GetDatabasePaths(currentSearchPath, ref message);

            // text writen database
            var textDatabase = GetDatabaseFromText(ref message);
            return currentDatabase.Concat(textDatabase);
        }

        public static IEnumerable<string> GetDatabaseFromText(ref string message)
        {
            var textWriteDatabase = new List<string>();

            var databaseText = WfComponent.Utils.FileUtils.FindFile(currentDir, databaseTxt);
            if (databaseText == null || !databaseText.Any()) 
            {  
                message += "not found configuration file, " + databaseText;
                return textWriteDatabase;    // text が 無ければ空。
            }

            var textLine = WfComponent.Utils.FileUtils.ReadFile(databaseText.First(), ref message);
            var textPaths = textLine.Where(s => !s.StartsWith("#"));
            if (textPaths == null || !textPaths.Any()) 
            {
                message += "No valid rows found, " + databaseText.First(); 
                return textWriteDatabase;
            }

            foreach (var textPath in textPaths)
            {
                textWriteDatabase.Concat(GetDatabasePaths(textPath, ref message));
            }

            return textWriteDatabase;
        }

        // search centrifuge file, return path with database name.
        private static IEnumerable<string> GetDatabasePaths(string searchPath, ref string message)
        {
            // return 
            var resDatabasePath = new List<string>();

            var databaseFiles = WfComponent.Utils.FileUtils.FindFile(searchPath, "*", centrifugeDbFooter);
            if (databaseFiles != null && databaseFiles.Any())
            {
                foreach (var path in databaseFiles)
                {
                    resDatabasePath.Add(
                            Path.ChangeExtension(
                                Path.ChangeExtension(path, null), null));
                }
            }
            else
            {
                message += "not found centrifuge-dababase-file, " + searchPath;
            }

            return resDatabasePath;
        }


        // 1st mapping reference
        public static IEnumerable<string> GetMappingReference(ref string message)
        {
            // current directory database
            var currentSearchPath = Path.Combine(currentDir, dataDir, referenceDir);
            // return 
            var resDatabasePath = new List<string>();

            var referenceFiles = WfComponent.Utils.FileUtils.FindFile(currentSearchPath, "*", referenceFooter);
            if (referenceFiles != null && referenceFiles.Any())
            {
                foreach (var path in referenceFiles)
                {
                    resDatabasePath.Add(
                            Path.ChangeExtension(
                                Path.ChangeExtension(path, null), null));
                }
            }
            else
            {
                message += "not found mapping-reference , " + currentSearchPath;
            }

            return resDatabasePath;


        }

        #region Kraken 
        // kraken ハッシュテーブル
        public static readonly string hashFile = "hash.k2d";
        public static readonly string kmersFooter = "*.kmer_distrib";

        // kraken database search...
        public static IEnumerable<string> GetKrakenDatabase(ref string message)
        {
            var currentDatabase = GetHashDir(
                                                    Path.Combine(currentDir, dataDir),
                                                    ref message);

            var textDatabase = GetKrakenDatabaseTextDir(ref message);
            var enbleDatabase= currentDatabase.Concat(textDatabase);

            return enbleDatabase;
        }

        public static IEnumerable<string> GetKrakenDatabaseTextDir(ref string message)
        {
            var datTxt = Path.Combine(
                                    currentDir,
                                    dataDir,
                                    databaseTxt);
            if (!File.Exists(datTxt))
            {
                message += "not found database.txt.   " + datTxt;
                return Enumerable.Empty<string>(); // その他Database設定なし
            }
            // ファイルの中身
            var lines =WfComponent.Utils.FileUtils.ReadFile(datTxt, ref message);
            if (!string.IsNullOrEmpty(message) || ! lines.Any())
            {
                message += "not found valid line. ";
                return Enumerable.Empty<string>(); // その他Database設定なし
            }
            // 記述のある最後の行がディレクトリで無かったら
            var databaseDir = lines.Where(s => !s.StartsWith("#") || ! string.IsNullOrEmpty(s)).Last();
            if (!Directory.Exists(databaseDir)) 
            {
                message += "not set directory path.";
                return Enumerable.Empty<string>(); // その他Database設定なし
            }

            return GetHashDir(databaseDir, ref message);
        }


        // hash.k2d が入っているディレクトリを返す
        public static IEnumerable<string> GetHashDir(string databaseDir, ref string message)
        {
            var databases =
                        WfComponent.Utils.FileUtils.FindFile(databaseDir, hashFile);
            if(!databases.Any())
            {
                message += "not found hashFile, in " + databaseDir;
                return Enumerable.Empty<string>();
            }

            // hash.k2d が入っているディレクトリを返す。
            return databases.Select(s => Path.GetDirectoryName(s)); 
        }

        // ディレクトリ内にある kmer ファイルから kmer リストにして返す。
        public static IEnumerable<string> GatKmers(string datDir)
        {

            var kmerFiles =Directory.EnumerateFiles(
                                    datDir, kmersFooter, System.IO.SearchOption.AllDirectories);

            if(!kmerFiles.Any()) {
                return Enumerable.Empty<string>(); // その他Database設定なし
            }


            var kmer = kmerFiles.Select(s => 
                                        Regex.Replace(
                                           Path.GetFileName(s), @"[^0-9]", ""));
            return kmer;
        }
        #endregion
    }
}

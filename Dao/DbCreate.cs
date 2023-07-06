using System;
using System.Collections.Generic;
using System.Data.SQLite;
using System.IO;

namespace ShotgunMetagenome.Dao
{
    public static class DbCreate
    {

        // public static readonly string createSequencerSql = "/Dao/CreateSQL/CreateSequencer.txt";
        // public static readonly string createMinionParameters = "/Dao/CreateSQL/CreateTableMinionParams.txt";
        // public static readonly string createMiseqParameters = "/Dao/CreateSQL/CreateTableMiseqParams.txt";
        // public static readonly string createTableSample = "/Dao/CreateSQL/CreateTableSample.txt";
        private static readonly string[] DbTables = new string[]
        {
             "ShotgunMetagenome.Dao.CreateSQL.CreateSequencer.txt",

        };
        public static string[] GetDbTables() => DbTables;

        public static bool CheckDb(string[] tables, ref string message)
        {
            var sqliteFile = Path.Combine(
                                    AppDomain.CurrentDomain.BaseDirectory.TrimEnd('\\'),
                                    DbCommon.sqliteFile);
            try
            {
                if (File.Exists(sqliteFile))
                    Dao.SequencerDao.GetSequencer(); // throwable exception
                else
                    foreach (var table in tables)
                        CreateDb(table);


            }
            catch (Exception e)
            {
                // 中間dataディレクトリが無い
                if (!Directory.Exists(Path.GetDirectoryName(sqliteFile)))
                {
                    Directory.CreateDirectory(Path.GetDirectoryName(sqliteFile));
                    CheckDb(tables, ref message);
                }
                System.Diagnostics.Debug.WriteLine("DB Check error, " + e.Message);
                message += "DB Check error, " + e.Message + Environment.NewLine;
                return false;  // Check 失敗　
            }

            return true;
        }

        private static void CreateDb(string createSqlTxtPath)
        {
            var createsql = GetLocalResouce(createSqlTxtPath);
            if (string.IsNullOrEmpty(createsql)) return;

            ExecCreateDb(createsql);
        }

        public static string ExecCreateDb(string createsql)
        {
            var message = string.Empty;
            try
            {
                var sqlConnectionSb = new SQLiteConnectionStringBuilder { DataSource = DbCommon.sqliteFile };
                using (var cn = new SQLiteConnection(sqlConnectionSb.ToString()))
                {
                    cn.Open();
                    using (var command = new SQLiteCommand(cn))
                    {
                        command.CommandText = createsql;
                        command.ExecuteNonQuery();
                    }
                }
            }
            catch (Exception e)
            {
                message = e.Message;
            }
            return message;
        }

        public static IEnumerable<string> GetDefaultCreateTabels()
        {
            var resSqls = new List<string>();
            foreach (var tbl in DbTables)
            {
                var createsql = GetLocalResouce(tbl);
                if (!string.IsNullOrEmpty(createsql))
                    resSqls.Add(createsql);
            }
            return resSqls;
        }

        public static string GetLocalResouce(string assemblyPath)
        {
            var resouceTxt = string.Empty;
            try
            {
                var assembly = System.Reflection.Assembly.GetExecutingAssembly();
                using (var stream = assembly.GetManifestResourceStream(assemblyPath))
                    if (stream != null)
                        using (var sr = new StreamReader(stream))
                            resouceTxt = sr.ReadToEnd();

            }
            catch (Exception e)
            {
                System.Diagnostics.Debug.WriteLine(e.Message);
                return string.Empty;
            }

            return resouceTxt;
        }

    }

}

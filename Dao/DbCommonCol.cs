using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SQLite;
using System.Linq;
using System.Reflection;

namespace ShotgunMetagenome.Dao
{
    public static partial class DbCommon 
    {
        public static List<string> ColumnName(string tableName)
        {
            // return object
            var clmNames = new List<string>();

            string selectSql = "PRAGMA table_info('" + tableName + "')";
            var sqlConnectionSb = new SQLiteConnectionStringBuilder { DataSource = sqliteFile };
            using (var connection = new SQLiteConnection(sqlConnectionSb.ToString()))
            {
                connection.Open();
                using (var command = new SQLiteCommand(connection))
                {
                    command.CommandText = selectSql;
                    SQLiteDataReader table = command.ExecuteReader();
                    while (table.Read())
                    {
                        // var clmPk = (long) table.GetValue(table.GetOrdinal("pk"));
                        // if ((long)table.GetValue(table.GetOrdinal("pk")) > 0) continue;
                        clmNames.Add(table.GetValue(table.GetOrdinal("name")).ToString());
                    }
                }
                // connection.Close();  //using
            }
            return clmNames;
        }

        public static List<string> EnableNames(string tableName, object properties)
        {
            var enableClm = new List<string>();
            var clmNames = ColumnName(tableName);
            if (properties == null) return enableClm;  // ありえない。

            PropertyInfo[] infoArray = properties.GetType().GetProperties();
            foreach (PropertyInfo info in infoArray)
            {
                // Console.WriteLine(info.Name + ": " + info.GetValue(propaties, null));
                if (info.GetValue(properties, null) != null)
                {
                    // Console.WriteLine("#### " + info.Name);
                    if (clmNames.Contains(info.Name.ToString()))
                        enableClm.Add(info.Name.ToString());
                }
            }
            return enableClm;
        }

        public static string InsertStatment(string tableName, object p, ref List<DbTypeValue> statmentSetVals, string[] withoutColumn = null)
        {
            // List<string> statmentClms = null;
            // statmentSetVals = GetTypeValuePairs(tableName,  p, ref statmentClms, withoutColumn);
            statmentSetVals = GetTypeValuePairs(tableName, p, withoutColumn);
            // Console.WriteLine();
            return "INSERT INTO " + tableName + " ( "
                   + string.Join(", ", statmentSetVals.Select(s => s.ColumnName))
                   + ") values ("
                   // + string.Join(", ", statmentVals)
                   + string.Join(", ", (new string('?', statmentSetVals.Count)).ToCharArray())
                   + " )";
        }

        public static string UpdateStatment(string tableName, object p, ref List<DbTypeValue> statmentSetVals, string[] withoutColumn = null)
        {
            statmentSetVals = GetTypeValuePairs(tableName, p, withoutColumn);
            var id = p.GetType().GetProperty("ID").GetValue(p);


            return "UPDATE " + tableName +
                     " set " + string.Join(" = ?,", statmentSetVals.Select(s => s.ColumnName))
                                + " = ? " +
                     " where ID = " + id;
        }

        // public static List<Model.TypeValuePair> GetTypeValuePairs(string tableName, object p, ref List<string> statmentClms, string[] withoutColumn = null)
        public static List<DbTypeValue> GetTypeValuePairs(string tableName, object p, string[] withoutColumn = null)
        {
            withoutColumn = (withoutColumn == null) ? Array.Empty<string>() :
                                                            withoutColumn.Select(s => s.ToLower()).ToArray();

            var statmentSetVals = new List<DbTypeValue>();  //　初期化？

            var type = p.GetType();
            foreach (var clm in EnableNames(tableName, p))
            {
                // if (clm.ToLower() == "id") continue;
                if (withoutColumn.Contains(clm.ToLower())) continue;
                var property = type.GetProperty(clm);
                var className = p.GetType().GetProperty(clm).PropertyType.FullName;

                // stringのとき
                if (className == typeof(string).FullName)
                {
                    // statmentClms.Add(clm);
                    statmentSetVals.Add(new DbTypeValue
                    {
                        ColumnName = clm,
                        Type = System.Data.DbType.String,
                        Value = property.GetValue(p).ToString(),
                    });
                }
                else if (className == typeof(bool).FullName)
                {
                    try
                    {
                        // statmentClms.Add(clm);
                        statmentSetVals.Add(new DbTypeValue
                        {
                            ColumnName = clm,
                            Type = System.Data.DbType.Boolean,
                            Value = (bool)property.GetValue(p),
                        });
                    }
                    catch (Exception e)
                    {
                        Console.WriteLine(e.Message);
                        var outDir = System.IO.Path.Combine(
                                            AppDomain.CurrentDomain.BaseDirectory,
                                            "logs");
                        if (!System.IO.Directory.Exists(outDir)) System.IO.Directory.CreateDirectory(outDir);
                        System.IO.File.WriteAllText(e.Message, System.IO.Path.Combine(outDir, "dberror.log"));
                    }
                }
                // そのた。（TODO:詳細に分けなければいけないかも？）
                else
                {
                    // statmentClms.Add(clm);
                    var val = property.GetValue(p);
                    var dbtype = Object2DbType(val);
                    statmentSetVals.Add(new DbTypeValue
                    {
                        ColumnName = clm,
                        Type = dbtype,
                        Value = val,
                    });
                }
            }
            return statmentSetVals;
        }

        public static System.Data.DbType Object2DbType(object obj)
        {
            var className = obj.GetType().FullName;
            if (className == typeof(int).FullName) return System.Data.DbType.Int32;
            if (className == typeof(long).FullName) return System.Data.DbType.Int64;
            if (className == typeof(float).FullName) return System.Data.DbType.Double;
            if (className == typeof(double).FullName) return System.Data.DbType.Double;
            if (className == typeof(DateTime).FullName) return System.Data.DbType.DateTime;
            if (className == typeof(bool).FullName) return System.Data.DbType.Boolean;

            // if (className == typeof(string).FullName) return System.Data.DbType.String;
            return System.Data.DbType.String; // Exception...
        }

        // db spliceing value
        public static string GetDbValue(string dbValue, int topNo)
        {
            var val = dbValue.Split(DbDelimiter);
            if (topNo < val.Length)
                return val[topNo];

            return string.Empty;
        }
    }
}

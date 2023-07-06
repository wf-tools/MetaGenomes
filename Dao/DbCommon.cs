using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SQLite;
using System.IO;

namespace ShotgunMetagenome.Dao
{
    /**
         SQLite の基幹クラス
         ローカルでしか使わないので詳細なSerializeは略。
         都度　Open->Close を行う。
      */
    public static partial class DbCommon
    {
        public const string sqliteFile = @".\data\data.dat";
        public static readonly string DbDelimiter = "///";

        // 子クラスからマッピング済みのString-SQLだけもらう。
        public static long ExecInsert(string[] insSqls)
        {
            long insertId = 0;
            var sqlConnectionSb = new SQLiteConnectionStringBuilder { DataSource = sqliteFile };
            using (var connection = new SQLiteConnection(sqlConnectionSb.ToString()))
            {
                connection.Open();
                using (var command = new SQLiteCommand(connection))
                {
                    command.Transaction = connection.BeginTransaction();
                    foreach (var slq in insSqls)
                    {
                        command.CommandText = slq;
                        command.ExecuteNonQuery();
                    }
                    // 
                    command.Transaction.Commit();
                    insertId = connection.LastInsertRowId;  // これでとれるはず
                }
                // connection.Close(); //using{}へ変更
            }
            return insertId;
        }

        // Tab区切りのテキストで返します。（面白そうだから作っただけ）
        /**
        public static List<string> Select2string(string selectSql)
        {
            var readsObj = new List<string>();
            var sqlConnectionSb = new SQLiteConnectionStringBuilder { DataSource = sqliteFile };
            using (var connection = new SQLiteConnection(sqlConnectionSb.ToString()))
            {
                connection.Open();
                using (var command = new SQLiteCommand(connection))
                {
                    command.CommandText = selectSql;
                    SQLiteDataReader selectResults = command.ExecuteReader();
                    while (selectResults.Read())
                        readsObj.Add(string.Join("\t", Enumerable.Range(0, selectResults.FieldCount).Select(x => selectResults.GetValue(x))));
                }
                // connection.Close();
            }
            return readsObj; //
        }
        */


        public static List<object> SelectTableAll(string tableName, Type objType)
        {
            if (!File.Exists(sqliteFile))
            {
                System.Diagnostics.Debug.WriteLine("not found " + Path.GetFullPath(sqliteFile));
                return new List<object>(); // TODO error...
            }
            var selectStr = "SELECT * FROM " + tableName;
            var sqlConnectionSb = new SQLiteConnectionStringBuilder { DataSource = sqliteFile };


            var resList = new List<object>();
            using (var cn = new SQLiteConnection(sqlConnectionSb.ToString()))
            {
                cn.Open();
                using (var command = new SQLiteCommand(cn))
                {
                    command.CommandText = selectStr;
                    using (var reader = command.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            int i = 0;
                            var parameter = Activator.CreateInstance(objType);

                            foreach (var clm in reader.GetValues().AllKeys)
                            {
                                var dbClmValue = reader.GetValue(i);
                                var propertyObj = parameter.GetType().GetProperty(clm);
                                i++;

                                // Console.WriteLine(clm + "  / " + dbClmValue);
                                if (propertyObj != null)
                                {
                                    var propertyObjClass = propertyObj.PropertyType.FullName;

                                    if (propertyObjClass == typeof(string).FullName)
                                        propertyObj.SetValue(parameter, dbClmValue.ToString());

                                    if (propertyObjClass == typeof(DateTime?).FullName)
                                        propertyObj.SetValue(parameter, (DateTime)dbClmValue);

                                    if (propertyObjClass == typeof(bool).FullName)
                                        propertyObj.SetValue(parameter, (bool)dbClmValue);

                                    if (propertyObjClass == typeof(Int32).FullName)
                                        propertyObj.SetValue(parameter, int.Parse(dbClmValue.ToString()));

                                    if (propertyObjClass == typeof(int).FullName)
                                        propertyObj.SetValue(parameter, int.Parse(dbClmValue.ToString()));
                                }
                            }
                            // 1-read end.
                            resList.Add(Convert.ChangeType(parameter, objType));
                        }
                    }
                }
            }
            dynamic res = resList;
            return res;
        }

        public static long InsertTable(string tableName, object propaties, string[] whitoutClms = null)
        {
            // 値のあるカラムだけ
            List<DbTypeValue> valList = null;
            var statment = InsertStatment(tableName, propaties, ref valList, whitoutClms);

            long insertId = 0;
            var sqlConnectionSb = new SQLiteConnectionStringBuilder { DataSource = sqliteFile };
            using (var connection = new SQLiteConnection(sqlConnectionSb.ToString()))
            {
                connection.Open();
                using (var command = new SQLiteCommand(connection))
                {
                    command.Transaction = connection.BeginTransaction();
                    command.CommandText = statment;
                    command.Parameters.Clear();
                    foreach (DbTypeValue val in valList)
                    {
                        var value = new SQLiteParameter { DbType = (DbType)val.Type, Value = val.Value };
                        command.Parameters.Add(value);
                    }
                    command.ExecuteNonQuery();
                    command.Transaction.Commit();
                    insertId = connection.LastInsertRowId;  // これでとれるはず
                }
                // connection.Close(); // using.
            }
            return insertId;
        }

        public static long UpdateRecodeById(string tableName, object propaties, string[] whitoutClms = null)
        {
            // 値のあるカラムだけ
            List<DbTypeValue> valList = null;
            var statment = UpdateStatment(tableName, propaties, ref valList, whitoutClms);

            long updateId = 0;
            var sqlConnectionSb = new SQLiteConnectionStringBuilder { DataSource = sqliteFile };
            using (var connection = new SQLiteConnection(sqlConnectionSb.ToString()))
            {
                connection.Open();
                using (var command = new SQLiteCommand(connection))
                {
                    command.Transaction = connection.BeginTransaction();
                    command.CommandText = statment;
                    command.Parameters.Clear();
                    foreach (DbTypeValue val in valList)
                    {
                        var value = new SQLiteParameter { DbType = (DbType)val.Type, Value = val.Value };
                        command.Parameters.Add(value);
                    }
                    command.ExecuteNonQuery();
                    command.Transaction.Commit();
                    updateId = connection.LastInsertRowId;  // これでとれるはず
                }
                // connection.Close();  // using
            }
            return updateId;
        }

        public static List<long> DeleteRecord(string tableName, long[] deleteIds, string deleteClumn = "ISDELETE")
        {
            var deleteFlag = "1";   // ０以外なら何でも。
            var statment = "update " +
                                    tableName +
                                    " set " + deleteClumn +
                                    " = \'" + deleteFlag + "\'" +
                                    " where ID = @ID";
            var effectIds = new List<long>();
            var sqlConnectionSb = new SQLiteConnectionStringBuilder { DataSource = sqliteFile };
            using (var connection = new SQLiteConnection(sqlConnectionSb.ToString()))
            {
                connection.Open();

                using (var command = new SQLiteCommand(connection))
                {
                    command.Transaction = connection.BeginTransaction();
                    command.CommandText = statment;
                    foreach (var id in deleteIds)
                    {
                        var value = new SQLiteParameter("@ID", id);
                        command.Parameters.Clear();
                        command.Parameters.Add(value);
                        command.ExecuteNonQuery();
                        effectIds.Add(connection.LastInsertRowId);  // これでとれるはず
                    }
                    command.Transaction.Commit();
                }
                // connection.Close(); // using{}
            }
            return effectIds;


        }



    }
}

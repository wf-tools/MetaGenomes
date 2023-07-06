using ShotgunMetagenome.Dao.Properties;
using System.Linq;

namespace ShotgunMetagenome.Dao
{
    public static class SequencerDao
    {
        private const string TableName = "Sequencer";

        // 1-insert.
        public static long InsertPatameter(SequencerProperties p)
        {
            var withoutClm = new string[] { "id" };  // Insert で ID は AutoInclimentだから指定しない。
            long insertId = DbCommon.InsertTable(TableName, p, withoutClm);
            return insertId;
        }

        public static SequencerProperties[] GetSequencer()
        {
            var allData = DbCommon.SelectTableAll(TableName, typeof(SequencerProperties));

            // 初期インストール時の 救済処置
            if (allData == null || allData.Count < 1)
            {
                var p = new SequencerProperties
                {
                    Name = "Default",
                    Note = "init",
                };
                InsertPatameter(p);
                var ps = DbCommon.SelectTableAll(TableName, typeof(SequencerProperties));
                allData = ps;
            }
            var list = allData.Select(s => s).Cast<SequencerProperties>().ToArray();

            // 必ず1件は在るはず（初期データベースに入れている）
            return (SequencerProperties[])list;
        }


        public static SequencerProperties GetParameters(string parameterName)
        {
            var allParam = GetSequencer();
            var lambda = allParam.Select(s => s).Cast<SequencerProperties>();

            // 予防措置
            var isNameExist = lambda.Where(p => p.Name == parameterName);
            if (!isNameExist.Any())
                return lambda.Where(p => p.Id == 0).First();  // 初期のDefaultパラメータ。

            var oneParam = lambda.Where(p => p.Name == parameterName)  // 同じ名前で一番日付の新しいもの
                                                .OrderByDescending(p => p.DATE)
                                                .First();
            return oneParam;
        }

    }
}

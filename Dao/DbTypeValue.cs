namespace ShotgunMetagenome.Dao
{
    public class DbTypeValue
    {
        public string ColumnName { get; set; }
        public System.Data.DbType Type { get; set; }
        public object Value { get; set; }
    }
}

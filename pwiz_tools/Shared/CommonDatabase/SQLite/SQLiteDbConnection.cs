using pwiz.Common.SystemUtil;
using System.Collections.Generic;
using System.Data.SQLite;
using System.Diagnostics.CodeAnalysis;
using System.Linq;

namespace CommonDatabase.SQLite
{
    [SuppressMessage("ReSharper", "LocalizableElement")]
    public class SQLiteDbConnection : DbConnection
    {
        public SQLiteDbConnection(SQLiteConnection connection) : base(connection)
        {
        }

        public override bool TableExists(string tableName)
        {
            using var cmd = Connection.CreateCommand();
            cmd.CommandText = @"SELECT 1 FROM sqlite_master WHERE type='table' AND name=?";
            cmd.Parameters.Add(new SQLiteParameter { Value = tableName });
            using var reader = cmd.ExecuteReader();
            return reader.Read();
        }

        public override void SetUnsafeJournalMode()
        {
            using var cmd = Connection.CreateCommand();
            cmd.CommandText = "PRAGMA synchronous = OFF";
            cmd.ExecuteNonQuery();
            cmd.CommandText = "PRAGMA journal_mode = MEMORY";
            cmd.ExecuteNonQuery();
        }

        public static SQLiteConnectionStringBuilder NewSQLiteConnectionStringBuilder(string path)
        {
            // when SQLite parses the connection string, it treats backslash as an escape character
            // This is not normally an issue, because backslashes followed by a non-reserved character
            // are not treated specially.

            string dataSource = path.Replace("\\", "\\\\");
            return new SQLiteConnectionStringBuilder()
            {
                DataSource = dataSource,
                ToFullPath = false
            };
        }

        protected override string GetBatchInsertSql(string tableName, IList<string> columnNames, int batchSize)
        {
            if (batchSize == 1)
            {
                return base.GetBatchInsertSql(tableName, columnNames, batchSize);
            }
            var columnNamesString = string.Join(", ", columnNames.Select(QuoteIdentifier));
            var paramsString = string.Join(", ", Enumerable.Repeat("?", columnNames.Count));
            var lines = new List<string>
            {
                "INSERT INTO " + QuoteIdentifier(tableName) + " (" + columnNamesString + ")",
                "SELECT " + string.Join(", ", columnNames.Select(name => "? AS " + QuoteIdentifier(name)))
            };
            lines.AddRange(Enumerable.Repeat("UNION ALL SELECT " + paramsString, batchSize - 1));
            return CommonTextUtil.LineSeparate(lines);
        }

        public override int GetMaxBatchInsertSize(int columnCount)
        {
            return 1024 / columnCount;
        }
    }
}

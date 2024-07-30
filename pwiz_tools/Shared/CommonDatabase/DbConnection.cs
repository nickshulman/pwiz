using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SQLite;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using CommonDatabase.SQLite;
using pwiz.Common.SystemUtil;

namespace CommonDatabase
{
    [SuppressMessage("ReSharper", "LocalizableElement")]
    public class DbConnection
    {
        public static DbConnection Of(IDbConnection connection)
        {
            if (connection is SQLiteConnection sqliteConnection)
            {
                return new SQLiteDbConnection(sqliteConnection);
            }

            return new DbConnection(connection);
        }


        public DbConnection(IDbConnection connection)
        {
            Connection = connection;
        }

        public IDbConnection Connection { get; }

        public virtual bool TableExists(string tableName)
        {
            try
            {
                ExecuteScalar("SELECT 1 FROM " + QuoteIdentifier(tableName) + " WHERE 1 = 0");
                return true;
            }
            catch
            {
                return false;
            }
        }

        public object ExecuteScalar(string sql)
        {
            using var cmd = Connection.CreateCommand();
            return cmd.ExecuteScalar();
        }

        public static string QuoteIdentifier(string identifier)
        {
            return "\"" + identifier.Replace("\"", "\"\"") + "\"";
        }

        private IEnumerable<string> GetColumnNames(string tableName)
        {
            using var cmd = Connection.CreateCommand();
            cmd.CommandText = "SELECT * FROM " + QuoteIdentifier(tableName) + " WHERE 1 = 0";
            using var reader = cmd.ExecuteReader();
            var schemaTable = reader.GetSchemaTable();
            if (schemaTable != null)
            {
                return schemaTable.Rows.Cast<DataRow>().Select(row => row["ColumnName"].ToString());
            }
            return Array.Empty<string>();
        }
        public virtual void SetUnsafeJournalMode()
        {
        }

        public virtual int GetMaxBatchInsertSize(int columnCount)
        {
            return 1;
        }

        public virtual IDbCommand CreateBatchInsertCommand(string tableName, IList<string> columnNames, int batchSize)
        {
            string sql = GetBatchInsertSql(tableName, columnNames, batchSize);
            var cmd = Connection.CreateCommand();
            cmd.CommandText = sql;
            for (int i = 0; i < columnNames.Count * batchSize; i++)
            {
                cmd.Parameters.Add(cmd.CreateParameter());
            }

            return cmd;
        }
        protected virtual string GetBatchInsertSql(string tableName, IList<string> columnNames, int batchSize)
        {
            return CommonTextUtil.LineSeparate(
                "INSERT INTO " + QuoteIdentifier(tableName) + " (" +
                string.Join(", ", columnNames.Select(QuoteIdentifier)) + ")",
                "VALUES (" + string.Join(", ", Enumerable.Repeat("?", columnNames.Count)) + ")");
        }
    }
}

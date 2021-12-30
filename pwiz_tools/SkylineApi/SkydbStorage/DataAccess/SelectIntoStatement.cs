using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SQLite;
using System.Text;
using SkydbStorage.Internal;

namespace SkydbStorage.DataAccess
{
    public class SelectIntoStatement : IDisposable
    {
        private IDbCommand _command;
        private IList<Tuple<Type, SQLiteParameter>> _parameters;
        public SelectIntoStatement(SkydbSchema schema, IDbConnection connection, string sourceSchema, string targetSchema, Type tableType)
        {
            string tableName = tableType.Name;

            var insertList = new List<string>{"Id"};
            var selectList = new List<string> {"Id + ?"};
            _parameters = new List<Tuple<Type, SQLiteParameter>> { Tuple.Create(tableType, new SQLiteParameter()) };
            foreach (var columnInfo in schema.GetColumns(tableType))
            {
                string columnName = SqliteOps.QuoteIdentifier(columnInfo.Name);
                insertList.Add(columnName);
                if (columnInfo.ForeignEntityType == null)
                {
                    selectList.Add(columnName);
                }
                else
                {
                    selectList.Add(columnName + " + ?");
                    _parameters.Add(Tuple.Create(columnInfo.ForeignEntityType, new SQLiteParameter()));
                }
            }

            _command = connection.CreateCommand();
            StringBuilder commandText = new StringBuilder();
            commandText.AppendLine("INSERT INTO " + SqliteOps.QuoteIdentifier(targetSchema, tableName) + " (" +
                                   string.Join(", ", insertList) + ")");
            commandText.AppendLine("SELECT " + string.Join(", ", selectList) + " FROM " +
                                   SqliteOps.QuoteIdentifier(sourceSchema, tableName));
            _command.CommandText = commandText.ToString();
            foreach (var tuple in _parameters)
            {
                _command.Parameters.Add(tuple.Item2);
            }
        }

        public void CopyData(IDictionary<Type, long> idOffsets)
        {
            foreach (var tuple in _parameters)
            {
                tuple.Item2.Value = idOffsets[tuple.Item1];
            }

            _command.ExecuteNonQuery();
        }

        public void Dispose()
        {
            _command.Dispose();
        }
    }
}

using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SQLite;
using System.Text;
using SkydbStorage.DataApi;
using SkydbStorage.Internal.Orm;

namespace SkydbStorage.Internal
{
    public class InsertSelectStatement : IDisposable
    {
        private IDbCommand _command;
        private IList<Tuple<Type, SQLiteParameter>> _parameters;
        public InsertSelectStatement(IDbConnection connection, string sourceSchema, string targetSchema, Type tableType)
        {
            string tableName = tableType.Name;

            var insertList = new List<string>{"Id"};
            var selectList = new List<string> {"Id + ?"};
            _parameters = new List<Tuple<Type, SQLiteParameter>> { Tuple.Create(tableType, new SQLiteParameter()) };
            foreach (var tuple in new SkydbSchema().GetColumns(tableType))
            {
                string columnName = SqliteOperations.QuoteIdentifier(tuple.Item1.Name);
                insertList.Add(columnName);
                if (tuple.Item2 == null)
                {
                    selectList.Add(columnName);
                }
                else
                {
                    selectList.Add(columnName + " + ?");
                    _parameters.Add(Tuple.Create(tuple.Item2, new SQLiteParameter()));
                }
            }

            _command = connection.CreateCommand();
            StringBuilder commandText = new StringBuilder();
            commandText.AppendLine("INSERT INTO " + SqliteOperations.QuoteIdentifier(targetSchema, tableName) + " (" +
                                   string.Join(", ", insertList) + ")");
            commandText.AppendLine("SELECT " + string.Join(", ", selectList) + " FROM " +
                                   SqliteOperations.QuoteIdentifier(sourceSchema, tableName));
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
        }

        public void Dispose()
        {
            _command.Dispose();
        }
    }
}

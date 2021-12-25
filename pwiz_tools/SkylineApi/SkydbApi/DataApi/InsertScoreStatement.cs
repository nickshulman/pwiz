using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SQLite;
using System.Linq;
using System.Text;
using SkydbApi.Orm;

namespace SkydbApi.DataApi
{
    public class InsertScoresStatement : IDisposable
    {
        private IDbCommand _command;
        private IList<string> _scoreNames;
        public InsertScoresStatement(IDbConnection connection, IEnumerable<string> scoreNames)
        {
            _scoreNames = scoreNames.ToList();
            StringBuilder strCommand = new StringBuilder("INSERT INTO Scores (");
            strCommand.Append(string.Join(",", _scoreNames.Select(SqliteOperations.QuoteIdentifier)));
            strCommand.Append(") VALUES (");
            strCommand.Append(string.Join(",", _scoreNames.Select(name => "?")));
            strCommand.Append("); select last_insert_rowid();");
            _command = connection.CreateCommand();
            _command.CommandText = strCommand.ToString();
            for (int i = 0; i < _scoreNames.Count; i++)
            {
                _command.Parameters.Add(new SQLiteParameter());
            }
        }

        public void Insert(Scores scores)
        {
            for (int i = 0; i < _scoreNames.Count; i++)
            {
                ((SQLiteParameter) _command.Parameters[i]).Value = (object) scores.GetScore(_scoreNames[i]) ?? DBNull.Value;
            }

            scores.Id = Convert.ToInt64(_command.ExecuteScalar());
        }

        public void Dispose()
        {
            _command.Dispose();
        }
    }
}

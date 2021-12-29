using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SQLite;
using System.Linq;
using System.Text;
using SkydbStorage.Internal;
using SkydbStorage.Internal.Orm;

namespace SkydbStorage.DataApi
{
    public class InsertScoresStatement : PreparedStatement
    {
        private IList<string> _scoreNames;

        public InsertScoresStatement(IDbConnection connection) : this(connection, GetScoreNames(connection)) 
        {

        }
        public InsertScoresStatement(IDbConnection connection, IEnumerable<string> scoreNames) : base(connection)
        {
            _scoreNames = scoreNames.ToList();
            StringBuilder strCommand = new StringBuilder("INSERT INTO Scores (");
            strCommand.Append(string.Join(",", _scoreNames.Select(SqliteOperations.QuoteIdentifier)));
            strCommand.Append(") VALUES (");
            strCommand.Append(string.Join(",", _scoreNames.Select(name => "?")));
            strCommand.Append("); select last_insert_rowid();");
            Command.CommandText = strCommand.ToString();
            for (int i = 0; i < _scoreNames.Count; i++)
            {
                Command.Parameters.Add(new SQLiteParameter());
            }
        }

        public void Insert(Scores scores)
        {
            for (int i = 0; i < _scoreNames.Count; i++)
            {
                ((SQLiteParameter) Command.Parameters[i]).Value = (object) scores.GetScore(_scoreNames[i]) ?? DBNull.Value;
            }

            scores.Id = Convert.ToInt64(Command.ExecuteScalar());
        }

        public void CopyAll(IDbConnection connection, EntityIdMap entityIdMap)
        {
            foreach (var entity in SelectAll(connection))
            {
                var oldId = entity.Id.Value;
                entity.Id = null;
                Insert(entity);
                entityIdMap.SetNewId(typeof(Scores), oldId, entity.Id.Value);
            }
        }

        public IEnumerable<Scores> SelectAll(IDbConnection connection)
        {
            var commandText = new StringBuilder("SELECT ");
            commandText.Append(string.Join(",", _scoreNames.Prepend("Id").Select(SqliteOperations.QuoteIdentifier)));
            commandText.Append(" FROM Scores");
            using (var cmd = connection.CreateCommand())
            {
                cmd.CommandText = commandText.ToString();
                using (var reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        var scores = new Scores()
                        {
                            Id = reader.GetInt64(0)
                        };
                        for (int iScore = 0; iScore < _scoreNames.Count; iScore++)
                        {
                            var scoreValue = reader.GetValue(iScore + 1);
                            if (scoreValue == null || scoreValue is DBNull)
                            {
                                continue;
                            }
                            scores.SetScore(_scoreNames[iScore], Convert.ToInt64(scoreValue));
                        }

                        yield return scores;
                    }
                }
            }
        }

        public static IEnumerable<string> GetScoreNames(IDbConnection connection)
        {
            return SqliteOperations.ListColumnNames(connection, "Scores").Where(name => "Id" != name);
        }
    }
}

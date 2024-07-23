using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using pwiz.Common.Database;

namespace SkydbApi.DataApi
{
    public class SkydbConnection : IDisposable
    {
        private IDbTransaction _transaction;
        public SkydbConnection(IDbConnection connection)
        {
            Connection = connection;
        }
        public IDbConnection Connection { get; }

        public void SetUnsafeJournalMode()
        {
            using (var cmd = Connection.CreateCommand())
            {
                cmd.CommandText = "PRAGMA synchronous = OFF";
                cmd.ExecuteNonQuery();
                cmd.CommandText = "PRAGMA journal_mode = MEMORY";
                cmd.ExecuteNonQuery();
                // cmd.CommandText = "PRAGMA automatic_indexing=OFF";
                // cmd.ExecuteNonQuery();
                // cmd.CommandText = "PRAGMA cache_size=30000";
                // cmd.ExecuteNonQuery();
                // cmd.CommandText = "PRAGMA temp_store=MEMORY";
                // cmd.ExecuteNonQuery();
                // cmd.CommandText = "PRAGMA mmap_size=70368744177664";
                // cmd.ExecuteNonQuery();
            }
        }

        public void BeginTransaction()
        {
            if (_transaction != null)
            {
                throw new InvalidOperationException();
            }
            _transaction = Connection.BeginTransaction();
        }

        public void CommitTransaction()
        {
            _transaction.Commit();
            _transaction = null;
        }

        public void Dispose()
        {
            Connection.Dispose();
        }

        public void EnsureScores(IEnumerable<string> scoreNames)
        {
            var namesToAdd = scoreNames.Except(SqliteOperations.ListColumnNames(Connection, "Scores")).ToList();
            if (namesToAdd.Count == 0)
            {
                return;
            }
            using (var cmd = Connection.CreateCommand())
            {
                cmd.CommandText = string.Join(Environment.NewLine, namesToAdd.Select(scoreName =>
                    "ALTER TABLE Scores ADD COLUMN " + SqliteOperations.QuoteIdentifier(scoreName) + " DOUBLE;"
                ));
                cmd.ExecuteNonQuery();
            }
        }
    }
}

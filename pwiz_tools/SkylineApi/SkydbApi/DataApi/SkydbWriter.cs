using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using SkydbApi.Orm;

namespace SkydbApi.DataApi
{
    public class SkydbWriter : IDisposable
    {
        private IDbTransaction _transaction;
        private Dictionary<Type, IDisposable> _insertStatements = new Dictionary<Type, IDisposable>();
        public SkydbWriter(IDbConnection connection)
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

        public void Insert<T>(T entity) where T : Entity
        {
            GetInsertStatement<T>().Insert(entity);
        }

        private InsertStatement<T> GetInsertStatement<T>() where T : Entity
        {
            if (_insertStatements.TryGetValue(typeof(T), out IDisposable statement))
            {
                return (InsertStatement<T>) statement;
            }

            var insertStatement = new InsertStatement<T>(Connection);
            _insertStatements.Add(typeof(T), insertStatement);
            return insertStatement;
        }

        public void Dispose()
        {
            foreach (var disposable in _insertStatements.Values)
            {
                disposable.Dispose();
            }
            _insertStatements.Clear();
            Connection.Close();
        }
    }
}

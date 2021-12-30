using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using SkydbStorage.Internal;
using SkydbStorage.Internal.Orm;

namespace SkydbStorage.DataAccess
{
    public class SkydbWriter : IDisposable
    {
        private IDbTransaction _transaction;
        private Dictionary<Type, IDisposable> _insertStatements = new Dictionary<Type, IDisposable>();
        public SkydbWriter(SkydbConnection skydbConnection)
        {
            SkydbConnection = skydbConnection;
        }

        public SkydbConnection SkydbConnection { get; }

        public IDbConnection Connection
        {
            get { return SkydbConnection.Connection; }
        }

        public SkydbSchema SkydbSchema
        {
            get { return SkydbConnection.SkydbSchema; }
        }

        public void BeginTransaction()
        {
            SkydbConnection.BeginTransaction();
        }

        public void CommitTransaction()
        {
            _transaction.Commit();
            _transaction = null;
        }


        public void EnsureScores(IEnumerable<string> scoreNames)
        {
            var namesToAdd = scoreNames.Except(SqliteOps.ListColumnNames(Connection, "Scores")).ToList();
            if (namesToAdd.Count == 0)
            {
                return;
            }
            using (var cmd = Connection.CreateCommand())
            {
                cmd.CommandText = string.Join(Environment.NewLine, namesToAdd.Select(scoreName =>
                    "ALTER TABLE Scores ADD COLUMN " + SqliteOps.QuoteIdentifier(scoreName) + " DOUBLE;"
                ));
                cmd.ExecuteNonQuery();
            }
        }

        public void Insert<T>(T entity) where T : Entity, new()
        {
            GetInsertStatement<T>().Insert(entity);
        }

        private InsertStatement<T> GetInsertStatement<T>() where T : Entity, new()
        {
            if (_insertStatements.TryGetValue(typeof(T), out IDisposable statement))
            {
                return (InsertStatement<T>) statement;
            }

            var insertStatement = new InsertStatement<T>(SkydbSchema, Connection);
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
        }
    }
}

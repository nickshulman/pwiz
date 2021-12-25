﻿using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using SkydbApi.Orm;

namespace SkydbApi.DataApi
{
    public class SkydbWriter : SkydbConnection
    {
        private IDbTransaction _transaction;
        private Dictionary<Type, PreparedStatement> _insertStatements = new Dictionary<Type, PreparedStatement>();
        public SkydbWriter(IDbConnection connection) : base(connection)
        {
        }

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


        public InsertStatement<CandidatePeak> GetInsertCandidatePeakStatement()
        {
            return GetInsertStatement<CandidatePeak>();
        }

        public InsertStatement<CandidatePeakGroup> GetInsertCandidatePeakGroupStatement()
        {
            return GetInsertStatement<CandidatePeakGroup>();
        }

        public InsertStatement<ChromatogramData> GetInsertChromatogramDataStatement()
        {
            return GetInsertStatement<ChromatogramData>();
        }

        public InsertStatement<MsDataFile> GetInsertMsDataFileStatement()
        {
            return GetInsertStatement<MsDataFile>();
        }

        public InsertStatement<SpectrumInfo> GetInsertScanInfoStatement()
        {
            return GetInsertStatement<SpectrumInfo>();
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
            if (_insertStatements.TryGetValue(typeof(T), out PreparedStatement statement))
            {
                return (InsertStatement<T>) statement;
            }

            var insertStatement = new InsertStatement<T>(Connection);
            RememberDisposable(insertStatement);
            _insertStatements.Add(typeof(T), insertStatement);
            return insertStatement;
        }
    }
}

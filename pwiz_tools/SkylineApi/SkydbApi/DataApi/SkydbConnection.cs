using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using pwiz.Common.Database;
using SkydbApi.Orm;

namespace SkydbApi.DataApi
{
    public class SkydbConnection : IDisposable
    {
        private IDbTransaction _transaction;
        private List<IDisposable> _statements = new List<IDisposable>();
        private InsertCandidatePeakStatement _insertCandidatePeakStatement;
        private InsertCandidatePeakGroupStatement _insertCandidatePeakGroupStatement;
        private InsertChromatogramDataStatement _insertChromatogramDataStatement;
        private InsertMsDataFileStatement _insertMsDataFileStatement;
        private InsertSpectrumInfoStatement _insertSpectrumInfoStatement;
        private SpectrumListStatement _spectrumListStatement;
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

        public void Dispose()
        {
            Connection.Close();
            foreach (var disposable in _statements)
            {
                disposable.Dispose();
            }
        }

        public InsertCandidatePeakStatement GetInsertCandidatePeakStatement()
        {
            return _insertCandidatePeakStatement =
                _insertCandidatePeakStatement ?? RememberDisposable(new InsertCandidatePeakStatement(Connection));
        }

        public InsertCandidatePeakGroupStatement GetInsertCandidatePeakGroupStatement()
        {
            return _insertCandidatePeakGroupStatement =
                _insertCandidatePeakGroupStatement ?? RememberDisposable(new InsertCandidatePeakGroupStatement(Connection));
        }

        public InsertChromatogramDataStatement GetInsertChromatogramDataStatement()
        {
            return _insertChromatogramDataStatement =
                _insertChromatogramDataStatement ?? RememberDisposable(new InsertChromatogramDataStatement(Connection));
        }

        public InsertMsDataFileStatement GetInsertMsDataFileStatement()
        {
            return _insertMsDataFileStatement = _insertMsDataFileStatement ?? RememberDisposable(new InsertMsDataFileStatement(Connection));
        }

        public InsertSpectrumInfoStatement GetInsertScanInfoStatement()
        {
            return _insertSpectrumInfoStatement = _insertSpectrumInfoStatement ?? RememberDisposable(new InsertSpectrumInfoStatement(Connection));
        }

        private T RememberDisposable<T>(T disposable) where T : IDisposable
        {
            _statements.Add(disposable);
            return disposable;
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

        public void Insert(SpectrumList spectrumList)
        {
            _spectrumListStatement =
                _spectrumListStatement ?? RememberDisposable(new SpectrumListStatement(Connection));
            _spectrumListStatement.Insert(spectrumList);
        }
    }
}

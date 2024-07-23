using SkydbApi.Orm;
using System;
using System.Collections.Generic;
using System.Threading;
using pwiz.Common.SystemUtil;

namespace SkydbApi.DataApi
{
    public class SkydbStatements : IDisposable
    {
        private List<IDisposable> _statements = new List<IDisposable>();
        private readonly EntityBatcher<CandidatePeak> _candidatePeakBatcher;
        private InsertCandidatePeakGroupStatement _insertCandidatePeakGroupStatement;
        private InsertChromatogramDataStatement _insertChromatogramDataStatement;
        private InsertMsDataFileStatement _insertMsDataFileStatement;
        private InsertSpectrumInfoStatement _insertSpectrumInfoStatement;
        private SpectrumListStatement _spectrumListStatement;

        public SkydbStatements(SkydbConnection connection)
        {
            Connection = connection;
            _candidatePeakBatcher = RememberDisposable(new EntityBatcher<CandidatePeak>(connection));
        }

        public SkydbConnection Connection { get; }

        public void Dispose()
        {
            foreach (var disposable in _statements)
            {
                disposable.Dispose();
            }
        }
        public InsertCandidatePeakGroupStatement GetInsertCandidatePeakGroupStatement()
        {
            return _insertCandidatePeakGroupStatement ??= RememberDisposable(new InsertCandidatePeakGroupStatement(Connection.Connection));
        }

        public InsertChromatogramDataStatement GetInsertChromatogramDataStatement()
        {
            return _insertChromatogramDataStatement ??= RememberDisposable(new InsertChromatogramDataStatement(Connection.Connection));
        }

        public InsertMsDataFileStatement GetInsertMsDataFileStatement()
        {
            return _insertMsDataFileStatement ??= RememberDisposable(new InsertMsDataFileStatement(Connection.Connection));
        }

        public InsertSpectrumInfoStatement GetInsertScanInfoStatement()
        {
            return _insertSpectrumInfoStatement ??= RememberDisposable(new InsertSpectrumInfoStatement(Connection.Connection));
        }

        private T RememberDisposable<T>(T disposable) where T : IDisposable
        {
            _statements.Add(disposable);
            return disposable;
        }

        public void Insert(SpectrumList spectrumList)
        {
            _spectrumListStatement ??= RememberDisposable(new SpectrumListStatement(Connection.Connection));
            _spectrumListStatement.Insert(spectrumList);
        }

        public void Insert(CandidatePeak candidatePeak)
        {
            _candidatePeakBatcher.Insert(candidatePeak);
        }

    }
}

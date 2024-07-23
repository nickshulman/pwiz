using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SQLite;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SkydbApi.Orm;

namespace SkydbApi.DataApi
{
    public class SkydbMerger
    {
        private long _lastCandidatePeakGroupId;
        private long _lastChromatogramDataId;
        private long _lastChromatogramGroupId;
        private long _lastTransitionChromatogramId;
        private long _lastMsDataFileId;
        private long _lastSpectrumInfoId;
        private long _lastSpectrumListId;

        public SkydbMerger(SkydbConnection connection)
        {
            Connection = connection;
        }

        public SkydbConnection Connection { get; }

        public void Merge(string path)
        {
            _lastCandidatePeakGroupId = GetLastId(nameof(CandidatePeakGroup));
            _lastChromatogramDataId = GetLastId(nameof(ChromatogramData));
            _lastChromatogramGroupId = GetLastId(nameof(ChromatogramGroup));
            _lastTransitionChromatogramId = GetLastId(nameof(TransitionChromatogram));
            _lastMsDataFileId = GetLastId(nameof(MsDataFile));
            _lastSpectrumInfoId = GetLastId(nameof(SpectrumInfo));
            _lastSpectrumListId = GetLastId(nameof(SpectrumList));
            AttachDatabase(path);
            Connection.BeginTransaction();
            CopyMsDataFiles();
            Connection.CommitTransaction();
            DetachDatabase();
        }

        private long GetLastId(string name)
        {
            using var cmd = Connection.Connection.CreateCommand();
            cmd.CommandText = "SELECT COALESCE(Max(Id), 0) FROM " + name;
            return Convert.ToInt64(cmd.ExecuteScalar());
        }

        private void AttachDatabase(string path)
        {
            using var cmd = Connection.Connection.CreateCommand();
            cmd.CommandText = "ATTACH DATABASE ? AS mergeDb";
            cmd.Parameters.Add(new SQLiteParameter(DbType.String) { Value = path});
            cmd.ExecuteNonQuery();
        }

        private void DetachDatabase()
        {
            using var cmd = Connection.Connection
                .CreateCommand();
            cmd.CommandText = "DETACH DATABASE mergeDb";
            cmd.ExecuteNonQuery();
        }

        private void CopyMsDataFiles()
        {
            using var cmd = Connection.Connection.CreateCommand();
            cmd.CommandText = "INSERT INTO MsDataFile (Id, FilePath) SELECT Id + ?, FilePath FROM mergeDb.MsDataFile";
            cmd.Parameters.Add(new SQLiteParameter(DbType.Int64, _lastMsDataFileId));
            cmd.ExecuteNonQuery();
        }
    }
}

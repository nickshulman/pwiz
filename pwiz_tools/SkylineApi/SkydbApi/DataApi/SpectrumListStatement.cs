using System;
using System.Data;
using Microsoft.Data.Sqlite;
using SkydbApi.Orm;

namespace SkydbApi.DataApi
{
    public class SpectrumListStatement : PreparedStatement
    {
        private IDbCommand _insertCommand;
        private SqliteParameter spectrumCount;
        private SqliteParameter spectrumIndexData;
        public SpectrumListStatement(IDbConnection connection) : base(connection)
        {
            _insertCommand = CreateCommand();
            _insertCommand.CommandText = "INSERT INTO SpectrumList(SpectrumCount, SpectrumIndexData) values (?, ?)";
            _insertCommand.Parameters.Add(spectrumCount = new SqliteParameter());
            _insertCommand.Parameters.Add(spectrumIndexData = new SqliteParameter());
        }

        public void Insert(SpectrumList spectrumList)
        {
            spectrumCount.Value = spectrumList.SpectrumCount;
            spectrumIndexData.Value = spectrumList.SpectrumIndexData;
            spectrumList.Id = Convert.ToInt64(_insertCommand.ExecuteScalar());
        }
    }
}

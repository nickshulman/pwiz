using System;
using System.Data;
using Microsoft.Data.Sqlite;
using SkydbApi.Orm;

namespace SkydbApi.DataApi
{
    public class InsertChromatogramDataStatement : PreparedStatement
    {
        private static string COMMAND_TEXT = "INSERT INTO ChromatogramData(SpectrumList, PointCount, RetentionTimesData, IntensitiesData, MassErrorsData) "
                                             + "VALUES(?,?,?,?,?); select last_insert_rowid();";

        private SqliteParameter spectrumList;
        private SqliteParameter pointCount;
        private SqliteParameter retentionTimesData;
        private SqliteParameter intensitiesData;
        private SqliteParameter massErrorsData;

        private IDbCommand Command { get; }

        public InsertChromatogramDataStatement(IDbConnection connection) : base(connection)
        {
            Command = CreateCommand();
            Command.CommandText = COMMAND_TEXT;
            Command.Parameters.Add(spectrumList = new SqliteParameter());
            Command.Parameters.Add(pointCount = new SqliteParameter());
            Command.Parameters.Add(retentionTimesData = new SqliteParameter());
            Command.Parameters.Add(intensitiesData = new SqliteParameter());
            Command.Parameters.Add(massErrorsData = new SqliteParameter());
        }

        public void Insert(ChromatogramData chromatogramData)
        {
            spectrumList.Value = chromatogramData.SpectrumList?.Id;
            pointCount.Value = chromatogramData.PointCount;
            retentionTimesData.Value = chromatogramData.RetentionTimesData;
            intensitiesData.Value = chromatogramData.IntensitiesData;
            massErrorsData.Value = chromatogramData.MassErrorsData;
            chromatogramData.Id = Convert.ToInt64(Command.ExecuteScalar());
        }
    }
}

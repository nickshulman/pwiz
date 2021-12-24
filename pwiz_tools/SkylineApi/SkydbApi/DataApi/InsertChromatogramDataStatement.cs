using System;
using System.Data;
using System.Data.SQLite;
using SkydbApi.Orm;

namespace SkydbApi.DataApi
{
    public class InsertChromatogramDataStatement : IDisposable
    {
        private static string COMMAND_TEXT = "INSERT INTO ChromatogramData(SpectrumList, PointCount, RetentionTimesData, IntensitiesData, MassErrorsData) "
                                             + "VALUES(?,?,?,?,?); select last_insert_rowid();";

        private SQLiteParameter spectrumList;
        private SQLiteParameter pointCount;
        private SQLiteParameter retentionTimesData;
        private SQLiteParameter intensitiesData;
        private SQLiteParameter massErrorsData;

        private IDbCommand Command { get; }

        public InsertChromatogramDataStatement(IDbConnection connection)
        {
            Command = connection.CreateCommand();
            Command.CommandText = COMMAND_TEXT;
            Command.Parameters.Add(spectrumList = new SQLiteParameter());
            Command.Parameters.Add(pointCount = new SQLiteParameter());
            Command.Parameters.Add(retentionTimesData = new SQLiteParameter());
            Command.Parameters.Add(intensitiesData = new SQLiteParameter());
            Command.Parameters.Add(massErrorsData = new SQLiteParameter());
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

        public void Dispose()
        {
            Command.Dispose();
        }
    }
}

using System;
using System.Data;
using System.Data.SQLite;
using SkydbApi.Orm;

namespace SkydbApi.DataApi
{
    public class InsertSpectrumInfoStatement : IDisposable
    {
        private static string COMMAND_TEXT = "INSERT INTO SpectrumInfo(MsDataFile, SpectrumIndex, SpectrumIdentifier, RetentionTime) "
                                             + "VALUES(?,?,?,?); select last_insert_rowid();";

        private SQLiteParameter msDataFile;
        private SQLiteParameter spectrumIndex;
        private SQLiteParameter spectrumIdentifier;
        private SQLiteParameter retentionTime;

        private IDbCommand Command { get; }

        public InsertSpectrumInfoStatement(IDbConnection connection)
        {
            Command = connection.CreateCommand();
            Command.CommandText = COMMAND_TEXT;
            Command.Parameters.Add(msDataFile = new SQLiteParameter());
            Command.Parameters.Add(spectrumIndex = new SQLiteParameter());
            Command.Parameters.Add(spectrumIdentifier = new SQLiteParameter());
            Command.Parameters.Add(retentionTime = new SQLiteParameter());
        }

        public void Dispose()
        {
            Command.Dispose();
        }

        public void Insert(SpectrumInfo spectrumInfo)
        {
            msDataFile.Value = spectrumInfo.MsDataFile?.Id;
            spectrumIndex.Value = spectrumInfo.SpectrumIndex;
            spectrumIdentifier.Value = spectrumInfo.SpectrumIdentifier;
            retentionTime.Value = spectrumInfo.RetentionTime;
            spectrumInfo.Id = Convert.ToInt64(Command.ExecuteScalar());
        }
    }
}

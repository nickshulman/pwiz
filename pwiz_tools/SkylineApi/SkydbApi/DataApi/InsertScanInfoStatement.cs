using System;
using System.Data;
using Microsoft.Data.Sqlite;
using SkydbApi.Orm;

namespace SkydbApi.DataApi
{
    public class InsertSpectrumInfoStatement : PreparedStatement
    {
        private static string COMMAND_TEXT = "INSERT INTO SpectrumInfo(MsDataFile, SpectrumIndex, SpectrumIdentifier, RetentionTime) "
                                             + "VALUES(?,?,?,?); select last_insert_rowid();";

        private SqliteParameter msDataFile;
        private SqliteParameter spectrumIndex;
        private SqliteParameter spectrumIdentifier;
        private SqliteParameter retentionTime;

        private IDbCommand Command { get; }

        public InsertSpectrumInfoStatement(IDbConnection connection) : base(connection)
        {
            Command = CreateCommand();
            Command.CommandText = COMMAND_TEXT;
            Command.Parameters.Add(msDataFile = new SqliteParameter());
            Command.Parameters.Add(spectrumIndex = new SqliteParameter());
            Command.Parameters.Add(spectrumIdentifier = new SqliteParameter());
            Command.Parameters.Add(retentionTime = new SqliteParameter());
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

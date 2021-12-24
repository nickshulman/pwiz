using System;
using System.Data;
using System.Data.SQLite;
using SkydbApi.Orm;

namespace SkydbApi.DataApi
{
    public class InsertCandidatePeakGroupStatement : IDisposable
    {
        private static string COMMAND_TEXT = "INSERT INTO CandidatePeakGroup(StartTime, EndTime, Identified) "
                                             + "VALUES(?,?,?); select last_insert_rowid();";
        private SQLiteParameter startTime;
        private SQLiteParameter endTime;
        private SQLiteParameter identified;


        public InsertCandidatePeakGroupStatement(IDbConnection connection)
        {
            Command = connection.CreateCommand();
            Command.CommandText = COMMAND_TEXT;
            Command.Parameters.Add(startTime = new SQLiteParameter());
            Command.Parameters.Add(endTime = new SQLiteParameter());
            Command.Parameters.Add(identified = new SQLiteParameter());
        }

        private IDbCommand Command { get; }

        public void Dispose()
        {
            Command.Dispose();
        }

        public void Insert(CandidatePeakGroup candidatePeakGroup)
        {
            startTime.Value = candidatePeakGroup.StartTime;
            endTime.Value = candidatePeakGroup.EndTime;
            identified.Value = candidatePeakGroup.Identified;
            candidatePeakGroup.Id = Convert.ToInt64(Command.ExecuteScalar());
        }
    }
}

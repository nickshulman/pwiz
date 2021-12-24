using System;
using System.Data;
using Microsoft.Data.Sqlite;
using SkydbApi.Orm;

namespace SkydbApi.DataApi
{
    public class InsertCandidatePeakGroupStatement : PreparedStatement
    {
        private static string COMMAND_TEXT = "INSERT INTO CandidatePeakGroup(StartTime, EndTime, Identified) "
                                             + "VALUES(?,?,?); select last_insert_rowid();";

        private SqliteParameter startTime;
        private SqliteParameter endTime;
        private SqliteParameter identified;


        public InsertCandidatePeakGroupStatement(IDbConnection connection) : base(connection)
        {
            Command = CreateCommand();
            Command.CommandText = COMMAND_TEXT;
            Command.Parameters.Add(startTime = new SqliteParameter());
            Command.Parameters.Add(endTime = new SqliteParameter());
            Command.Parameters.Add(identified = new SqliteParameter());
        }

        private IDbCommand Command { get; }

        public void Insert(CandidatePeakGroup candidatePeakGroup)
        {
            startTime.Value = candidatePeakGroup.StartTime;
            endTime.Value = candidatePeakGroup.EndTime;
            identified.Value = candidatePeakGroup.Identified;
            candidatePeakGroup.Id = Convert.ToInt64(Command.ExecuteScalar());
        }
    }
}

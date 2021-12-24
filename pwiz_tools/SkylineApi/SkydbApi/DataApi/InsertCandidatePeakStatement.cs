using System;
using System.Data;
using System.Data.Common;
using Microsoft.Data.Sqlite;
using SkydbApi.Orm;

namespace SkydbApi.DataApi
{
    public class InsertCandidatePeakStatement : PreparedStatement
    {
        private SqliteParameter candidatePeakGroup;
        private SqliteParameter startTime;
        private SqliteParameter endTime;
        private SqliteParameter area;
        private SqliteParameter backgroundArea;
        private SqliteParameter height;
        private SqliteParameter fullWidthAtHalfMax;
        private SqliteParameter pointsAcross;
        private SqliteParameter degenerateFwhm;
        private SqliteParameter forcedIntegration;
        private SqliteParameter timeNormalized;
        private SqliteParameter truncated;
        private SqliteParameter massError;

        private static string COMMAND_TEXT = "INSERT INTO CandidatePeak(CandidatePeakGroup, StartTime, EndTime, Area, BackgroundArea, "
                                             + "Height, FullWidthAtHalfMax, PointsAcross, DegenerateFwhm, ForcedIntegration, TimeNormalized, Truncated, MassError) "
                                             + "VALUES(?,?,?,?,?,?,?,?,?,?,?,?,?);"; //"select last_insert_rowid();";

        private IDbCommand Command { get; }
        public InsertCandidatePeakStatement(IDbConnection connection) : base(connection)
        {
            Command = CreateCommand();
            Command.CommandText = COMMAND_TEXT;
            Command.Parameters.Add(candidatePeakGroup = new SqliteParameter());
            Command.Parameters.Add(startTime = new SqliteParameter());
            Command.Parameters.Add(endTime = new SqliteParameter());
            Command.Parameters.Add(area = new SqliteParameter());
            Command.Parameters.Add(backgroundArea = new SqliteParameter());
            Command.Parameters.Add(height = new SqliteParameter());
            Command.Parameters.Add(fullWidthAtHalfMax = new SqliteParameter());
            Command.Parameters.Add(pointsAcross = new SqliteParameter());
            Command.Parameters.Add(degenerateFwhm = new SqliteParameter());
            Command.Parameters.Add(forcedIntegration = new SqliteParameter());
            Command.Parameters.Add(timeNormalized = new SqliteParameter());
            Command.Parameters.Add(truncated = new SqliteParameter());
            Command.Parameters.Add(massError = new SqliteParameter());
        }

        public void Insert(CandidatePeak row)
        {
            candidatePeakGroup.Value = row.CandidatePeakGroup?.Id;
            startTime.Value = row.StartTime;
            endTime.Value = row.EndTime;
            area.Value = row.Area;
            backgroundArea.Value = row.BackgroundArea;
            height.Value = row.Height;
            fullWidthAtHalfMax.Value = row.FullWidthAtHalfMax;
            pointsAcross.Value = row.PointsAcross;
            degenerateFwhm.Value = row.DegenerateFwhm;
            forcedIntegration.Value = row.ForcedIntegration;
            timeNormalized.Value = row.TimeNormalized;
            truncated.Value = row.Truncated;
            massError.Value = row.MassError;
            Command.ExecuteNonQuery();
        }
    }
}

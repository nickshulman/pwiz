using System;
using System.Data;
using System.Data.Common;
using System.Data.SQLite;
using SkydbApi.Orm;

namespace SkydbApi.DataApi
{
    public class InsertCandidatePeakStatement : IDisposable
    {
        private SQLiteParameter candidatePeakGroup;
        private SQLiteParameter startTime;
        private SQLiteParameter endTime;
        private SQLiteParameter area;
        private SQLiteParameter backgroundArea;
        private SQLiteParameter height;
        private SQLiteParameter fullWidthAtHalfMax;
        private SQLiteParameter pointsAcross;
        private SQLiteParameter degenerateFwhm;
        private SQLiteParameter forcedIntegration;
        private SQLiteParameter timeNormalized;
        private SQLiteParameter truncated;
        private SQLiteParameter massError;

        private static string COMMAND_TEXT = "INSERT INTO CandidatePeak(CandidatePeakGroup, StartTime, EndTime, Area, BackgroundArea, "
                                             + "Height, FullWidthAtHalfMax, PointsAcross, DegenerateFwhm, ForcedIntegration, TimeNormalized, Truncated, MassError) "
                                             + "VALUES(?,?,?,?,?,?,?,?,?,?,?,?,?);"; //"select last_insert_rowid();";

        private IDbCommand Command { get; }
        public InsertCandidatePeakStatement(IDbConnection connection)
        {
            Command = connection.CreateCommand();
            Command.CommandText = COMMAND_TEXT;
            Command.Parameters.Add(candidatePeakGroup = new SQLiteParameter());
            Command.Parameters.Add(startTime = new SQLiteParameter());
            Command.Parameters.Add(endTime = new SQLiteParameter());
            Command.Parameters.Add(area = new SQLiteParameter());
            Command.Parameters.Add(backgroundArea = new SQLiteParameter());
            Command.Parameters.Add(height = new SQLiteParameter());
            Command.Parameters.Add(fullWidthAtHalfMax = new SQLiteParameter());
            Command.Parameters.Add(pointsAcross = new SQLiteParameter());
            Command.Parameters.Add(degenerateFwhm = new SQLiteParameter());
            Command.Parameters.Add(forcedIntegration = new SQLiteParameter());
            Command.Parameters.Add(timeNormalized = new SQLiteParameter());
            Command.Parameters.Add(truncated = new SQLiteParameter());
            Command.Parameters.Add(massError = new SQLiteParameter());
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

        public void Dispose()
        {
            Command.Dispose();
        }
    }
}

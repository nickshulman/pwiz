using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SQLite;
using System.Linq;
using SkydbApi.Orm;

namespace SkydbApi.DataApi
{
    public class InsertCandidatePeakStatement : PreparedStatement
    {
        private enum Parameter
        {
            candidatePeakGroup,
            startTime,
            endTime,
            area,
            backgroundArea,
            height,
            fullWidthAtHalfMax,
            pointsAcross,
            degenerateFwhm,
            forcedIntegration,
            timeNormalized,
            truncated,
            massError,
        }

        private static string SINGLE_COMMAND = "INSERT INTO CandidatePeak(CandidatePeakGroupId, StartTime, EndTime, Area, BackgroundArea, "
                                             + "Height, FullWidthAtHalfMax, PointsAcross, DegenerateFwhm, ForcedIntegration, TimeNormalized, Truncated, MassError) "
                                             + "VALUES(?,?,?,?,?,?,?,?,?,?,?,?,?);";

        private static string MULTI_COMMAND =
            "INSERT INTO CandidatePeak(CandidatePeakGroupId, StartTime, EndTime, Area, BackgroundArea, Height, FullWidthAtHalfMax, PointsAcross, DegenerateFwhm, ForcedIntegration, TimeNormalized, Truncated, MassError)" +
            "SELECT ? AS CandidatePeakGroupId, ? AS StartTime, ? AS EndTime, ? AS Area, ? AS BackgroundArea, ? AS Height, ? AS FullWidthAtHalfMax, ? AS PointsAcross, ? AS DegenerateFwhm, ? AS ForcedIntegration, ? AS TimeNormalized, ? AS Truncated, ? AS MassError " +
            "UNION ALL SELECT ?,?,?,?,?,?,?,?,?,?,?,?,? " +
            "UNION ALL SELECT ?,?,?,?,?,?,?,?,?,?,?,?,? " +
            "UNION ALL SELECT ?,?,?,?,?,?,?,?,?,?,?,?,?";

        private Dictionary<Parameter, SQLiteParameter>[] _paramMaps;
        private List<CandidatePeak> _batch = new List<CandidatePeak>();
        public int BatchSize
        {
            get { return _paramMaps.Length; }
        }

        private IDbCommand SingleCommand { get; }
        private IDbCommand MultiCommand { get; }
        public InsertCandidatePeakStatement(IDbConnection connection) : base(connection)
        {
            SingleCommand = CreateCommand();
            SingleCommand.CommandText = SINGLE_COMMAND;
            MultiCommand = CreateCommand();
            MultiCommand.CommandText = MULTI_COMMAND;
            _paramMaps = new Dictionary<Parameter, SQLiteParameter>[4];
            for (int i = 0; i < _paramMaps.Length; i++)
            {
                _paramMaps[i] = Enum.GetValues(typeof(Parameter)).Cast<Parameter>()
                    .ToDictionary(param => param, param => new SQLiteParameter());
                AddParams(MultiCommand, _paramMaps[i]);
            }
            AddParams(SingleCommand, _paramMaps[0]);
        }

        public void Insert(CandidatePeak row)
        {
            _batch.Add(row);
            if (_batch.Count == _paramMaps.Length)
            {
                for (int i = 0; i < _batch.Count; i++)
                {
                    SetParameters(_paramMaps[i], _batch[i]);
                }
                MultiCommand.ExecuteNonQuery();
                _batch.Clear();
            }
        }

        public void Flush()
        {
            foreach (var row in _batch)
            {
                SetParameters(_paramMaps[0], row);
                SingleCommand.ExecuteNonQuery();
            }
            _batch.Clear();
        }

        private void SetParameters(Dictionary<Parameter, SQLiteParameter> parameters, CandidatePeak candidatePeak)
        {
            parameters[Parameter.candidatePeakGroup].Value = candidatePeak.CandidatePeakGroupId;
            parameters[Parameter.startTime].Value = candidatePeak.StartTime;
            parameters[Parameter.endTime].Value = candidatePeak.EndTime;
            parameters[Parameter.area].Value = candidatePeak.Area;
            parameters[Parameter.backgroundArea].Value = candidatePeak.BackgroundArea;
            parameters[Parameter.height].Value = candidatePeak.Height;
            parameters[Parameter.fullWidthAtHalfMax].Value = candidatePeak.FullWidthAtHalfMax;
            parameters[Parameter.pointsAcross].Value = candidatePeak.PointsAcross;
            parameters[Parameter.degenerateFwhm].Value = candidatePeak.DegenerateFwhm;
            parameters[Parameter.forcedIntegration].Value = candidatePeak.ForcedIntegration;
            parameters[Parameter.timeNormalized].Value = candidatePeak.TimeNormalized;
            parameters[Parameter.truncated].Value = candidatePeak.Truncated;
            parameters[Parameter.massError].Value = candidatePeak.MassError;
        }

        private void AddParams(IDbCommand command, Dictionary<Parameter, SQLiteParameter> paramMap)
        {
            foreach (Parameter parameter in Enum.GetValues(typeof(Parameter)))
            {
                command.Parameters.Add(paramMap[parameter]);
            }
        }
    }
}

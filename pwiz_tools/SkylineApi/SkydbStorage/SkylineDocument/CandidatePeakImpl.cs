using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SkydbStorage.DataAccess.Orm;
using SkylineApi;

namespace SkydbStorage.SkylineDocument
{
    public class CandidatePeakImpl : ICandidatePeak
    {
        public CandidatePeakImpl(CandidatePeakGroupImpl candidatePeakGroupImpl, CandidatePeak candidatePeak)
        {
            PeakGroup = candidatePeakGroupImpl;
            CandidatePeak = candidatePeak;
        }

        public CandidatePeakGroupImpl PeakGroup { get; }

        public CandidatePeak CandidatePeak { get; }

        public double StartTime => CandidatePeak.StartTime ?? PeakGroup.PeakGroup.StartTime.GetValueOrDefault();

        public double EndTime => CandidatePeak.EndTime ?? PeakGroup.PeakGroup.EndTime.GetValueOrDefault();

        public double Area => CandidatePeak.Area.GetValueOrDefault();

        public double BackgroundArea => CandidatePeak.BackgroundArea.GetValueOrDefault();

        public double Height => CandidatePeak.Height.GetValueOrDefault();

        public double FullWidthAtHalfMax => CandidatePeak.FullWidthAtHalfMax.GetValueOrDefault();

        public int? PointsAcross => CandidatePeak.PointsAcross;

        public bool DegenerateFwhm => CandidatePeak.DegenerateFwhm;

        public bool ForcedIntegration => CandidatePeak.ForcedIntegration;

        public bool? Truncated => CandidatePeak.Truncated;

        public double? MassError => CandidatePeak.MassError;
    }
}

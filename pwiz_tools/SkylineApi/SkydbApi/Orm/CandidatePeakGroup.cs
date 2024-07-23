
using NHibernate.Mapping.Attributes;

namespace SkydbApi.Orm
{
    [Class(Lazy = false, Table=nameof(CandidatePeakGroup))]
    public class CandidatePeakGroup : Entity
    {
        [Property]
        public double? StartTime { get; set; }
        [Property]
        public double? EndTime { get; set; }
        [Property]
        public int Identified { get; set; }
    }

    [Class(Lazy = false, Table = nameof(CandidatePeak))]
    public class CandidatePeak : Entity
    {
        [Property]
        public long CandidatePeakGroupId { get; set; }
        [Property]
        public long TransitionChromatogramId { get; set; }
        [Property]
        public long? ScoresId { get; set; }
        [Property]
        public double? StartTime { get; set; }
        [Property]
        public double? EndTime { get; set; }
        [Property]
        public double? Area { get; set; }
        [Property]
        public double? BackgroundArea { get; set; }
        [Property]
        public double? Height { get; set; }
        [Property]
        public double? FullWidthAtHalfMax { get; set; }
        [Property]
        public int? PointsAcross { get; set; }
        [Property]
        public bool DegenerateFwhm { get; set; }
        [Property]
        public bool ForcedIntegration { get; set; }
        [Property]
        public bool TimeNormalized { get; set; }
        [Property]
        public bool? Truncated { get; set; }
        [Property]
        public double? MassError { get; set; }
    }
}

using NHibernate.Mapping.Attributes;
using SkydbStorage.Internal.Orm;

namespace SkydbStorage.DataAccess.Orm
{
    [Class(Lazy = false)]
    public class CandidatePeak : Entity<CandidatePeakGroup>
    {
        [ManyToOne(ClassType = typeof(CandidatePeakGroup), Index = "CandidatePeak_CandidatePeakGroup")]
        public long CandidatePeakGroup { get; set; }
        [ManyToOne(ClassType = typeof(Chromatogram))]
        public long Chromatogram { get; set; }
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
        public bool? Truncated { get; set; }
        [Property]
        public double? MassError { get; set; }
    }
}
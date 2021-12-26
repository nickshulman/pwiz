using NHibernate.Mapping.Attributes;

namespace SkydbStorage.Internal.Orm
{
    [Class(Lazy = false)]
    public class CandidatePeak : Entity<CandidatePeakGroup>
    {
        [ManyToOne]
        public virtual CandidatePeakGroup CandidatePeakGroup { get; set; }
        [ManyToOne(NotFound = NotFoundMode.Ignore)]
        public virtual Chromatogram Chromatogram { get; set; }
        [Property]
        public virtual double? StartTime { get; set; }
        [Property]
        public virtual double? EndTime { get; set; }
        [Property]
        public virtual double? Area { get; set; }
        [Property]
        public virtual double? BackgroundArea { get; set; }
        [Property]
        public virtual double? Height { get; set; }
        [Property]
        public virtual double? FullWidthAtHalfMax { get; set; }
        [Property]
        public virtual int? PointsAcross { get; set; }
        [Property]
        public virtual bool DegenerateFwhm { get; set; }
        [Property]
        public virtual bool ForcedIntegration { get; set; }
        [Property]
        public virtual bool? Truncated { get; set; }
        [Property]
        public virtual double? MassError { get; set; }
    }
}

using NHibernate.Mapping.Attributes;

namespace SkydbApi.Orm
{
    [Class()]
    public class CandidatePeakGroup : Entity<CandidatePeakGroup>
    {
        [ManyToOne(NotFound = NotFoundMode.Ignore)]
        public virtual Scores Scores { get; set; }
        [Property]
        public virtual double? StartTime { get; set; }
        [Property]
        public virtual double? EndTime { get; set; }
        [Property]
        public virtual int Identified { get; set; }
    }

    [Class]
    public class CandidatePeak : Entity<CandidatePeakGroup>
    {
        [ManyToOne(NotFound = NotFoundMode.Ignore)]
        public virtual CandidatePeakGroup CandidatePeakGroup { get; set; }
        [ManyToOne(NotFound = NotFoundMode.Ignore)]
        public virtual TransitionChromatogram TransitionChromatogram { get; set; }
        [ManyToOne(NotFound = NotFoundMode.Ignore)]
        public virtual Scores Scores { get; set; }
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
        public virtual bool TimeNormalized { get; set; }
        [Property]
        public virtual bool? Truncated { get; set; }
        [Property]
        public virtual double? MassError { get; set; }
    }
}

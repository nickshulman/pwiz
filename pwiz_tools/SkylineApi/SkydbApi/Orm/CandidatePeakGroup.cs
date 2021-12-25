
using SkydbApi.Orm.Attributes;

namespace SkydbApi.Orm
{
    public class CandidatePeakGroup : Entity
    {
        [Column]
        public virtual double? StartTime { get; set; }
        [Column]
        public virtual double? EndTime { get; set; }
        [Column]
        public virtual int Identified { get; set; }
    }

    public class CandidatePeak : Entity
    {
        [Column]
        public virtual CandidatePeakGroup CandidatePeakGroup { get; set; }
        //[Column]
        public virtual TransitionChromatogram TransitionChromatogram { get; set; }
        //[Column]
        public virtual Scores Scores { get; set; }
        [Column]
        public virtual double? StartTime { get; set; }
        [Column]
        public virtual double? EndTime { get; set; }
        [Column]
        public virtual double? Area { get; set; }
        [Column]
        public virtual double? BackgroundArea { get; set; }
        [Column]
        public virtual double? Height { get; set; }
        [Column]
        public virtual double? FullWidthAtHalfMax { get; set; }
        [Column]
        public virtual int? PointsAcross { get; set; }
        [Column]
        public virtual bool DegenerateFwhm { get; set; }
        [Column]
        public virtual bool ForcedIntegration { get; set; }
        [Column]
        public virtual bool TimeNormalized { get; set; }
        [Column]
        public virtual bool? Truncated { get; set; }
        [Column]
        public virtual double? MassError { get; set; }
    }
}

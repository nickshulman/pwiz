
namespace SkydbApi.Orm
{
    public class CandidatePeakGroup : Entity
    {
        public virtual double? StartTime { get; set; }
        public virtual double? EndTime { get; set; }
        public virtual int Identified { get; set; }
    }

    public class CandidatePeak : Entity
    {
        public virtual CandidatePeakGroup CandidatePeakGroup { get; set; }
        public virtual TransitionChromatogram TransitionChromatogram { get; set; } 
        public virtual Scores Scores { get; set; }
        public virtual double? StartTime { get; set; }
        public virtual double? EndTime { get; set; }
        public virtual double? Area { get; set; }
        public virtual double? BackgroundArea { get; set; }
        public virtual double? Height { get; set; }
        public virtual double? FullWidthAtHalfMax { get; set; }
        public virtual int? PointsAcross { get; set; }
        public virtual bool DegenerateFwhm { get; set; }
        public virtual bool ForcedIntegration { get; set; }
        public virtual bool TimeNormalized { get; set; }
        public virtual bool? Truncated { get; set; }
        public virtual double? MassError { get; set; }
    }
}

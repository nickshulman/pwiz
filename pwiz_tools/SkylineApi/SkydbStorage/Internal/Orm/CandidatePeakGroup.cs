
using NHibernate.Mapping.Attributes;

namespace SkydbStorage.Internal.Orm
{
    [Class(Lazy = false)]
    public class CandidatePeakGroup : Entity<CandidatePeakGroup>
    {
        [ManyToOne]
        public virtual Scores Scores { get; set; }
        [ManyToOne]
        public virtual ChromatogramGroup ChromatogramGroup { get; set; }
        [Property]
        public virtual double? StartTime { get; set; }
        [Property]
        public virtual double? EndTime { get; set; }
        [Property]
        public virtual int Identified { get; set; }
        [Property]
        public virtual bool IsBestPeak { get; set; }
    }
}


using NHibernate.Mapping.Attributes;
using SkydbStorage.DataAccess.Orm;

namespace SkydbStorage.Internal.Orm
{
    [Class(Lazy = false)]
    public class CandidatePeakGroup : Entity<CandidatePeakGroup>
    {
        [ManyToOne(ClassType = typeof(Scores))]
        public long? Scores { get; set; }
        [ManyToOne(ClassType = typeof(ChromatogramGroup), Index = "CandidatePeakGroup_ChromatogramGroup")]
        public long ChromatogramGroup { get; set; }
        [Property]
        public double? StartTime { get; set; }
        [Property]
        public double? EndTime { get; set; }
        [Property]
        public int Identified { get; set; }
        [Property]
        public bool IsBestPeak { get; set; }
    }
}

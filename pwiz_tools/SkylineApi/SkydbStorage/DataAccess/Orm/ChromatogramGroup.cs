using NHibernate.Mapping.Attributes;
using SkydbStorage.Internal.Orm;

namespace SkydbStorage.DataAccess.Orm
{
    [Class(Lazy = false)]
    public class ChromatogramGroup : Entity<ChromatogramGroup>
    {
        [ManyToOne(ClassType = typeof(ExtractedFile))]
        public long File { get; set; }
        [Property]
        public string TextId { get; set; }
        [Property]
        public double PrecursorMz { get; set; }
        [Property]
        public double? StartTime { get; set; }
        [Property]
        public double? EndTime { get; set; }
        [Property]
        public double? CollisionalCrossSection { get; set; }

        [Property]
        public double? InterpolationStartTime { get; set; }
        [Property]
        public double? InterpolationEndTime { get; set; }
        [Property]
        public int? InterpolationNumberOfPoints { get; set; }
        [Property]
        public double? InterpolationIntervalDelta { get; set; }
        [Property]
        public bool? InterpolationInferZeroes { get; set; }
    }
}

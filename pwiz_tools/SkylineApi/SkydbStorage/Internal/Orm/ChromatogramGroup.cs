using NHibernate.Mapping.Attributes;

namespace SkydbStorage.Internal.Orm
{
    [Class(Lazy = false)]
    public class ChromatogramGroup : Entity<ChromatogramGroup>
    {
        [ManyToOne]
        public ExtractedChromatograms MaDataFile { get; set; }
        [Property]
        public string TextId { get; set; }
        [Property]
        public double PrecursorMz { get; set; }
        [Property]
        public double? StartTime { get; set; }
        [Property]
        public double? EndTime { get; set; }

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

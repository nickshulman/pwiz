using System;
using NHibernate.Mapping.Attributes;

namespace SkydbStorage.Internal.Orm
{
    [Class(Lazy = false)]
    public class ExtractedFile : Entity<ExtractedFile>
    {
        [Property]
        public string FilePath { get; set; }
        [Property]
        public DateTime? LastWriteTime { get; set; }
        [Property]
        public bool HasCombinedIonMobility { get; set; }
        [Property]
        public bool Ms1Centroid { get; set; }
        [Property]
        public bool Ms2Centroid { get; set; }
        [Property]
        public DateTime? RunStartTime { get; set; }
        [Property]
        public double? MaxRetentionTime { get; set; }
        [Property]
        public double? MaxIntensity { get; set; }
        [Property]
        public double? TotalIonCurrentArea { get; set; }
        [Property]
        public string SampleId { get; set; }
        [Property]
        public string InstrumentSerialNumber { get; set; }
    }
}

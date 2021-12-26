using NHibernate.Mapping.Attributes;

namespace SkydbStorage.Internal.Orm
{
    [Class(Lazy = false)]
    public class SpectrumList : Entity<SpectrumList>
    {
        [ManyToOne]
        public ExtractedChromatograms MsDataFile { get; set; }
        [Property]
        public int SpectrumCount { get; set; }
        [Property]
        public byte[] SpectrumIndexBlob { get; set; }
        [Property]
        public byte[] RetentionTimeBlob { get; set; }
    }
}

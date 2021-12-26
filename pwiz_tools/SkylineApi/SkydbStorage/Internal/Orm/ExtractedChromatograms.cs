using NHibernate.Mapping.Attributes;

namespace SkydbStorage.Internal.Orm
{
    [Class(Lazy = false)]
    public class ExtractedChromatograms : Entity<ExtractedChromatograms>
    {
        [Property]
        public virtual string FilePath { get; set; }
    }
}

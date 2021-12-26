using NHibernate.Mapping.Attributes;

namespace SkydbStorage.Internal.Orm
{
    [Class(Lazy = false)]
    public class ExtractedFile : Entity<ExtractedFile>
    {
        [Property]
        public virtual string FilePath { get; set; }
    }
}

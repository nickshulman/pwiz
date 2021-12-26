using System.Collections.Generic;
using NHibernate.Mapping.Attributes;

namespace SkydbApi.Orm
{
    [Class(Lazy = false)]
    public class ChromatogramData : Entity<ChromatogramData>
    {
        [ManyToOne(NotFound = NotFoundMode.Ignore)]
        public virtual SpectrumList SpectrumList { get; set; }
        [Property]
        public virtual int PointCount { get; set; }
        [Property]
        public virtual byte[] IntensitiesBlob { get; set; }
        [Property]
        public virtual byte[] MassErrorsBlob { get; set; }
    }
}

using System.Collections.Generic;
using NHibernate.Mapping.Attributes;

namespace SkydbApi.Orm
{
    [Class]
    public class ChromatogramData : Entity<CandidatePeakGroup>
    {
        [ManyToOne(NotFound = NotFoundMode.Ignore)]
        public virtual SpectrumList SpectrumList { get; set; }
        [Property]
        public virtual int PointCount { get; set; }
        [Property]
        public virtual byte[] IntensitiesData { get; set; }
        [Property]
        public virtual byte[] MassErrorsData { get; set; }
    }
}

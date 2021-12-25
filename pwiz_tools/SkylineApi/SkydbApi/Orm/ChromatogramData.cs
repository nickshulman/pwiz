using System.Collections.Generic;
using SkydbApi.Orm.Attributes;

namespace SkydbApi.Orm
{
    public class ChromatogramData : Entity
    {
        [Column]
        public virtual SpectrumList SpectrumList { get; set; }
        [Column]
        public virtual int PointCount { get; set; }
        [Column]
        public virtual byte[] RetentionTimesData { get; set; }
        [Column]
        public virtual byte[] IntensitiesData { get; set; }
        [Column]
        public virtual byte[] MassErrorsData { get; set; }
    }
}

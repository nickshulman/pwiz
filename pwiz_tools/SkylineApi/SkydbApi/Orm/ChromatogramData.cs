using System.Collections.Generic;

namespace SkydbApi.Orm
{
    public class ChromatogramData : Entity
    {
        public virtual SpectrumList SpectrumList { get; set; }
        public virtual int PointCount { get; set; }
        public virtual byte[] RetentionTimesData { get; set; }
        public virtual byte[] IntensitiesData { get; set; }
        public virtual byte[] MassErrorsData { get; set; }
    }
}

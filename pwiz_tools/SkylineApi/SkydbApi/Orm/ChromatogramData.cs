using NHibernate.Mapping.Attributes;
using System.Collections.Generic;

namespace SkydbApi.Orm
{
    [Class(Lazy = false, Table = nameof(ChromatogramData))]

    public class ChromatogramData : Entity
    {
        [Property]
        public long SpectrumListId { get; set; }
        [Property]
        public int PointCount { get; set; }
        [Property]
        public byte[] RetentionTimesData { get; set; }
        [Property]
        public byte[] IntensitiesData { get; set; }
        [Property]
        public byte[] MassErrorsData { get; set; }
    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NHibernate.Mapping.Attributes;

namespace SkydbApi.Orm
{
    [Class(Lazy = false)]
    public class ChromatogramGroup : Entity<ChromatogramGroup>
    {
        [ManyToOne(NotFound = NotFoundMode.Ignore)]
        public MsDataFile MaDataFile { get; set; }
        [Property]
        public string TextId { get; set; }
        [Property]
        public double PrecursorMz { get; set; }
        [Property]
        public double? StartTime { get; set; }
        [Property]
        public double? EndTime { get; set; }
    }
}

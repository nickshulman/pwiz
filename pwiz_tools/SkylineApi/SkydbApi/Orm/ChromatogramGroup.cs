using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NHibernate.Mapping.Attributes;

namespace SkydbApi.Orm
{
    [Class]
    public class ChromatogramGroup : Entity<ChromatogramGroup>
    {
        [Property]
        public virtual string TextId { get; set; }
        [Property]
        public virtual double PrecursorMz { get; set; }
        [Property]
        public virtual double? StartTime { get; set; }
        [Property]
        public virtual double? EndTime { get; set; }
    }
}

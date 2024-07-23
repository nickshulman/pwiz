using NHibernate.Mapping.Attributes;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SkydbApi.Orm
{
    [Class(Lazy = false, Table = "ChromatogramGroup")]
    public class ChromatogramGroup : Entity
    {
        [Property]
        public string TextId { get; set; }
        [Property]
        public double PrecursorMz { get; set; }
    }
}

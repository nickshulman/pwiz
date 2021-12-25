using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SkydbApi.Orm.Attributes;

namespace SkydbApi.Orm
{
    public class ChromatogramGroup : Entity
    {
        [Column]
        public virtual string TextId { get; set; }
        [Column]
        public virtual double PrecursorMz { get; set; }
    }
}

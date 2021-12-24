using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SkydbApi.Orm
{
    public class ChromatogramGroup : Entity
    {
        public virtual string TextId { get; set; }
        public virtual double PrecursorMz { get; set; }
    }
}

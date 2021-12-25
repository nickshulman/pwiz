using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SkydbApi.Orm.Attributes;

namespace SkydbApi.Orm
{
    public class SpectrumList : Entity
    {
        [Column]
        public virtual int SpectrumCount { get; set; }
        [Column]
        public virtual byte[] SpectrumIndexData { get; set; }
    }
}

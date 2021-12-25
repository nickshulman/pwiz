using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NHibernate.Mapping.Attributes;

namespace SkydbApi.Orm
{
    [Class]
    public class SpectrumList : Entity<SpectrumList>
    {
        [Property]
        public virtual int SpectrumCount { get; set; }
        [Property]
        public virtual byte[] SpectrumIndexData { get; set; }
    }
}

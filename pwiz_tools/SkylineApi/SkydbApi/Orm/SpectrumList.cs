using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SkydbApi.Orm
{
    public class SpectrumList : Entity
    {
        public virtual int SpectrumCount { get; set; }
        public virtual byte[] SpectrumIndexData { get; set; }
    }
}

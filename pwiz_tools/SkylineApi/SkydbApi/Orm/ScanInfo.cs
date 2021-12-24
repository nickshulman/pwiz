using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SkydbApi.Orm
{
    public class SpectrumInfo : Entity
    {
        public virtual MsDataFile MsDataFile { get; set; }
        public virtual int SpectrumIndex { get; set; }
        public virtual string SpectrumIdentifier { get; set; }
        public virtual double RetentionTime { get; set; }
    }
}

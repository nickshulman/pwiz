using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SkydbApi.Orm.Attributes;

namespace SkydbApi.Orm
{
    public class SpectrumInfo : Entity
    {
        [Column]
        public virtual MsDataFile MsDataFile { get; set; }
        [Column]
        public virtual int SpectrumIndex { get; set; }
        [Column]
        public virtual string SpectrumIdentifier { get; set; }
        [Column]
        public virtual double RetentionTime { get; set; }
    }
}

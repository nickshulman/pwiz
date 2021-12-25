using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NHibernate.Mapping.Attributes;

namespace SkydbApi.Orm
{
    [Class]
    public class SpectrumInfo : Entity<SpectrumInfo>
    {
        [ManyToOne(NotFound = NotFoundMode.Ignore)]
        public virtual MsDataFile MsDataFile { get; set; }
        [Property]
        public virtual int SpectrumIndex { get; set; }
        [Property]
        public virtual string SpectrumIdentifier { get; set; }
        [Property]
        public virtual double RetentionTime { get; set; }
    }
}

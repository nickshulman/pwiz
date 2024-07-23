using NHibernate.Mapping.Attributes;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SkydbApi.Orm
{

    [Class(Lazy = false, Table = nameof(SpectrumInfo))]
    public class SpectrumInfo : Entity
    {
        [Property]
        public long MsDataFileId { get; set; }
        [Property]
        public int SpectrumIndex { get; set; }
        [Property]
        public string SpectrumIdentifier { get; set; }
        [Property]
        public double RetentionTime { get; set; }
    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NHibernate.Mapping.Attributes;

namespace SkydbStorage.Internal.Orm
{
    [Class(Lazy = false)]
    public class InstrumentInfo : Entity<InstrumentInfo>
    {
        [ManyToOne(ClassType = typeof(ExtractedFile))]
        public long ExtractedFile { get; set; }
        [Property]
        public string Model { get; set; }
        [Property]
        public string Ionization { get; set; }
        [Property]
        public string Analyzer { get; set; }
        [Property]
        public string Detector { get; set; }
    }
}

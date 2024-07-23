using NHibernate.Mapping.Attributes;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SkydbApi.Orm
{
    [Class(Lazy = false, Table = nameof(TransitionChromatogram))]
    public class TransitionChromatogram : Entity
    {
        [Property]
        public virtual long ChromatogramGroupId { get; set; }
        [Property]
        public virtual double ProductMz { get; set; }
        [Property]
        public virtual double ExtractionWidth { get; set; }
        [Property]
        public virtual double? IonMobilityValue { get; set; }
        [Property]
        public virtual double? IonMobilityExtractionWidth { get; set; }
        [Property]

        public virtual int Source { get; set; }
    }
}

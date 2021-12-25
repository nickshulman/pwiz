using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SkydbApi.Orm.Attributes;

namespace SkydbApi.Orm
{
    public class TransitionChromatogram : Entity
    {
        [Column]
        public virtual ChromatogramGroup ChromatogramGroup { get; set; }
        [Column]
        public virtual double ProductMz { get; set; }
        [Column]
        public virtual double ExtractionWidth { get; set; }
        [Column]
        public virtual double? IonMobilityValue { get; set; }
        [Column]
        public virtual double? IonMobilityExtractionWidth { get; set; }
        [Column]

        public virtual int Source { get; set; }
    }
}

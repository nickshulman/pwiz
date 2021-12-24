using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SkydbApi.Orm
{
    public class TransitionChromatogram : Entity
    {
        public virtual ChromatogramGroup ChromatogramGroup { get; set; }
        public virtual double ProductMz { get; set; }
        public virtual double ExtractionWidth { get; set; }
        public virtual double? IonMobilityValue { get; set; }
        public virtual double? IonMobilityExtractionWidth { get; set; }

        public virtual int Source { get; set; }
    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NHibernate.Mapping.Attributes;

namespace SkydbApi.Orm
{
    [Class]
    public class TransitionChromatogram : Entity<CandidatePeakGroup>
    {
        [ManyToOne(NotFound = NotFoundMode.Ignore)]
        public virtual ChromatogramGroup ChromatogramGroup { get; set; }
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

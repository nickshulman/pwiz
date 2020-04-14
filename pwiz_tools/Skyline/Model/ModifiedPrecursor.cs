using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using pwiz.Skyline.Model.Results;
using pwiz.Skyline.Util;

namespace pwiz.Skyline.Model
{
    public abstract class ModifiedPrecursor
    {

        public IsotopeDistInfo IsotopeDist { get; private set; }
        public TypedMass PrecursorMz { get; private set; }
    }

    public class ModifiedMoleculePrecursor : ModifiedPrecursor
    {
        public CustomMolecule CustomMolecule { get; private set; }
    }

    public class ModifiedPeptidePrecursor : ModifiedPrecursor
    {
        public string Sequence { get; private set; }

    }
}

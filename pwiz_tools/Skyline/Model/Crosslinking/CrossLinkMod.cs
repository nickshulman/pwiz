using System.Collections.Generic;
using pwiz.Common.Collections;
using pwiz.Common.SystemUtil;

namespace pwiz.Skyline.Model.Crosslinking
{
    public class CrosslinkMod : Immutable
    {
        public CrosslinkMod(int indexAa, CrosslinkerDef crosslinkerDef, IEnumerable<LinkedPeptide> linkedPeptides)
        {
            IndexAa = indexAa;
            CrosslinkerDef = crosslinkerDef;
            LinkedPeptides = ImmutableList.ValueOf(linkedPeptides);
        }

        public int IndexAa { get; private set; }

        public CrosslinkerDef CrosslinkerDef { get; private set; }

        public ImmutableList<LinkedPeptide> LinkedPeptides { get; private set; }
    }
}

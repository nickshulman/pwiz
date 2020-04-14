using System;
using System.Collections.Generic;
using pwiz.Common.Collections;
using pwiz.Common.SystemUtil;
using pwiz.Skyline.Model.DocSettings;

namespace pwiz.Skyline.Model.Crosslinking
{
    public class LinkedPeptide : Immutable
    {
        public LinkedPeptide(int indexAa, string sequence, IEnumerable<ExplicitMod> explicitMods)
        {
            IndexAa = indexAa;
            Sequence = sequence;
            ExplicitMods = ImmutableList.ValueOf(explicitMods);
        }

        public int IndexAa { get; private set; }

        public String Sequence { get; private set; }
        public ImmutableList<ExplicitMod> ExplicitMods { get; private set; }
    }
}

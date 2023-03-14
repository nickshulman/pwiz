using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using pwiz.Common.Collections;

namespace pwiz.Skyline.Model.GroupComparison
{
    public class PeptideListQuantifier
    {
        public PeptideListQuantifier(IEnumerable<PeptideQuantifier> peptideQuantifiers)
        {
            PeptideQuantifiers = ImmutableList.ValueOf(peptideQuantifiers);
        }

        public ImmutableList<PeptideQuantifier> PeptideQuantifiers { get; }
    }
}

using System.Collections.Generic;
using pwiz.Common.Collections;

namespace pwiz.Skyline.Model.PeakImputation
{
    public class MoleculePeaks
    {
        public MoleculePeaks(IdentityPath identityPath, IEnumerable<AlignedPeak> peaks)
        {
            PeptideIdentityPath = identityPath;
            Peaks = ImmutableList.ValueOf(peaks);
        }

        public IdentityPath PeptideIdentityPath { get; }

        public ImmutableList<AlignedPeak> Peaks { get; }
    }
}
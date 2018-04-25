using System.Collections.Generic;
using pwiz.Common.Collections;
using pwiz.Common.SystemUtil;

namespace TopographTool.Model
{
    public class Precursor : Immutable
    {
        public Precursor(ResultRow row, IEnumerable<Transition> transitions)
        {
            ModifiedSequence = ModifiedSequence.Parse(row.ModifiedSequenceFullNames);
            PrecursorCharge = row.PrecursorCharge;
            PrecursorMz = row.PrecursorMz;
            Transitions = ImmutableList.ValueOf(transitions);
        }

        public ModifiedSequence ModifiedSequence { get; private set; }
        public int PrecursorCharge { get; private set; }
        public double PrecursorMz { get; private set; }
        public ImmutableList<Transition> Transitions { get; private set; }
    }
}

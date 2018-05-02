using System.Collections.Generic;
using pwiz.Common.Chemistry;
using pwiz.Common.Collections;
using pwiz.Common.SystemUtil;
using TopographTool.Model.DataRows;

namespace TopographTool.Model
{
    public class Precursor : Immutable
    {
        public Precursor(TransitionRow row, IEnumerable<Transition> transitions)
        {
            ModifiedSequence = ModifiedSequence.Parse(row.ModifiedSequenceFullNames);
            PrecursorCharge = row.PrecursorCharge;
            PrecursorMz = row.PrecursorMz;
            Transitions = ImmutableList.ValueOf(transitions);
            MzDistribution = RowReader.ParseMassDistribution(row.PrecursorMzs, row.PrecursorAbundances);
        }

        public ModifiedSequence ModifiedSequence { get; private set; }
        public int PrecursorCharge { get; private set; }
        public double PrecursorMz { get; private set; }
        public MassDistribution MzDistribution { get; private set; }
        public ImmutableList<Transition> Transitions { get; private set; }
    }
}

using System.Collections.Generic;
using pwiz.Common.Collections;
using pwiz.Common.SystemUtil;

namespace TopographTool.Model
{
    public class Peptide : Immutable
    {
        public Peptide(ResultRow row, IEnumerable<Precursor> precursors)
        {
            PeptideModifiedSequence = ModifiedSequence.Parse(row.PeptideModifiedSequenceFullNames);
            Precursors = ImmutableList.ValueOf(precursors);
        }
        public ModifiedSequence PeptideModifiedSequence { get; private set; }
        public ImmutableList<Precursor> Precursors { get; private set; }

        public override string ToString()
        {
            return PeptideModifiedSequence.ToString();
        }

        public int GetLabelCount(Precursor precursor)
        {
            return precursor.ModifiedSequence.Modifications.Count - PeptideModifiedSequence.Modifications.Count;
        }
    }
}

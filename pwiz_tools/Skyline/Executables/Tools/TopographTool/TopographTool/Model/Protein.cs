using System.Collections.Generic;
using pwiz.Common.Collections;
using pwiz.Common.SystemUtil;
using TopographTool.Model.DataRows;

namespace TopographTool.Model
{
    public class Protein : Immutable
    {
        public Protein(TransitionRow resultRow, IEnumerable<Peptide> children)
        {
            Name = resultRow.Protein;
            Peptides = ImmutableList.ValueOf(children);
        }
        public string Name { get; private set; }
        public ImmutableList<Peptide> Peptides { get; private set; }
    }
}

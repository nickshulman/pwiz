using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using pwiz.Common.Collections;

namespace pwiz.Skyline.Model
{
    public class PeptideGroupLookup
    {
        private readonly ILookup<Peptide, PeptideGroup> _lookup;

        private PeptideGroupLookup(ILookup<Peptide, PeptideGroup> lookup)
        {
            _lookup = lookup;
        }

        public static PeptideGroupLookup FromPeptideGroups(IEnumerable<PeptideGroupDocNode> peptideGroups)
        {
            var lookup = peptideGroups.SelectMany(peptideGroup => peptideGroup.Molecules
                    .Select(molecule => Tuple.Create(peptideGroup.PeptideGroup, molecule.Peptide)))
                .ToLookup<Tuple<PeptideGroup, Peptide>, Peptide, PeptideGroup>(tuple => tuple.Item2,
                    tuple => tuple.Item1, DocNodeChildren.IDENTITY_EQUALITY_COMPARER);
            return new PeptideGroupLookup(lookup);
        }

        public static DocNodeChildren MakePeptideGroupLookup(DocNodeChildren peptideGroups, out PeptideGroupLookup lookup)
        {
            lookup = FromPeptideGroups(peptideGroups.Cast<PeptideGroupDocNode>());
            PeptideGroupDocNode[] newPeptideGroups = null;
            foreach (var grouping in lookup._lookup)
            {
                PeptideGroup firstPeptideGroup = null;
                PeptideDocNode peptideDocNode = null;
                foreach (var peptideGroup in grouping)
                {
                    if (firstPeptideGroup == null)
                    {
                        firstPeptideGroup = peptideGroup;
                        continue;
                    }

                    if (peptideDocNode == null)
                    {
                        var firstPeptideGroupDocNode = (PeptideGroupDocNode)peptideGroups[
                            peptideGroups.IndexOf(firstPeptideGroup)];
                        peptideDocNode = (PeptideDocNode)firstPeptideGroupDocNode.FindNode(grouping.Key);
                    }

                    int peptideGroupIndex = peptideGroups.IndexOf(peptideGroup);
                    PeptideGroupDocNode peptideGroupDocNode = newPeptideGroups?[peptideGroupIndex] ??
                                                              (PeptideGroupDocNode)peptideGroups[peptideGroupIndex];
                    if (ReferenceEquals(peptideGroupDocNode.FindNode(grouping.Key), peptideDocNode))
                    {
                        continue;
                    }

                    if (newPeptideGroups == null)
                    {
                        newPeptideGroups = peptideGroups.Cast<PeptideGroupDocNode>().ToArray();
                    }

                    newPeptideGroups[peptideGroupIndex] =
                        (PeptideGroupDocNode)newPeptideGroups[peptideGroupIndex].ReplaceChild(peptideDocNode);
                }
            }

            if (newPeptideGroups == null)
            {
                return peptideGroups;
            }

            return new DocNodeChildren(newPeptideGroups, peptideGroups);
        }
    }
}

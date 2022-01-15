using System;
using System.Linq;
using pwiz.Skyline.Model.DocSettings;

namespace pwiz.Skyline.Model
{
    public class ProteinPeptide
    {
        public ProteinPeptide(string peptideSequence)
        {
            PeptideSequence = peptideSequence;
        }
        public string PeptideSequence { get; private set; }
        public int? Begin { get; private set; }

        public int? End
        {
            get
            {
                return Begin + PeptideSequence.Length;
            }
        }

        public char? PrevAa { get; private set; }
        public char? NextAa { get; private set; }

        public static ProteinPeptide FindPeptide(string peptideSequence, Enzyme enzyme, string proteinSequence)
        {
            var enzymeLocation = enzyme?.FindPeptideInProtein(peptideSequence, proteinSequence).Take(1).ToArray() ?? Array.Empty<int>();
            int? begin = null;
            if (enzymeLocation.Length > 0)
            {
                begin = enzymeLocation[0];
            }
            else
            {
                int index = proteinSequence.IndexOf(peptideSequence, StringComparison.Ordinal);
                if (index >= 0)
                {
                    begin = index;
                }
            }

            return MakeProteinPeptide(peptideSequence, begin, proteinSequence);
        }

        public static ProteinPeptide MakeProteinPeptide(string peptideSequence, int? begin, string proteinSequence)
        {
            var proteinPeptide = new ProteinPeptide(peptideSequence);
            if (begin.HasValue)
            {
                proteinPeptide.Begin = begin;
                if (begin > 0)
                {
                    proteinPeptide.PrevAa = proteinSequence[begin.Value - 1];
                }

                var end = begin.Value + peptideSequence.Length;
                if (end < proteinSequence.Length - 1)
                {
                    proteinPeptide.NextAa = proteinSequence[end + 1];
                }
            }

            return proteinPeptide;
        }
    }
}

using System.Collections.Generic;
using System.Linq;
using pwiz.Common.Chemistry;
using pwiz.Common.SystemUtil;
using pwiz.Skyline.Model.DocSettings;

namespace pwiz.Skyline.Model.Results.Deconvolution
{
    public class FragmentDistribution : Immutable
    {
        public FragmentDistribution(FragmentedMolecule.Settings settings, FragmentedMolecule fragmentedMolecule)
        {
            FragmentedMolecule = fragmentedMolecule;
            FragmentedMoleculeSettings = settings;
            PrecursorMzDistribution = settings.GetMassDistribution(fragmentedMolecule.PrecursorFormula,
                fragmentedMolecule.PrecursorMassShift, fragmentedMolecule.PrecursorCharge);
            PrecursorMonoMass = settings.GetMonoMass(fragmentedMolecule.PrecursorFormula,
                fragmentedMolecule.PrecursorMassShift, 0);
            FragmentMzDistribution = settings.GetMassDistribution(fragmentedMolecule.FragmentFormula,
                fragmentedMolecule.FragmentMassShift, fragmentedMolecule.FragmentCharge);
            ComplementaryFragmentMzDistribution = settings.GetMassDistribution(
                fragmentedMolecule.GetComplementaryProductFormula(), 0, fragmentedMolecule.PrecursorCharge);
        }

        public FragmentedMolecule FragmentedMolecule { get; private set; }
        public FragmentedMolecule.Settings FragmentedMoleculeSettings { get; private set; }
        public MassDistribution PrecursorMzDistribution { get; private set; }
        public double PrecursorMonoMass { get; private set; }
        public MassDistribution FragmentMzDistribution { get; private set; }
        public MassDistribution ComplementaryFragmentMzDistribution { get; private set; }
        public int FragmentCharge { get { return FragmentedMolecule.FragmentCharge; } }
        public int PrecursorCharge { get { return FragmentedMolecule.PrecursorCharge; } }

        public IDictionary<double, double> GetFragmentDistribution(double? precursorMinMz, double? precursorMaxMz)
        {
            var result = new Dictionary<double, double>();
            foreach (var entry in FragmentMzDistribution)
            {
                var fragmentPrecursorMz = entry.Key * FragmentCharge / PrecursorCharge;
                double? minOtherMz = precursorMinMz - fragmentPrecursorMz;
                double? maxOtherMz = precursorMaxMz - fragmentPrecursorMz;
                var otherFragmentAbundance = ComplementaryFragmentMzDistribution
                    .Where(oFrag => !minOtherMz.HasValue || oFrag.Key >= minOtherMz
                                    && !maxOtherMz.HasValue || oFrag.Key <= maxOtherMz).Sum(frag => frag.Value);
                if (otherFragmentAbundance > 0)
                {
                    result.Add(entry.Key, otherFragmentAbundance * entry.Value);
                }
            }
            return result;
        }

        public FragmentDistribution ChangeTransition(SrmSettings srmSettings, PeptideDocNode peptideDocNode,
            TransitionGroupDocNode transitionGroupDocNode, TransitionDocNode transitionDocNode)
        {
            return ChangeProp(ImClone(this), im =>
            {
                im.FragmentedMolecule = FragmentedMolecule.GetFragmentedMolecule(srmSettings, peptideDocNode,
                    transitionGroupDocNode, transitionDocNode);
                im.UpdateFragmentDistribution();
            });
        }

        private void UpdateFragmentDistribution()
        {
            FragmentMzDistribution = FragmentedMoleculeSettings.GetMassDistribution(
                FragmentedMolecule.FragmentFormula,
                FragmentedMolecule.FragmentMassShift, FragmentedMolecule.FragmentCharge);
            ComplementaryFragmentMzDistribution = FragmentedMoleculeSettings.GetMassDistribution(
                FragmentedMolecule.GetComplementaryProductFormula(), 0, FragmentedMolecule.PrecursorCharge);
        }
    }
}

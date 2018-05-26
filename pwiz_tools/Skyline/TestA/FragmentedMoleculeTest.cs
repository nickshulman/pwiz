using System;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using pwiz.Common.Chemistry;
using pwiz.Skyline.Model;
using pwiz.Skyline.Model.DocSettings;
using pwiz.Skyline.Model.Results.Deconvolution;
using pwiz.Skyline.Properties;
using pwiz.Skyline.Util;
using pwiz.SkylineTestUtil;

namespace pwiz.SkylineTestA
{
    [TestClass]
    public class FragmentedMoleculeTest : AbstractUnitTest
    {
        [TestMethod]
        public void TestGetPrecursorFormula()
        {
            var modifiedSequence = new ModifiedSequence("PEPTIDE", new ModifiedSequence.Modification[0], MassType.Monoisotopic);
            var fragmentedMolecule = FragmentedMolecule.EMPTY.ChangeModifiedSequence(modifiedSequence);
            var precursorFormula = fragmentedMolecule.PrecursorFormula;
            Assert.AreEqual(0, fragmentedMolecule.PrecursorMassShift);
            var sequenceMassCalc = new SequenceMassCalc(MassType.Monoisotopic);
            var expectedFormula = Molecule.Parse(sequenceMassCalc.GetMolecularFormula(modifiedSequence.GetUnmodifiedSequence()));
            Assert.AreEqual(expectedFormula.Count, precursorFormula.Count);
            foreach (var entry in expectedFormula)
            {
                Assert.AreEqual(entry.Value, precursorFormula.GetElementCount(entry.Key));
            }
        }

        [TestMethod]
        public void TestGetFragmentFormula()
        {
            var pepseq = "PEPTIDE";
            var sequenceMassCalc = new SequenceMassCalc(MassType.Monoisotopic);
            var precursor = FragmentedMolecule.EMPTY.ChangeModifiedSequence(
                new ModifiedSequence(pepseq, new ModifiedSequence.Modification[0], MassType.Monoisotopic));
            var peptide = new Peptide(precursor.UnmodifiedSequence);
            var transitionGroup = new TransitionGroup(peptide, Adduct.SINGLY_PROTONATED, IsotopeLabelType.light);
            var settings = SrmSettingsList.GetDefault();
            var transitionGroupDocNode = new TransitionGroupDocNode(transitionGroup, Annotations.EMPTY, settings,
                ExplicitMods.EMPTY, null, null, null, new TransitionDocNode[0], false);
            foreach (var ionType in new[] {IonType.a, IonType.b, IonType.c, IonType.x, IonType.y, IonType.z})
            {
                for (int ordinal = 1; ordinal < pepseq.Length; ordinal++)
                {
                    var transition = new Transition(transitionGroup, ionType, Transition.OrdinalToOffset(ionType, ordinal, pepseq.Length), 0, Adduct.SINGLY_PROTONATED);
                    var fragment = precursor.ChangeFragmentIon(ionType, ordinal);
                    var actualMassDistribution = DistributionSettings.DEFAULT.GetMassDistribution(fragment.FragmentFormula);
                    var expectedMz = sequenceMassCalc.GetFragmentMass(transition, transitionGroupDocNode.IsotopeDist);
                    var actualMz = actualMassDistribution.MostAbundanceMass;
                    if (Math.Abs(expectedMz - actualMz) > .001)
                    {
                        Assert.AreEqual(expectedMz, actualMz, .001);
                    }
                }
            }
        }

        [TestMethod]
        public void TestFragmentDistribution()
        {
            var staticMod = UniMod.GetModification("Acetyl (N-term)", true);
            var modifiedSequence = new ModifiedSequence("MQNDAGEFVDLYVPR",
                new[]
                {
                    new ModifiedSequence.Modification(new ExplicitMod(0, staticMod), staticMod.MonoisotopicMass.Value,
                        staticMod.AverageMass.Value)
                }, MassType.Monoisotopic);
            var fragmentedMolecule = FragmentedMolecule.EMPTY.ChangeModifiedSequence(modifiedSequence)
                .ChangePrecursorCharge(2)
                .ChangeFragmentIon(IonType.y, 10)
                .ChangeFragmentCharge(1);
            var distributionCache = new DistributionCache(DistributionSettings.DEFAULT);
            const double isolationMax = 899.9;
            var precursorDistribution = distributionCache.GetMzDistribution(fragmentedMolecule.PrecursorFormula, 0,
                fragmentedMolecule.PrecursorCharge);
            var fullFragmentDistribution = distributionCache.GetMzDistribution(fragmentedMolecule.FragmentFormula, 0,
                fragmentedMolecule.FragmentCharge);
            Assert.AreEqual(1, fullFragmentDistribution.Sum(kvp=>kvp.Value), .01);
            var filteredFragmentDistribution =
                fragmentedMolecule.GetFragmentDistribution(distributionCache, null, isolationMax);
            double filteredPrecursorAbundance = precursorDistribution.Where(kvp => kvp.Key <= isolationMax).Sum(kvp => kvp.Value);
            double filteredFragmentAbundance = filteredFragmentDistribution.Sum(kvp => kvp.Value);
            Assert.AreEqual(filteredPrecursorAbundance, filteredFragmentAbundance, .01);
        }
    }
}

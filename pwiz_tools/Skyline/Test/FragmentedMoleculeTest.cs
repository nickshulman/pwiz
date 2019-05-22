using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using pwiz.Common.Chemistry;
using pwiz.Skyline.Model;
using pwiz.Skyline.Model.DocSettings;
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
                    var fragment = precursor.ChangeFragmentIon(ionType, ordinal).ChangeFragmentCharge(1);
                    var compare = fragment.ChangeModifiedSequence(fragment.ModifiedSequence);
                    var actualMassDistribution = FragmentedMolecule.Settings.DEFAULT.GetMassDistribution(
                        fragment.FragmentFormula, 0, 0);
                    var expectedMz = sequenceMassCalc.GetFragmentMass(transition, transitionGroupDocNode.IsotopeDist);
                    var actualMz = actualMassDistribution.MostAbundanceMass;
                    if (Math.Abs(expectedMz - actualMz) > .001)
                    {
                        Assert.AreEqual(expectedMz, actualMz, .001);
                    }

                    if (ordinal > 1)
                    {
                        var incremented = precursor.ChangeFragmentIon(ionType, ordinal - 1).ChangeFragmentCharge(1).IncrementFragmentOrdinal();
                        VerifySame(fragment, incremented);
                    }
                }
            }
        }

        private void VerifySame(FragmentedMolecule mol1, FragmentedMolecule mol2)
        {
            Assert.AreEqual(mol1.FragmentOrdinal, mol2.FragmentOrdinal);
            Assert.AreEqual(mol1.FragmentFormula, mol2.FragmentFormula);
            Assert.AreEqual(mol1.FragmentMassShift, mol2.FragmentMassShift);
            Assert.AreEqual(mol1.FragmentCharge, mol2.FragmentCharge);
            Assert.AreEqual(mol1.FragmentIonType, mol2.FragmentIonType);
            Assert.AreEqual(mol1.FragmentLosses, mol2.FragmentLosses);
            Assert.AreEqual(mol1.FragmentMassShift, mol2.FragmentMassShift);
            Assert.AreEqual(mol1.FragmentMassType, mol2.FragmentMassType);
            Assert.AreEqual(mol1.ModifiedSequence, mol2.ModifiedSequence);
            Assert.AreEqual(mol1.PrecursorCharge, mol2.PrecursorCharge);
            Assert.AreEqual(mol1.PrecursorFormula, mol2.PrecursorFormula);
            Assert.AreEqual(mol1.PrecursorMassShift, mol2.PrecursorMassShift);
            Assert.AreEqual(mol1.PrecursorMassType, mol2.PrecursorMassType);

        }
    }
}

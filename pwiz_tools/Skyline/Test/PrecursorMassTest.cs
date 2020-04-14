using Microsoft.VisualStudio.TestTools.UnitTesting;
using pwiz.Skyline.Model;
using pwiz.Skyline.Model.DocSettings;
using pwiz.Skyline.Properties;
using pwiz.Skyline.Util;
using pwiz.SkylineTestUtil;

namespace pwiz.SkylineTest
{
    [TestClass]
    public class PrecursorMassTest : AbstractUnitTest
    {
        [TestMethod]
        public void TestCustomMoleculePrecursorMass()
        {
            var peptide = new Peptide(new CustomMolecule("H2O"));
            var transitionGroup = new TransitionGroup(peptide, Adduct.FromCharge(1, Adduct.ADDUCT_TYPE.charge_only), IsotopeLabelType.light);
            var monoGroupDocNode = MakeTransitionGroupDocNode(transitionGroup, GetSrmSettings(MassType.Monoisotopic));
            Assert.AreEqual(MassType.Monoisotopic, monoGroupDocNode.PrecursorMzMassType);
            Assert.AreEqual(18.010016, monoGroupDocNode.PrecursorMz, 0.000001);
            var averageGroupDocNode = MakeTransitionGroupDocNode(transitionGroup, GetSrmSettings(MassType.Average));
            Assert.AreEqual(MassType.Average, averageGroupDocNode.PrecursorMzMassType);
            Assert.AreEqual(18.014731, averageGroupDocNode.PrecursorMz, 0.000001);
        }

        [TestMethod]
        public void TestPeptidePrecursorMass()
        {
            var peptide = new Peptide("PEPTIDE");
            var transitionGroup = new TransitionGroup(peptide, Adduct.FromChargeProtonated(1), IsotopeLabelType.light);
            var monoGroupDocNode = MakeTransitionGroupDocNode(transitionGroup, GetSrmSettings(MassType.Monoisotopic));
            Assert.AreEqual(MassType.MonoisotopicMassH, monoGroupDocNode.PrecursorMzMassType);
            Assert.AreEqual(800.367240305, monoGroupDocNode.PrecursorMz, .000001);
            var averageGroupDocNode = MakeTransitionGroupDocNode(transitionGroup, GetSrmSettings(MassType.Average));
            Assert.AreEqual(MassType.AverageMassH, averageGroupDocNode.PrecursorMzMassType);
            Assert.AreEqual(800.834896, averageGroupDocNode.PrecursorMz, .000001);
        }

        private static SrmSettings GetSrmSettings(MassType massType)
        {
            var settings = SrmSettingsList.GetDefault();
            settings = settings.ChangeTransitionSettings(settings.TransitionSettings.ChangePrediction(settings
                .TransitionSettings.Prediction.ChangePrecursorMassType(massType).ChangeFragmentMassType(massType)));
            return settings;
        }

        private static TransitionGroupDocNode MakeTransitionGroupDocNode(TransitionGroup transitionGroup,
            SrmSettings settings)
        {
            return new TransitionGroupDocNode(transitionGroup, Annotations.EMPTY, settings, ExplicitMods.EMPTY, null, ExplicitTransitionGroupValues.EMPTY, null, null, false);
        }
    }
}

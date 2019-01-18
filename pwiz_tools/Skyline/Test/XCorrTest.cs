using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using pwiz.Skyline.Model;
using pwiz.Skyline.Model.DocSettings;
using pwiz.Skyline.Model.Results;
using pwiz.Skyline.Model.XCorr;
using pwiz.Skyline.Properties;
using pwiz.Skyline.Util;
using pwiz.SkylineTestUtil;

namespace pwiz.SkylineTest
{
    [TestClass]
    public class XCorrTest : AbstractUnitTest
    {
        [TestMethod]
        public void TestGetYIons()
        {
            var peptide = new Peptide("ELVIS");
            var transitionGroup = new TransitionGroup(peptide, Adduct.SINGLY_PROTONATED, IsotopeLabelType.light);
            var srmSettings = SrmSettingsList.GetDefault();
            var transitionGroupDocNode = new TransitionGroupDocNode(transitionGroup, Annotations.EMPTY, srmSettings,
                ExplicitMods.EMPTY, null, null, null, new TransitionDocNode[0], false);
            var yIons = ArrayXCorrCalculator.GetFragmentIons(transitionGroupDocNode, IonType.y).ToArray();
            Assert.AreEqual(5, yIons.Length);
            Assert.AreEqual(106.049, yIons[0].Mass, .01);
        }
    }
}

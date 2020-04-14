using Microsoft.VisualStudio.TestTools.UnitTesting;
using pwiz.Skyline.Model;
using pwiz.Skyline.Model.Crosslinking;
using pwiz.SkylineTestUtil;

namespace pwiz.SkylineTest.Crosslinking
{
    [TestClass]
    public class CrosslinkModTest : AbstractUnitTest
    {
        [TestMethod]
        public void TestCrosslinkMod()
        {
            CrosslinkerDef crosslinkerDef = new CrosslinkerDef("Disulfide", new FormulaMass("-H2"));

        }
    }
}

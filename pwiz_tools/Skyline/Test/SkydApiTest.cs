using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using pwiz.SkylineTestUtil;
using SkydbApi.DataApi;

namespace pwiz.SkylineTest
{
    [TestClass]
    public class SkydbApiTest : AbstractUnitTest
    {
        [TestMethod]
        public void TestSkydbApi()
        {
            var filePath = Path.Combine(TestContext.TestDir, "test.skydb");
            var skydbFile = SkydbFile.CreateNewSkydbFile(filePath);

        }
    }
}

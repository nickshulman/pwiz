using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using pwiz.Skyline.Model.Serialization;
using pwiz.SkylineTestUtil;

namespace pwiz.SkylineTestFunctional
{
    [TestClass]
    public class SavePerformanceTest : AbstractFunctionalTest
    {
        [TestMethod]
        public void TestSavePerformance()
        {
            RunFunctionalTest();
        }

        protected override void DoTest()
        {
            RunUI(()=>SkylineWindow.OpenFile(@"K:\bugs_k\maccoss\SpeedTest\TRX-Pelt-Astral-Pt4of10.sky"));
            WaitForDocumentLoaded(720000);
            var times = new List<long>();
            for (int iteration = 0; iteration < 10; iteration++)
            {
                WaitForDocumentLoaded(720000);
                var stopWatch = new Stopwatch();
                stopWatch.Start();
                RunUI(()=>SkylineWindow.SaveDocument());
                times.Add(stopWatch.ElapsedMilliseconds);
            }

            Console.Out.WriteLine("Average time:{0}s", times.Average() / 1000);
            RunUI(()=>SkylineWindow.NewDocument());
        }
    }
}

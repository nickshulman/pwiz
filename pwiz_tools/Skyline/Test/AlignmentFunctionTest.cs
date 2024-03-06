using System.Linq;
using System.Threading;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using pwiz.Skyline.Model.RetentionTimes;
using pwiz.SkylineTestUtil;

namespace pwiz.SkylineTest
{
    [TestClass]
    public class AlignmentFunctionTest : AbstractUnitTest
    {
        [TestMethod]
        public void TestPiecewiseLinearAlignmentFunction()
        {
            var xValues = Enumerable.Range(0, 100).Select(i => i + .01).ToArray();
            var yValues = Enumerable.Range(0, 100).Select(i => (double) i * i).ToArray();
            var kdeAligner = new KdeAligner(1,2);
            kdeAligner.Train(xValues, yValues, CancellationToken.None);
            var alignmentFunction = kdeAligner.ToAlignmentFunction();
            foreach (var x in xValues)
            {
                AssertEx.AreEqual(alignmentFunction.GetY(x), kdeAligner.GetValue(x));
            }
        }
    }
}

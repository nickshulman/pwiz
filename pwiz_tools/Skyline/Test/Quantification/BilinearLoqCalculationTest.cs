using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using pwiz.Skyline.Model.DocSettings.AbsoluteQuantification;
using pwiz.SkylineTestUtil;

namespace pwiz.SkylineTest.Quantification
{
    [TestClass]
    public class BilinearLoqCalculationTest : AbstractUnitTest
    {
        public void TestOnePeptide()
        {
            var points = new List<WeightedPoint>
            {
                new WeightedPoint(1,15000000),
                new WeightedPoint(1,15100000),
                new WeightedPoint(1,13000000),
                new WeightedPoint(0.7,14800000),
                new WeightedPoint(0.7,10100000),
                new WeightedPoint(0.7,13500000),
                new WeightedPoint(0.5,7744250.5),
                new WeightedPoint(0.5,7270129.5),
                new WeightedPoint(0.5,6329029.5),
                new WeightedPoint(0.3,4502537),
                new WeightedPoint(0.3,3921876),
                new WeightedPoint(0.3,4751570),
                new WeightedPoint(0.1,1282782.5),
                new WeightedPoint(0.1,1446317.4),
                new WeightedPoint(0.1,461786.84),
                new WeightedPoint(0.03,288215.06),
                new WeightedPoint(0.03,197678.08),
                new WeightedPoint(0.03,90000.51),
                new WeightedPoint(0.01,74921.63),
                new WeightedPoint(0.01,38558.312),
                new WeightedPoint(0.01,15350.016),
                new WeightedPoint(0.007,34329.16),
                new WeightedPoint(0.007,0),
                new WeightedPoint(0.007,15687.2),
                new WeightedPoint(0.005,24384.94),
                new WeightedPoint(0.005,0),
                new WeightedPoint(0.005,0),
                new WeightedPoint(0.003,35251.355),
                new WeightedPoint(0.003,5041.397),
                new WeightedPoint(0.003,28582.852),
                new WeightedPoint(0.001,78376.914),
                new WeightedPoint(0.001,0),
                new WeightedPoint(0.001,26775.148),
                new WeightedPoint(0,21026.031),
                new WeightedPoint(0,0),
                new WeightedPoint(0,18674.63),
                new WeightedPoint(0.07,891551.5),
                new WeightedPoint(0.07,176382.44),
                new WeightedPoint(0.07,701183.6),
                new WeightedPoint(0.05,448789.62),
                new WeightedPoint(0.05,355915.88),
                new WeightedPoint(0.05,293610.2),
            };
        }
    }
}

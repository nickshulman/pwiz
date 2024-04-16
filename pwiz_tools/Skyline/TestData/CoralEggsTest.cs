using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using pwiz.ProteowizardWrapper;
using pwiz.Skyline.Model.Results.Spectra;
using pwiz.Skyline.Model.Results.Spectra.Alignment;
using pwiz.SkylineTestUtil;

namespace pwiz.SkylineTestData
{
    [TestClass]
    public class CoralEggsTest : AbstractUnitTest
    {
        private ResultFileMetaData GetResultFileMetadata(string path)
        {
            using var msDataFile = new MsDataFileImpl(TestFilesDir.GetTestPath(path));
            var spectrumSummaries = new List<SpectrumSummary>();
            for (int spectrumIndex = 0; spectrumIndex < msDataFile.SpectrumCount; spectrumIndex++)
            {
                spectrumSummaries.Add(SpectrumSummary.FromSpectrum(msDataFile.GetSpectrum(spectrumIndex)));
            }
            return new ResultFileMetaData(spectrumSummaries);
        }

        [TestMethod]
        public void TestCoralEggs()
        {
            TestFilesDir = new TestFilesDir(TestContext, @"TestData\CoralEggsTest.zip");
            var metadata1 =
                GetResultFileMetadata(TestFilesDir.GetTestPath("2021_01_20_coraleggs_10B_23.raw"));
            var metadata2 =
                GetResultFileMetadata(TestFilesDir.GetTestPath("2021_01_20_coraleggs_10NB_13.raw"));

            var similarityMatrix = metadata1.SpectrumSummaries.GetSimilarityMatrix(null, null,
                metadata2.SpectrumSummaries);
            Console.Out.WriteLine(similarityMatrix.Points.Count());
        }
    }
}

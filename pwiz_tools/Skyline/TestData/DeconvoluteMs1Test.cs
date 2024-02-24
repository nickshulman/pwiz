using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using pwiz.Skyline.Model;
using pwiz.Skyline.Model.Results;
using pwiz.Skyline.Properties;
using pwiz.SkylineTestUtil;

namespace pwiz.SkylineTestData
{
    [TestClass]
    public class DeconvoluteMs1Test : AbstractUnitTest
    {
        [TestMethod]
        public void TestDeconvoluteMs1()
        {
            TestFilesDir = new TestFilesDir(TestContext, @"TestData\DeconvoluteMs1Test.zip");
            string docPath = TestFilesDir.GetTestPath("DeconvolutionTest.sky");
            using var documentContainer =
                new ResultsTestDocumentContainer(new SrmDocument(SrmSettingsList.GetDefault()), docPath);
            documentContainer.SetDocument(ResultsUtil.DeserializeDocument(docPath), documentContainer.Document);
            documentContainer.WaitForComplete();
            var doc = documentContainer.Document;
            var chromatogramCaucus = new ChromatogramCaucus(documentContainer.Document, 0,
                doc.Settings.MeasuredResults.Chromatograms[0].MSDataFilePaths.First());
            var peptideIdentityPath = doc.GetPathTo((int)SrmDocument.Level.Molecules, 0);
            var peptideDocNode = (PeptideDocNode) doc.FindNode(peptideIdentityPath);
            foreach (var transitionGroup in peptideDocNode.TransitionGroups)
            {
                Assert.IsTrue(chromatogramCaucus.AddPrecursor(new IdentityPath(peptideIdentityPath, transitionGroup.TransitionGroup)));
            }

            var deconvolutedChromatograms = chromatogramCaucus.GetDeconvolutedChromatograms();
            Assert.IsNotNull(deconvolutedChromatograms);
        }
    }
}

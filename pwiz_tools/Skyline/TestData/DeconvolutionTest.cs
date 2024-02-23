using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using pwiz.Skyline.Model;
using pwiz.Skyline.Model.Results;
using pwiz.SkylineTestUtil;

namespace pwiz.SkylineTestData
{
    [TestClass]
    public class DeconvolutionTest : AbstractUnitTest
    {
        [TestMethod]
        public void TestDeconvolution()
        {
            TestFilesDir = new TestFilesDir(TestContext, @"TestData\DeconvolutionTest.zip");
            string docPath = TestFilesDir.GetTestPath("DeconvolutionTest.sky");
            using var documentContainer = new ResultsTestDocumentContainer(ResultsUtil.DeserializeDocument(docPath), docPath, true);
            var doc = documentContainer.Document;
            var chromatogramCaucus = new ChromatogramCaucus(documentContainer.Document, 0,
                doc.Settings.MeasuredResults.Chromatograms[0].MSDataFilePaths.First());
            var peptideIdentityPath = doc.GetPathTo((int)SrmDocument.Level.Molecules, 0);
            var peptideDocNode = (PeptideDocNode) doc.FindNode(peptideIdentityPath);
            foreach (var transitionGroup in peptideDocNode.TransitionGroups)
            {
                chromatogramCaucus.AddPrecursor(new IdentityPath(peptideIdentityPath, transitionGroup.TransitionGroup));
            }

            var deconvolutedChromatograms = chromatogramCaucus.GetDeconvolutedChromatograms();
            Assert.IsNotNull(deconvolutedChromatograms);
        }
    }
}

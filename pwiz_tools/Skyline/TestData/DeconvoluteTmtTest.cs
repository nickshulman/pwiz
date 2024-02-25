using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Serialization;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using pwiz.Skyline.Model.Results;
using pwiz.Skyline.Model;
using pwiz.Skyline.Model.DocSettings;
using pwiz.Skyline.Properties;
using pwiz.SkylineTestUtil;

namespace pwiz.SkylineTestData
{
    [TestClass]
    public class DeconvoluteTmtTest : AbstractUnitTest
    {
        [TestMethod]
        public void TestDeconvoluteTmt()
        {
            TestFilesDir = new TestFilesDir(TestContext, @"TestData\DeconvoluteTmtTest.zip");
            string docPath = TestFilesDir.GetTestPath("TMT_chromatogram_test.sky");
            using var documentContainer =
                new ResultsTestDocumentContainer(new SrmDocument(SrmSettingsList.GetDefault()), docPath);
            documentContainer.SetDocument(ResultsUtil.DeserializeDocument(docPath), documentContainer.Document);
            documentContainer.WaitForComplete();
            var doc = documentContainer.Document;
            var multiplexMatrix = GetMultiplexMatrix();
            doc = doc.ChangeSettings(doc.Settings.ChangePeptideSettings(
                doc.Settings.PeptideSettings.ChangeAbsoluteQuantification(
                    doc.Settings.PeptideSettings.Quantification.ChangeMultiplexMatrix(multiplexMatrix))));
            var chromatogramCaucus = new ChromatogramCaucus(doc, 0,
                doc.Settings.MeasuredResults.Chromatograms[0].MSDataFilePaths.First());
            var precursorIdentityPath = doc.GetPathTo((int)SrmDocument.Level.TransitionGroups, 0);
            Assert.IsTrue(chromatogramCaucus.AddPrecursor(precursorIdentityPath));


            var multiplexChromatograms = chromatogramCaucus.GetMultiplexedChromatograms((TransitionGroup) precursorIdentityPath.Child);
            Assert.AreEqual(multiplexMatrix.Replicates.Count, multiplexChromatograms.Count);
        }

        private static MultiplexMatrix GetMultiplexMatrix()
        {
            return (MultiplexMatrix)new XmlSerializer(typeof(MultiplexMatrix))
                .Deserialize(new StringReader(strMultiplex));
        }

        private const string strMultiplex = @"<multiplex name='TMT10_Roche_QD212963'>
	<replicate name='126'>
		<ion name='TMT-126' weight='100'/>
		<ion name='TMT-127H' weight='6.9'/>
	</replicate>
	<replicate name='127N'>
		<ion name='TMT-126' weight='0.2'/>
		<ion name='TMT-127L' weight='100'/>
		<ion name='TMT-128L' weight='5.9'/>
	</replicate>
	<replicate name='127C'>
		<ion name='TMT-126' weight='0.6'/>
		<ion name='TMT-127H' weight='100'/>
		<ion name='TMT-128H' weight='6.4'/>
	</replicate>
	<replicate name='128N'>
		<ion name='TMT-127L' weight='0.4'/>
		<ion name='TMT-128L' weight='100'/>
		<ion name='TMT-129L' weight='3.4'/>
	</replicate>
	<replicate name='128C'>
		<ion name='TMT-127H' weight='0.6'/>
		<ion name='TMT-128H' weight='100'/>
		<ion name='TMT-129H' weight='4.2'/>
	</replicate>
	<replicate name='129N'>
		<ion name='TMT-128L' weight='0.7'/>
		<ion name='TMT-129L' weight='100'/>
		<ion name='TMT-130L' weight='3.1'/>
	</replicate>
	<replicate name='129C'>
		<ion name='TMT-128H' weight='1.3'/>
		<ion name='TMT-129H' weight='100'/>
		<ion name='TMT-130H' weight='2.9'/>
	</replicate>
	<replicate name='130N'>
		<ion name='TMT-129L' weight='1.3'/>
		<ion name='TMT-130L' weight='100'/>
		<ion name='TMT-131L' weight='2.8'/>
	</replicate>
	<replicate name='130C'>
		<ion name='TMT-129H' weight='1.6'/>
		<ion name='TMT-130H' weight='100'/>
	</replicate>
	<replicate name='131'>
		<ion name='TMT-129L' weight='0.3'/>
		<ion name='TMT-130L' weight='1.7'/>
		<ion name='TMT-131L' weight='100'/>
	</replicate>
</multiplex>";
    }
}

using Microsoft.VisualStudio.TestTools.UnitTesting;
using pwiz.Skyline.Alerts;
using pwiz.Skyline.Model.DocSettings;
using pwiz.SkylineTestUtil;

namespace pwiz.SkylineTestFunctional
{
    [TestClass]
    public class DemuxObsoleteTest : AbstractFunctionalTest
    {
        [TestMethod]
        public void TestDemuxObsolete()
        {
            TestFilesZip = @"TestFunctional\DemuxObsoleteTest.zip";
            RunFunctionalTest();
        }

        protected override void DoTest()
        {
            var file1Path = TestFilesDir.GetTestPath("MsxTest.sky");
            var messageDlg = ShowDialog<AlertDlg>(()=>SkylineWindow.OpenFile(file1Path));
            OkDialog(messageDlg, messageDlg.ClickOk);
            WaitForDocumentLoaded();
            Assert.AreEqual(IsolationScheme.SpecialHandlingType.NONE, SkylineWindow.Document.Settings.TransitionSettings.FullScan.IsolationScheme.SpecialHandling);
            Assert.AreEqual(file1Path, SkylineWindow.DocumentFilePath);
            var file1Doc = SkylineWindow.Document;
            var file2Path = TestFilesDir.GetTestPath("OverlapTest.sky");

            ShowDialog<AlertDlg>(() => SkylineWindow.OpenFile(file2Path));
            OkDialog(messageDlg, messageDlg.ClickCancel);
            Assert.AreEqual(file1Path, SkylineWindow.DocumentFilePath);
            Assert.AreEqual(file1Doc, SkylineWindow.Document);

            ShowDialog<AlertDlg>(() => SkylineWindow.OpenFile(file2Path));
            OkDialog(messageDlg, messageDlg.ClickOk);
            Assert.AreEqual(file2Path, SkylineWindow.DocumentFilePath);
            Assert.AreNotEqual(file1Doc, SkylineWindow.Document);
        }
    }
}

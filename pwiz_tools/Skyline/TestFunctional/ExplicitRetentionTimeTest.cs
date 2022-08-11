using System.Collections.Generic;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using pwiz.Common.DataBinding;
using pwiz.Common.DataBinding.Controls.Editor;
using pwiz.Skyline.Controls.Databinding;
using pwiz.Skyline.Model.Databinding.Entities;
using pwiz.Skyline.Model.DocSettings;
using pwiz.Skyline.Model.Lib;
using pwiz.Skyline.SettingsUI;
using pwiz.Skyline.Util.Extensions;
using pwiz.SkylineTestUtil;

namespace pwiz.SkylineTestFunctional
{
    [TestClass]
    public class ExplicitRetentionTimeTest : AbstractFunctionalTest
    {
        [TestMethod]
        public void TestExplicitRetentionTime()
        {
            TestFilesZip = @"TestFunctional\ExplicitRetentionTimeTest.zip";
            RunFunctionalTest();
        }

        protected override void DoTest()
        {
            RunUI(()=>
            {
                SkylineWindow.OpenFile(TestFilesDir.GetTestPath(@"ExplicitRetentionTime.sky"));
            });
            Assert.AreEqual(RetentionTimeFilterType.scheduling_windows, SkylineWindow.Document.Settings.TransitionSettings.FullScan.RetentionTimeFilterType);
            Assert.AreEqual(5.0, SkylineWindow.Document.Settings.TransitionSettings.FullScan.RetentionTimeFilterLength);
            SetExplicitRetentionTimes();
            ImportResultsFile(TestFilesDir.GetTestPath("S_1.mzML"));
            PauseTest();
            var chromatogramTimeRanges = GetChromatogramTimeRanges();
            VerifyTimeRange("TNFDAFPDK", 2, 5, 35, chromatogramTimeRanges);
            VerifyTimeRange("VIFLENYR", 2, 35, 45, chromatogramTimeRanges);
            VerifyTimeRange("QIIEQLSSGFFSPK", 3, 15, 25, chromatogramTimeRanges);
            VerifyTimeRange("QIIEQLSSGFFSPK", 2, 45, 55, chromatogramTimeRanges);
        }

        private void SetExplicitRetentionTimes()
        {
            RunUI(()=>SkylineWindow.ShowDocumentGrid(true));
            var documentGridForm = FindOpenForm<DocumentGridForm>();
            RunDlg<ViewEditor>(documentGridForm.NavBar.CustomizeView, viewEditor =>
            {
                viewEditor.ViewName = "Explicit Retention Times";
                viewEditor.ChooseColumnsTab.RemoveColumns(0, viewEditor.ChooseColumnsTab.ColumnCount);
                var ppPeptides = PropertyPath.Root.Property(nameof(SkylineDocument.Proteins)).LookupAllItems()
                    .Property(nameof(Protein.Peptides)).LookupAllItems();
                var ppPrecursors = ppPeptides.Property(nameof(Peptide.Precursors)).LookupAllItems();
                viewEditor.ChooseColumnsTab.AddColumn(ppPeptides.Property(nameof(Peptide.Sequence)));
                viewEditor.ChooseColumnsTab.AddColumn(ppPeptides.Property(nameof(Peptide.ExplicitRetentionTime)));
                viewEditor.ChooseColumnsTab.AddColumn(ppPrecursors.Property(nameof(Precursor.Charge)));
                viewEditor.OkDialog();
            });
            WaitForConditionUI(() => documentGridForm.IsComplete);
            RunUI(() =>
            {
                Assert.AreEqual(6, documentGridForm.RowCount);
                Assert.AreEqual(3, documentGridForm.ColumnCount);
                var explicitRetentionTimes = new double?[] { 10, null, 20, 30, 40, 50 };
                SetClipboardText(TextUtil.LineSeparate(explicitRetentionTimes.Select(t=>t.ToString())));
                documentGridForm.DataGridView.CurrentCell = documentGridForm.DataGridView.Rows[0].Cells[1];
                documentGridForm.DataGridView.SendPaste();
            });
            var protein1 = SkylineWindow.Document.MoleculeGroups.First();
            var protein2 = SkylineWindow.Document.MoleculeGroups.Skip(1).First();
            Assert.AreEqual(protein1.Children.Count, protein2.Children.Count);

            var tnf1 = protein1.Molecules.First();
            Assert.AreEqual("TNFDAFPDK", tnf1.ModifiedSequence);
            Assert.AreEqual(10, tnf1.ExplicitRetentionTime.RetentionTime);
            var tnf2 = protein2.Molecules.First();
            Assert.AreEqual(tnf1.ModifiedSequence, tnf2.ModifiedSequence);
            Assert.AreEqual(30, tnf2.ExplicitRetentionTime.RetentionTime);

            var vif1 = protein1.Molecules.Skip(1).First();
            Assert.AreEqual("VIFLENYR", vif1.ModifiedSequence);
            Assert.IsNull(vif1.ExplicitRetentionTime);
            var vif2 = protein2.Molecules.Skip(1).First();
            Assert.AreEqual(vif1.ModifiedSequence, vif2.ModifiedSequence);
            Assert.AreEqual(40, vif2.ExplicitRetentionTime.RetentionTime);

            var qii1 = protein1.Molecules.Skip(2).First();
            Assert.AreEqual("QIIEQLSSGFFSPK", qii1.ModifiedSequence);
            Assert.AreEqual(20, qii1.ExplicitRetentionTime.RetentionTime);
            Assert.AreEqual(1, qii1.TransitionGroupCount);
            Assert.AreEqual(3, qii1.TransitionGroups.First().PrecursorCharge);

            var qii2 = protein2.Molecules.Skip(2).First();
            Assert.AreEqual(qii1.ModifiedSequence, qii2.ModifiedSequence);
            Assert.AreEqual(50, qii2.ExplicitRetentionTime.RetentionTime);
            Assert.AreEqual(1, qii2.TransitionGroupCount);
            Assert.AreEqual(2, qii2.TransitionGroups.First().PrecursorCharge);
        }

        Dictionary<PeptideLibraryKey, (float StartTime, float EndTime)> GetChromatogramTimeRanges()
        {
            var document = SkylineWindow.Document;
            var result = new Dictionary<PeptideLibraryKey, (float StartTime, float EndTime)>();
            foreach (var peptideDocNode in document.Molecules)
            {
                foreach (var precursor in peptideDocNode.TransitionGroups)
                {
                    AssertEx.IsTrue(document.Settings.MeasuredResults.TryLoadChromatogram(0, peptideDocNode, precursor,
                        .1f, out var chromatogramInfos));
                    Assert.AreEqual(1, chromatogramInfos.Length);
                    var chromatogramInfo = chromatogramInfos[0];
                    var key = (PeptideLibraryKey) precursor.GetLibKey(document.Settings, peptideDocNode).LibraryKey;
                    (float StartTime, float EndTime) range = (chromatogramInfo.TimeIntensitiesGroup.MinTime,
                        chromatogramInfo.TimeIntensitiesGroup.MaxTime);
                    if (result.TryGetValue(key, out var existing))
                    {
                        Assert.AreEqual(range, existing);
                    }
                    else
                    {
                        result.Add(key, range);
                    }
                }
            }

            return result;
        }

        private void VerifyTimeRange(string peptideSequence, int charge, double expectedStart, double expectedEnd,
            IDictionary<PeptideLibraryKey, (float StartTime, float EndTime)> timeRanges)
        {
            var key = new PeptideLibraryKey(peptideSequence, charge);
            Assert.IsTrue(timeRanges.TryGetValue(key, out var range));
            Assert.AreEqual(expectedStart, range.StartTime, .5);
            Assert.AreEqual(expectedEnd, range.EndTime, .5);
        }
    }
}

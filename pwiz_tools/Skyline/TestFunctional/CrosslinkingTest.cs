﻿using Microsoft.VisualStudio.TestTools.UnitTesting;
using pwiz.Skyline.EditUI;
using pwiz.Skyline.Model;
using pwiz.Skyline.Model.Crosslinking;
using pwiz.Skyline.Model.DocSettings;
using pwiz.Skyline.SettingsUI;
using pwiz.SkylineTestUtil;

namespace pwiz.SkylineTestFunctional
{
    [TestClass]
    public class CrosslinkingTest : AbstractFunctionalTest
    {
        [TestMethod]
        public void TestCrosslinking()
        {
            TestFilesZip = @"TestFunctional\CrosslinkingTest.zip";
            RunFunctionalTest();
        }

        protected override void DoTest()
        {
            RunUI(()=>SkylineWindow.OpenFile(TestFilesDir.GetTestPath("CrosslinkingTest.sky")));
            const string crosslinkerName = "DSS";
            var peptideSettingsUi = ShowDialog<PeptideSettingsUI>(SkylineWindow.ShowPeptideSettingsUI);
            RunUI(()=>
            {
                peptideSettingsUi.SelectedTab = PeptideSettingsUI.TABS.Digest;
                peptideSettingsUi.MaxMissedCleavages = 2;
                peptideSettingsUi.SelectedTab = PeptideSettingsUI.TABS.Modifications;
            });
            var editModListDlg = ShowEditStaticModsDlg(peptideSettingsUi);
            var editStaticModDlg = ShowDialog<EditStaticModDlg>(editModListDlg.AddItem);
            RunUI(()=> { editStaticModDlg.Modification = new StaticMod(crosslinkerName, "K", null, "C8H12O3"); });
            var editCrosslinkerDlg = ShowDialog<EditCrosslinkerDlg>(editStaticModDlg.ShowEditCrosslinkerDlg);
            RunUI(() =>
            {
                editCrosslinkerDlg.IsCrosslinker = true;
                editCrosslinkerDlg.Formula = "C8H10O2";
            });
            OkDialog(editCrosslinkerDlg, editCrosslinkerDlg.OkDialog);
            OkDialog(editStaticModDlg, editStaticModDlg.OkDialog);
            OkDialog(editModListDlg, editModListDlg.OkDialog);
            OkDialog(peptideSettingsUi, peptideSettingsUi.OkDialog);
            var transitionSettingsUi = ShowDialog<TransitionSettingsUI>(SkylineWindow.ShowTransitionSettingsUI);
            RunUI(()=>
            {
                transitionSettingsUi.SelectedTab = TransitionSettingsUI.TABS.Filter;
                transitionSettingsUi.PrecursorCharges = "4,3,2";
                transitionSettingsUi.ProductCharges = "4,3,2,1";
                transitionSettingsUi.FragmentTypes = "y";
            });
            
            OkDialog(transitionSettingsUi, transitionSettingsUi.OkDialog);
            RunUI(()=>
            {
                SkylineWindow.Paste("DDSPDLPKLKPDPNTLCDEFK\r\nSLGKVGTR");
                SkylineWindow.SelectedPath = SkylineWindow.Document.GetPathTo(1, 0);
            });

            var modifyPeptideDlg = ShowDialog<EditPepModsDlg>(SkylineWindow.ModifyPeptide);
            RunUI(() =>
            {
                modifyPeptideDlg.SelectModification(IsotopeLabelType.light, 9, crosslinkerName);
            });
            var editCrosslinkModDlg = ShowDialog<EditLinkedPeptideDlg>(() => modifyPeptideDlg.EditLinkedPeptide(9));
            RunUI(() =>
            {
                editCrosslinkModDlg.PeptideSequence = "SLGKVGTR";
                editCrosslinkModDlg.AttachmentOrdinal = 4;
            });
            OkDialog(editCrosslinkModDlg, editCrosslinkModDlg.OkDialog);
            OkDialog(modifyPeptideDlg, modifyPeptideDlg.OkDialog);
        }
    }
}

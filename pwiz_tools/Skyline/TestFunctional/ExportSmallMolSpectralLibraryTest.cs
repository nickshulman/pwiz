﻿using System;
using System.Collections.Generic;
using System.Data.SQLite;
using System.IO;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using pwiz.Skyline.Alerts;
using pwiz.Skyline.FileUI;
using pwiz.Skyline.Model;
using pwiz.Skyline.Model.Lib;
using pwiz.Skyline.Model.Lib.BlibData;
using pwiz.Skyline.Properties;
using pwiz.Skyline.Util;
using pwiz.SkylineTestUtil;

namespace pwiz.SkylineTestFunctional
{
    [TestClass]
    public class ExportSmallMolSpectralLibraryTest : AbstractFunctionalTest
    {
        [TestMethod]
        public void TestExportSmallMolSpectralLibrary()
        {
            TestFilesZip = @"TestFunctional\ExportSpectralLibraryTest.zip";
            RunFunctionalTest();
        }

        protected override void DoTest()
        {
            // Export and check spectral library
            RunUI(() => SkylineWindow.OpenFile(TestFilesDir.GetTestPath("msstatstest.sky")));
            var docOrig = WaitForDocumentLoaded();
            var refine = new RefinementSettings();
            var doc = refine.ConvertToSmallMolecules(docOrig, TestFilesDirs[0].FullPath); 
            SkylineWindow.SetDocument(doc, docOrig);
            var exported = TestFilesDir.GetTestPath("exportSM.blib");
            var libraryExporter = new SpectralLibraryExporter(SkylineWindow.Document, SkylineWindow.DocumentFilePath);
            libraryExporter.ExportSpectralLibrary(exported, null);
            Assert.IsTrue(File.Exists(exported));

            var refSpectra = new List<DbRefSpectra>();
            using (var connection = new SQLiteConnection(string.Format("Data Source='{0}';Version=3", exported)))
            {
                connection.Open();
                using (var select = new SQLiteCommand(connection)
                {
                    CommandText = "SELECT * FROM RefSpectra"
                })
                using (var reader = select.ExecuteReader())
                {
                    var iAdduct = reader.GetOrdinal("precursorAdduct");
                    while (reader.Read())
                    {
                        refSpectra.Add(new DbRefSpectra
                        {
                            PeptideSeq = reader["peptideSeq"].ToString(),
                            PeptideModSeq = reader["peptideModSeq"].ToString(),
                            PrecursorCharge = int.Parse(reader["precursorCharge"].ToString()),
                            PrecursorAdduct = reader[iAdduct].ToString(),
                            MoleculeName = reader["moleculeName"].ToString(),
                            ChemicalFormula = reader["chemicalFormula"].ToString(),
                            PrecursorMZ = double.Parse(reader["precursorMZ"].ToString()),
                            NumPeaks = ushort.Parse(reader["numPeaks"].ToString())
                        });
                    }
                }
            }
            CheckRefSpectra(refSpectra, "APVPTGEVYFADSFDR", "C81H115N19O26", "[M+2H]", 885.9203087025, 4);
            CheckRefSpectra(refSpectra, "APVPTGEVYFADSFDR", "C81H115N19O26", "[M6C134N15+2H]", 890.9244430127, 4);
            CheckRefSpectra(refSpectra, "AVTELNEPLSNEDR", "C65H107N19O27", "[M+2H]", 793.8864658775, 4);
            CheckRefSpectra(refSpectra, "AVTELNEPLSNEDR", "C65H107N19O27", "[M6C134N15+2H]", 798.8906001877, 4);
            CheckRefSpectra(refSpectra, "DQGGELLSLR", "C45H78N14O17", "[M+2H]", 544.29074472, 4);
            CheckRefSpectra(refSpectra, "DQGGELLSLR", "C45H78N14O17", "[M6C134N15+2H]", 549.2948790302, 4);
            CheckRefSpectra(refSpectra, "ELLTTMGDR", "C42H74N12O16S", "[M+2H]", 518.260598685, 4);
            CheckRefSpectra(refSpectra, "ELLTTMGDR", "C42H74N12O16S", "[M6C134N15+2H]", 523.2647329952, 4);
            CheckRefSpectra(refSpectra, "FEELNADLFR", "C57H84N14O18", "[M+2H]", 627.31167714, 3);
            CheckRefSpectra(refSpectra, "FEELNADLFR", "C57H84N14O18", "[M6C134N15+2H]", 632.3158114502, 4);
            CheckRefSpectra(refSpectra, "FHQLDIDDLQSIR", "C70H110N20O23", "[M+2H]", 800.40991117, 3);
            CheckRefSpectra(refSpectra, "FHQLDIDDLQSIR", "C70H110N20O23", "[M6C134N15+2H]", 805.4140454802, 4);
            CheckRefSpectra(refSpectra, "FLIPNASQAESK", "C58H93N15O19", "[M+2H]", 652.8458841125, 4);
            CheckRefSpectra(refSpectra, "FLIPNASQAESK", "C58H93N15O19", "[M6C132N15+2H]", 656.8529835243, 4);
            CheckRefSpectra(refSpectra, "FTPGTFTNQIQAAFR", "C78H115N21O22", "[M+2H]", 849.9335534425, 4);
            CheckRefSpectra(refSpectra, "FTPGTFTNQIQAAFR", "C78H115N21O22", "[M6C134N15+2H]", 854.9376877527, 4);
            CheckRefSpectra(refSpectra, "ILTFDQLALDSPK", "C67H109N15O21", "[M+2H]", 730.9033990225, 4);
            CheckRefSpectra(refSpectra, "ILTFDQLALDSPK", "C67H109N15O21", "[M6C132N15+2H]", 734.9104984343, 4);
            CheckRefSpectra(refSpectra, "LSSEMNTSTVNSAR", "C58H101N19O25S", "[M+2H]", 748.8541114925, 4);
            CheckRefSpectra(refSpectra, "LSSEMNTSTVNSAR", "C58H101N19O25S", "[M6C134N15+2H]", 753.8582458027, 4);
            CheckRefSpectra(refSpectra, "NIVEAAAVR", "C40H71N13O13", "[M+2H]", 471.7719908375, 4);
            CheckRefSpectra(refSpectra, "NIVEAAAVR", "C40H71N13O13", "[M6C134N15+2H]", 476.7761251477, 4);
            CheckRefSpectra(refSpectra, "NLQYYDISAK", "C55H83N13O18", "[M+2H]", 607.8062276225, 4);
            CheckRefSpectra(refSpectra, "NLQYYDISAK", "C55H83N13O18", "[M6C132N15+2H]", 611.8133270343, 4);
            CheckRefSpectra(refSpectra, "TSAALSTVGSAISR", "C54H97N17O21", "[M+2H]", 660.8595228125, 4);
            CheckRefSpectra(refSpectra, "TSAALSTVGSAISR", "C54H97N17O21", "[M6C134N15+2H]", 665.8636571227, 4);
            CheckRefSpectra(refSpectra, "VHIEIGPDGR", "C47H77N15O15", "[M+2H]", 546.7934545725, 4);
            CheckRefSpectra(refSpectra, "VHIEIGPDGR", "C47H77N15O15", "[M6C134N15+2H]", 551.7975888827, 4);
            CheckRefSpectra(refSpectra, "VLTPELYAELR", "C60H98N14O18", "[M+2H]", 652.366452385, 4);
            CheckRefSpectra(refSpectra, "VLTPELYAELR", "C60H98N14O18", "[M6C134N15+2H]", 657.3705866952, 4);
            CheckRefSpectra(refSpectra, "VNLAELFK", "C44H72N10O12", "[M+2H]", 467.27383504, 4);
            CheckRefSpectra(refSpectra, "VNLAELFK", "C44H72N10O12", "[M6C132N15+2H]", 471.2809344518, 4);
            CheckRefSpectra(refSpectra, "VPDFSEYR", "C46H65N11O15", "[M+2H]", 506.7403563625, 4);
            CheckRefSpectra(refSpectra, "VPDFSEYR", "C46H65N11O15", "[M6C134N15+2H]", 511.7444906727, 4);
            CheckRefSpectra(refSpectra, "VPDGMVGFIIGR", "C57H93N15O15S", "[M+2H]", 630.8420902025, 4);
            CheckRefSpectra(refSpectra, "VPDGMVGFIIGR", "C57H93N15O15S", "[M6C134N15+2H]", 635.8462245127, 4);
            CheckRefSpectra(refSpectra, "ADVTPADFSEWSK", "C65H93N15O23", "[M+2H]", 726.8357133725, 3);
            CheckRefSpectra(refSpectra, "DGLDAASYYAPVR", "C62H92N16O21", "[M+2H]", 699.338423225, 3);
            CheckRefSpectra(refSpectra, "GAGSSEPVTGLDAK", "C53H89N15O22", "[M+2H]", 644.8226059875, 3);
            CheckRefSpectra(refSpectra, "GTFIIDPAAVIR", "C59H97N15O16", "[M+2H]", 636.8691622375, 3);
            CheckRefSpectra(refSpectra, "GTFIIDPGGVIR", "C57H93N15O16", "[M+2H]", 622.8535121675, 3);
            CheckRefSpectra(refSpectra, "LFLQFGAQGSPFLK", "C76H113N17O18", "[M+2H]", 776.9297511475, 3);
            CheckRefSpectra(refSpectra, "LGGNEQVTR", "C39H68N14O15", "[M+2H]", 487.256704915, 3);
            CheckRefSpectra(refSpectra, "TPVISGGPYEYR", "C61H91N15O19", "[M+2H]", 669.8380590775, 3);
            CheckRefSpectra(refSpectra, "TPVITGAPYEYR", "C63H95N15O19", "[M+2H]", 683.8537091475, 3);
            CheckRefSpectra(refSpectra, "VEATFGVDESNAK", "C58H91N15O23", "[M+2H]", 683.8278883375, 3);
            CheckRefSpectra(refSpectra, "YILAGVENSK", "C49H80N12O16", "[M+2H]", 547.29803844, 3);
            Assert.IsTrue(!refSpectra.Any());

            // Try to export spectral library with no results
            var manageResultsDlg = ShowDialog<ManageResultsDlg>(SkylineWindow.ManageResults);
            RunUI(() => manageResultsDlg.RemoveAllReplicates());
            OkDialog(manageResultsDlg, manageResultsDlg.OkDialog);
            WaitForDocumentChangeLoaded(doc);
            var errDlg1 = ShowDialog<MessageDlg>(SkylineWindow.ShowExportSpectralLibraryDialog);
            Assert.AreEqual(Resources.SkylineWindow_ShowExportSpectralLibraryDialog_The_document_must_contain_results_to_export_a_spectral_library_, errDlg1.Message);
            OkDialog(errDlg1, errDlg1.OkDialog);
            RunUI(() => SkylineWindow.Undo());

            // Try to export spectral library with no precursors
            RunUI(() => SkylineWindow.NewDocument());
            var errDlg2 = ShowDialog<MessageDlg>(SkylineWindow.ShowExportSpectralLibraryDialog);
            Assert.AreEqual(Resources.SkylineWindow_ShowExportSpectralLibraryDialog_The_document_must_contain_at_least_one_peptide_precursor_to_export_a_spectral_library_, errDlg2.Message);
            OkDialog(errDlg2, errDlg2.OkDialog);
        }

        private static void CheckRefSpectra(IList<DbRefSpectra> spectra, string name, string formula, string precursorAdduct, double precursorMz, ushort numPeaks)
        {
            name = RefinementSettings.TestingConvertedFromProteomicPeptideNameDecorator + name;
            for (var i = 0; i < spectra.Count; i++)
            {
                var spectrum = spectra[i];
                if (spectrum.MoleculeName.Equals(name) &&
                    spectrum.ChemicalFormula.Equals(formula) &&
                    spectrum.PrecursorCharge.Equals(Adduct.FromStringAssumeProtonated(precursorAdduct).AdductCharge) &&
                    spectrum.PrecursorAdduct.Equals(precursorAdduct) &&
                    Math.Abs(spectrum.PrecursorMZ - precursorMz) < 0.001 &&
                    spectrum.NumPeaks.Equals(numPeaks))
                {
                    spectra.RemoveAt(i);
                    return;
                }
            }
            Assume.Fail(string.Format("{0}, {1}, precursor charge {2}, precursor m/z {3}, with {4} peaks not found", name, formula, precursorAdduct, precursorMz, numPeaks));
        }
    }
}

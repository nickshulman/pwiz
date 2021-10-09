/*
 * Original author: Brian Pratt <bspratt .at. u.washington.edu>,
 *                  MacCoss Lab, Department of Genome Sciences, UW
 *
 * Copyright 2021 University of Washington - Seattle, WA
 * 
 * Licensed under the Apache License, Version 2.0 (the "License");
 * you may not use this file except in compliance with the License.
 * You may obtain a copy of the License at
 *
 *     http://www.apache.org/licenses/LICENSE-2.0
 *
 * Unless required by applicable law or agreed to in writing, software
 * distributed under the License is distributed on an "AS IS" BASIS,
 * WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
 * See the License for the specific language governing permissions and
 * limitations under the License.
 */


using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using pwiz.Skyline.FileUI;
using pwiz.Skyline.Model;
using pwiz.Skyline.Model.DocSettings;
using pwiz.Skyline.Model.IonMobility;
using pwiz.Skyline.Model.Results;
using pwiz.Skyline.SettingsUI;
using pwiz.Skyline.SettingsUI.IonMobility;
using pwiz.SkylineTestUtil;

namespace TestPerf 
{
    /// <summary>
    /// Verify measured drift time operation for a MobilIon .mbi file and an Agilent SLIM file (not the same data set, but two ways to represent SLIM data)
    /// </summary>
    [TestClass]
    public class PerfMobilIonTest : AbstractFunctionalTestEx
    {

        [TestMethod]
        public void TestMobilIon()  
        {
            // RunPerfTests = true; // Enables perftests to run from the IDE (you don't want to commit this line without commenting it out)

            TestFilesZipPaths = new[]
            {
                GetPerfTestDataURL(@"PerfMobilIonTest.zip"),
            };

            TestFilesPersistent = new[]
            {
                "2020-12-28-18-21-56-20201228_PeptideMap-NISTmAbOxidized.mbi", // Mobilion HD5 format
                "2021-03-18-21-07-12-IM_C_DC5_MA-d3-c3-Min20-Spk.d" // Agilent SLIM format
            };

            RunFunctionalTest();
        }

        private string DataPath { get { return TestFilesDirs.Last().PersistentFilesDir; } }

        protected override void DoTest()
        {
/* Waiting for CCS<->DT support in .mbi reader
            Test(@"PerfMobilIonTest.sky", 2, 163, 163, 1006, ".mbi", null, 95.07976, 6028.9375, true); // Mobilion HD5 format
*/            
            Test(@"slim.sky", 12, 12, 30, 30, ".d", 258.3, 308.71, 4450.05859, false); // Agilent SLIM format
        }

        void Test(string docFile, int groups, int peptides, int tranGroups, int transitions, string dataExt, double? ccs, double drift, double areaExpected,  bool findDriftPeaks)
        {

            // Empty doc with suitable full scan settings
            RunUI(() =>
            {
                SkylineWindow.NewDocument(true);
                SkylineWindow.OpenFile(TestFilesDirs[0].GetTestPath(docFile));
            });

            var document = WaitForDocumentLoaded();
            AssertEx.IsDocumentState(document, null, groups, peptides, tranGroups, transitions);

            // Importing raw data
            var importResults = ShowDialog<ImportResultsDlg>(SkylineWindow.ImportResults);
            RunUI(() => importResults.ImportSimultaneousIndex = 2);
            var openDataSourceDialog = ShowDialog<OpenDataSourceDialog>(importResults.OkDialog);
            RunUI(() =>
            {
                openDataSourceDialog.CurrentDirectory = new MsDataFilePath(DataPath);
                openDataSourceDialog.SelectAllFileType(dataExt);
            });

            OkDialog(openDataSourceDialog, openDataSourceDialog.Open);
            document = WaitForDocumentLoaded();

            var area = document.MoleculePrecursorPairs.First().NodeGroup.Results.First().First().AreaMs1;
            AssertEx.IsTrue(area > 0);

            if (findDriftPeaks)
            {
                // Locate drift peaks
                var transitionSettingsUI = ShowDialog<TransitionSettingsUI>(SkylineWindow.ShowTransitionSettingsUI);
                RunUI(() => transitionSettingsUI.SelectedTab = TransitionSettingsUI.TABS.IonMobility);
                RunUI(() => transitionSettingsUI.IonMobilityControl.WindowWidthType = IonMobilityWindowWidthCalculator.IonMobilityWindowWidthType.resolving_power);
                RunUI(() => transitionSettingsUI.IonMobilityControl.IonMobilityFilterResolvingPower = 50);
                var editIonMobilityLibraryDlg = ShowDialog<EditIonMobilityLibraryDlg>(transitionSettingsUI.IonMobilityControl.AddIonMobilityLibrary);
                var libName = docFile.Replace(SrmDocument.EXT, IonMobilityDb.EXT);
                var databasePath = TestFilesDir.GetTestPath(libName);
                RunUI(() =>
                {
                    editIonMobilityLibraryDlg.LibraryName = libName;
                    editIonMobilityLibraryDlg.CreateDatabaseFile(databasePath); // Simulate user click on Create button
                    editIonMobilityLibraryDlg.GetIonMobilitiesFromResults();
                });
                OkDialog(editIonMobilityLibraryDlg, () => editIonMobilityLibraryDlg.OkDialog());

                RunUI(() =>
                {
                    AssertEx.AreEqual(libName, transitionSettingsUI.IonMobilityControl.SelectedIonMobilityLibrary);
                });
                OkDialog(transitionSettingsUI, transitionSettingsUI.OkDialog);

                WaitForDocumentChangeLoaded(document);
                var docFiltered = SkylineWindow.Document;

                RunDlg<ManageResultsDlg>(SkylineWindow.ManageResults, manageDlg =>
                {
                    manageDlg.SelectedChromatograms = SkylineWindow.Document.Settings.MeasuredResults.Chromatograms.Take(1);
                    manageDlg.ReimportResults();
                    manageDlg.OkDialog();
                });
                docFiltered = WaitForDocumentChangeLoaded(docFiltered);

                // If drift filtering was engaged, peak area should be less
                var transitionGroupChromInfo = docFiltered.MoleculePrecursorPairs.First().NodeGroup.Results.First().First();
                var areaFiltered = transitionGroupChromInfo.AreaMs1;
                AssertEx.IsTrue(area > areaFiltered);
                AssertEx.IsTrue(areaFiltered > 0);
            }
            document = WaitForDocumentLoaded();
            var chromInfo = document.MoleculePrecursorPairs.First().NodeGroup.Results.First().First();
            AssertEx.AreEqual(ccs, chromInfo.IonMobilityInfo.CollisionalCrossSection, .01);
            AssertEx.AreEqual(drift, chromInfo.IonMobilityInfo.DriftTimeMS1, .01);
            AssertEx.AreEqual(areaExpected, (double) chromInfo.AreaMs1, .01);
        }
    }
}

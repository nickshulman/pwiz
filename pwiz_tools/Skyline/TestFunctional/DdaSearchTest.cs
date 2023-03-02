﻿/*
 * Original author: Matt Chambers <matt.chambers42 .at. gmail.com >
 *
 * Copyright 2020 University of Washington - Seattle, WA
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

using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using pwiz.Common.Chemistry;
using pwiz.Skyline.Alerts;
using pwiz.Skyline.Controls;
using pwiz.Skyline.EditUI;
using pwiz.Skyline.FileUI;
using pwiz.Skyline.FileUI.PeptideSearch;
using pwiz.Skyline.Model.DdaSearch;
using pwiz.Skyline.Model.DocSettings;
using pwiz.Skyline.Model.Results;
using pwiz.Skyline.Properties;
using pwiz.Skyline.SettingsUI;
using pwiz.Skyline.Util;
using pwiz.Skyline.Util.Extensions;
using pwiz.SkylineTestUtil;

namespace pwiz.SkylineTestFunctional
{
    [TestClass]
    public class DdaSearchTest : AbstractFunctionalTest
    {
        public struct ExpectedResults
        {
            public int proteinCount;
            public int peptideCount;
            public int precursorCount;
            public int transitionCount;

            public ExpectedResults(int proteinCount, int peptideCount, int precursorCount, int transitionCount)
            {
                this.proteinCount = proteinCount;
                this.peptideCount = peptideCount;
                this.precursorCount = precursorCount;
                this.transitionCount = transitionCount;
            }
        }

        public struct DdaTestSettings
        {
            private SearchSettingsControl.SearchEngine _searchEngine;
            public SearchSettingsControl.SearchEngine SearchEngine
            {
                get => _searchEngine;
                set
                {
                    _searchEngine = value;
                    HasMissingDependencies = !SearchSettingsControl.HasRequiredFilesDownloaded(value);
                }
            }

            public string FragmentIons { get; set; }
            public string Ms2Analyzer { get; set; }
            public MzTolerance PrecursorTolerance { get; set; }
            public MzTolerance FragmentTolerance { get; set; }
            public List<KeyValuePair<string, string>> AdditionalSettings { get; set; }
            public ExpectedResults ExpectedResults { get; set; }
            public ExpectedResults ExpectedResultsFinal { get; set; }
            public bool HasMissingDependencies { get; private set; }
        }

        DdaTestSettings TestSettings;

        [TestMethod, NoParallelTesting(TestExclusionReason.RESOURCE_INTENSIVE)]
        public void TestDdaSearch()
        {
            TestFilesZip = @"TestFunctional\DdaSearchTest.zip";

            // Test that correct error is issued when MSAmanda tries to parse a missing file (enzymes.xml)
            // Automating this test turned out to be more difficult than I thought and not worth the effort.
            //var skylineDir = System.IO.Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location);
            //File.Move(System.IO.Path.Combine(skylineDir, "enzymes.xml"), System.IO.Path.Combine(skylineDir, "not-the-enzymes-you-are-looking-for.xml"));

            TestSettings = new DdaTestSettings
            {
                SearchEngine = SearchSettingsControl.SearchEngine.MSAmanda,
                FragmentIons = "b, y",
                Ms2Analyzer = "Default",
                PrecursorTolerance = new MzTolerance(15, MzTolerance.Units.ppm),
                FragmentTolerance = new MzTolerance(25, MzTolerance.Units.ppm),
                AdditionalSettings = new List<KeyValuePair<string, string>>(),
                ExpectedResults = new ExpectedResults(1131, 110, 110, 330),
                ExpectedResultsFinal = new ExpectedResults(100, 111, 111, 333)
            };

            RunFunctionalTest();
        }

        [TestMethod, NoParallelTesting(TestExclusionReason.RESOURCE_INTENSIVE)]
        public void TestDdaSearchMsgfPlus()
        {
            TestFilesZip = @"TestFunctional\DdaSearchTest.zip";

            if (RedownloadTools)
                foreach (var requiredFile in MsgfPlusSearchEngine.FilesToDownload)
                    if (requiredFile.Unzip)
                        DirectoryEx.SafeDelete(requiredFile.InstallPath);
                    else
                        FileEx.SafeDelete(Path.Combine(requiredFile.InstallPath, requiredFile.Filename));

            TestSettings = new DdaTestSettings
            {
                SearchEngine = SearchSettingsControl.SearchEngine.MSGFPlus,
                FragmentIons = "CID",
                Ms2Analyzer = "Orbitrap/FTICR/Lumos",
                PrecursorTolerance = new MzTolerance(15, MzTolerance.Units.ppm),
                FragmentTolerance = new MzTolerance(25, MzTolerance.Units.ppm),
                AdditionalSettings = new List<KeyValuePair<string, string>>(),
                ExpectedResults = new ExpectedResults(1131, 79, 79, 237),
                ExpectedResultsFinal = new ExpectedResults(73, 80, 80, 240)
            };

            RunFunctionalTest();
        }

        [TestMethod, NoParallelTesting(TestExclusionReason.RESOURCE_INTENSIVE)]
        public void TestDdaSearchMsFragger()
        {
            TestFilesZip = @"TestFunctional\DdaSearchTest.zip";

            if (RedownloadTools)
                foreach (var requiredFile in MsFraggerSearchEngine.FilesToDownload)
                    if (requiredFile.Unzip)
                        DirectoryEx.SafeDelete(requiredFile.InstallPath);
                    else
                        FileEx.SafeDelete(Path.Combine(requiredFile.InstallPath, requiredFile.Filename));

            TestSettings = new DdaTestSettings
            {
                SearchEngine = SearchSettingsControl.SearchEngine.MSFragger,
                FragmentIons = "b,y",
                Ms2Analyzer = "Default",
                PrecursorTolerance = new MzTolerance(50, MzTolerance.Units.ppm),
                FragmentTolerance = new MzTolerance(50, MzTolerance.Units.ppm),
                AdditionalSettings = new List<KeyValuePair<string, string>>
                {
                    new KeyValuePair<string, string>("check_spectral_files", "0"),
                    new KeyValuePair<string, string>("calibrate_mass", "0"),
                    new KeyValuePair<string, string>("train-fdr", Convert.ToString(0.1, CultureInfo.CurrentCulture))
                },
                ExpectedResults = new ExpectedResults(1131, 90, 90, 270),
                ExpectedResultsFinal = new ExpectedResults(82, 91, 91, 273)
            };

            RunFunctionalTest();
        }

        public bool IsRecordMode => false;
        private bool RedownloadTools => !IsRecordMode && IsPass0;

        private string GetTestPath(string path)
        {
            return TestFilesDir.GetTestPath(path);
        }

        private IEnumerable<string> SearchFiles
        {
            get
            {
                return new[]
                {
                    GetTestPath("Rpal_Std_2d_FullMS_Orbi30k_MSMS_Orbi7k_Centroid_Run1_102006_02.mzML"),
                    GetTestPath("Rpal_Std_2d_FullMS_Orbi30k_MSMS_Orbi7k_Centroid_Run1_102006_03.mzML")
                };
            }
        }

        private string[] SearchFilesSameName
        {
            get
            {
                return new[]
                {
                    GetTestPath("Rpal_Std_2d_FullMS_Orbi30k_MSMS_Orbi7k_Centroid_Run1_102006_02.mzML"),
                    GetTestPath(Path.Combine("subdir", "Rpal_Std_2d_FullMS_Orbi30k_MSMS_Orbi7k_Centroid_Run1_102006_02.mzML"))
                };
            }
        }

        protected override void DoTest()
        {
            TestSearch();
        }

        /// <summary>
        /// Quick test that DDA search works with MSAmanda.
        /// </summary>
        private void TestSearch()
        {
            PrepareDocument("TestDdaSearch.sky");

            // Launch the wizard
            var importPeptideSearchDlg = ShowDialog<ImportPeptideSearchDlg>(SkylineWindow.ShowImportPeptideSearchDlg);

            // We're on the "Build Spectral Library" page of the wizard.
            // Add the test xml file to the search files list and try to 
            // build the document library.

            RunUI(() =>
            {
                Assert.IsTrue(importPeptideSearchDlg.CurrentPage == ImportPeptideSearchDlg.Pages.spectra_page);
                importPeptideSearchDlg.BuildPepSearchLibControl.PerformDDASearch = true;
                importPeptideSearchDlg.BuildPepSearchLibControl.DdaSearchDataSources = SearchFiles.Select(o => (MsDataFileUri)new MsDataFilePath(o)).Take(1).ToArray();
                importPeptideSearchDlg.BuildPepSearchLibControl.WorkflowType = ImportPeptideSearchDlg.Workflow.dia; // will go back and switch to DDA
                importPeptideSearchDlg.BuildPepSearchLibControl.CutOffScore = 0.9;
                Assert.IsTrue(importPeptideSearchDlg.ClickNextButton());
            });

            // With only 1 source, no add/remove prefix/suffix dialog

            // We're on the "Match Modifications" page. Add M+16 mod.
            RunUI(() =>
            {
                Assert.IsTrue(importPeptideSearchDlg.CurrentPage == ImportPeptideSearchDlg.Pages.match_modifications_page);
                // Test going back and switching workflow to DDA. This used to cause an exception.
                Assert.IsTrue(importPeptideSearchDlg.ClickBackButton());
                importPeptideSearchDlg.BuildPepSearchLibControl.WorkflowType = ImportPeptideSearchDlg.Workflow.dda;
                Assert.IsTrue(importPeptideSearchDlg.ClickNextButton());
                Assert.IsTrue(importPeptideSearchDlg.CurrentPage == ImportPeptideSearchDlg.Pages.match_modifications_page);
            });

            // In PerformDDASearch mode, ClickAddStructuralModification launches edit list dialog
            var editListUI =
                ShowDialog<EditListDlg<SettingsListBase<StaticMod>, StaticMod>>(importPeptideSearchDlg.MatchModificationsControl.ClickAddStructuralModification);
            RunDlg<EditStaticModDlg>(editListUI.AddItem, editModDlg =>
            {
                editModDlg.SetModification("Oxidation (M)", true); // Not L10N
                editModDlg.OkDialog();
            });

            // Test a non-Unimod mod that won't affect the search
            RunDlg<EditStaticModDlg>(editListUI.AddItem, editModDlg =>
            {
                editModDlg.Modification = new StaticMod("NotUniModMod (U)", "U", null, "C3P1O1", LabelAtoms.None, null, null);
                editModDlg.OkDialog();
            });

            // Test a N terminal mod with no AA
            RunDlg<EditStaticModDlg>(editListUI.AddItem, editModDlg =>
            {
                editModDlg.Modification = new StaticMod("NotUniModMod (N-term)", null, ModTerminus.N, "C42", LabelAtoms.None, null, null);
                editModDlg.Modification = editModDlg.Modification.ChangeVariable(true);
                editModDlg.OkDialog();
            });

            // Test a C terminal mod with no AA and one with AA - commented out because it changes results a bit to include it
            /*RunDlg<EditStaticModDlg>(editListUI.AddItem, editModDlg =>
            {
                editModDlg.Modification = new StaticMod("NotUniModMod (C-term)", null, ModTerminus.C, null, LabelAtoms.None, 0.01, 0.01);
                editModDlg.Modification = editModDlg.Modification.ChangeVariable(true);
                editModDlg.OkDialog();
            }); 
            RunDlg<EditStaticModDlg>(editListUI.AddItem, editModDlg =>
            {
                editModDlg.Modification = new StaticMod("NotUniModMod4 (C-term)", "K,R", null, null, LabelAtoms.None, -1.01, -1.01);
                editModDlg.OkDialog();
            });*/
            OkDialog(editListUI, editListUI.OkDialog);

            // Test back/next buttons
            RunUI(() =>
            {
                Assert.IsTrue(importPeptideSearchDlg.ClickBackButton());
                Assert.IsTrue(importPeptideSearchDlg.ClickNextButton());

                Assert.IsTrue(importPeptideSearchDlg.CurrentPage == ImportPeptideSearchDlg.Pages.match_modifications_page);

                Assert.IsTrue(importPeptideSearchDlg.ClickNextButton());
            });

            // We're on the MS1 full scan settings page.
            RunUI(() =>
            {
                Assert.IsTrue(importPeptideSearchDlg.CurrentPage == ImportPeptideSearchDlg.Pages.full_scan_settings_page);
                importPeptideSearchDlg.FullScanSettingsControl.PrecursorCharges = new[] { 2, 3 };
                Assert.IsTrue(importPeptideSearchDlg.ClickNextButton());
            });

            // We're on the "Import FASTA" page.
            RunUI(() =>
            {
                Assert.IsTrue(importPeptideSearchDlg.CurrentPage == ImportPeptideSearchDlg.Pages.import_fasta_page);
                Assert.IsFalse(importPeptideSearchDlg.ImportFastaControl.DecoyGenerationEnabled);
                importPeptideSearchDlg.ImportFastaControl.SetFastaContent(GetTestPath("rpal-subset.fasta"));
                Assert.IsTrue(importPeptideSearchDlg.ClickNextButton());
            });

            // We're on the "Adjust Search Settings" page
            bool? searchSucceeded = null;
            TryWaitForOpenForm(typeof(ImportPeptideSearchDlg.DDASearchSettingsPage));   // Stop to show this form during form testing
            RunUI(() =>
            {
                Assert.IsTrue(importPeptideSearchDlg.CurrentPage == ImportPeptideSearchDlg.Pages.dda_search_settings_page);
            });

            SkylineWindow.BeginInvoke(new Action(() => importPeptideSearchDlg.SearchSettingsControl.SelectedSearchEngine = TestSettings.SearchEngine));

            if (RedownloadTools || TestSettings.HasMissingDependencies)
            {
                if (TestSettings.SearchEngine == SearchSettingsControl.SearchEngine.MSFragger)
                {
                    var msfraggerDownloaderDlg = TryWaitForOpenForm<MsFraggerDownloadDlg>(2000);
                    if (msfraggerDownloaderDlg != null)
                    {
                        RunUI(() => msfraggerDownloaderDlg.SetValues("Matt Chambers (testing download from Skyline)", "matt.chambers42@gmail.com", "UW"));
                        OkDialog(msfraggerDownloaderDlg, msfraggerDownloaderDlg.ClickAccept);
                    }
                }

                if (TestSettings.SearchEngine != SearchSettingsControl.SearchEngine.MSAmanda)
                {
                    var downloaderDlg = TryWaitForOpenForm<MultiButtonMsgDlg>(2000);
                    if (downloaderDlg != null)
                    {
                        OkDialog(downloaderDlg, downloaderDlg.ClickYes);
                        var waitDlg = WaitForOpenForm<LongWaitDlg>();
                        WaitForClosedForm(waitDlg);
                    }
                }
            }

            RunUI(() =>
            {
                foreach (var setting in TestSettings.AdditionalSettings)
                    importPeptideSearchDlg.SearchSettingsControl.SetAdditionalSetting(setting.Key, setting.Value);
                importPeptideSearchDlg.SearchSettingsControl.PrecursorTolerance = TestSettings.PrecursorTolerance;
                importPeptideSearchDlg.SearchSettingsControl.FragmentTolerance = TestSettings.FragmentTolerance;
                importPeptideSearchDlg.SearchSettingsControl.FragmentIons = TestSettings.FragmentIons;
                importPeptideSearchDlg.SearchSettingsControl.Ms2Analyzer = TestSettings.Ms2Analyzer;

                // Run the search
                Assert.IsTrue(importPeptideSearchDlg.ClickNextButton());
            });
            TryWaitForOpenForm(typeof(ImportPeptideSearchDlg.DDASearchPage));   // Stop to show this form during form testing
            RunUI(() =>
            {
                importPeptideSearchDlg.SearchControl.OnSearchFinished += (success) => searchSucceeded = success;
                importPeptideSearchDlg.BuildPepSearchLibControl.IncludeAmbiguousMatches = true;

                // Cancel search
                importPeptideSearchDlg.SearchControl.Cancel();
            });

            WaitForConditionUI(60000, () => searchSucceeded.HasValue);
            Assert.IsFalse(searchSucceeded.Value);
            searchSucceeded = null;

            // Go back and test 2 input files with the same name
            RunUI(() =>
            {
                Assert.IsTrue(importPeptideSearchDlg.ClickBackButton());
                Assert.IsTrue(importPeptideSearchDlg.ClickBackButton());
                Assert.IsTrue(importPeptideSearchDlg.ClickBackButton());
                Assert.IsTrue(importPeptideSearchDlg.ClickBackButton());
                Assert.IsTrue(importPeptideSearchDlg.ClickBackButton());

                importPeptideSearchDlg.BuildPepSearchLibControl.DdaSearchDataSources = SearchFilesSameName.Select(o => (MsDataFileUri) new MsDataFilePath(o)).ToArray();
            });

            // Cancel without changing the replicate names
            {
                var removeSuffix = ShowDialog<ImportResultsNameDlg>(() => importPeptideSearchDlg.ClickNextButton());
                OkDialog(removeSuffix, removeSuffix.CancelDialog);
            }

            // Test with 2 files (different name)
            RunUI(() =>
            {
                // CONSIDER: Why does this end up on the next page after a cancel?
                Assert.IsTrue(importPeptideSearchDlg.ClickBackButton());
                Assert.IsTrue(importPeptideSearchDlg.CurrentPage == ImportPeptideSearchDlg.Pages.spectra_page);
                importPeptideSearchDlg.BuildPepSearchLibControl.DdaSearchDataSources = SearchFiles.Select(o => (MsDataFileUri)new MsDataFilePath(o)).ToArray();
            });

            // With 2 sources, we get the remove prefix/suffix dialog; accept default behavior
            {
                var removeSuffix2 = ShowDialog<ImportResultsNameDlg>(() => importPeptideSearchDlg.ClickNextButton());
                OkDialog(removeSuffix2, () => removeSuffix2.YesDialog());
            }

            RunUI(() =>
            {
                // The document should not have changed, but code used to wait for it to be loaded
                Assert.IsTrue(SkylineWindow.DocumentUI.IsLoaded, TextUtil.LineSeparate("Document not loaded:",
                    TextUtil.LineSeparate(SkylineWindow.DocumentUI.NonLoadedStateDescriptions)));
                // We're on the "Match Modifications" page again.
                Assert.IsTrue(importPeptideSearchDlg.CurrentPage == ImportPeptideSearchDlg.Pages.match_modifications_page);
                importPeptideSearchDlg.MatchModificationsControl.ChangeItem(0, false); // uncheck C+57
                for (int i = 1; i < importPeptideSearchDlg.MatchModificationsControl.MatchedModifications.Count(); ++i)
                    importPeptideSearchDlg.MatchModificationsControl.ChangeItem(i, true); // check everything else
                Assert.IsTrue(importPeptideSearchDlg.ClickNextButton());
                Assert.IsTrue(importPeptideSearchDlg.ClickNextButton());
                Assert.IsTrue(importPeptideSearchDlg.ClickNextButton());
                Assert.IsTrue(importPeptideSearchDlg.ClickNextButton());
            });

            try
            {
                WaitForConditionUI(60000, () => searchSucceeded.HasValue);
                RunUI(() => Assert.IsTrue(searchSucceeded.Value, importPeptideSearchDlg.SearchControl.LogText));
            }
            finally
            {
                File.WriteAllText("SearchControlLog.txt", importPeptideSearchDlg.SearchControl.LogText);
            }

            var emptyProteinsDlg = ShowDialog<AssociateProteinsDlg>(importPeptideSearchDlg.ClickNextButtonNoCheck);
            WaitForConditionUI(() => emptyProteinsDlg.DocumentFinalCalculated);
            RunUI(()=>
            {
                int proteinCount, peptideCount, precursorCount, transitionCount;
                /*emptyProteinsDlg.NewTargetsAll(out proteinCount, out peptideCount, out precursorCount, out transitionCount);
                if (Environment.Is64BitProcess)
                {
                    // TODO: reenable these checks for 32 bit once intermittent failures are debugged
                    if (IsRecordMode)
                    {
                        Console.WriteLine();
                        Console.WriteLine($@"{proteinCount}, {peptideCount}, {precursorCount}, {transitionCount}");
                    }
                    else
                    {
                        Assert.AreEqual(TestSettings.ExpectedResults.proteinCount, proteinCount);
                        Assert.AreEqual(TestSettings.ExpectedResults.peptideCount, peptideCount);
                        Assert.AreEqual(TestSettings.ExpectedResults.precursorCount, precursorCount);
                        Assert.AreEqual(TestSettings.ExpectedResults.transitionCount, transitionCount);
                    }
                }*/
                emptyProteinsDlg.NewTargetsFinalSync(out proteinCount, out peptideCount, out precursorCount, out transitionCount);
                if (Environment.Is64BitProcess)
                {
                    if (IsRecordMode)
                    {
                        Console.WriteLine($@"{proteinCount}, {peptideCount}, {precursorCount}, {transitionCount}");
                    }
                    else
                    {
                        Assert.AreEqual(TestSettings.ExpectedResultsFinal.proteinCount, proteinCount);
                        Assert.AreEqual(TestSettings.ExpectedResultsFinal.peptideCount, peptideCount);
                        Assert.AreEqual(TestSettings.ExpectedResultsFinal.precursorCount, precursorCount);
                        Assert.AreEqual(TestSettings.ExpectedResultsFinal.transitionCount, transitionCount);
                    }
                }
            });
            OkDialog(emptyProteinsDlg, emptyProteinsDlg.OkDialog);

            WaitForDocumentLoaded();
            RunUI(() => SkylineWindow.SaveDocument());
        }

        private void PrepareDocument(string documentFile)
        {
            RunUI(SkylineWindow.NewDocument);
            RunUI(() => SkylineWindow.ModifyDocument("Set default settings", 
                doc => doc.ChangeSettings(SrmSettingsList.GetDefault())));
            RunUI(() => SkylineWindow.SaveDocument(GetTestPath(documentFile)));
        }
    }
}

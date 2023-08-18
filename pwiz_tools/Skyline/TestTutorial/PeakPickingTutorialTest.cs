﻿/*
 * Original author: Dario Amodei <damodei .at. stanford.edu>,
 *                  Mallick Lab, Department of Radiology, Stanford
 *
 * Copyright 2013 University of Washington - Seattle, WA
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
using pwiz.Common.DataBinding;
using pwiz.Common.DataBinding.Controls.Editor;
using pwiz.Common.SystemUtil;
using pwiz.Skyline.Controls;
using pwiz.Skyline.Controls.Databinding;
using pwiz.Skyline.Controls.Graphs;
using pwiz.Skyline.EditUI;
using pwiz.Skyline.FileUI;
using pwiz.Skyline.Model;
using pwiz.Skyline.Model.DocSettings;
using pwiz.Skyline.Model.Results;
using pwiz.Skyline.Model.Results.Scoring;
using pwiz.Skyline.Properties;
using pwiz.Skyline.SettingsUI;
using pwiz.Skyline.Util.Extensions;
using pwiz.SkylineTestUtil;

namespace pwiz.SkylineTestTutorial
{
    [TestClass]
    public class PeakPickingTutorialTest : DocumentationGeneratorTest
    {
        private readonly string[] _importFiles =
            {
                "olgas_S130501_006_StC-DosR_B2",
                "olgas_S130501_007_StC-DosR_C2",
                "olgas_S130501_008_StC-DosR_A4",
                "olgas_S130501_009_StC-DosR_B4",
                "olgas_S130501_010_StC-DosR_C4"
            };

        protected override bool UseRawFiles
        {
            get { return !ForceMzml && ExtensionTestContext.CanImportAbWiff; }
        }

        [TestMethod]
        public void TestPeakPickingTutorial()
        {
            // Set true to look at tutorial screenshots.
//            IsPauseForScreenShots = true;
//            IsCoverShotMode = true;
            CoverShotName = "PeakPicking";

            ForceMzml = false;  // Mzml isn't faster for this test.

            LinkPdf = "https://skyline.gs.washington.edu/labkey/_webdav/home/software/Skyline/%40files/tutorials/PeakPicking_2-5.pdf";

            TestFilesZipPaths = new[]
                {
                    UseRawFiles
                        ? @"https://skyline.gs.washington.edu/tutorials/PeakPicking.zip"
                        : @"https://skyline.gs.washington.edu/tutorials/PeakPickingMzml.zip",
                    @"TestTutorial\PeakPickingViews.zip"
                };
            RunFunctionalTest();

            Assert.IsFalse(IsRecordMode);   // Make sure this doesn't get committed as true
        }

        private string GetTestPath(string relativePath)
        {
            var folderTutorial = UseRawFiles ? "PeakPicking" : "PeakPickingMzml";
            return TestFilesDirs[0].GetTestPath(Path.Combine(folderTutorial, relativePath));
        }

        /// <summary>
        /// Change to true to write coefficient arrays
        /// </summary>
        private bool IsRecordMode { get { return false; } }

        private readonly string[] EXPECTED_COEFFICIENTS =
        {
            "-0.0783|-0.7492|2.2424|1.2062|0.0331|0.1515|0.1767| null |0.4983|6.3997|-0.0482|0.5476|0.6281| null | null | null | null | null ",
            "0.2900| null | null |5.9842|-0.0624|0.6681|0.7968| null | null | null | null | null | null | null | null | null | null | null ",
        };

        protected override void DoTest()
        {
            Settings.Default.PeakScoringModelList.Clear();

            // Open the file
            RunUI(() => SkylineWindow.OpenFile(GetTestPath("SRMCourse_DosR-hDP__20130501-tutorial-empty.sky")));
            WaitForDocumentLoaded();

            // Add decoys
            var generateDecoysDlg = ShowDialog<GenerateDecoysDlg>(() => SkylineWindow.ShowGenerateDecoysDlg());
            RunUI(() =>
            {
                generateDecoysDlg.DecoysMethod = DecoyGeneration.REVERSE_SEQUENCE;
                generateDecoysDlg.NumDecoys = 29;
            });
            RunUISaveScreenshot(generateDecoysDlg, "GenerateDecoysDlg");

            OkDialog(generateDecoysDlg, generateDecoysDlg.OkDialog);

            RestoreViewOnScreen(3);
            RunUI(() =>
            {
                SkylineWindow.SequenceTree.TopNode = SkylineWindow.SequenceTree.Nodes[11];
                SkylineWindow.SelectedPath = new IdentityPath(SkylineWindow.DocumentUI.Children.Last().Id);
            });
            RunUISaveScreenshot(SkylineWindow.SequenceTree, "SequenceTree");

            // Open the file with decoys
            RunUI(() => SkylineWindow.OpenFile(GetTestPath("SRMCourse_DosR-hDP__20130501-tutorial-empty-decoys.sky")));
            WaitForDocumentLoaded();

            // Import the raw data
            var importResultsDlg = ShowDialog<ImportResultsDlg>(SkylineWindow.ImportResults);
            RunUI(() =>
            {
                importResultsDlg.RadioAddNewChecked = true;
                var path = new KeyValuePair<string, MsDataFileUri[]>[5];
                for (int i = 0; i < 5; ++i)
                {
                    path[i] = new KeyValuePair<string, MsDataFileUri[]>(_importFiles[i],
                                            new[] { MsDataFileUri.Parse(GetTestPath(_importFiles[i] + ExtAbWiff)) });
                }

                importResultsDlg.NamedPathSets = path;
            });
            var importResultsNameDlg = ShowDialog<ImportResultsNameDlg>(importResultsDlg.OkDialog);
            RunUISaveScreenshot(importResultsNameDlg, "ImportResultsCommonPrefix");
            RunUI(() =>
            {
                string prefix = importResultsNameDlg.Prefix;
                importResultsNameDlg.Prefix = prefix.Substring(0, prefix.Length - 1);
                importResultsNameDlg.YesDialog();
            });
            WaitForClosedForm(importResultsNameDlg);
            WaitForClosedForm(importResultsDlg);
            WaitForConditionUI(5 * 60 * 1000, () => 
                SkylineWindow.DocumentUI.Settings.HasResults &&
                SkylineWindow.DocumentUI.Settings.MeasuredResults.IsLoaded);    // 5 minutes
            RestoreViewOnScreen(5);
            const string peptideSeqHighlight = "LPDGNGIELCR";
            RunUI(() =>
                {
                    var nodeGroup = SkylineWindow.DocumentUI.PeptideTransitionGroups.ToArray()[71];
                    Assert.AreEqual(nodeGroup.TransitionGroup.Peptide.Sequence, peptideSeqHighlight);
                    var chromGroupInfo = nodeGroup.ChromInfos.ToList()[0];
                    Assert.IsNotNull(chromGroupInfo.RetentionTime);
                    // TODO: Fix the tutorial.  This was supposed to be an incorrectly picked peak, but our default scoring is now good enough to pick it correctly
                    Assert.AreEqual(chromGroupInfo.RetentionTime.Value, 18.0, 0.1);
                    SkylineWindow.ChangeTextSize(TreeViewMS.LRG_TEXT_FACTOR);
                });
            RunDlg<ChromChartPropertyDlg>(SkylineWindow.ShowChromatogramProperties, dlg =>
                {
                    dlg.FontSize = GraphFontSize.LARGE;
                    dlg.OkDialog();
                });
            RunUISaveScreenshot(SkylineWindow, "MainWindow");

            // Test different point types on RTLinearRegressionGraph
            RunUI(() =>
            {
                SkylineWindow.ShowRTRegressionGraphScoreToRun();
                SkylineWindow.ShowPlotType(PlotTypeRT.correlation);
                SkylineWindow.ChooseCalculator("iRT_SRMAtlas_20121202_noLGG");
            });
            const int numDecoys = 30;
            CheckPointsTypeRT(PointsTypeRT.targets, SkylineWindow.Document.PeptideCount - numDecoys);
            CheckPointsTypeRT(PointsTypeRT.standards, SkylineWindow.Document.GetRetentionTimeStandards().Count);
            CheckPointsTypeRT(PointsTypeRT.decoys, numDecoys);
            RunUI(() => SkylineWindow.ShowGraphRetentionTime(false));
            WaitForDocumentLoaded();

            // Train the peak scoring model
            var reintegrateDlg = ShowDialog<ReintegrateDlg>(SkylineWindow.ShowReintegrateDialog);
            RunUISaveScreenshot(reintegrateDlg, "ReintegrateForm");
            var editDlg = ShowDialog<EditPeakScoringModelDlg>(reintegrateDlg.AddPeakScoringModel);
            RunUI(() => editDlg.TrainModel());
            RunUISaveScreenshot(editDlg, "EditPeakScoringModelFormTrainedModel");
            RunUI(() => Assert.AreEqual(0.5992, editDlg.PeakCalculatorsGrid.Items[3].PercentContribution ?? 0, 0.005));

            RunUI(() => editDlg.SelectedGraphTab = 2);
            // TODO(nicksh): Save an emf instead of a png
            RunUISaveScreenshot(editDlg.ZedGraphPValues, "EditPeakScoringModelFormPValueGraph");

            RunUI(() => editDlg.SelectedGraphTab = 3);
            RunUISaveScreenshot(editDlg.ZedGraphQValues, "EditPeakScoringModelFormQValueGraph");

            RunUI(() => editDlg.SelectedGraphTab = 1);
            RunUI(() => editDlg.PeakCalculatorsGrid.SelectRow(3));
            RunUISaveScreenshot(editDlg, "EditPeakScoringModelFormFeatureScore");

            RunUI(() =>
            {
                Assert.AreEqual(18, editDlg.PeakCalculatorsGrid.RowCount);
                // The rows which the tutorial says are missing scores are in fact missing scores
                foreach (int i in new[] { 2, 7, 8, 9, 10, 11, 13, 15 }) // MS1 scores are now missing, 19, 20, 21, 22
                {
                    Assert.IsFalse(editDlg.IsActiveCell(i, 0));
                }
                editDlg.IsFindButtonVisible = true;
                editDlg.FindMissingValues(2);   // Retention time
                editDlg.PeakScoringModelName = "test1";
            });
            RunUI(() =>
            {
                editDlg.ToolStripFind.Visible = true;
                SaveScreenshot(editDlg.ToolStripFind, "FindButton");
                SaveScreenshot(editDlg, "EditPeakScoringModelFormFindMissingScores");
            });

            OkDialog(editDlg, editDlg.OkDialog);
            OkDialog(reintegrateDlg, reintegrateDlg.CancelDialog);
            var findResultsForm = FindOpenForm<FindResultsForm>();
            // TODO(nicksh): make sure form is tall enough to show all 6 items
            RunUISaveScreenshot(findResultsForm, "FindResultsViewClippedFromMainWindow");
            // Remove the peptide with no library dot product, and train again
            var missingPeptides = new List<string> { "LGGNEQVTR", "IPVDSIYSPVLK", "YFNDGDIVEGTIVK", 
                                                     "DFDSLGTLR", "GGYAGMLVGSVGETVAQLAR", "GGYAGMLVGSVGETVAQLAR"};
            var isDecoys = new List<bool> {false, false, false, false, false, true};

            RunUI(() =>
            {
                findResultsForm = FormUtil.OpenForms.OfType<FindResultsForm>().FirstOrDefault();
                Assert.IsNotNull(findResultsForm);
// ReSharper disable once PossibleNullReferenceException
                Assert.AreEqual(findResultsForm.ItemCount, 6);
                for (int i = 0; i < 6; ++i)
                {
                    findResultsForm.ActivateItem(i);
                    Assert.AreEqual(SkylineWindow.SelectedPeptideSequence, missingPeptides[i]);
                    if (0 < i && i < 5)
                        SkylineWindow.SetStandardType(PeptideDocNode.STANDARD_TYPE_QC);
                }
            });

            RunUI(() => findResultsForm.Close());

            for (int i = 0; i < 6; ++i)
            {
                if (!(0 < i && i < 5))
                    RemovePeptide(missingPeptides[i], isDecoys[i]);
            }

            if (IsCoverShotMode)
            {
                RestoreCoverViewOnScreen();
                var reintegrateDlgCover = ShowDialog<ReintegrateDlg>(SkylineWindow.ShowReintegrateDialog);
                var editModelCover = ShowDialog<EditPeakScoringModelDlg>(reintegrateDlgCover.AddPeakScoringModel);
                RunUI(() =>
                {
                    editModelCover.Top = SkylineWindow.Top + 8;
                    editModelCover.Left = SkylineWindow.Right - editModelCover.Width - 8;
                    editModelCover.PeakScoringModelName = "SRMCourse";
                    editModelCover.TrainModelClick();
                });
                TakeCoverShot();

                OkDialog(editModelCover, editModelCover.CancelDialog);
                OkDialog(reintegrateDlgCover, reintegrateDlgCover.CancelDialog);
                return;
            }

            var reintegrateDlgNew = ShowDialog<ReintegrateDlg>(SkylineWindow.ShowReintegrateDialog);
            var editListLibrary = ShowDialog<EditListDlg<SettingsListBase<PeakScoringModelSpec>, PeakScoringModelSpec>>(
                reintegrateDlgNew.EditPeakScoringModel);

            RunUI(() => editListLibrary.SelectItem("test1"));
            var editDlgLibrary = ShowDialog<EditPeakScoringModelDlg>(editListLibrary.EditItem);
            RunUI(() =>
                {
                    foreach (int i in new[] { 2, 8, 9, 10, 11 })
                    {
                        Assert.IsTrue(editDlgLibrary.IsActiveCell(i, 0));
                        Assert.IsFalse(editDlgLibrary.PeakCalculatorsGrid.Items[i].IsEnabled);
                        editDlgLibrary.PeakCalculatorsGrid.Items[i].IsEnabled = true;
                    }
                    editDlgLibrary.TrainModel(true);
                });
            RunUISaveScreenshot(editDlgLibrary, "EditPeakScoringModelFormWithLibraryScore");

            RunUI(() => editDlgLibrary.SelectedGraphTab = 3);
            RunUISaveScreenshot(editDlgLibrary.ZedGraphQValues, "EditPeakScoringModelFormQValueGraphWithLibraryScore");

            OkDialog(editDlgLibrary, editDlgLibrary.OkDialog);

            // Open up the model again for editing, re-train with second best peaks and removing some scores
            RunUI(() => editListLibrary.SelectItem("test1"));
            var editDlgNew = ShowDialog<EditPeakScoringModelDlg>(editListLibrary.EditItem);
            RunUI(() =>
                {
                    Assert.IsFalse(editDlgNew.UsesSecondBest);
                    Assert.IsTrue(editDlgNew.UsesDecoys);
                    Assert.IsTrue(editDlgNew.PeakCalculatorsGrid.Items[4].IsEnabled);
                    Assert.IsTrue(editDlgNew.PeakCalculatorsGrid.Items[4].PercentContribution < 0);
                    Assert.IsTrue(editDlgNew.PeakCalculatorsGrid.Items[2].IsEnabled);
                    Assert.IsTrue(editDlgNew.PeakCalculatorsGrid.Items[2].PercentContribution > 0);
                    editDlgNew.UsesSecondBest = true;
                    editDlgNew.PeakCalculatorsGrid.Items[4].IsEnabled = false;
                    editDlgNew.PeakCalculatorsGrid.Items[2].IsEnabled = false;
                    editDlgNew.TrainModel(true);
                    // Check that these cells are still active even though they've been unchecked
                    Assert.IsTrue(editDlgNew.IsActiveCell(6, 0));
                });
            RunUISaveScreenshot(editDlgNew, "EditPeakScoringModelFormWithSecondBest");

            OkDialog(editDlgNew, editDlgNew.CancelDialog);
            OkDialog(editListLibrary, editListLibrary.OkDialog);

            // Apply the model to reintegrate peaks
            RunUI(() =>
            {
                reintegrateDlgNew.ComboPeakScoringModelSelected = "test1";
                reintegrateDlgNew.ReintegrateAll = true;
                reintegrateDlgNew.OverwriteManual = true;
            });
            RunUISaveScreenshot(reintegrateDlgNew, "ReintegrateFormNew");

            OkDialog(reintegrateDlgNew, reintegrateDlgNew.OkDialog);
            RunUI(() =>
            {
                var nodeGroup = SkylineWindow.DocumentUI.PeptideTransitionGroups.ToArray()[70];
                Assert.AreEqual(nodeGroup.TransitionGroup.Peptide.Sequence, peptideSeqHighlight);
                var chromGroupInfo = nodeGroup.ChromInfos.ToList()[0];
                Assert.IsNotNull(chromGroupInfo.RetentionTime);
                Assert.AreEqual(18.0, chromGroupInfo.RetentionTime.Value, 0.1);
            });
            FindNode(peptideSeqHighlight);
            RunUISaveScreenshot(GetActiveGraphChromatogram(), "ChromatogramGraphCorrectedPeakAt18");

            // Reintegrate slightly differently, with a q value cutoff
            var reintegrateDlgQ = ShowDialog<ReintegrateDlg>(SkylineWindow.ShowReintegrateDialog);
            RunUI(() =>
                {
                    reintegrateDlgQ.ReintegrateAll = false;
                    reintegrateDlgQ.Cutoff = 0.001;
                    reintegrateDlgQ.OverwriteManual = true;
                });
            OkDialog(reintegrateDlgQ, reintegrateDlgQ.OkDialog);
            WaitForGraphs();
            RunUISaveScreenshot(SkylineWindow, "TargetsViewWithSomeNullPeaks");
            RunUISaveScreenshot(GetActiveGraphChromatogram(), "ChromatogramGraphWithNoPickedPeak");

            RestoreViewOnScreen(14);
            FindNode((622.3086).ToString(CultureInfo.CurrentCulture) + "++");
            RunUISaveScreenshot(SkylineWindow, "MainWindowWithInterferenceOnTransition");

            // Export the mProphet features
            var mProphetExportDlg = ShowDialog<MProphetFeaturesDlg>(SkylineWindow.ShowMProphetFeaturesDialog);

            RunUI(() => mProphetExportDlg.BestScoresOnly = true);
            RunUISaveScreenshot(mProphetExportDlg, "ExportMProphetFeaturesForm");
            
            // TODO: actually write the features here using WriteFeatures
            OkDialog(mProphetExportDlg, mProphetExportDlg.CancelDialog);

            // Export a report
            string pathReport = GetTestPath("qValues_Exported_report.csv");
            const string qvalueHeader = "annotation_QValue";
            string reportName = Resources.ReportSpecList_GetDefaults_Peptide_RT_Results;
            var reportExportDlg = ShowDialog<ExportLiveReportDlg>(SkylineWindow.ShowExportReportDialog);
            var manageViewsForm = ShowDialog<ManageViewsForm>(reportExportDlg.EditList);
            RunUI(() => manageViewsForm.SelectView(reportName));
            RunUISaveScreenshot(manageViewsForm, "ManageReportsForm");

            var customizeViewDlg = ShowDialog<ViewEditor>(manageViewsForm.EditView);
            RunUISaveScreenshot(customizeViewDlg, "EditReportForm");

            RunUI(() => customizeViewDlg.ChooseColumnsTab.AddColumn(PropertyPath.Parse("Proteins!*.Peptides!*.Precursors!*.Results!*.Value")
                .Property(AnnotationDef.ANNOTATION_PREFIX + qvalueHeader)));
            RunUISaveScreenshot(customizeViewDlg, "EditReportFormWithSelectedColumns");

            OkDialog(customizeViewDlg, customizeViewDlg.OkDialog);
            OkDialog(manageViewsForm, manageViewsForm.Close);
            RunUI(() => reportExportDlg.ReportName = reportName);
            OkDialog(reportExportDlg, () => reportExportDlg.OkDialog(pathReport, TextUtil.CsvSeparator));

            Assert.IsTrue(File.Exists(pathReport));
            using (var reader = new StreamReader(pathReport))
            {
                string line = reader.ReadLine();
                Assert.IsNotNull(line);
                var fieldHeaders = line.Split(TextUtil.CsvSeparator);
                const int qvalueColumnIndex = 6;
                Assert.AreEqual(qvalueColumnIndex + 1, fieldHeaders.Length);
                Assert.AreEqual(qvalueHeader, fieldHeaders[qvalueColumnIndex]);
                int qvalueCount = 0;
                while ((line = reader.ReadLine()) != null)
                {
                    var fields = line.Split(TextUtil.CsvSeparator);
                    if (double.TryParse(fields[qvalueColumnIndex], out _))
                        qvalueCount++;
                }
                Assert.AreEqual(290, qvalueCount); // PrecursorResults field means 29 peptides * 5 replicates * 2 label types
            }

            // Open OpenSWATH gold standard dataset
            RunUI(() => SkylineWindow.OpenFile(GetTestPath("AQUA4_Human_picked_napedro2-mod2.sky")));
            WaitForDocumentLoaded();

            // Perform re-score of DIA data
            var manageResults = ShowDialog<ManageResultsDlg>(SkylineWindow.ManageResults);
            RunUISaveScreenshot(manageResults, "ManageResultsForm");

            var rescoreResultsDlg = ShowDialog<RescoreResultsDlg>(manageResults.Rescore);
            RunUISaveScreenshot(rescoreResultsDlg, "ReScoreResultsForm");

            RunUI(() => rescoreResultsDlg.Rescore(false));
            WaitForCondition(10 * 60 * 1000, () => SkylineWindow.Document.Settings.MeasuredResults.IsLoaded);    // 10 minutes (usually needs less, but code coverage analysis can be slow)
            WaitForClosedForm(rescoreResultsDlg);
            WaitForClosedForm(manageResults);
            WaitForConditionUI(() => FindOpenForm<AllChromatogramsGraph>() == null);
            WaitForDocumentLoaded();

            // Train the peak scoring model for the DIA dataset
            var reintegrateDlgDia = ShowDialog<ReintegrateDlg>(SkylineWindow.ShowReintegrateDialog);

            // Open the previous scoring model for use with the DIA dataset
            var editListDia = ShowDialog<EditListDlg<SettingsListBase<PeakScoringModelSpec>, PeakScoringModelSpec>>(
                    reintegrateDlgDia.EditPeakScoringModel);
            RunUI(() => editListDia.SelectItem("test1"));
            var editDlgFromSrm = ShowDialog<EditPeakScoringModelDlg>(editListDia.EditItem);
            RunUISaveScreenshot(editDlgFromSrm, "EditPeakScoringModelFormSrmModelAppliedToDiaData");
            RunUI(() =>
                {
                    ValidateCoefficients(editDlgFromSrm, 0);

                    for (int j = 0; j < editDlgFromSrm.PeakCalculatorsGrid.Items.Count; ++j)
                    {
                        Assert.AreEqual(editDlgFromSrm.PeakCalculatorsGrid.Items[j].PercentContribution, null);
                    }
                    int i = 0;
                    Assert.IsTrue(editDlgFromSrm.IsActiveCell(i++, 0));
                    Assert.IsFalse(editDlgFromSrm.IsActiveCell(i++, 0));
//                    Assert.IsFalse(editDlgFromSrm.IsActiveCell(i++, 0));
                    Assert.IsFalse(editDlgFromSrm.IsActiveCell(i++, 0));
                    Assert.IsTrue(editDlgFromSrm.IsActiveCell(i++, 0));
                    Assert.IsTrue(editDlgFromSrm.IsActiveCell(i++, 0));
                    Assert.IsTrue(editDlgFromSrm.IsActiveCell(i++, 0));
                    Assert.IsTrue(editDlgFromSrm.IsActiveCell(i++, 0));
                    Assert.IsFalse(editDlgFromSrm.IsActiveCell(i++, 0));
                    Assert.IsFalse(editDlgFromSrm.IsActiveCell(i++, 0));
                    Assert.IsFalse(editDlgFromSrm.IsActiveCell(i++, 0));
                    Assert.IsFalse(editDlgFromSrm.IsActiveCell(i, 0));
                });
            
            OkDialog(editDlgFromSrm, editDlgFromSrm.CancelDialog);
            OkDialog(editListDia, editListDia.CancelDialog);

            // Train a new model for the DIA dataset
            var editDlgDia = ShowDialog<EditPeakScoringModelDlg>(reintegrateDlgDia.AddPeakScoringModel);
            RunUI(() =>
                {
                    editDlgDia.UsesDecoys = false;
                    editDlgDia.UsesSecondBest = true;
                    editDlgDia.TrainModel();
                });

            RunUI(() => ValidateCoefficients(editDlgDia, 1));

            RunUISaveScreenshot(editDlgDia, "EditPeakScoringModelFormDiaPeakScoringDialogWithSecondBest");
            
            RunUI(() =>
                {
                    editDlgDia.SelectedGraphTab = 1;
                    editDlgDia.PeakCalculatorsGrid.SelectRow(2);
                    editDlgDia.IsFindButtonVisible = true;
                    editDlgDia.FindMissingValues(2);    // Retention times
                    editDlgDia.PeakScoringModelName = "testDIA";
                });
            OkDialog(editDlgDia, editDlgDia.OkDialog);
            RunUI(() =>
            {
                reintegrateDlgDia.ReintegrateAll = true;
                reintegrateDlgDia.OverwriteManual = true;
            });
            OkDialog(reintegrateDlgDia, reintegrateDlgDia.OkDialog);

            findResultsForm = FormUtil.OpenForms.OfType<FindResultsForm>().FirstOrDefault();
            Assert.IsNotNull(findResultsForm);
            Assert.AreEqual(34, findResultsForm.ItemCount);
        }

        private void ValidateCoefficients(EditPeakScoringModelDlg editDlgFromSrm, int coeffIndex)
        {
            string coefficients = string.Join(@"|", GetCoefficientStrings(editDlgFromSrm));
            if (IsRecordMode)
                Console.WriteLine(@"""{0}"",", coefficients);
            else
                AssertEx.AreEqualLines(EXPECTED_COEFFICIENTS[coeffIndex], coefficients);
        }

        private void CheckPointsTypeRT(PointsTypeRT pointsType, int expectedPoints)
        {
            RunUI(() => SkylineWindow.ShowPointsType(pointsType));
            WaitForGraphs();
            WaitForRegression();
            RunUI(() =>
            {
                RTLinearRegressionGraphPane pane;
                Assert.IsTrue(SkylineWindow.RTGraphController.GraphSummary.TryGetGraphPane(out pane));
                Assert.AreEqual(expectedPoints, pane.StatisticsRefined.ListRetentionTimes.Count);
            });
        }

        private GraphChromatogram GetActiveGraphChromatogram()
        {
            GraphChromatogram result = null;
            RunUI(() =>
            {
                var chromatogramSet =
                    SkylineWindow.DocumentUI.MeasuredResults.Chromatograms[SkylineWindow.SelectedResultsIndex];
                foreach (var graph in FormUtil.OpenForms.OfType<GraphChromatogram>())
                {
                    if (graph.NameSet == chromatogramSet.Name)
                    {
                        result = graph;
                    }
                }
            });
            return result;
        }
    }
}

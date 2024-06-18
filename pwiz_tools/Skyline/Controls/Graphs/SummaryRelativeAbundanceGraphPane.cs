﻿/*
 * Original author: Henry Sanford <henrytsanford .at. u.washington.edu>,
 *                  MacCoss Lab, Department of Genome Sciences, UW
 *
 * Copyright 2023 University of Washington - Seattle, WA
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
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using pwiz.Common.DataBinding;
using pwiz.Common.SystemUtil;
using pwiz.Skyline.Controls.GroupComparison;
using pwiz.Skyline.Controls.SeqNode;
using pwiz.Skyline.Model;
using pwiz.Skyline.Model.AuditLog;
using pwiz.Skyline.Model.Databinding;
using pwiz.Skyline.Model.Databinding.Entities;
using pwiz.Skyline.Model.DocSettings;
using pwiz.Skyline.Model.GroupComparison;
using pwiz.Skyline.Properties;
using pwiz.Skyline.Util;
using ZedGraph;
using pwiz.Skyline.Util.Extensions;
using Peptide = pwiz.Skyline.Model.Databinding.Entities.Peptide;

namespace pwiz.Skyline.Controls.Graphs
{

    public abstract class SummaryRelativeAbundanceGraphPane : SummaryBarGraphPaneBase
    {
        protected GraphData _graphData;
        private bool _areaProteinTargets;
        private bool _excludePeptideLists;
        private bool _excludeStandards;
        private readonly List<LabeledPoint> _labeledPoints;
        private static RelativeAbundanceFormatting _formattingOverride;
        protected SummaryRelativeAbundanceGraphPane(GraphSummary graphSummary)
            : base(graphSummary)
        {
            var xAxisTitle =
                Helpers.PeptideToMoleculeTextMapper.Translate(GraphsResources.SummaryIntensityGraphPane_SummaryIntensityGraphPane_Protein_Rank,
                    graphSummary.DocumentUIContainer.DocumentUI.DocumentType);
            XAxis.Title.Text = xAxisTitle;
            XAxis.Type = AxisType.Linear;
            XAxis.Scale.Max = GraphSummary.DocumentUIContainer.DocumentUI.MoleculeGroupCount;
            _areaProteinTargets = Settings.Default.AreaProteinTargets;
            _excludePeptideLists = Settings.Default.ExcludePeptideListsFromAbundanceGraph;
            _excludeStandards = Settings.Default.ExcludeStandardsFromAbundanceGraph;
            _labeledPoints = new List<LabeledPoint>();

            AxisChangeEvent += this_AxisChangeEvent;
            Settings.Default.PropertyChanged += OnLabelOverlapPropertyChange;
            graphSummary.GraphControl.EditModifierKeys = Keys.Alt;  // enable label drag with Alt key
        }

        public override void OnClose(EventArgs e)
        {
            base.OnClose(e);
            AxisChangeEvent -= this_AxisChangeEvent;
            Settings.Default.PropertyChanged -= OnLabelOverlapPropertyChange;
        }

        protected override int SelectedIndex
        {
            get { return _graphData != null ? _graphData.SelectedIndex : -1; }
        }

        protected override IdentityPath GetIdentityPath(CurveItem curveItem, int barIndex)
        {
            var pointData = (GraphPointData)curveItem[barIndex].Tag;
            return pointData.IdentityPath;
        }

        /// <summary>
        /// Have any of the settings relevant to this graph pane changed since the last update?
        /// </summary>
        /// <returns>True if relevant settings have changed, false if not</returns>
        private bool IsAbundanceGraphSettingsChanged()
        {
            var settingsChanged = false;
            if (Settings.Default.AreaProteinTargets != _areaProteinTargets)
            {
                _areaProteinTargets = Settings.Default.AreaProteinTargets;
                settingsChanged = true;
            }
            if (Settings.Default.ExcludePeptideListsFromAbundanceGraph != _excludePeptideLists)
            {
                _excludePeptideLists = Settings.Default.ExcludePeptideListsFromAbundanceGraph;
                settingsChanged = true;
            }
            if (Settings.Default.ExcludeStandardsFromAbundanceGraph != _excludeStandards)
            {
                _excludeStandards = Settings.Default.ExcludeStandardsFromAbundanceGraph;
                settingsChanged = true;
            }
            return settingsChanged;
        }

        public void ShowFormattingDialog()
        {
            using var dlg = new VolcanoPlotFormattingDlg(this,
                GraphSummary.DocumentUIContainer.DocumentUI.Settings.DataSettings.RelativeAbundanceFormatting.ColorRows,
                _graphData.PointPairList.Select(pointPair => (GraphPointData)pointPair.Tag).ToArray(),
                UpdateFormatting);
            if (dlg.ShowDialog(Program.MainWindow) == DialogResult.OK)
            {
                if (_formattingOverride != null)
                {
                    Program.MainWindow.ModifyDocument(string.Empty,
                        doc => doc.ChangeSettings(doc.Settings.ChangeDataSettings(
                            doc.Settings.DataSettings.ChangeRelativeAbundanceFormatting(_formattingOverride))),
                        AuditLogEntry.SettingsLogFunction);
                    _formattingOverride = null;
                    Program.MainWindow.UpdatePeakAreaGraph();
                }
            }
        }

        private void UpdateFormatting(IEnumerable<MatchRgbHexColor> colorRows)
        {
            var formatting = _formattingOverride ?? GraphSummary.DocumentUIContainer.DocumentUI.Settings.DataSettings.RelativeAbundanceFormatting;
            formatting = formatting.ChangeColorRows(colorRows);
            if (!Equals(formatting, _formattingOverride))
            {
                _formattingOverride = formatting;
                Program.MainWindow.UpdatePeakAreaGraph();
            }
        }

        /// <summary>
        /// Detect changes in settings shared with <see cref="FoldChangeVolcanoPlot"/> right-click menu
        /// </summary>
        public void OnLabelOverlapPropertyChange(object sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == @"GroupComparisonAvoidLabelOverlap")
                GraphSummary.UpdateUI();
            else if (e.PropertyName == @"GroupComparisonSuspendLabelLayout")
            {
                if (!Settings.Default.GroupComparisonSuspendLabelLayout)
                {
                    AdjustLabelSpacings(_labeledPoints, GraphSummary.GraphControl);
                    GraphSummary.GraphControl.Invalidate();
                }
            }
        }

        public override bool HandleMouseDownEvent(ZedGraphControl sender, MouseEventArgs mouseEventArgs)
        {
            // if Alt button is pressed this is a label drag event, no need to change the selection
            if (Control.ModifierKeys == GraphSummary.GraphControl.EditModifierKeys)
                return false;

            var ctrl = Control.ModifierKeys.HasFlag(Keys.Control); //CONSIDER allow override of modifier keys?
            int iNearest;
            var axis = GetNearestXAxis(sender, mouseEventArgs);
            if (axis != null)
            {
                iNearest = (int)axis.Scale.ReverseTransform(mouseEventArgs.X - axis.MajorTic.Size);
                if (iNearest < 0)
                {
                    return false;
                }
                ChangeSelection(iNearest, GraphSummary.StateProvider.SelectedPath, ctrl);
                return true;
            }

            IdentityPath identityPath = null;
            if (GraphSummary.GraphControl.GraphPane.IsOverLabel(new Point(mouseEventArgs.X, mouseEventArgs.Y),
                    out var labPoint))
            {
                var selectedRow = (GraphPointData)labPoint.Point.Tag;
                identityPath = selectedRow.IdentityPath;
            }
            if (FindNearestPoint(new PointF(mouseEventArgs.X, mouseEventArgs.Y), out var nearestCurve, out iNearest))
            {
                identityPath = GetIdentityPath(nearestCurve, iNearest);
            }
            if (identityPath == null)
                return false;
            ChangeSelection(iNearest, identityPath, ctrl);
            return true;
        }

        private void ChangeSelection(int selectedIndex, IdentityPath identityPath, bool ctrl)
        {
            if (ctrl)
            {
                DotPlotUtil.MultiSelect(Program.MainWindow, identityPath);
            }
            else
            {
                ChangeSelection(selectedIndex, identityPath);
            }
        }

        protected override void ChangeSelection(int selectedIndex, IdentityPath identityPath)
        {
            
            if (0 <= selectedIndex && selectedIndex < _graphData.XScalePaths.Length)
            {
                GraphSummary.StateProvider.SelectedPath = identityPath;
            }
        }

        public override void UpdateGraph(bool selectionChanged)
        {
            PeptideGroupDocNode selectedProtein = null;
            Clear();
            var selectedTreeNode = GraphSummary.StateProvider.SelectedNode as SrmTreeNode;
            if (selectedTreeNode != null)
            {
                var proteinTreeNode = selectedTreeNode.GetNodeOfType<PeptideGroupTreeNode>();
                if (proteinTreeNode != null)
                {
                    selectedProtein = proteinTreeNode.DocNode;
                }
            }

            var document = GraphSummary.DocumentUIContainer.DocumentUI;
            var graphSettings = GraphSettings.FromSettings();

            // Only create graph data (and recalculate abundances)
            // if settings have changed, the document has changed, or if it
            // is not yet created
            if (_graphData?.GraphPointList == null ||
                !ReferenceEquals(document, _graphData.Document) ||
                !Equals(graphSettings, _graphData.GraphSettings))
            {
                _graphData = CreateGraphData(document, graphSettings);
            }
            // Calculate y values and order which can change based on the
            // replicate display option or the show CV option
            _graphData.CalcDataPositions(GraphSummary.ResultsIndex, selectedProtein);

            // For proper z-order, add the selected points, then the matched points, then the unmatched points
            var selectedPoints = new PointPairList();
            if (ShowSelection)
            {
                foreach (var point in from point in _graphData.PointPairList let 
                             pointData = (GraphPointData)point.Tag where 
                             null != DotPlotUtil.GetSelectedPath(Program.MainWindow, pointData.IdentityPath) select 
                             point)
                {
                    selectedPoints.Add(point);
                }
                AddPoints(new PointPairList(selectedPoints), GraphSummary.ColorSelected, DotPlotUtil.PointSizeToFloat(PointSize.large), true, PointSymbol.Circle, true);
            }
            var pointList = _graphData.PointPairList;
            // For each valid match expression specified by the user
            var unmatchedPoints = new List<PointPair>(pointList);
            var colorRows = (_formattingOverride ?? document.Settings.DataSettings.RelativeAbundanceFormatting).ColorRows;
            foreach (var colorRow in colorRows.Where(r => r.MatchExpression != null))
            {
                var matchedPoints = pointList.Where(p =>
                {
                    var pointData = (GraphPointData)p.Tag;
                    if (colorRow.MatchExpression.Matches(document, pointData.Protein, pointData.Peptide, null, null) && !selectedPoints.Contains(p))
                    {
                        unmatchedPoints.Remove(p);
                        return true;
                    }
                    else
                        return false;
                }).ToArray();

                if (matchedPoints.Any())
                {
                    AddPoints(new PointPairList(matchedPoints), colorRow.Color, DotPlotUtil.PointSizeToFloat(colorRow.PointSize), colorRow.Labeled, colorRow.PointSymbol);
                }
            }
            AddPoints(new PointPairList(unmatchedPoints), Color.Gray, DotPlotUtil.PointSizeToFloat(PointSize.normal), false, PointSymbol.Circle);
            UpdateAxes();
            if (Settings.Default.GroupComparisonAvoidLabelOverlap)
                AdjustLabelSpacings(_labeledPoints, GraphSummary.GraphControl);
            else
                DotPlotUtil.AdjustLabelLocations(_labeledPoints, GraphSummary.GraphControl.GraphPane.YAxis.Scale, GraphSummary.GraphControl.GraphPane.Rect.Height);
        }

        private void this_AxisChangeEvent(GraphPane pane)
        {
            if (Settings.Default.GroupComparisonAvoidLabelOverlap && !Settings.Default.GroupComparisonSuspendLabelLayout)
            {
                AdjustLabelSpacings(_labeledPoints, GraphSummary.GraphControl);
            }
        }

        private void AddPoints(PointPairList points, Color color, float size, bool labeled, PointSymbol pointSymbol, bool selected = false)
        {
            var symbolType = DotPlotUtil.PointSymbolToSymbolType(pointSymbol);

            LineItem lineItem;
            if (DotPlotUtil.HasOutline(pointSymbol))
            {
                lineItem = new LineItem(null, points, Color.Black, symbolType)
                {
                    Line = { IsVisible = false },
                    Symbol = { Border = { IsVisible = false }, Fill = new Fill(color), Size = size, IsAntiAlias = true }
                };
            }
            else
            {
                lineItem = new LineItem(null, points, Color.Black, symbolType)
                {
                    Line = { IsVisible = false },
                    Symbol = { Border = { IsVisible = true, Color = color }, Size = size, IsAntiAlias = true }
                };
            }

            if (labeled)
            {
                foreach (var point in points)
                {
                    var pointData = point.Tag as GraphPointData;
                    if (pointData == null)
                    {
                        continue;
                    }
                    var label = DotPlotUtil.CreateLabel(point, pointData.Protein, pointData.Peptide, color, size);
                    _labeledPoints.Add(new LabeledPoint(selected) {Point = point, Label = label, Curve = lineItem });
                    GraphObjList.Add(label);
                }
            }
            CurveList.Add(lineItem);
        }

        protected abstract GraphData CreateGraphData(SrmDocument document, GraphSettings graphSettings);

        protected virtual void UpdateAxes()
        {
            if (GraphSummary.DocumentUIContainer.DocumentUI.HasSmallMolecules)
            {
                XAxis.Title.Text = GraphsResources.SummaryRelativeAbundanceGraphPane_UpdateAxes_Molecule_Rank;
            }
            else
            {
                XAxis.Title.Text = Settings.Default.AreaProteinTargets ? GraphsResources.SummaryIntensityGraphPane_SummaryIntensityGraphPane_Protein_Rank : GraphsResources.AreaPeptideGraphPane_UpdateAxes_Peptide_Rank;
            }
            const double xAxisGrace = 0;
            XAxis.Scale.MaxGrace = xAxisGrace;
            XAxis.Scale.MinGrace = xAxisGrace;
            YAxis.Scale.MinGrace = xAxisGrace;
            YAxis.Scale.MaxGrace = xAxisGrace;
            YAxis.Scale.MaxAuto = true;
            YAxis.Scale.MinAuto = true;
            XAxis.Scale.MaxAuto = true;
            XAxis.Scale.MinAuto = true;
            if (Settings.Default.AreaLogScale )
            {
                YAxis.Title.Text = TextUtil.SpaceSeparate(GraphsResources.SummaryPeptideGraphPane_UpdateAxes_Log, YAxis.Title.Text);
                YAxis.Type = AxisType.Log;
            }
            else
            {
                YAxis.Type = AxisType.Linear;
                if (_graphData.MinY.HasValue)
                {
                    if (!IsZoomed && !YAxis.Scale.MinAuto)
                        YAxis.Scale.MinAuto = true;
                }
                else
                {
                    YAxis.Scale.MinAuto = false;
                    FixedYMin = YAxis.Scale.Min = 0;
                    YAxis.Scale.Max = _graphData.MaxY * 1.05;
                }
            }
            var aggregateOp = GraphValues.AggregateOp.FromCurrentSettings();
            if (aggregateOp.Cv)
            {
                YAxis.Title.Text = aggregateOp.AnnotateTitle(YAxis.Title.Text);
            }

            if (!_graphData.MinY.HasValue && aggregateOp.Cv)
            {
                if (_graphData.MaxCvSetting != 0)
                {
                    YAxis.Scale.MaxAuto = false;
                    YAxis.Scale.Max = _graphData.MaxCvSetting;
                }
                else if (!IsZoomed && !YAxis.Scale.MaxAuto)
                {
                    YAxis.Scale.MaxAuto = true;
                }
            }
            else if (_graphData.MaxValueSetting != 0 || _graphData.MinValueSetting != 0)
            {
                if (_graphData.MaxValueSetting != 0)
                {
                    YAxis.Scale.MaxAuto = false;
                    YAxis.Scale.Max = _graphData.MaxValueSetting;
                }
                if (_graphData.MinValueSetting != 0)
                {
                    YAxis.Scale.MinAuto = false;
                    YAxis.Scale.Min = _graphData.MinValueSetting;
                    if (!_graphData.MinY.HasValue)
                        FixedYMin = YAxis.Scale.Min;
                }
            }

            AxisChange();
        }

        private static bool ContainsStandards(PeptideGroupDocNode nodeGroupPep)
        {
            return nodeGroupPep.Children.Cast<PeptideDocNode>().Any(IsStandard);
        }

        private static bool IsStandard(PeptideDocNode pepDocNode)
        {
            return pepDocNode.GlobalStandardType != null;
        }

        public void OnSuspendLayout(object sender, EventArgs eventArgs)
        {
            Settings.Default.GroupComparisonSuspendLabelLayout = !Settings.Default.GroupComparisonSuspendLabelLayout;
        }


        public abstract class GraphData : Immutable
        {
            protected GraphData(SrmDocument document, GraphSettings graphSettings)
            {
                Document = document;
                GraphSettings = graphSettings;
                var schema = SkylineDataSchema.MemoryDataSchema(document, DataSchemaLocalizer.INVARIANT);
                bool anyMolecules = document.HasSmallMolecules;
                // Build the list of points to show.
                var listPoints = new List<GraphPointData>();
                foreach (var nodeGroupPep in document.MoleculeGroups)
                {
                    if (nodeGroupPep.IsPeptideList && Settings.Default.ExcludePeptideListsFromAbundanceGraph &&
                        !anyMolecules)
                    {
                        continue;
                    }

                    if (Settings.Default.ExcludeStandardsFromAbundanceGraph && ContainsStandards(nodeGroupPep))
                    {
                        continue;
                    }

                    if (Settings.Default.AreaProteinTargets && !anyMolecules)
                    {
                        var path = new IdentityPath(IdentityPath.ROOT, nodeGroupPep.PeptideGroup);
                        var protein = new Protein(schema, path);
                        listPoints.Add(new GraphPointData(protein));
                    }
                    else
                    {
                        foreach (PeptideDocNode nodePep in nodeGroupPep.Children)
                        {
                            var pepPath = new IdentityPath(nodeGroupPep.PeptideGroup,
                                nodePep.Peptide);
                            var peptide = new Peptide(schema, pepPath);
                            listPoints.Add(new GraphPointData(peptide));
                        }
                    }
                }
                GraphPointList = listPoints;
            }

            public SrmDocument Document { get; }
            public GraphSettings GraphSettings { get; }

            public void CalcDataPositions(int iResult, PeptideGroupDocNode selectedProtein)
            {
                // Init calculated values
                var xscalePaths = new List<IdentityPath>();
                double maxY = 0;
                var minY = double.MaxValue;
                var selectedIndex = -1;

                var pointPairList = new PointPairList();

                foreach (var dataPoint in GraphPointList)
                {
                    double groupMaxY = 0;
                    var groupMinY = double.MaxValue;
                    // ReSharper disable DoNotCallOverridableMethodsInConstructor
                    var pointPair = CreatePointPair(dataPoint, ref groupMaxY, ref groupMinY, iResult);
                    // ReSharper restore DoNotCallOverridableMethodsInConstructor
                    pointPairList.Add(pointPair);
                    maxY = Math.Max(maxY, groupMaxY);
                    minY = Math.Min(minY, groupMinY);
                }

                pointPairList.Sort(CompareYValues);
                for (var i = 0; i < pointPairList.Count; i++)
                {
                    // Save the selected index and its y extent
                    var dataPoint = (GraphPointData)pointPairList[i].Tag;
                    if (ReferenceEquals(selectedProtein?.PeptideGroup, dataPoint.IdentityPath.GetIdentity(0)))
                    {
                        selectedIndex = i;
                    }
                    // 1-index the proteins
                    pointPairList[i].X = i + 1;
                    xscalePaths.Add(dataPoint.IdentityPath);
                }
                PointPairList = pointPairList;
                XScalePaths = xscalePaths.ToArray();
                SelectedIndex = selectedIndex - 1;
                MaxY = maxY;
                if (minY != double.MaxValue)
                {
                    MinY = minY;
                }
            }

            private static int CompareYValues(PointPair p1, PointPair p2)
            {
                return Comparer.Default.Compare(p2.Y, p1.Y);
            }
            public List<GraphPointData> GraphPointList;
            public PointPairList PointPairList { get; private set; }
            public IdentityPath[] XScalePaths { get; private set; }
            public double MaxY { get; private set; }
            public double? MinY { get; private set; }
            public int SelectedIndex { get; private set; }

            public virtual double MaxValueSetting { get { return 0; } }
            public virtual double MinValueSetting { get { return 0; } }
            public virtual double MaxCvSetting { get { return 0; } }

            protected virtual PointPair CreatePointPair(GraphPointData pointData, ref double maxY, ref double minY, int? resultIndex)
            {
                var yValue = GetY(pointData, resultIndex);
                var pointPair = new PointPair(0, yValue)
                    { Tag = pointData };
                maxY = Math.Max(maxY, pointPair.Y);
                minY = Math.Min(minY, pointPair.Y);
                return pointPair;
            }

            private static double GetY(GraphPointData pointData, int? resultIndex)
            {
                Statistics statValues;
                if (RTLinearRegressionGraphPane.ShowReplicate == ReplicateDisplay.single && resultIndex.HasValue)
                {
                    statValues = new Statistics(pointData.ReplicateAreas[resultIndex.Value]);
                }
                else
                {
                    statValues = new Statistics(pointData.ReplicateAreas.SelectMany(grouping => grouping));
                }

                if (Settings.Default.ShowPeptideCV)
                {
                    var cv = statValues.StdDev() / statValues.Mean();
                    return cv;
                }

                if (statValues.Length == 0)
                {
                    return 0;
                }

                if (RTLinearRegressionGraphPane.ShowReplicate == ReplicateDisplay.best)
                {
                    return statValues.Max();
                }

                return statValues.Mean();
            }
        }

        public class GraphPointData 
        {
            public GraphPointData(Protein protein)
            {
                Protein = protein;
                IdentityPath = protein.IdentityPath;
                ReplicateAreas = protein.GetProteinAbundances()
                    .ToLookup(kvp => kvp.Key, kvp => kvp.Value.TransitionSummed);
            }

            public GraphPointData(Peptide peptide)
            {
                Protein = peptide.Protein;
                Peptide = peptide;
                IdentityPath = peptide.IdentityPath;
                ReplicateAreas = peptide.Results.Values.ToLookup(
                    peptideResult => peptideResult.ResultFile.Replicate.ReplicateIndex,
                    peptideResult => peptideResult.GetQuantificationResult()?.NormalizedArea?.Raw ?? 0);
            }
            public Protein Protein { get; }
            public Peptide Peptide { get; }
            public ILookup<int, double> ReplicateAreas { get; set; }
            public IdentityPath IdentityPath { get; set; }
        }

        public class GraphSettings : Immutable
        {
            public bool AreaProteinTargets { get; private set; }

            public GraphSettings ChangeAreaProteinTargets(bool value)
            {
                return ChangeProp(ImClone(this), im => im.AreaProteinTargets = value);
            }

            public bool ExcludePeptideLists { get; private set; }

            public GraphSettings ChangeExcludePeptideLists(bool value)
            {
                return ChangeProp(ImClone(this), im => im.ExcludePeptideLists = value);
            }

            public bool ExcludeStandards { get; private set; }

            public GraphSettings ChangeExcludeStandards(bool value)
            {
                return ChangeProp(ImClone(this), im => im.ExcludeStandards = value);
            }

            public static GraphSettings FromSettings()
            {
                return new GraphSettings
                {
                    AreaProteinTargets = Settings.Default.AreaProteinTargets,
                    ExcludePeptideLists = Settings.Default.ExcludePeptideListsFromAbundanceGraph,
                    ExcludeStandards = Settings.Default.ExcludeStandardsFromAbundanceGraph
                };
            }

            protected bool Equals(GraphSettings other)
            {
                return AreaProteinTargets == other.AreaProteinTargets
                       && ExcludePeptideLists == other.ExcludePeptideLists
                       && ExcludeStandards == other.ExcludeStandards;
            }

            public override bool Equals(object obj)
            {
                if (ReferenceEquals(null, obj)) return false;
                if (ReferenceEquals(this, obj)) return true;
                if (obj.GetType() != GetType()) return false;
                return Equals((GraphSettings)obj);
            }

            public override int GetHashCode()
            {
                unchecked
                {
                    var hashCode = AreaProteinTargets.GetHashCode();
                    hashCode = (hashCode * 397) ^ ExcludePeptideLists.GetHashCode();
                    hashCode = (hashCode * 397) ^ ExcludeStandards.GetHashCode();
                    return hashCode;
                }
            }
        }
    }
}

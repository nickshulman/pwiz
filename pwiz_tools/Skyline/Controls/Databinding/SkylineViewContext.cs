/*
 * Original author: Nicholas Shulman <nicksh .at. u.washington.edu>,
 *                  MacCoss Lab, Department of Genome Sciences, UW
 *
 * Copyright 2012 University of Washington - Seattle, WA
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
using System.ComponentModel;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Threading;
using System.Windows.Forms;
using System.Xml.Serialization;
using pwiz.Common.Collections;
using pwiz.Common.DataBinding;
using pwiz.Common.DataBinding.Controls;
using pwiz.Common.DataBinding.Controls.Editor;
using pwiz.Common.DataBinding.Layout;
using pwiz.Common.SystemUtil;
using pwiz.Skyline.Alerts;
using pwiz.Skyline.Controls.Databinding.RowActions;
using pwiz.Skyline.Model;
using pwiz.Skyline.Model.AuditLog;
using pwiz.Skyline.Model.Databinding;
using pwiz.Skyline.Model.Databinding.Collections;
using pwiz.Skyline.Model.Databinding.Entities;
using pwiz.Skyline.Model.Hibernate;
using pwiz.Skyline.Properties;
using pwiz.Skyline.Util;
using pwiz.Skyline.Util.Extensions;

namespace pwiz.Skyline.Controls.Databinding
{
    public class SkylineViewContext : BaseSkylineViewContext
    {
        private ViewChangeListener _viewChangeListener;
        public SkylineViewContext(SkylineDataSchema dataSchema, IEnumerable<RowSourceInfo> rowSources) : base(dataSchema, rowSources)
        {
        }

        public SkylineViewContext(ColumnDescriptor parentColumn, IRowSource rowSource)
            : base(
                parentColumn.DataSchema,
                new[] {new RowSourceInfo(rowSource, GetDefaultViewInfo(parentColumn))})
        {
        }

        public SkylineDataSchema SkylineDataSchema { get { return (SkylineDataSchema) DataSchema; } }

        // ReSharper disable LocalizableElement
        public static ViewInfo GetDefaultViewInfo(ColumnDescriptor columnDescriptor)
        {
            ViewSpec viewSpec = GetDefaultViewSpec(columnDescriptor);
            bool addAnnotations = false;
            if (columnDescriptor.PropertyType == typeof (TransitionResult))
            {
                viewSpec = viewSpec.SetColumns(new[]
                {
                    new ColumnSpec().SetName("PrecursorResult.PeptideResult.ResultFile.Replicate"),
                    new ColumnSpec().SetName("Note"),
                    new ColumnSpec().SetName("RetentionTime"),
                    new ColumnSpec().SetName("Fwhm"),
                    new ColumnSpec().SetName("StartTime"),
                    new ColumnSpec().SetName("EndTime"),
                    new ColumnSpec().SetName("Area"),
                    new ColumnSpec().SetName("Background"),
                    new ColumnSpec().SetName("AreaRatio"),
                    new ColumnSpec().SetName("Height"),
                    new ColumnSpec().SetName("PeakRank"),
                });
                addAnnotations = true;
            } 
            else if (columnDescriptor.PropertyType == typeof (PrecursorResult))
            {
                viewSpec = viewSpec.SetColumns(new[]
                {
                    new ColumnSpec().SetName("PeptideResult.ResultFile.Replicate"),
                    new ColumnSpec().SetName("Note"), 
                    new ColumnSpec().SetName("PrecursorPeakFoundRatio"),
                    new ColumnSpec().SetName("BestRetentionTime"),
                    new ColumnSpec().SetName("MaxFwhm"),
                    new ColumnSpec().SetName("MinStartTime"),
                    new ColumnSpec().SetName("MaxEndTime"),
                    new ColumnSpec().SetName("TotalArea"),
                    new ColumnSpec().SetName("TotalBackground"),
                    new ColumnSpec().SetName("TotalAreaRatio"),
                    new ColumnSpec().SetName("MaxHeight"),
                    new ColumnSpec().SetName("LibraryDotProduct"),
                    new ColumnSpec().SetName("IsotopeDotProduct"),
                });
                addAnnotations = true;
            }
            else if (columnDescriptor.PropertyType == typeof (PeptideResult))
            {
                viewSpec = viewSpec.SetColumns(new[]
                {
                    new ColumnSpec().SetName("ResultFile.Replicate"),
                    new ColumnSpec().SetName("PeptidePeakFoundRatio"), 
                    new ColumnSpec().SetName("PeptideRetentionTime"), 
                    new ColumnSpec().SetName("RatioToStandard"), 
                });
                
                var skylineDataSchema = (SkylineDataSchema)columnDescriptor.DataSchema;
                PropertyPath propertyPathReplicate = PropertyPath.Parse("ResultFile.Replicate");
                viewSpec = viewSpec.SetColumns(viewSpec.Columns.Concat(
                    skylineDataSchema.GetAnnotations(typeof (Replicate))
                        .Select(pd => new ColumnSpec(propertyPathReplicate.Property(pd.Name))))
                    );
            }
            else
            {
                var columnsToRemove = new HashSet<PropertyPath> 
                    {PropertyPath.Root.Property("Locator")};
                bool addRoot = false;
                bool docHasCustomIons = ((SkylineDataSchema)columnDescriptor.DataSchema).Document.CustomIonCount != 0;
                bool docHasOnlyCustomIons = docHasCustomIons && ((SkylineDataSchema)columnDescriptor.DataSchema).Document.PeptideCount == 0;
                
                if (columnDescriptor.PropertyType == typeof(Protein))
                {
                    columnsToRemove.Add(PropertyPath.Root.Property("Name"));
                    columnsToRemove.Add(PropertyPath.Root.Property(nameof(Protein.AutoSelectPeptides)));
                    columnsToRemove.Add(PropertyPath.Root.Property(nameof(Protein.ProteinSequenceCoverage)));
                    if (docHasOnlyCustomIons)
                    {
                        // Peptide-oriented fields that make no sense in a small molecule context
                        columnsToRemove.Add(PropertyPath.Root.Property("Accession"));
                        columnsToRemove.Add(PropertyPath.Root.Property("PreferredName"));
                        columnsToRemove.Add(PropertyPath.Root.Property("Gene"));
                        columnsToRemove.Add(PropertyPath.Root.Property("Species"));
                        columnsToRemove.Add(PropertyPath.Root.Property("Sequence"));
                    }
                    addRoot = true;
                }
                else if (columnDescriptor.PropertyType == typeof(Model.Databinding.Entities.Peptide))
                {
                    columnsToRemove.Add(PropertyPath.Root.Property(nameof(Model.Databinding.Entities.Peptide.AutoSelectPrecursors)));
                    columnsToRemove.Add(PropertyPath.Root.Property("Sequence"));
                    columnsToRemove.Add(PropertyPath.Root.Property("SequenceLength"));
                    columnsToRemove.Add(PropertyPath.Root.Property("PreviousAa"));
                    columnsToRemove.Add(PropertyPath.Root.Property("NextAa"));
                    columnsToRemove.Add(PropertyPath.Root.Property("RetentionTimeCalculatorScore"));
                    columnsToRemove.Add(PropertyPath.Root.Property("DocumentLocation"));
                    columnsToRemove.Add(PropertyPath.Root.Property("ConcentrationMultiplier"));
                    columnsToRemove.Add(PropertyPath.Root.Property("InternalStandardConcentration"));
                    columnsToRemove.Add(PropertyPath.Root.Property("CalibrationCurve"));
                    columnsToRemove.Add(PropertyPath.Root.Property("FiguresOfMerit"));
                    columnsToRemove.Add(PropertyPath.Root.Property("NormalizationMethod"));
                    columnsToRemove.Add(PropertyPath.Root.Property(nameof(Model.Databinding.Entities.Peptide.AutoSelectPrecursors)));
                    columnsToRemove.Add(PropertyPath.Root.Property(nameof(Model.Databinding.Entities.Peptide.AttributeGroupId)));
                    columnsToRemove.Add(PropertyPath.Root.Property(nameof(Model.Databinding.Entities.Peptide.SurrogateExternalStandard)));
                    foreach (var prop in MoleculeAccessionNumbers.PREFERRED_ACCESSION_TYPE_ORDER)
                        columnsToRemove.Add(PropertyPath.Root.Property(prop)); // By default don't show CAS, InChI etc
                    if (docHasOnlyCustomIons)
                    {
                        // Peptide-oriented fields that make no sense in a small molecule context
                        columnsToRemove.Add(PropertyPath.Root.Property("ModifiedSequence"));
                        columnsToRemove.Add(PropertyPath.Root.Property(nameof(Model.Databinding.Entities.Peptide.FirstPosition)));
                        columnsToRemove.Add(PropertyPath.Root.Property(nameof(Model.Databinding.Entities.Peptide.LastPosition)));
                        columnsToRemove.Add(PropertyPath.Root.Property("MissedCleavages"));
                    }
                    if (!docHasCustomIons)
                    {
                        columnsToRemove.Add(PropertyPath.Root.Property("MoleculeName"));
                        columnsToRemove.Add(PropertyPath.Root.Property("MoleculeFormula"));
                        columnsToRemove.Add(PropertyPath.Root.Property("ExplicitRetentionTime"));
                        columnsToRemove.Add(PropertyPath.Root.Property("ExplicitRetentionTimeWindow"));
                    }
                    addRoot = true;
                }
                else if (columnDescriptor.PropertyType == typeof(Precursor))
                {
                    if (docHasOnlyCustomIons)
                    {
                        columnsToRemove.Add(PropertyPath.Root.Property("ModifiedSequence"));
                        columnsToRemove.Add(PropertyPath.Root.Property("IsDecoy"));
                    }
                    if (!docHasCustomIons)
                    {
                        columnsToRemove.Add(PropertyPath.Root.Property("IonName"));
                        columnsToRemove.Add(PropertyPath.Root.Property("IonFormula"));
                        columnsToRemove.Add(PropertyPath.Root.Property("NeutralFormula"));
                        columnsToRemove.Add(PropertyPath.Root.Property("Adduct"));
                    }
                    columnsToRemove.Add(PropertyPath.Root.Property("CollisionEnergy"));
                    columnsToRemove.Add(PropertyPath.Root.Property("ResultSummary"));
                    columnsToRemove.Add(PropertyPath.Root.Property("NeutralMass"));
                    columnsToRemove.Add(PropertyPath.Root.Property("TransitionCount"));
                    columnsToRemove.Add(PropertyPath.Root.Property("DeclusteringPotential"));
                    columnsToRemove.Add(PropertyPath.Root.Property("LibraryScore1"));
                    columnsToRemove.Add(PropertyPath.Root.Property("LibraryScore2"));
                    columnsToRemove.Add(PropertyPath.Root.Property("LibraryScore3"));
                    columnsToRemove.Add(PropertyPath.Root.Property("IsDecoy"));
                    columnsToRemove.Add(PropertyPath.Root.Property("DecoyMzShift"));
                    columnsToRemove.Add(PropertyPath.Root.Property("ExplicitDriftTimeMsec"));
                    columnsToRemove.Add(PropertyPath.Root.Property("ExplicitCollisionalCrossSection"));
                    columnsToRemove.Add(PropertyPath.Root.Property("ExplicitIonMobility"));
                    columnsToRemove.Add(PropertyPath.Root.Property("ExplicitIonMobilityUnits"));
                    columnsToRemove.Add(PropertyPath.Root.Property("ExplicitCompensationVoltage"));
                    columnsToRemove.Add(PropertyPath.Root.Property("PrecursorConcentration"));
                    columnsToRemove.Add(PropertyPath.Root.Property(nameof(Precursor.AutoSelectTransitions)));
                    columnsToRemove.Add(PropertyPath.Root.Property(nameof(Precursor.TargetQualitativeIonRatio)));
                    columnsToRemove.Add(PropertyPath.Root.Property(nameof(Precursor.LibraryIonMobility)));
                    columnsToRemove.Add(PropertyPath.Root.Property(nameof(Precursor.SpectrumFilter)));
                    addRoot = true;
                }
                else if (columnDescriptor.PropertyType == typeof(Model.Databinding.Entities.Transition))
                {
                    columnsToRemove.Add(PropertyPath.Root.Property("ExplicitCollisionEnergy"));
                    columnsToRemove.Add(PropertyPath.Root.Property("ExplicitDriftTimeHighEnergyOffsetMsec"));
                    columnsToRemove.Add(PropertyPath.Root.Property("ExplicitIonMobilityHighEnergyOffset"));
                    columnsToRemove.Add(PropertyPath.Root.Property("ExplicitCompensationVoltage"));
                    columnsToRemove.Add(PropertyPath.Root.Property("ExplicitDeclusteringPotential"));
                    columnsToRemove.Add(PropertyPath.Root.Property("ExplicitSLens"));
                    columnsToRemove.Add(PropertyPath.Root.Property("ExplicitConeVoltage"));
                    columnsToRemove.Add(PropertyPath.Root.Property("ResultSummary"));
                    columnsToRemove.Add(PropertyPath.Root.Property("ProductNeutralMass"));
                    columnsToRemove.Add(PropertyPath.Root.Property("FragmentIonType"));
                    columnsToRemove.Add(PropertyPath.Root.Property("FragmentIonOrdinal"));
                    columnsToRemove.Add(PropertyPath.Root.Property("CleavageAa"));
                    columnsToRemove.Add(PropertyPath.Root.Property("LossNeutralMass"));
                    columnsToRemove.Add(PropertyPath.Root.Property("LossFormulas"));
                    columnsToRemove.Add(PropertyPath.Root.Property("IsDecoy"));
                    columnsToRemove.Add(PropertyPath.Root.Property("ProductDecoyMzShift"));
                    columnsToRemove.Add(PropertyPath.Root.Property("IsotopeDistIndex"));
                    columnsToRemove.Add(PropertyPath.Root.Property("IsotopeDistRank"));
                    columnsToRemove.Add(PropertyPath.Root.Property("FullScanFilterWidth"));
                    columnsToRemove.Add(PropertyPath.Root.Property("LibraryRank"));
                    columnsToRemove.Add(PropertyPath.Root.Property("LibraryIntensity"));
                    columnsToRemove.Add(PropertyPath.Root.Property("IsotopeDistProportion"));

                    if (docHasOnlyCustomIons)
                    {
                        columnsToRemove.Add(PropertyPath.Root.Property("FragmentIon")); // Not interesting - only one product per precursor for small molecules
                        columnsToRemove.Add(PropertyPath.Root.Property("Losses")); // Doesn't mean anything for non-peptides
                    }
                    if (!docHasCustomIons)
                    {
                        // Stuff that only applies to small molecules
                        columnsToRemove.Add(PropertyPath.Root.Property("ProductIonFormula"));
                        columnsToRemove.Add(PropertyPath.Root.Property("ProductNeutralFormula"));
                        columnsToRemove.Add(PropertyPath.Root.Property("ProductAdduct"));
                    }
                    addRoot = true;
                }
                else if (columnDescriptor.PropertyType == typeof(Replicate))
                {
                    columnsToRemove.Add(PropertyPath.Root.Property("Name"));
                    columnsToRemove.Add(PropertyPath.Root.Property("SampleDilutionFactor"));
                    columnsToRemove.Add(PropertyPath.Root.Property(nameof(Replicate.BatchName)));
                    addRoot = true;
                }
                viewSpec = viewSpec.SetSublistId(GetReplicateSublist(columnDescriptor.PropertyType));
                if (addRoot)
                {
                    viewSpec = viewSpec.SetColumns(new[] { new ColumnSpec(PropertyPath.Root) }.Concat(viewSpec.Columns));
                }
                viewSpec = viewSpec.SetColumns(viewSpec.Columns
                    .Where(columnSpec => !columnsToRemove.Contains(columnSpec.PropertyPath)));
            }
            if (addAnnotations)
            {
                var skylineDataSchema = (SkylineDataSchema) columnDescriptor.DataSchema;
                viewSpec = viewSpec.SetColumns(viewSpec.Columns.Concat(
                    skylineDataSchema.GetAnnotations(columnDescriptor.PropertyType)
                        .Select(pd => new ColumnSpec(PropertyPath.Root.Property(pd.Name)))));
            }
            return new ViewInfo(columnDescriptor, viewSpec).ChangeViewGroup(ViewGroup.BUILT_IN);
        }

        public static PropertyPath GetReplicateSublist(Type rowType)
        {
            if (rowType == typeof(SkylineDocument))
            {
                return PropertyPath.Root.Property("Replicates").LookupAllItems();
            }
            if (rowType == typeof(Replicate))
            {
                return PropertyPath.Root.Property("Files").LookupAllItems();
            }

            if (rowType == typeof(Protein))
            {
                return PropertyPath.Root.Property(nameof(Protein.Results)).LookupAllItems().Property("Value")
                    .Property(nameof(Replicate.Files));
            }
            return PropertyPath.Root.Property("Results").LookupAllItems();
        }
        // ReSharper restore LocalizableElement

        public void SetRowSources(IList<RowSourceInfo> rowSources)
        {
            RowSources = rowSources;
        }

        public static IEnumerable<RowSourceInfo> GetDocumentGridRowSources(SkylineDataSchema dataSchema)
        {
            bool proteomic = dataSchema.DefaultUiMode == UiModes.PROTEOMIC;
            yield return MakeRowSource<Protein>(dataSchema, 
                proteomic ? Resources.SkylineViewContext_GetDocumentGridRowSources_Proteins : DatabindingResources.SkylineViewContext_GetDocumentGridRowSources_Molecule_Lists,
                () => new Proteins(dataSchema));
            yield return MakeRowSource<Model.Databinding.Entities.Peptide>(dataSchema, 
                proteomic ? Resources.SkylineViewContext_GetDocumentGridRowSources_Peptides : Resources.SkylineViewContext_GetDocumentGridRowSources_Molecules,
                () => new Peptides(dataSchema, new[] {IdentityPath.ROOT}));
            yield return MakeRowSource<Precursor>(dataSchema, Resources.SkylineViewContext_GetDocumentGridRowSources_Precursors, 
                () => new Precursors(dataSchema, new[] { IdentityPath.ROOT }));
            yield return MakeRowSource<Model.Databinding.Entities.Transition>(dataSchema, Resources.SkylineViewContext_GetDocumentGridRowSources_Transitions, 
                () => new Transitions(dataSchema, new[] { IdentityPath.ROOT }));
            yield return MakeRowSource<Replicate>(dataSchema, Resources.SkylineViewContext_GetDocumentGridRowSources_Replicates, 
                () => new ReplicateList(dataSchema));
            yield return new RowSourceInfo(typeof(SkylineDocument), new StaticRowSource(new SkylineDocument[0]), new ViewInfo[0]);
        }

        private static RowSourceInfo MakeRowSource<T>(SkylineDataSchema dataSchema, string name, Func<IRowSource> rowProvider) where T : SkylineObject
        {
            var parentColumn = ColumnDescriptor.RootColumn(dataSchema, typeof(T));
            var viewInfo = new ViewInfo(parentColumn, GetDefaultViewInfo(parentColumn).GetViewSpec().SetName(name));
            return new RowSourceInfo(rowProvider(), viewInfo);
        }

        protected override TColumn InitializeColumn<TColumn>(TColumn column, PropertyDescriptor propertyDescriptor)
        {
            column = base.InitializeColumn(column, propertyDescriptor);
            // Disable sorting by clicking on column headers because DataboundGridForm shows
            // context menu on left click.
            column.SortMode = DataGridViewColumnSortMode.Programmatic;
            if (IsInvariantLanguage())
            {
                // When outputting floats and doubles as "Invariant" (e.g. for external tools) 
                // always use the Round Trip number format
                var type = propertyDescriptor.PropertyType;
                if (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(FormattableList<>))
                {
                    type = type.GenericTypeArguments[0];
                }
                if (type == typeof (float) ||
                    type == typeof (float?) ||
                    type == typeof (double) ||
                    type == typeof (double?))
                {
                    column.DefaultCellStyle.Format = Formats.RoundTrip;
                }
            }
            return column;
        }

        private static readonly IDictionary<string, Tuple<int, int>> _imageIndexes = new Dictionary<string, Tuple<int, int>>
        {
            // ReSharper disable RedundantNameQualifier
            // ReSharper disable AssignNullToNotNullAttribute
            {typeof (Model.Databinding.Entities.Protein).FullName, Tuple.Create(1, 6)},
            {typeof (Model.Databinding.Entities.Peptide).FullName, Tuple.Create(2, 7)},
            {typeof (Model.Databinding.Entities.Precursor).FullName, Tuple.Create(3, 3)},
            {typeof (Model.Databinding.Entities.Transition).FullName, Tuple.Create(4, 4)},
            {typeof (Model.Databinding.Entities.Replicate).FullName, Tuple.Create(5, 5)}
            // ReSharper restore AssignNullToNotNullAttribute
            // ReSharper restore RedundantNameQualifier
        };

        public override Image[] GetImageList()
        {
            return new []
            {
                Resources.Folder,
                Resources.Protein,
                Resources.Peptide,
                Resources.TransitionGroup,
                Resources.Fragment,
                Resources.Replicate,
                Resources.MoleculeList,
                Resources.Molecule,
            };
        }

        public override int GetImageIndex(ViewSpec viewSpec)
        {
            Tuple<int, int> imageIndex;
            if (_imageIndexes.TryGetValue(viewSpec.RowSource, out imageIndex))
            {
                if (DataSchema.NormalizeUiMode(viewSpec.UiMode) == UiModes.PROTEOMIC)
                {
                    return imageIndex.Item1;
                }
                return imageIndex.Item2;
            }
            return -1;
        }

        public override event Action ViewsChanged
        {
            add
            {
                if (_viewChangeListener == null)
                {
                    _viewChangeListener = new ViewChangeListener(Settings.Default.PersistedViews, SkylineDataSchema);
                }
                _viewChangeListener.AddListener(value);
            }
            remove
            {
                _viewChangeListener.RemoveListener(value);
            }
        }

        private class ViewChangeListener : IDocumentChangeListener
        {
            private readonly PersistedViews _persistedViews;
            private readonly SkylineDataSchema _skylineDataSchema;
            private ViewSpecList _documentViews;
            private HashSet<Action> _listeners;
            public ViewChangeListener(PersistedViews persistedViews, SkylineDataSchema skylineDataSchema)
            {
                _persistedViews = persistedViews;
                _skylineDataSchema = skylineDataSchema;
            }

            public void AddListener(Action listener)
            {
                if (_listeners == null)
                {
                    if (null != _persistedViews)
                    {
                        _persistedViews.Changed += PersistedViewsOnChanged;
                    }
                    if (null != _skylineDataSchema)
                    {
                        _documentViews = _skylineDataSchema.Document.Settings.DataSettings.ViewSpecList;
                        _skylineDataSchema.Listen(this);
                    }
                    _listeners = new HashSet<Action>();
                }
                if (!_listeners.Add(listener))
                {
                    throw new InvalidOperationException(@"Listener already added");
                }
            }

            public void RemoveListener(Action listener)
            {
                if (!_listeners.Remove(listener))
                {
                    throw new InvalidOperationException(@"No such listener");
                }
                if (_listeners.Count == 0)
                {
                    if (null != _persistedViews)
                    {
                        _persistedViews.Changed -= PersistedViewsOnChanged;
                    }
                    if (null != _skylineDataSchema)
                    {
                        _skylineDataSchema.Unlisten(this);
                    }
                    _listeners = null;
                }
            }

            private void PersistedViewsOnChanged()
            {
                FireChanged();
            }

            void IDocumentChangeListener.DocumentOnChanged(object sender, DocumentChangedEventArgs args)
            {
                var viewSpecList = _skylineDataSchema.Document.Settings.DataSettings.ViewSpecList;
                if (!Equals(viewSpecList, _documentViews))
                {
                    _documentViews = viewSpecList;
                    FireChanged();
                }
            }

            private void FireChanged()
            {
                foreach (var listener in _listeners.ToArray())
                {
                    listener();
                }
            }
        }

        protected void DeleteSkylineDocNodes(BoundDataGridView dataGridView, IEnumerable<SkylineDocNode> docNodes)
        {
            DeleteNodesAction.DeleteSkylineDocNodes(SkylineDataSchema.SkylineWindow, dataGridView, docNodes);
        }

        public override IEnumerable<IUiModeInfo> AvailableUiModes
        {
            get
            {
                var skylineWindow = SkylineDataSchema.SkylineWindow;
                if (skylineWindow == null)
                {
                    return UiModes.AllModes;
                }

                return UiModes.AvailableModes(SkylineDataSchema.SkylineWindow.ModeUI);
            }
        }

        public override bool CanDisplayView(ViewSpec viewSpec)
        {
            if (!base.CanDisplayView(viewSpec))
            {
                return false;
            }

            var reportUiMode = DataSchema.NormalizeUiMode(viewSpec.UiMode);
            switch (DataSchema.DefaultUiMode)
            {
                case UiModes.PROTEOMIC:
                    return reportUiMode != UiModes.SMALL_MOLECULES;
                case UiModes.SMALL_MOLECULES:
                    return reportUiMode != UiModes.PROTEOMIC;
            }

            return true;
        }

        protected override ViewEditor CreateViewEditor(ViewGroup viewGroup, ViewSpec viewSpec)
        {
            var viewEditor = base.CreateViewEditor(viewGroup, viewSpec);
            viewEditor.Alphabetical = Settings.Default.AlphabeticalReportEditor;
            viewEditor.Closed += ViewEditorClosed;
            return viewEditor;
        }

        private void ViewEditorClosed(object sender, EventArgs eventArgs)
        {
            var viewEditor = sender as ViewEditor;
            if (viewEditor?.DialogResult == DialogResult.OK)
            {
                Settings.Default.AlphabeticalReportEditor = viewEditor.Alphabetical;
            }
        }

        protected override SkylineWindow GetSkylineWindow()
        {
            return SkylineDataSchema.SkylineWindow;
        }
    }
}

﻿using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Threading;
using System.Windows.Forms;
using pwiz.Common.Collections;
using pwiz.Common.DataBinding;
using pwiz.Common.Spectra;
using pwiz.Common.SystemUtil;
using pwiz.Skyline.Alerts;
using pwiz.Skyline.Controls.Databinding;
using pwiz.Skyline.FileUI;
using pwiz.Skyline.Model;
using pwiz.Skyline.Model.AuditLog;
using pwiz.Skyline.Model.Databinding;
using pwiz.Skyline.Model.DocSettings;
using pwiz.Skyline.Model.Results;
using pwiz.Skyline.Model.Results.Spectra;
using pwiz.Skyline.Properties;
using pwiz.Skyline.Util.Extensions;

namespace pwiz.Skyline.Controls.Spectra
{
    public partial class SpectraGridForm : DataboundGridForm
    {
        private SkylineDataSchema _dataSchema;
        private SequenceTree _sequenceTree;
        private HashSet<IdentityPath> _selectedPrecursorPaths = new HashSet<IdentityPath>();
        private List<SpectrumClassRow> _spectrumClasses;
        private BindingList<SpectrumClassRow> _bindingList;

        private Dictionary<DataFileItem, SpectrumMetadataList> _spectrumLists =
            new Dictionary<DataFileItem, SpectrumMetadataList>();
        private readonly SpectrumReader _spectrumReader;
        private List<DataFileItem> _dataFileList = new List<DataFileItem>();
        private HashSet<MsDataFileUri> _dataFileSet = new HashSet<MsDataFileUri>();
        private ImmutableList<SpectrumClassColumn> _allSpectrumClassColumns;
        private bool _updatePending;

        public SpectraGridForm(SkylineWindow skylineWindow)
        {
            InitializeComponent();
            SkylineWindow = skylineWindow;
            _allSpectrumClassColumns = SpectrumClassColumn.ALL;
            checkedListBoxSpectrumClassColumns.Items.AddRange(_allSpectrumClassColumns.ToArray());
            for (int i = 0; i < checkedListBoxSpectrumClassColumns.Items.Count; i++)
            {
                checkedListBoxSpectrumClassColumns.SetItemCheckState(i, CheckState.Indeterminate);
            }
            checkedListBoxSpectrumClassColumns.ItemCheck += CheckedListBoxSpectrumClassColumns_OnItemCheck;
            _dataSchema = new SkylineDataSchema(skylineWindow, SkylineDataSchema.GetLocalizedSchemaLocalizer());
            BindingListSource.QueryLock = _dataSchema.QueryLock;
            _spectrumClasses = new List<SpectrumClassRow>();
            _bindingList = new BindingList<SpectrumClassRow>(_spectrumClasses);
            var viewContext = new SkylineViewContext(_dataSchema, MakeRowSourceInfos());
            BindingListSource.SetViewContext(viewContext);
            Text = TabText = Resources.SpectraGridForm_SpectraGridForm_Spectrum_Grid;
            DataboundGridControl.Parent.Controls.Remove(DataboundGridControl);
            splitContainer1.Panel2.Controls.Add(DataboundGridControl);
            DataboundGridControl.Dock = DockStyle.Fill;
            _spectrumReader = new SpectrumReader(this);
        }

        private void CheckedListBoxSpectrumClassColumns_OnItemCheck(object sender, ItemCheckEventArgs e)
        {
            QueueUpdateSpectrumRows();
        }

        private void QueueUpdateSpectrumRows()
        {
            if (_updatePending)
            {
                return;
            }

            _updatePending = true;
            BeginInvoke(new Action(UpdateSpectrumRows));
        }

        private IList<RowSourceInfo> MakeRowSourceInfos()
        {
            var rootColumn = ColumnDescriptor.RootColumn(_dataSchema, typeof(SpectrumClassRow));
            return ImmutableList.Singleton(new RowSourceInfo(BindingListRowSource.Create(_bindingList),
                new ViewInfo(rootColumn, GetDefaultViewSpec())));
        }

        public IEnumerable<SpectrumClassColumn> GetActiveClassColumns()
        {
            var tolerance = SkylineWindow.Document.Settings.TransitionSettings.Instrument.MzMatchTolerance;
            for (int i = 0; i < _allSpectrumClassColumns.Count; i++)
            {
                var checkedState = checkedListBoxSpectrumClassColumns.GetItemCheckState(i);
                if (checkedState == CheckState.Unchecked)
                {
                    continue;
                }

                var classColumn = _allSpectrumClassColumns[i];
                if (checkedState == CheckState.Indeterminate)
                {
                    if (!_spectrumLists.Values.Any(spectrumList =>
                            HasMultipleValues(spectrumList, classColumn, tolerance)))
                    {
                        continue;
                    }
                }

                yield return classColumn;
            }
        }

        private ViewSpec GetDefaultViewSpec()
        {
            var columns = new List<ColumnSpec>();
            var tolerance = SkylineWindow.Document.Settings.TransitionSettings.Instrument.MzMatchTolerance;
            var ppSpectrumClass = PropertyPath.Root.Property(nameof(SpectrumClassRow.Properties));
            for (int i = 0; i < _allSpectrumClassColumns.Count; i++)
            {
                var checkedState = checkedListBoxSpectrumClassColumns.GetItemCheckState(i);
                if (checkedState == CheckState.Unchecked)
                {
                    continue;
                }

                var classColumn = _allSpectrumClassColumns[i];
                if (checkedState == CheckState.Indeterminate)
                {
                    if (!_spectrumLists.Values.Any(spectrumList =>
                            HasMultipleValues(spectrumList, classColumn, tolerance)))
                    {
                        continue;
                    }
                }

                columns.Add(new ColumnSpec(ppSpectrumClass.Concat(classColumn.PropertyPath)));
            }
            columns.Add(new ColumnSpec(PropertyPath.Root.Property(nameof(SpectrumClassRow.Files)).DictionaryValues()));
            return new ViewSpec().SetName(Resources.SpectraGridForm_GetDefaultViewSpec_Default).SetColumns(columns);
        }

        private bool HasMultipleValues(SpectrumMetadataList spectra, SpectrumClassColumn column, double tolerance)
        {
            if (spectra.Ms1Spectra.Select(column.GetValue).Distinct().Skip(1).Any())
            {
                return true;
            }

            object lastValue = null;
            double? lastPrecursor = null;
            foreach (var keyValuePair in spectra.SpectraByPrecursor)
            {
                var precursor = keyValuePair.Key;
                foreach (var spectrum in keyValuePair.Value)
                {
                    var value = column.GetValue(spectrum);
                    if (lastPrecursor.HasValue && precursor - lastPrecursor.Value <= tolerance)
                    {
                        if (!Equals(value, lastValue))
                        {
                            return true;
                        }
                    }

                    lastPrecursor = precursor;
                    lastValue = value;
                }
            }
            return false;
        }

        public SkylineWindow SkylineWindow { get; }

        protected override void OnHandleCreated(EventArgs e)
        {
            base.OnHandleCreated(e);
            SkylineWindow.DocumentUIChangedEvent += SkylineWindow_DocumentUIChangedEvent;
            OnDocumentChanged();
        }

        private void SkylineWindow_DocumentUIChangedEvent(object sender, DocumentChangedEventArgs e)
        {
            OnDocumentChanged();
        }

        private void OnDocumentChanged()
        {
            SetSequenceTree(SkylineWindow.SequenceTree);
            QueueUpdateSpectrumRows();
        }

        private void SetSequenceTree(SequenceTree sequenceTree)
        {
            if (ReferenceEquals(_sequenceTree, sequenceTree))
            {
                return;
            }

            if (null != _sequenceTree)
            {
                _sequenceTree.AfterSelect -= SequenceTreeOnAfterSelect;
            }

            _sequenceTree = sequenceTree;
            if (null != _sequenceTree)
            {
                _sequenceTree.AfterSelect += SequenceTreeOnAfterSelect;
                SetSelectedPrecursorPaths(GetSelectedPrecursorPaths());
            }
        }

        private void SequenceTreeOnAfterSelect(object sender, TreeViewEventArgs args)
        {
            SetSelectedPrecursorPaths(GetSelectedPrecursorPaths());
        }

        public void SetSelectedPrecursorPaths(HashSet<IdentityPath> paths)
        {
            if (paths.SetEquals(_selectedPrecursorPaths))
            {
                return;
            }

            _selectedPrecursorPaths = paths;
            QueueUpdateSpectrumRows();
        }

        public IEnumerable<SpectrumClassColumn> GetEnabledClassColumns()
        {
            for (int i = 0; i < checkedListBoxSpectrumClassColumns.Items.Count; i++)
            {
                if (checkedListBoxSpectrumClassColumns.GetItemCheckState(i) != CheckState.Unchecked)
                {
                    yield return _allSpectrumClassColumns[i];
                }
            }
        }

        public void UpdateSpectrumRows()
        {
            _updatePending = false;
            var document = SkylineWindow.DocumentUI;
            AddReplicatesWithMetadata(document);
            var spectrumClasses = new Dictionary<SpectrumClassKey, SpectrumClassRow>();
            var classColumns = ImmutableList.ValueOf(GetEnabledClassColumns());
            IList<TransitionGroupDocNode> transitionGroupDocNodes = null;
            if (_selectedPrecursorPaths.Count > 0)
            {
                transitionGroupDocNodes = _selectedPrecursorPaths.Select(path => document.FindNode(path))
                    .OfType<TransitionGroupDocNode>().ToList();
            }

            foreach (var dataFileItem in _dataFileList)
            {
                if (!_spectrumLists.TryGetValue(dataFileItem, out var spectrumList ))
                {
                    continue;
                }

                IReadOnlyList<SpectrumMetadata> spectra = spectrumList;
                if (transitionGroupDocNodes != null)
                {
                    spectra = ImmutableList.ValueOf(spectra.Where(spectrum =>
                        FindMatchingTransitionGroups(document.Settings.TransitionSettings, spectrum, transitionGroupDocNodes).Any()));
                }

                foreach (var spectrumGroup in spectra.GroupBy(spectrum=>new SpectrumClassKey(classColumns, spectrum)))
                {
                    if (!spectrumClasses.TryGetValue(spectrumGroup.Key, out var spectrumClass))
                    {
                        spectrumClass = new SpectrumClassRow(new SpectrumClass(spectrumGroup.Key));
                        spectrumClasses.Add(spectrumGroup.Key, spectrumClass);
                    }
                    spectrumClass.Files.Add(dataFileItem.ToString(), new FileSpectrumInfo(_dataSchema, dataFileItem.MsDataFileUri, spectrumGroup));
                }
            }

            _spectrumClasses.Clear();
            _spectrumClasses.AddRange(spectrumClasses.Values);
            _bindingList.ResetBindings();
            UpdateViewContext();
        }

        private void AddReplicatesWithMetadata(SrmDocument document)
        {
            if (!document.Settings.HasResults)
            {
                return;
            }

            var resultFileMetadatas = document.Settings.MeasuredResults.GetResultFileMetadatas();
            if (!resultFileMetadatas.Any())
            {
                return;
            }

            foreach (var chromatogramSet in document.Settings.MeasuredResults.Chromatograms)
            {
                foreach (var chromFileInfo in chromatogramSet.MSDataFileInfos)
                {
                    if (!resultFileMetadatas.TryGetValue(chromFileInfo.FilePath, out var resultFileMetadata))
                    {
                        continue;
                    }

                    var key = new DataFileItem(chromatogramSet.Name, chromFileInfo.FilePath);
                    if (_spectrumLists.ContainsKey(key))
                    {
                        continue;
                    }

                    _spectrumLists.Add(key, new SpectrumMetadataList(resultFileMetadata.SpectrumMetadatas));
                    _dataFileList.Add(key);
                    listBoxFiles.Items.Add(key);
                }
            }
        }

        private void UpdateViewContext()
        {
            var viewContext = BindingListSource.ViewContext as SkylineViewContext;
            if (viewContext == null)
            {
                return;
            }

            viewContext.SetRowSources(MakeRowSourceInfos());
            if (null != BindingListSource.ViewInfo && null != BindingListSource.ViewInfo.ViewGroup && ViewGroup.BUILT_IN.Id.Equals(BindingListSource.ViewInfo.ViewGroup.Id))
            {
                var viewName = BindingListSource.ViewInfo.ViewGroup.Id.ViewName(BindingListSource.ViewInfo.Name);
                var newViewInfo = viewContext.GetViewInfo(viewName);
                if (null != newViewInfo && !ColumnsEqual(newViewInfo, BindingListSource.ViewInfo))
                {
                    BindingListSource.SetView(newViewInfo, BindingListSource.RowSource);
                }
            }
        }

        private IEnumerable<TransitionGroupDocNode> FindMatchingTransitionGroups(TransitionSettings transitionSettings, SpectrumMetadata spectrumMetadata, IList<TransitionGroupDocNode> transitionGroups)
        {
            if (1 == spectrumMetadata.MsLevel)
            {
                if (!transitionSettings.FullScan.IsEnabledMs)
                {
                    yield break;
                }

                foreach (var transitionGroup in transitionGroups)
                {
                    if (transitionGroup.Transitions.Any(t => t.IsMs1))
                    {
                        yield return transitionGroup;
                    }
                }

                yield break;
            }
            var tolerance = transitionSettings.Instrument.MzMatchTolerance;
            foreach (var spectrumPrecursor in spectrumMetadata.GetPrecursors(1))
            {
                foreach (var transitionGroup in transitionGroups)
                {
                    if (0 == transitionGroup.PrecursorMz.CompareTolerant(spectrumPrecursor.PrecursorMz, tolerance))
                    {
                        yield return transitionGroup;
                    }
                }
            }
        }

        protected override void OnHandleDestroyed(EventArgs e)
        {
            SkylineWindow.DocumentUIChangedEvent -= SkylineWindow_DocumentUIChangedEvent;
            SetSequenceTree(null);
            _spectrumReader.RemoveAll();
            base.OnHandleDestroyed(e);
        }

        private void btnBrowse_Click(object sender, EventArgs e)
        {
            using (var dataSrcDlg = new OpenDataSourceDialog(Settings.Default.RemoteAccountList))
            {
                if (!string.IsNullOrEmpty(SkylineWindow.DocumentFilePath))
                {
                    dataSrcDlg.InitialDirectory =
                        new MsDataFilePath(Path.GetDirectoryName(SkylineWindow.DocumentFilePath));
                }

                if (dataSrcDlg.ShowDialog(this) != DialogResult.OK)
                {
                    return;
                }

                foreach (var msDataFileUri in dataSrcDlg.DataSources)
                {
                    AddFile(msDataFileUri);
                }
            }
        }

        public void AddFile(MsDataFileUri msDataFileUri)
        {
            if (_dataFileSet.Add(msDataFileUri))
            {
                var dataFileItem = new DataFileItem(null, msDataFileUri);
                _dataFileList.Add(dataFileItem);
                if (!_spectrumLists.ContainsKey(dataFileItem))
                {
                    _spectrumReader.AddFile(msDataFileUri);
                    listBoxFiles.Items.Add(dataFileItem);
                }
            }
        }

        public void SetSpectra(MsDataFileUri dataFile, IList<SpectrumMetadata> spectra)
        {
            _spectrumLists[new DataFileItem(null, dataFile)] = new SpectrumMetadataList(spectra);
            QueueUpdateSpectrumRows();
            statusPanel.Visible = false;
        }

        public HashSet<IdentityPath> GetSelectedPrecursorPaths()
        {
            return GetPrecursorPathsFromSelectedPaths(SkylineWindow.DocumentUI,
                SkylineWindow.SequenceTree.SelectedPaths);
        }

        public HashSet<IdentityPath> GetPrecursorPathsFromSelectedPaths(SrmDocument document,
            IEnumerable<IdentityPath> selectedPaths)
        {
            HashSet<IdentityPath> result = new HashSet<IdentityPath>();
            foreach (var identityPath in selectedPaths)
            {
                if (identityPath.Length == 1)
                {
                    var peptideGroupDocNode = (PeptideGroupDocNode) document.FindNode(identityPath);
                    if (peptideGroupDocNode != null)
                    {
                        result.UnionWith(peptideGroupDocNode.Molecules.SelectMany(molecule =>
                            molecule.TransitionGroups.Select(tg => new IdentityPath(peptideGroupDocNode.PeptideGroup,
                                molecule.Peptide, tg.TransitionGroup))));
                    }
                }
                else if (identityPath.Length == 2)
                {
                    var molecule = (PeptideDocNode) document.FindNode(identityPath);
                    if (molecule != null)
                    {
                        result.UnionWith(molecule.TransitionGroups.Select(tg =>
                            new IdentityPath(identityPath, tg.TransitionGroup)));
                    }
                }
                else if (identityPath.Length > 2)
                {
                    result.Add(identityPath.GetPathTo(2));
                }
            }

            return result;
        }

        private void UpdateProgress(string text, int value)
        {
            statusPanel.Visible = true;
            lblStatus.Text = text;
            progressBar1.Value = value;
        }

        public new bool IsComplete()
        {
            return _spectrumReader.IsComplete() && DataboundGridControl.BindingListSource.IsComplete;
        }

        class SpectrumReader
        {
            private SpectraGridForm _form;
            private List<MsDataFileUri> _files;
            private bool _isRunning;

            public SpectrumReader(SpectraGridForm form)
            {
                _form = form;
                _files = new List<MsDataFileUri>();
            }

            public void AddFile(MsDataFileUri file)
            {
                lock (this)
                {
                    if (_files.Contains(file))
                    {
                        return;
                    }
                    _files.Add(file);
                    if (!_isRunning)
                    {
                        ActionUtil.RunAsync(ReadSpectra, @"Spectrum Reader");
                        _isRunning = true;
                    }
                }
            }

            public void RemoveFile(MsDataFileUri file)
            {
                lock (this)
                {
                    _files.Remove(file);
                }
            }

            public void RemoveAll()
            {
                lock (this)
                {
                    _files.Clear();
                }
            }

            public void Stop()
            {
                lock (this)
                {
                    _files = null;
                    Monitor.PulseAll(this);
                }
            }

            private void ReadSpectra()
            {
                while (true)
                {
                    MsDataFileUri msDataFileUri;
                    lock (this)
                    {
                        if (_files.Count == 0)
                        {
                            CommonActionUtil.SafeBeginInvoke(_form, () => _form.statusPanel.Visible = false);
                            _isRunning = false;
                            return;
                        }

                        msDataFileUri = _files.First();
                    }

                    if (ReadSpectraFromFile(msDataFileUri))
                    {
                        lock (this)
                        {
                            _files.Remove(msDataFileUri);
                        }
                    }
                }
            }

            private bool ReadSpectraFromFile(MsDataFileUri file)
            {
                string message = string.Format(Resources.SpectrumReader_ReadSpectraFromFile_Reading_spectra_from__0_, file.GetFileName());
                CommonActionUtil.SafeBeginInvoke(_form, () => _form.UpdateProgress(message, 0));
                using (var msDataFile = file.OpenMsDataFile(true, false, false, false, true))
                {
                    var spectra = new List<SpectrumMetadata>();
                    int spectrumCount = msDataFile.SpectrumCount;
                    int lastProgress = 0;
                    for (int i = 0; i < spectrumCount; i++)
                    {
                        lock (this)
                        {
                            if (!Equals(file, _files.FirstOrDefault()))
                            {
                                return false;
                            }
                        }

                        int progress = i * 100 / spectrumCount;
                        if (progress != lastProgress)
                        {
                            lastProgress = progress;
                            CommonActionUtil.SafeBeginInvoke(_form, () => _form.UpdateProgress(message, progress));
                        }

                        var spectrumMetadata = msDataFile.GetSpectrumMetadata(i);
                        if (spectrumMetadata != null)
                        {
                            spectra.Add(spectrumMetadata);
                        }
                    }

                    CommonActionUtil.SafeBeginInvoke(_form, () => _form.SetSpectra(file, spectra));
                    return true;
                }
            }

            public bool IsComplete()
            {
                lock (this)
                {
                    return !_isRunning;
                }
            }
        }

        private class DataFileItem
        {
            public DataFileItem(string replicateName, MsDataFileUri dataFileUri)
            {
                ReplicateName = replicateName;
                MsDataFileUri = dataFileUri;
            }

            public string ReplicateName { get; }
            public MsDataFileUri MsDataFileUri { get; }
            public override string ToString()
            {
                return ReplicateName ?? MsDataFileUri.GetFileName();
            }

            protected bool Equals(DataFileItem other)
            {
                return ReplicateName == other.ReplicateName && Equals(MsDataFileUri, other.MsDataFileUri);
            }

            public override bool Equals(object obj)
            {
                if (ReferenceEquals(null, obj)) return false;
                if (ReferenceEquals(this, obj)) return true;
                if (obj.GetType() != this.GetType()) return false;
                return Equals((DataFileItem) obj);
            }

            public override int GetHashCode()
            {
                unchecked
                {
                    return ((ReplicateName != null ? ReplicateName.GetHashCode() : 0) * 397) ^ (MsDataFileUri != null ? MsDataFileUri.GetHashCode() : 0);
                }
            }
        }

        private void btnAddSpectrumFilter_Click(object sender, EventArgs e)
        {
            IList<SpectrumClassRow> spectrumClassRows = new List<SpectrumClassRow>();
            if (DataboundGridControl.DataGridView.SelectedRows.Count == 0)
            {
                var spectrumClass = (BindingListSource.Current as RowItem)?.Value as SpectrumClassRow;
                if (spectrumClass != null)
                {
                    spectrumClassRows.Add(spectrumClass);
                }
            }
            else
            {
                foreach (var dataGridViewRow in DataboundGridControl.DataGridView.SelectedRows.Cast<DataGridViewRow>())
                {
                    if (dataGridViewRow.Index >= 0 && dataGridViewRow.Index < BindingListSource.Count)
                    {
                        var spectrumClass =
                            (BindingListSource[dataGridViewRow.Index] as RowItem)?.Value as SpectrumClassRow;
                        if (spectrumClass != null)
                        {
                            spectrumClassRows.Add(spectrumClass);
                        }
                    }
                }
            }

            if (spectrumClassRows.Any())
            {
                AddSpectrumFilters(spectrumClassRows);
            }
        }

        public void AddSpectrumFilters(IList<SpectrumClassRow> spectrumClassRows)
        {
            var filters = new List<SpectrumClassFilter>();
            var transitionIdentityPathLists = new List<ICollection<IdentityPath>>();
            var activeClassColumns = GetActiveClassColumns().ToList();
            using (var longWaitDlg = new LongWaitDlg()
                   {
                       Message = Resources.SpectraGridForm_AddSpectrumFilters_Examining_filters
                   })
            {
                var document = SkylineWindow.DocumentUI;
                longWaitDlg.PerformWork(this, 1000, broker =>
                {
                    for (int i = 0; i < spectrumClassRows.Count; i++)
                    {
                        broker.ProgressValue = 100 * i / spectrumClassRows.Count;
                        broker.CancellationToken.ThrowIfCancellationRequested();
                        var spectrumClassRow = spectrumClassRows[i];
                        var filter = MakeFilter(spectrumClassRow, activeClassColumns);
                        if (filter.FilterSpecs.Count == 0)
                        {
                            continue;
                        }

                        var firstSpectrumMetadata = spectrumClassRow.Files.SelectMany(entry => entry.Value.GetSpectra())
                            .FirstOrDefault();
                        ICollection<IdentityPath> precursorPaths = _selectedPrecursorPaths;
                        var transitionSettings = document.Settings.TransitionSettings;
                        if (firstSpectrumMetadata != null)
                        {
                            precursorPaths = precursorPaths.Where(path =>
                            {
                                var docNode = (TransitionGroupDocNode) document.FindNode(path);
                                return docNode != null &&
                                       FindMatchingTransitionGroups(transitionSettings, firstSpectrumMetadata,
                                               new[] {docNode})
                                           .Any();
                            }).ToList();
                        }

                        filters.Add(filter);
                        transitionIdentityPathLists.Add(precursorPaths);
                    }
                });
            }

            if (filters.Count == 0)
            {
                if (spectrumClassRows.Count == 0)
                {
                    MessageDlg.Show(this, Resources.SpectraGridForm_AddSpectrumFilters_The_selected_row_does_not_have_any_filters);
                }
                else
                {
                    MessageDlg.Show(this, Resources.SpectraGridForm_AddSpectrumFilters_The_selected_rows_do_not_have_any_filters);
                }

                return;
            }

            if (transitionIdentityPathLists.All(list => list.Count == 0))
            {
                MessageDlg.Show(this, Resources.SpectraGridForm_AddSpectrumFilters_There_were_no_matching_precursors_to_add_any_filters_to_);
                return;
            }

            lock (SkylineWindow.GetDocumentChangeLock())
            {
                SkylineWindow.ModifyDocument(Resources.SpectraGridForm_AddSpectrumFilters_Change_spectrum_filter, doc =>
                        AddFilters(doc, filters, transitionIdentityPathLists),
                    docPair => AuditLogEntry.CreateSimpleEntry(MessageType.added_spectrum_filter,
                        docPair.NewDocumentType));
            }
        }

        private SrmDocument AddFilters(SrmDocument doc, IList<SpectrumClassFilter> filters,
            IList<ICollection<IdentityPath>> transitionGroupIdentityPaths)
        {
            SrmDocument newDocument = doc;
            using (var longWaitDlg = new LongWaitDlg(SkylineWindow))
            {
                longWaitDlg.PerformWork(this, 1000, broker =>
                {
                    for (int i = 0; i < filters.Count; i++)
                    {
                        broker.CancellationToken.ThrowIfCancellationRequested();
                        broker.ProgressValue = i * 100 / filters.Count;
                        doc = SkylineWindow.EditMenu.ChangeSpectrumFilter(doc, transitionGroupIdentityPaths[i],
                            filters[i], true);
                    }

                    newDocument = doc;
                });
            }

            return newDocument;
        }

        public SpectrumClassFilter MakeFilter(SpectrumClassRow row, IList<SpectrumClassColumn> activeClassColumns)
        {
            var filterSpecs = new List<FilterSpec>();
            foreach (var classColumn in activeClassColumns)
            {
                object value = classColumn.GetValue(row.Properties);
                FilterPredicate filterPredicate;
                filterPredicate = FilterPredicate.CreateFilterPredicate(FilterOperations.OP_EQUALS, value);
                filterSpecs.Add(new FilterSpec(classColumn.PropertyPath, filterPredicate));
            }
            return new SpectrumClassFilter(filterSpecs);
        }

        private void checkedListBoxSpectrumClassColumns_ItemCheck(object sender, ItemCheckEventArgs e)
        {
            switch (e.CurrentValue)
            {
                case CheckState.Checked:
                    e.NewValue = CheckState.Unchecked;
                    break;

                case CheckState.Indeterminate:
                    e.NewValue = CheckState.Checked;
                    break;

                case CheckState.Unchecked:
                    e.NewValue = CheckState.Indeterminate;
                    break;
            }
        }
    }
}

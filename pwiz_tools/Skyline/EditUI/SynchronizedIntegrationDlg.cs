﻿/*
 * Original author: Kaipo Tamura <kaipot .at. u.washington.edu>,
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
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Windows.Forms;
using pwiz.Common.Collections;
using pwiz.Skyline.Alerts;
using pwiz.Skyline.Controls.Graphs;
using pwiz.Skyline.Model;
using pwiz.Skyline.Model.DocSettings;
using pwiz.Skyline.Model.Results;
using pwiz.Skyline.Model.RetentionTimes;
using pwiz.Skyline.Properties;
using pwiz.Skyline.Util;

namespace pwiz.Skyline.EditUI
{
    public partial class SynchronizedIntegrationDlg : ModeUIInvariantFormEx  // This dialog is the same in all UI modes
    {
        private readonly SkylineWindow _skylineWindow;
        private readonly bool _originalAlignRtPrediction;
        private readonly AlignmentTarget _originalAlignFile;

        private int _idxLastSelected = -1;

        private Tuple<ReplicateValue, HashSet<object>> _memory = Tuple.Create((ReplicateValue)null, new HashSet<object>());

        private SrmDocument Document => _skylineWindow.Document;
        private GroupByItem SelectedGroupBy => (GroupByItem) comboGroupBy.SelectedItem;

        public string GroupByPersistedString => SelectedGroupBy.PersistedString;

        public string GroupBy
        {
            get => SelectedGroupBy.ToString();
            set
            {
                var idx = GetItemIndex(value, false);
                if (idx.HasValue)
                    comboGroupBy.SelectedIndex = idx.Value;
            }
        }

        public bool IsAll => listSync.Items.Count > 0 && listSync.CheckedItems.Count == listSync.Items.Count;

        public IEnumerable<string> Targets
        {
            get => listSync.CheckedItems.Cast<object>().Select(o => o.ToString());
            set => SetCheckedItems(value.ToHashSet());
        }
        public IEnumerable<string> TargetsInvariant => listSync.CheckedItems.Cast<object>().Select(o => Convert.ToString(o, CultureInfo.InvariantCulture));

        public IEnumerable<string> GroupByOptions => comboGroupBy.Items.Cast<GroupByItem>().Select(item => item.ToString());
        public IEnumerable<string> TargetOptions => listSync.Items.Cast<object>().Select(o => o.ToString());

        public SynchronizedIntegrationDlg(SkylineWindow skylineWindow)
        {
            InitializeComponent();

            _skylineWindow = skylineWindow;
            _originalAlignRtPrediction = skylineWindow.AlignToRtPrediction;
            _originalAlignFile = skylineWindow.AlignmentTarget;

            var groupByReplicates = new GroupByItem(null);
            comboGroupBy.Items.Add(groupByReplicates);
            comboGroupBy.Items.AddRange(ReplicateValue.GetGroupableReplicateValues(Document).Select(v => new GroupByItem(v)).ToArray());

            if (!Document.GetSynchronizeIntegrationChromatogramSets().Any())
            {
                // Synchronized integration is off, select everything
                comboGroupBy.SelectedIndex = 0;
                SetCheckedItems(TargetOptions.ToHashSet());
            }
            else
            {
                var settingsIntegration = Document.Settings.TransitionSettings.Integration;
                comboGroupBy.SelectedIndex = GetItemIndex(settingsIntegration.SynchronizedIntegrationGroupBy, true) ?? 0;
                SetCheckedItems((settingsIntegration.SynchronizedIntegrationAll ? TargetOptions : settingsIntegration.SynchronizedIntegrationTargets).ToHashSet());
            }

            var alignItems = AlignmentTarget.GetOptions(Document)
                .Select(target => new AlignItem(target.ToString(), target)).Prepend(AlignItem.None).ToList();
            var selectedItem = new AlignItem(_skylineWindow.AlignmentTarget);
            if (!alignItems.Contains(selectedItem))
            {
                alignItems.Insert(1, selectedItem);
            }
            comboAlign.Items.AddRange(alignItems.ToArray());
            comboAlign.SelectedItem = selectedItem;
        }

        private int? GetItemIndex(string s, bool persistedString)
        {
            if (string.IsNullOrEmpty(s))
                return null;

            for (var i = 0; i < comboGroupBy.Items.Count; i++)
            {
                var item = (GroupByItem)comboGroupBy.Items[i];
                if (persistedString && Equals(s, item.ReplicateValue?.ToPersistedString()) ||
                    !persistedString && Equals(s, item.ToString()))
                {
                    return i;
                }
            }
            return null;
        }

        private void SetCheckedItems(ICollection<string> items)
        {
            for (var i = 0; i < listSync.Items.Count; i++)
                listSync.SetItemChecked(i, items != null && items.Contains(listSync.Items[i].ToString()));
        }

        private void comboGroupBy_SelectedIndexChanged(object sender, EventArgs e)
        {
            var annotationCalc = new AnnotationCalculator(Document);
            var newItems = SelectedGroupBy.GetItems(Document, annotationCalc).ToArray();
            if (Enumerable.SequenceEqual(listSync.Items.Cast<object>(), newItems))
                return;

            _idxLastSelected = -1;

            listSync.Items.Clear();
            listSync.Items.AddRange(newItems);

            var selectedChroms = _memory.Item1 == null
                ? Document.MeasuredResults.Chromatograms.Where(chromSet => _memory.Item2.Contains(chromSet.Name)).ToHashSet()
                : Document.MeasuredResults.Chromatograms.Where(chromSet => _memory.Item2.Contains(_memory.Item1.GetValue(annotationCalc, chromSet) ?? string.Empty)).ToHashSet();

            var toCheck = new HashSet<string>();
            if (string.IsNullOrEmpty(SelectedGroupBy.PersistedString))
            {
                // replicates
                toCheck = selectedChroms.Select(chromSet => chromSet.Name).ToHashSet();
            }
            else
            {
                // annotation
                foreach (var item in newItems)
                {
                    var thisChroms = Document.MeasuredResults.Chromatograms.Where(chromSet =>
                        Equals(item, SelectedGroupBy.ReplicateValue.GetValue(annotationCalc, chromSet) ?? string.Empty)).ToArray();
                    var selectCount = thisChroms.Count(chromSet => selectedChroms.Contains(chromSet));
                    if (selectCount == thisChroms.Length)
                    {
                        toCheck.Add(item.ToString());
                    }
                    else if (selectCount != 0)
                    {
                        toCheck.Clear();
                        break;
                    }
                }
            }

            listSync.ItemCheck -= listSync_ItemCheck;
            cbSelectAll.CheckedChanged -= cbSelectAll_CheckedChanged;
            SetCheckedItems(toCheck);
            cbSelectAll.Checked = listSync.CheckedItems.Count > 0;
            listSync.ItemCheck += listSync_ItemCheck;
            cbSelectAll.CheckedChanged += cbSelectAll_CheckedChanged;
        }

        private void cbSelectAll_CheckedChanged(object sender, EventArgs e)
        {
            listSync.ItemCheck -= listSync_ItemCheck;
            for (var i = 0; i < listSync.Items.Count; i++)
                listSync.SetItemChecked(i, cbSelectAll.Checked);
            listSync.ItemCheck += listSync_ItemCheck;

            _memory = Tuple.Create(SelectedGroupBy.ReplicateValue, listSync.CheckedItems.Cast<object>().ToHashSet());
        }

        private void listSync_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (ModifierKeys == Keys.Shift && _idxLastSelected != - 1 && _idxLastSelected != listSync.SelectedIndex)
            {
                var start = Math.Min(_idxLastSelected, listSync.SelectedIndex);
                var end = Math.Max(_idxLastSelected, listSync.SelectedIndex);
                for (var i = start; i <= end; i++)
                    listSync.SetItemChecked(i, listSync.GetItemChecked(listSync.SelectedIndex));
            }
            _idxLastSelected = listSync.SelectedIndex;
        }

        private void listSync_ItemCheck(object sender, ItemCheckEventArgs e)
        {
            var checkedItems = listSync.CheckedItems.Cast<object>().ToHashSet();
            if (e.NewValue == CheckState.Checked)
                checkedItems.Add(listSync.Items[e.Index]);
            else
                checkedItems.Remove(listSync.Items[e.Index]);

            cbSelectAll.CheckedChanged -= cbSelectAll_CheckedChanged;
            var anyChecked = checkedItems.Count > 0;
            if (!cbSelectAll.Checked && anyChecked)
                cbSelectAll.Checked = true;
            else if (cbSelectAll.Checked && !anyChecked)
                cbSelectAll.Checked = false;
            cbSelectAll.CheckedChanged += cbSelectAll_CheckedChanged;

            _memory = Tuple.Create(SelectedGroupBy.ReplicateValue, checkedItems);
        }

        private void comboAlign_SelectedIndexChanged(object sender, EventArgs e)
        {
            _skylineWindow.AlignmentTarget = comboAlign.SelectedItem as AlignmentTarget;
        }

        private void btnOk_Click(object sender, EventArgs e)
        {
            OkDialog();
        }

        public void OkDialog()
        {
            DialogResult = DialogResult.OK;
        }

        private void btnCancel_Click(object sender, EventArgs e)
        {
            _skylineWindow.AlignToRtPrediction = _originalAlignRtPrediction;
            _skylineWindow.AlignmentTarget = _originalAlignFile;
        }

        #region Functional test support

        public AlignItem SelectedAlignItem => (AlignItem)comboAlign.SelectedItem;

        public bool SelectNone()
        {
            foreach (AlignItem item in comboAlign.Items)
            {
                if (Equals(item.IsNone))
                {
                    comboAlign.SelectedItem = item;
                    return true;
                }
            }
            return false;
        }

        public bool SelectAlignRt()
        {
            foreach (AlignmentTarget item in comboAlign.Items)
            {
                if (item.RtValueType == RtValueType.IRT)
                {
                    comboAlign.SelectedItem = item;
                    return true;
                }
            }
            return false;
        }
        #endregion

        private class GroupByItem
        {
            public ReplicateValue ReplicateValue { get; }

            public GroupByItem(ReplicateValue replicateValue)
            {
                ReplicateValue = replicateValue;
            }

            public IEnumerable<object> GetItems(SrmDocument doc, AnnotationCalculator annotationCalc)
            {
                return ReplicateValue == null
                    ? doc.Settings.MeasuredResults.Chromatograms.Select(c => c.Name)
                    : doc.Settings.MeasuredResults.Chromatograms
                        .Select(chromSet => ReplicateValue.GetValue(annotationCalc, chromSet))
                        .Distinct()
                        .OrderBy(o => o, CollectionUtil.ColumnValueComparer)
                        .Select(o => o ?? string.Empty); // replace nulls with empty strings so they can go into the listbox
            }

            public string PersistedString => ReplicateValue?.ToPersistedString();

            public override string ToString()
            {
                return ReplicateValue != null ? ReplicateValue.Title : Resources.GroupByItem_ToString_Replicates;
            }
        }

        public class AlignItem
        {
            public static AlignItem None
            {
                get
                {
                    return new AlignItem(null);
                }
            }

            public AlignItem(string display, AlignmentTarget target)
            {
                Display = display;
                Target = target;
            }

            public AlignItem(AlignmentTarget target) : this(target?.ToString() ?? "None", target)
            {

            }

            public string Display { get; }
            public AlignmentTarget Target { get; }

            public override string ToString()
            {
                return Display;
            }

            protected bool Equals(AlignItem other)
            {
                return Display == other.Display && Equals(Target, other.Target);
            }

            public override bool Equals(object obj)
            {
                if (ReferenceEquals(null, obj)) return false;
                if (ReferenceEquals(this, obj)) return true;
                if (obj.GetType() != this.GetType()) return false;
                return Equals((AlignItem)obj);
            }

            public override int GetHashCode()
            {
                unchecked
                {
                    return ((Display != null ? Display.GetHashCode() : 0) * 397) ^
                           (Target != null ? Target.GetHashCode() : 0);
                }
            }

            public bool IsNone
            {
                get
                {
                    return Equals(None);
                }
            }
        }
    }
}

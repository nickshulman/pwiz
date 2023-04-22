﻿/*
 * Original author: Nicholas Shulman <nicksh .at. u.washington.edu>,
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
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Windows.Forms;
using pwiz.Common.DataBinding;
using pwiz.Skyline.Alerts;
using pwiz.Skyline.Model.Databinding;
using pwiz.Skyline.Model.Results.Spectra;
using pwiz.Skyline.Util;

namespace pwiz.Skyline.EditUI
{
    public partial class EditSpectrumFilterDlg : FormEx
    {
        private List<Row> _rowList;
        private BindingList<Row> _rowBindingList;
        private SpectrumClassFilter _originalSpectrumClassFilter;
        private DataSchema _dataSchema;

        public EditSpectrumFilterDlg(SpectrumClassFilter spectrumClassFilter)
        {
            InitializeComponent();
            _dataSchema = new DataSchema(SkylineDataSchema.GetLocalizedSchemaLocalizer());
            _originalSpectrumClassFilter = spectrumClassFilter;
            _rowList = new List<Row>();
            _rowList.AddRange(GetRows(spectrumClassFilter));
            _rowBindingList = new BindingList<Row>(_rowList);
            dataGridViewEx1.DataSource = _rowBindingList;
            SpectrumClassFilter = spectrumClassFilter;
            propertyColumn.Items.AddRange(SpectrumClassColumn.ALL
                .OrderBy(c=>c.ToString(), StringComparer.OrdinalIgnoreCase).Cast<object>().ToArray());
            operationColumn.Items.AddRange(FilterOperations.ListOperations().Select(op=>(object) op.DisplayName).ToArray());
        }

        public SpectrumClassFilter SpectrumClassFilter { get; private set; }
        public SpectrumFilterAutoComplete AutoComplete { get; set; }

        public class Row
        {
            public SpectrumClassColumn Property { get; set; }
            public string Operation { get; set; }
            public string Value { get; set; }

            public void SetOperation(IFilterOperation filterOperation)
            {
                Operation = (filterOperation ?? FilterOperations.OP_HAS_ANY_VALUE).DisplayName;
            }

            public void SetValue(object value)
            {
                Value = value?.ToString() ?? string.Empty;
            }
        }

        private void btnOk_Click(object sender, EventArgs e)
        {
            OkDialog();
        }

        public IEnumerable<Row> GetRows(SpectrumClassFilter spectrumClassFilter)
        {
            if (spectrumClassFilter == null)
            {
                yield break;
}
            foreach (var filterSpec in spectrumClassFilter.FilterSpecs)
            {
                var spectrumClassColumn = SpectrumClassColumn.FindColumn(filterSpec.ColumnId);
                if (spectrumClassColumn == null)
                {
                    continue;
                }
                yield return new Row
                {
                    Property = spectrumClassColumn,
                    Operation = filterSpec.Operation.DisplayName,
                    Value = filterSpec.Predicate.GetOperandDisplayText(_dataSchema, spectrumClassColumn.ValueType)
                };
            }
        }

        public void OkDialog()
        {
            var filterSpecs = new List<FilterSpec>();
            for (int iRow = 0; iRow < _rowList.Count; iRow++)
            {
                var row = _rowList[iRow];
                var filterOperation = FilterOperations.ListOperations()
                    .FirstOrDefault(op => op.DisplayName == row.Operation);
                if (filterOperation == null || filterOperation == FilterOperations.OP_HAS_ANY_VALUE)
                {
                    continue;
                }
                var column = row.Property;
                FilterPredicate filterPredicate;
                try
                {
                    filterPredicate =
                        FilterPredicate.CreateFilterPredicate(_dataSchema, column.ValueType, filterOperation,
                            row.Value);
                }
                catch (Exception ex)
                {
                    MessageDlg.ShowWithException(this, ex.Message, ex);
                    dataGridViewEx1.CurrentCell = dataGridViewEx1.Rows[iRow].Cells[valueDataGridViewTextBoxColumn.Index];
                    return;
                }

                var filterSpec = new FilterSpec(row.Property.PropertyPath, filterPredicate);
                filterSpecs.Add(filterSpec);
            }

            SpectrumClassFilter = new SpectrumClassFilter(filterSpecs);
            DialogResult = DialogResult.OK;
        }

        private void dataGridViewEx1_DataError(object sender, DataGridViewDataErrorEventArgs e)
        {
            MessageDlg.ShowWithException(this, e.Exception.Message, e.Exception);
        }

        private void btnDeleteFilter_Click(object sender, EventArgs e)
        {
            var rowIndex = dataGridViewEx1.CurrentRow?.Index ?? -1;
            if (rowIndex >= 0 && rowIndex < _rowBindingList.Count)
            {
                _rowBindingList.RemoveAt(rowIndex);
            }
        }

        public bool CreateCopy
        {
            get { return cbCreateCopy.Checked; }
            set { cbCreateCopy.Checked = value; }
        }

        public bool CreateCopyEnabled
        {
            get
            {
                return cbCreateCopy.Enabled;
            }
            set
            {
                cbCreateCopy.Enabled = value;
            }
        }

        private void btnReset_Click(object sender, EventArgs e)
        {
            Reset();
        }

        public void Reset()
        {
            _rowList.Clear();
            _rowList.AddRange(GetRows(_originalSpectrumClassFilter));
            _rowBindingList.ResetBindings();
        }

        public BindingList<Row> RowBindingList
        {
            get { return _rowBindingList; }
        }

        private void dataGridViewEx1_EditingControlShowing(object sender, DataGridViewEditingControlShowingEventArgs e)
        {
            int columnIndex = dataGridViewEx1.CurrentCell.ColumnIndex;
            int rowIndex = dataGridViewEx1.CurrentCell.RowIndex;

            AutoCompleteStringCollection autoCompleteStringCollection = null;
            if (AutoComplete != null && columnIndex == valueColumn.Index && rowIndex >= 0 && rowIndex < _rowBindingList.Count)
            {
                var property = _rowBindingList[rowIndex].Property;
                if (property != null)
                {
                    autoCompleteStringCollection = AutoComplete.GetAutoCompleteValues(property);
                }
            }
            TextBox textBox = e.Control as TextBox;
            if (textBox != null)
            {
                if (autoCompleteStringCollection == null)
                {
                    textBox.AutoCompleteMode = AutoCompleteMode.None;
                    textBox.AutoCompleteCustomSource = null;
                    textBox.AutoCompleteSource = AutoCompleteSource.None;
                }
                else
                {
                    textBox.AutoCompleteMode = AutoCompleteMode.Suggest;
                    textBox.AutoCompleteCustomSource = autoCompleteStringCollection;
                    textBox.AutoCompleteSource = AutoCompleteSource.CustomSource;
                }
            }
        }
    }
}

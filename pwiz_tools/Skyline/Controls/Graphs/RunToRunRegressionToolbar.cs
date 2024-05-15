/*
 * Original author: Max Horowitz-Gelb <maxhg .at. u.washington.edu>,
 *                  MacCoss Lab, Department of Genome Sciences, UW
 *
 * Copyright 2017 University of Washington - Seattle, WA
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
using System.Linq;
using System.Windows.Forms;
using pwiz.Skyline.Model;
using pwiz.Skyline.Model.Results;
using pwiz.Skyline.Util;

namespace pwiz.Skyline.Controls.Graphs
{
    public partial class RunToRunRegressionToolbar : GraphSummaryToolbar //UserControl// for editing in the designer
    {
        private bool _inUpdate;
        public RunToRunRegressionToolbar(GraphSummary graphSummary) :
            base(graphSummary)
        {
            InitializeComponent();

            toolStrip1_Resize(null, null);
        }

        public override bool Visible
        {
            get { return true; }
        }

        public override void OnDocumentChanged(SrmDocument oldDocument, SrmDocument newDocument)
        {
            // Need at least 2 replicates to do run to run regression.
            bool visibleOld = oldDocument.MeasuredResults != null &&
                              oldDocument.MeasuredResults.Chromatograms.Count > 1;
            bool visibleNew = newDocument.MeasuredResults != null &&
                              newDocument.MeasuredResults.Chromatograms.Count > 1;
            if (visibleNew != visibleOld && visibleNew)
            {
                // Use first two replicates to avoid comparing the first replicate to itself
                var newChoices = ReplicateFileInfo.List(newDocument.MeasuredResults).Take(2).ToList();
                _graphSummary.SetResultIndexes(newChoices[0], newChoices[1], false);
            }
        }

        public override void UpdateUI()
        {
            if (_inUpdate)
            {
                return;
            }
            var results = _graphSummary.DocumentUIContainer.DocumentUI.MeasuredResults;

            if (results == null)
                return;
            try
            {
                _inUpdate = true;
                // Check to see if the list of files has changed.
                var listNames = ReplicateFileInfo.List(results).ToList();
                var targetIndex = ResetResultsCombo(listNames.Prepend(ReplicateFileInfo.All).ToList(), toolStripComboBoxTargetReplicates);
                if (targetIndex == null)
                {
                    targetIndex = ReplicateFileInfo.All;
                }
                var origIndex = ResetResultsCombo(listNames.Prepend(ReplicateFileInfo.Consensus).ToList(), toolStripComboOriginalReplicates);
                if (origIndex == null)
                    origIndex = ReplicateFileInfo.Consensus;
                _graphSummary.SetResultIndexes(targetIndex, origIndex, false);
                toolStripComboBoxTargetReplicates.SelectedItem = targetIndex;
                toolStripComboOriginalReplicates.SelectedItem = origIndex;
            }
            finally
            {
                _inUpdate = false;
            }
        }

        private ReplicateFileInfo ResetResultsCombo(List<ReplicateFileInfo> listNames, ToolStripComboBox combo)
        {
            ReplicateFileId selected = (combo.SelectedItem as ReplicateFileInfo)?.ReplicateFileId;
            combo.Items.Clear();
            ReplicateFileInfo selectedInfo = null;
            int selectedIndex = -1;
            foreach (var name in listNames)
            {
                combo.Items.Add(name);
                if (true == selected?.Equals(name.ReplicateFileId))
                {
                    selectedIndex = combo.Items.Count;
                    selectedInfo = name;
                }
            }
            combo.SelectedIndex = selectedIndex;
            ComboHelper.AutoSizeDropDown(combo);
            return selectedInfo;
        }

        private void toolStripComboBoxTargetReplicates_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (_inUpdate)
                return;
            _graphSummary.SetResultIndexes(
                toolStripComboBoxTargetReplicates.SelectedItem as ReplicateFileInfo ?? ReplicateFileInfo.All,
                toolStripComboOriginalReplicates.SelectedItem as ReplicateFileInfo ?? ReplicateFileInfo.Consensus);
        }

        private void toolStripComboOriginalReplicates_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (_inUpdate)
                return;
            _graphSummary.SetResultIndexes(_graphSummary.TargetResultsIndex,
                toolStripComboOriginalReplicates.SelectedItem as ReplicateFileInfo);
        }

        private void toolStrip1_Resize(object sender, EventArgs e)
        {
            toolStripComboOriginalReplicates.Width = toolStripComboBoxTargetReplicates.Width = (toolStrip1.Width - toolStripLabel1.Width - 24) / 2;
        }

        #region Functional Test Support

        public ToolStripComboBox RunToRunTargetReplicate { get { return toolStripComboBoxTargetReplicates; } }

        public ToolStripComboBox RunToRunOriginalReplicate { get { return toolStripComboOriginalReplicates; } }

        #endregion
    }
}

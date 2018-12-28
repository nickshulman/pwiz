﻿/*
 * Original author: Brendan MacLean <brendanx .at. u.washington.edu>,
 *                  MacCoss Lab, Department of Genome Sciences, UW
 *
 * Copyright 2009 University of Washington - Seattle, WA
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
using System.Linq;
using System.Windows.Forms;
using pwiz.Skyline.Model;
using pwiz.Skyline.Util;

namespace pwiz.Skyline.Alerts
{
    /// <summary>
    /// Dialog which presents options for what information to include in a .sky.zip file.
    /// </summary>
    public partial class ShareTypeDlg : FormEx
    {
        public ShareTypeDlg(SrmDocument document)
        {
            InitializeComponent();
            cbxDocumentReports.Enabled = document.Settings.DataSettings.ViewSpecList.Views.Any();
            cbxMinimizeLibraries.Enabled = document.Settings.HasLibraries || document.Settings.HasBackgroundProteome ||
                                           document.Settings.HasRTCalcPersisted;
            comboSkylineVersion.Items.AddRange(SkylineVersion.SupportedForSharing().Cast<object>().ToArray());
            comboSkylineVersion.SelectedIndex = 0;
        }

        public ShareType ShareType { get; set; }

        protected override void CreateHandle()
        {
            base.CreateHandle();

            Text = Program.Name;
        }

        public void OkDialog()
        {
            DialogResult = DialogResult.OK;
            ShareType = new ShareType(cbxMinimizeLibraries.Checked, (SkylineVersion) comboSkylineVersion.SelectedItem)
                .ChangeIncludeDocumentReports(cbxDocumentReports.Checked);
            Close();
        }

        private void btnShare_Click(object sender, EventArgs e)
        {
            OkDialog();
        }
    }
}

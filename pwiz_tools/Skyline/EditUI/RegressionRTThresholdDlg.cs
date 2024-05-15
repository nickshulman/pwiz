/*
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
using System.Windows.Forms;
using pwiz.Common.SystemUtil;
using pwiz.Skyline.Controls;
using pwiz.Skyline.Model.DocSettings;
using pwiz.Skyline.Util;

namespace pwiz.Skyline.EditUI
{
    public partial class RegressionRTThresholdDlg : FormEx
    {
        private double _threshold;
        private double? _variance;

        public RegressionRTThresholdDlg()
        {
            InitializeComponent();
        }

        public double Threshold
        {
            get { return _threshold; }
            set
            {
                _threshold = value;
                textThreshold.Text = _threshold.ToString(LocalizationHelper.CurrentCulture);
            }
        }

        public double? Variance
        {
            get
            {
                return _variance;
            }
            set
            {
                _variance = value;
                tbxRetentionTimeVariance.Text = Variance?.ToString() ?? string.Empty;
            }
        }

        public void OkDialog()
        {
            var helper = new MessageBoxHelper(this);
            if (!helper.ValidateDecimalTextBox(textThreshold, 0, 1.0, out _threshold))
                return;

            if (string.IsNullOrEmpty(tbxRetentionTimeVariance.Text.Trim()))
            {
                _variance = null;
            }
            else
            {

                if (!helper.ValidateDecimalTextBox(tbxRetentionTimeVariance, 0, null, out var variance))
                {
                    return;
                }

                _variance = variance;
            }

            // Round to precision used in calculating optimal regressions
            _threshold = Math.Round(_threshold, RetentionTimeRegression.ThresholdPrecision);

            DialogResult = DialogResult.OK;
        }

        private void btnOk_Click(object sender, EventArgs e)
        {
            OkDialog();
        }
    }
}

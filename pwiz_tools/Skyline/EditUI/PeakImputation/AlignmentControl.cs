using System;
using System.Linq;
using System.Windows.Forms;
using pwiz.Skyline.Model;
using pwiz.Skyline.Model.Results;
using pwiz.Skyline.Model.RetentionTimes;
using pwiz.Skyline.Util;

namespace pwiz.Skyline.EditUI.PeakImputation
{
    public partial class AlignmentControl : UserControl
    {
        private bool _inChange;
        private IDocumentUIContainer _documentUiContainer;
        private bool _handleCreated;
        public AlignmentControl()
        {
            InitializeComponent();
            comboAlignmentType.Items.AddRange(new object[]
            {
                RegressionMethodRT.linear,
                RegressionMethodRT.kde,
                RegressionMethodRT.loess
            });
        }

        public IDocumentUIContainer DocumentUiContainer
        {
            get
            {
                return _documentUiContainer;
            }
            set
            {
                if (ReferenceEquals(DocumentUiContainer, value))
                {
                    return;
                }

                if (_handleCreated)
                {
                    DocumentUiContainer?.UnlistenUI(OnDocumentChange);
                }

                _documentUiContainer = value;
                if (_handleCreated)
                {
                    DocumentUiContainer?.ListenUI(OnDocumentChange);
                }
            }
        }

        protected override void OnHandleCreated(EventArgs e)
        {
            base.OnHandleCreated(e);
            DocumentUiContainer?.ListenUI(OnDocumentChange);
            _handleCreated = true;
        }

        protected override void OnHandleDestroyed(EventArgs e)
        {
            _handleCreated = false;
            DocumentUiContainer?.UnlistenUI(OnDocumentChange);
            base.OnHandleDestroyed(e);
        }

        private void OnDocumentChange(object sender, DocumentChangedEventArgs args)
        {
            if (_inChange)
            {
                return;
            }

            try
            {
                _inChange = true;
                var document = _documentUiContainer.DocumentUI;
                ComboHelper.ReplaceItems(comboValuesToAlign, RtValueType.ForDocument(document).Cast<object>().Prepend(string.Empty));
                var rtValueType = RtValueType;
                if (true == rtValueType?.HasFileTargets)
                {
                    ComboHelper.ReplaceItems(comboAlignToFile, rtValueType.ListTargets(document).Cast<object>().Prepend(null));
                }
            }
            finally
            {
                _inChange = false;
            }
        }

        public AlignmentTarget AlignmentTarget
        {
            get
            {
                var valueType = RtValueType;
                if (valueType == null)
                {
                    return null;
                }

                return new AlignmentTarget(TargetFile, AverageType.MEDIAN, valueType, RegressionMethodRT);
            }
            set
            {
                if (value == null)
                {
                    RtValueType = null;
                    return;
                }

                RtValueType = value.RtValueType;
                TargetFile = value.File;
                RegressionMethodRT = value.RegressionMethod;
            }
        }

        public event EventHandler AlignmentTargetChange;

        public RtValueType RtValueType
        {
            get
            {
                return comboValuesToAlign.SelectedItem as RtValueType;
            }
            set
            {
                comboValuesToAlign.SelectedItem = value;
            }
        }

        public RegressionMethodRT RegressionMethodRT
        {
            get
            {
                return comboAlignmentType.SelectedItem as RegressionMethodRT? ?? RegressionMethodRT.linear;
            }
            set
            {
                comboAlignmentType.SelectedItem = value;
            }
        }

        public MsDataFileUri TargetFile
        {
            get
            {
                return comboAlignToFile.SelectedItem as MsDataFileUri;
            }
            set
            {
                comboAlignToFile.SelectedItem = value;
            }
        }

        private void SelectedValueChange(object sender, EventArgs e)
        {
            if (_inChange)
            {
                return;
            }
            var rtValueType = RtValueType;
            if (rtValueType == null)
            {
                comboAlignToFile.Enabled = false;
                comboAlignmentType.Enabled = false;
            }
            else
            {
                comboAlignToFile.Enabled = rtValueType.HasFileTargets;
                comboAlignmentType.Enabled = true;
            }
            AlignmentTargetChange?.Invoke(this, EventArgs.Empty);
        }
    }
}

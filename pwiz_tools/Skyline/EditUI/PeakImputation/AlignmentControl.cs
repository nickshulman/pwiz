using System;
using System.Collections.Generic;
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
            comboAlignmentType.SelectedIndex = 0;
            ComboHelper.AutoSizeDropDown(comboAlignmentType);
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
            UpdateControls();
        }

        protected override void OnHandleDestroyed(EventArgs e)
        {
            _handleCreated = false;
            DocumentUiContainer?.UnlistenUI(OnDocumentChange);
            base.OnHandleDestroyed(e);
        }

        private void OnDocumentChange(object sender, DocumentChangedEventArgs args)
        {
            UpdateControls();
        }

        private void UpdateControls()
        {
            if (DocumentUiContainer == null || _inChange)
            {
                return;
            }

            try
            {
                _inChange = true;
                var document = _documentUiContainer.DocumentUI;
                ComboHelper.ReplaceItems(comboValuesToAlign, RtValueType.ForDocument(document).Cast<object>().Prepend(string.Empty));
                var rtValueType = RtValueType;
                if (rtValueType != null)
                {
                    ComboHelper.ReplaceItems(comboAlignToFile, MakeTargetFileOptions(document, rtValueType.ListTargets(document)).Prepend(TargetFileOption.EMPTY));
                }
                comboAlignToFile.Enabled = rtValueType != null;
                comboAlignmentType.Enabled = rtValueType != null;
            }
            finally { _inChange = false;}
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
                return (comboAlignToFile.SelectedItem as TargetFileOption)?.File;
            }
            set
            {
                for (int i = 0; i < comboAlignToFile.Items.Count; i++)
                {
                    var option = (TargetFileOption) comboAlignToFile.Items[i];
                    if (Equals(option.File, value))
                    {
                        comboAlignToFile.SelectedIndex = i;
                    }
                }
            }
        }

        private void SelectedValueChange(object sender, EventArgs e)
        {
            if (_inChange)
            {
                return;
            }
            UpdateControls();
            AlignmentTargetChange?.Invoke(this, EventArgs.Empty);
        }

        public class TargetFileOption
        {
            public static readonly TargetFileOption EMPTY = new TargetFileOption("", null);
            public TargetFileOption(string display, MsDataFileUri file)
            {
                Display = display;
                File = file;
            }

            public TargetFileOption(MsDataFileUri file)
            {
                File = file;
                Display = File?.GetFileNameWithoutExtension() ?? string.Empty;
            }

            public string Display { get; }
            public MsDataFileUri File { get; }
            public override string ToString()
            {
                return Display;
            }

            protected bool Equals(TargetFileOption other)
            {
                return Equals(File, other.File);
            }

            public override bool Equals(object obj)
            {
                if (ReferenceEquals(null, obj)) return false;
                if (ReferenceEquals(this, obj)) return true;
                if (obj.GetType() != this.GetType()) return false;
                return Equals((TargetFileOption)obj);
            }

            public override int GetHashCode()
            {
                return (File != null ? File.GetHashCode() : 0);
            }
        }

        private IEnumerable<TargetFileOption> MakeTargetFileOptions(SrmDocument document,
            IEnumerable<MsDataFileUri> files)
        {
            return files.Select(file => new TargetFileOption(file));
        }
    }
}

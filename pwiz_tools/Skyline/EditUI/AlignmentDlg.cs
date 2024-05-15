using System;
using pwiz.Skyline.Model.RetentionTimes;
using pwiz.Skyline.Util;

namespace pwiz.Skyline.EditUI
{
    public partial class AlignmentDlg : FormEx
    {
        public AlignmentDlg(SkylineWindow skylineWindow)
        {
            InitializeComponent();
            SkylineWindow = skylineWindow;
            alignmentControl1.DocumentUiContainer = skylineWindow;
            alignmentControl1.AlignmentTarget = skylineWindow.AlignmentTarget;
            alignmentControl1.AlignmentTargetChange += AlignmentControl1OnAlignmentTargetChange;
        }

        public SkylineWindow SkylineWindow { get; }

        protected override void OnHandleCreated(EventArgs e)
        {
            base.OnHandleCreated(e);
            if (SkylineWindow != null)
            {
                SkylineWindow.AlignmentTargetChange += SkylineWindowOnAlignmentTargetChange;
            }
            
        }

        private void SkylineWindowOnAlignmentTargetChange(object sender, EventArgs e)
        {
            alignmentControl1.AlignmentTarget = SkylineWindow.AlignmentTarget;
        }

        protected override void OnHandleDestroyed(EventArgs e)
        {
            if (SkylineWindow != null)
            {
                SkylineWindow.AlignmentTargetChange -= SkylineWindowOnAlignmentTargetChange;
            }
            base.OnHandleDestroyed(e);
        }

        protected override void OnClosed(EventArgs e)
        {
            base.OnClosed(e);
            Dispose(true);
        }

        private void AlignmentControl1OnAlignmentTargetChange(object sender, EventArgs e)
        {
            SkylineWindow.AlignmentTarget = alignmentControl1.AlignmentTarget;
        }


        public AlignmentTarget AlignmentTarget
        {
            get
            {
                return alignmentControl1.AlignmentTarget;
            }
            set
            {
                alignmentControl1.AlignmentTarget = value;
            }
        }
    }
}

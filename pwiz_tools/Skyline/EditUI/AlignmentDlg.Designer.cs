namespace pwiz.Skyline.EditUI
{
    partial class AlignmentDlg
    {
        /// <summary>
        /// Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        /// Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows Form Designer generated code

        /// <summary>
        /// Required method for Designer support - do not modify
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            this.alignmentControl1 = new pwiz.Skyline.EditUI.PeakImputation.AlignmentControl();
            this.SuspendLayout();
            // 
            // alignmentControl1
            // 
            this.alignmentControl1.AlignmentTarget = null;
            this.alignmentControl1.DocumentUiContainer = null;
            this.alignmentControl1.Location = new System.Drawing.Point(12, 3);
            this.alignmentControl1.Name = "alignmentControl1";
            this.alignmentControl1.RegressionMethodRT = pwiz.Skyline.Model.RetentionTimes.RegressionMethodRT.linear;
            this.alignmentControl1.RtValueType = null;
            this.alignmentControl1.Size = new System.Drawing.Size(206, 157);
            this.alignmentControl1.TabIndex = 0;
            this.alignmentControl1.TargetFile = null;
            // 
            // AlignmentDlg
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(257, 186);
            this.Controls.Add(this.alignmentControl1);
            this.Name = "AlignmentDlg";
            this.Text = "Alignment Settings";
            this.ResumeLayout(false);

        }

        #endregion

        private PeakImputation.AlignmentControl alignmentControl1;
    }
}
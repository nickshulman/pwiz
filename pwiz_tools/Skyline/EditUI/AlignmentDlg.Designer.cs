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
            this.btnOk = new System.Windows.Forms.Button();
            this.btnCancel = new System.Windows.Forms.Button();
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
            // btnOk
            // 
            this.btnOk.DialogResult = System.Windows.Forms.DialogResult.OK;
            this.btnOk.Location = new System.Drawing.Point(229, 12);
            this.btnOk.Name = "btnOk";
            this.btnOk.Size = new System.Drawing.Size(75, 23);
            this.btnOk.TabIndex = 1;
            this.btnOk.Text = "OK";
            this.btnOk.UseVisualStyleBackColor = true;
            // 
            // btnCancel
            // 
            this.btnCancel.DialogResult = System.Windows.Forms.DialogResult.Cancel;
            this.btnCancel.Location = new System.Drawing.Point(229, 41);
            this.btnCancel.Name = "btnCancel";
            this.btnCancel.Size = new System.Drawing.Size(75, 23);
            this.btnCancel.TabIndex = 2;
            this.btnCancel.Text = "Cancel";
            this.btnCancel.UseVisualStyleBackColor = true;
            // 
            // AlignmentDlg
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(316, 160);
            this.Controls.Add(this.btnCancel);
            this.Controls.Add(this.btnOk);
            this.Controls.Add(this.alignmentControl1);
            this.Name = "AlignmentDlg";
            this.Text = "AlignmentDlg";
            this.ResumeLayout(false);

        }

        #endregion

        private PeakImputation.AlignmentControl alignmentControl1;
        private System.Windows.Forms.Button btnOk;
        private System.Windows.Forms.Button btnCancel;
    }
}
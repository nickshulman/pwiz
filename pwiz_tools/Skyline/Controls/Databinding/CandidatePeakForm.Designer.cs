namespace pwiz.Skyline.Controls.Databinding
{
    partial class CandidatePeakForm
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
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(CandidatePeakForm));
            this.panel1 = new System.Windows.Forms.Panel();
            this.checkBoxUseAlignment = new System.Windows.Forms.CheckBox();
            this.comboPeakScoringModel = new System.Windows.Forms.ComboBox();
            this.lblModel = new System.Windows.Forms.Label();
            this.lblBestReplicate = new System.Windows.Forms.Label();
            this.linkLabelBestReplicate = new System.Windows.Forms.LinkLabel();
            this.panel1.SuspendLayout();
            this.SuspendLayout();
            // 
            // databoundGridControl
            // 
            resources.ApplyResources(this.databoundGridControl, "databoundGridControl");
            // 
            // panel1
            // 
            this.panel1.Controls.Add(this.linkLabelBestReplicate);
            this.panel1.Controls.Add(this.lblBestReplicate);
            this.panel1.Controls.Add(this.checkBoxUseAlignment);
            this.panel1.Controls.Add(this.comboPeakScoringModel);
            this.panel1.Controls.Add(this.lblModel);
            resources.ApplyResources(this.panel1, "panel1");
            this.panel1.Name = "panel1";
            // 
            // checkBoxUseAlignment
            // 
            resources.ApplyResources(this.checkBoxUseAlignment, "checkBoxUseAlignment");
            this.checkBoxUseAlignment.Name = "checkBoxUseAlignment";
            this.checkBoxUseAlignment.UseVisualStyleBackColor = true;
            this.checkBoxUseAlignment.CheckedChanged += new System.EventHandler(this.checkBoxUseAlignment_CheckedChanged);
            // 
            // comboPeakScoringModel
            // 
            this.comboPeakScoringModel.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.comboPeakScoringModel.FormattingEnabled = true;
            resources.ApplyResources(this.comboPeakScoringModel, "comboPeakScoringModel");
            this.comboPeakScoringModel.Name = "comboPeakScoringModel";
            this.comboPeakScoringModel.SelectedIndexChanged += new System.EventHandler(this.comboPeakScoringModel_SelectedIndexChanged);
            // 
            // lblModel
            // 
            resources.ApplyResources(this.lblModel, "lblModel");
            this.lblModel.Name = "lblModel";
            // 
            // lblBestReplicate
            // 
            resources.ApplyResources(this.lblBestReplicate, "lblBestReplicate");
            this.lblBestReplicate.Name = "lblBestReplicate";
            // 
            // linkLabelBestReplicate
            // 
            resources.ApplyResources(this.linkLabelBestReplicate, "linkLabelBestReplicate");
            this.linkLabelBestReplicate.Name = "linkLabelBestReplicate";
            // 
            // CandidatePeakForm
            // 
            resources.ApplyResources(this, "$this");
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.Controls.Add(this.panel1);
            this.Name = "CandidatePeakForm";
            this.Controls.SetChildIndex(this.panel1, 0);
            this.Controls.SetChildIndex(this.databoundGridControl, 0);
            this.panel1.ResumeLayout(false);
            this.panel1.PerformLayout();
            this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.Panel panel1;
        private System.Windows.Forms.ComboBox comboPeakScoringModel;
        private System.Windows.Forms.Label lblModel;
        private System.Windows.Forms.CheckBox checkBoxUseAlignment;
        private System.Windows.Forms.LinkLabel linkLabelBestReplicate;
        private System.Windows.Forms.Label lblBestReplicate;
    }
}
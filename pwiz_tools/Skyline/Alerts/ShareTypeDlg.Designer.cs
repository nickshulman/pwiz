namespace pwiz.Skyline.Alerts
{
    partial class ShareTypeDlg
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
            this.components = new System.ComponentModel.Container();
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(ShareTypeDlg));
            this.btnCancel = new System.Windows.Forms.Button();
            this.panelButtonBar = new System.Windows.Forms.FlowLayoutPanel();
            this.btnShare = new System.Windows.Forms.Button();
            this.comboSkylineVersion = new System.Windows.Forms.ComboBox();
            this.panel2 = new System.Windows.Forms.Panel();
            this.label1 = new System.Windows.Forms.Label();
            this.cbxMinimizeLibraries = new System.Windows.Forms.CheckBox();
            this.toolTip1 = new System.Windows.Forms.ToolTip(this.components);
            this.label2 = new System.Windows.Forms.Label();
            this.cbxDocumentReports = new System.Windows.Forms.CheckBox();
            this.panelButtonBar.SuspendLayout();
            this.panel2.SuspendLayout();
            this.SuspendLayout();
            // 
            // btnCancel
            // 
            resources.ApplyResources(this.btnCancel, "btnCancel");
            this.btnCancel.DialogResult = System.Windows.Forms.DialogResult.Cancel;
            this.btnCancel.Name = "btnCancel";
            this.btnCancel.UseVisualStyleBackColor = true;
            // 
            // panelButtonBar
            // 
            this.panelButtonBar.BackColor = System.Drawing.SystemColors.Control;
            this.panelButtonBar.Controls.Add(this.btnCancel);
            this.panelButtonBar.Controls.Add(this.btnShare);
            resources.ApplyResources(this.panelButtonBar, "panelButtonBar");
            this.panelButtonBar.Name = "panelButtonBar";
            // 
            // btnShare
            // 
            resources.ApplyResources(this.btnShare, "btnShare");
            this.btnShare.Name = "btnShare";
            this.btnShare.UseVisualStyleBackColor = true;
            this.btnShare.Click += new System.EventHandler(this.btnShare_Click);
            // 
            // comboSkylineVersion
            // 
            this.comboSkylineVersion.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            resources.ApplyResources(this.comboSkylineVersion, "comboSkylineVersion");
            this.comboSkylineVersion.FormattingEnabled = true;
            this.comboSkylineVersion.Name = "comboSkylineVersion";
            // 
            // panel2
            // 
            resources.ApplyResources(this.panel2, "panel2");
            this.panel2.Controls.Add(this.label1);
            this.panel2.Name = "panel2";
            // 
            // label1
            // 
            resources.ApplyResources(this.label1, "label1");
            this.label1.Name = "label1";
            // 
            // cbxMinimizeLibraries
            // 
            resources.ApplyResources(this.cbxMinimizeLibraries, "cbxMinimizeLibraries");
            this.cbxMinimizeLibraries.Name = "cbxMinimizeLibraries";
            this.toolTip1.SetToolTip(this.cbxMinimizeLibraries, resources.GetString("cbxMinimizeLibraries.ToolTip"));
            this.cbxMinimizeLibraries.UseVisualStyleBackColor = true;
            // 
            // label2
            // 
            resources.ApplyResources(this.label2, "label2");
            this.label2.Name = "label2";
            // 
            // cbxDocumentReports
            // 
            resources.ApplyResources(this.cbxDocumentReports, "cbxDocumentReports");
            this.cbxDocumentReports.Name = "cbxDocumentReports";
            this.cbxDocumentReports.UseVisualStyleBackColor = true;
            // 
            // ShareTypeDlg
            // 
            this.AcceptButton = this.btnShare;
            resources.ApplyResources(this, "$this");
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.BackColor = System.Drawing.SystemColors.Control;
            this.CancelButton = this.btnCancel;
            this.Controls.Add(this.cbxDocumentReports);
            this.Controls.Add(this.comboSkylineVersion);
            this.Controls.Add(this.label2);
            this.Controls.Add(this.cbxMinimizeLibraries);
            this.Controls.Add(this.panel2);
            this.Controls.Add(this.panelButtonBar);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.Name = "ShareTypeDlg";
            this.ShowInTaskbar = false;
            this.panelButtonBar.ResumeLayout(false);
            this.panel2.ResumeLayout(false);
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.Button btnCancel;
        private System.Windows.Forms.FlowLayoutPanel panelButtonBar;
        private System.Windows.Forms.Button btnShare;
        private System.Windows.Forms.Panel panel2;
        private System.Windows.Forms.ComboBox comboSkylineVersion;
        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.CheckBox cbxMinimizeLibraries;
        private System.Windows.Forms.ToolTip toolTip1;
        private System.Windows.Forms.Label label2;
        private System.Windows.Forms.CheckBox cbxDocumentReports;
    }
}
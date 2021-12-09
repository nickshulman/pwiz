﻿namespace pwiz.Skyline.Alerts
{
    partial class MsFraggerDownloadDlg
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
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(MsFraggerDownloadDlg));
            this.tbUsageConditions = new System.Windows.Forms.TextBox();
            this.cbAgreeToLicense = new System.Windows.Forms.CheckBox();
            this.rtbAgreeToLicense = new System.Windows.Forms.RichTextBox();
            this.tbName = new System.Windows.Forms.TextBox();
            this.lblName = new System.Windows.Forms.Label();
            this.lblEmail = new System.Windows.Forms.Label();
            this.tbEmail = new System.Windows.Forms.TextBox();
            this.lblInstitution = new System.Windows.Forms.Label();
            this.tbInstitution = new System.Windows.Forms.TextBox();
            this.cbReceiveUpdateEmails = new System.Windows.Forms.CheckBox();
            this.btnAccept = new System.Windows.Forms.Button();
            this.btnCancel = new System.Windows.Forms.Button();
            this.SuspendLayout();
            // 
            // tbUsageConditions
            // 
            resources.ApplyResources(this.tbUsageConditions, "tbUsageConditions");
            this.tbUsageConditions.BorderStyle = System.Windows.Forms.BorderStyle.None;
            this.tbUsageConditions.Name = "tbUsageConditions";
            this.tbUsageConditions.ReadOnly = true;
            // 
            // cbAgreeToLicense
            // 
            resources.ApplyResources(this.cbAgreeToLicense, "cbAgreeToLicense");
            this.cbAgreeToLicense.Name = "cbAgreeToLicense";
            this.cbAgreeToLicense.UseVisualStyleBackColor = true;
            this.cbAgreeToLicense.CheckedChanged += new System.EventHandler(this.cbAgreeToLicense_CheckedChanged);
            // 
            // rtbAgreeToLicense
            // 
            this.rtbAgreeToLicense.BackColor = System.Drawing.SystemColors.Control;
            this.rtbAgreeToLicense.BorderStyle = System.Windows.Forms.BorderStyle.None;
            resources.ApplyResources(this.rtbAgreeToLicense, "rtbAgreeToLicense");
            this.rtbAgreeToLicense.Name = "rtbAgreeToLicense";
            this.rtbAgreeToLicense.ReadOnly = true;
            // 
            // tbName
            // 
            resources.ApplyResources(this.tbName, "tbName");
            this.tbName.Name = "tbName";
            // 
            // lblName
            // 
            resources.ApplyResources(this.lblName, "lblName");
            this.lblName.Name = "lblName";
            // 
            // lblEmail
            // 
            resources.ApplyResources(this.lblEmail, "lblEmail");
            this.lblEmail.Name = "lblEmail";
            // 
            // tbEmail
            // 
            resources.ApplyResources(this.tbEmail, "tbEmail");
            this.tbEmail.Name = "tbEmail";
            // 
            // lblInstitution
            // 
            resources.ApplyResources(this.lblInstitution, "lblInstitution");
            this.lblInstitution.Name = "lblInstitution";
            // 
            // tbInstitution
            // 
            resources.ApplyResources(this.tbInstitution, "tbInstitution");
            this.tbInstitution.Name = "tbInstitution";
            // 
            // cbReceiveUpdateEmails
            // 
            resources.ApplyResources(this.cbReceiveUpdateEmails, "cbReceiveUpdateEmails");
            this.cbReceiveUpdateEmails.Name = "cbReceiveUpdateEmails";
            this.cbReceiveUpdateEmails.UseVisualStyleBackColor = true;
            // 
            // btnAccept
            // 
            resources.ApplyResources(this.btnAccept, "btnAccept");
            this.btnAccept.Name = "btnAccept";
            this.btnAccept.UseVisualStyleBackColor = true;
            this.btnAccept.Click += new System.EventHandler(this.btnAccept_Click);
            // 
            // btnCancel
            // 
            resources.ApplyResources(this.btnCancel, "btnCancel");
            this.btnCancel.DialogResult = System.Windows.Forms.DialogResult.Cancel;
            this.btnCancel.Name = "btnCancel";
            this.btnCancel.UseVisualStyleBackColor = true;
            // 
            // MsFraggerDownloadDlg
            // 
            this.AcceptButton = this.btnAccept;
            resources.ApplyResources(this, "$this");
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.CancelButton = this.btnCancel;
            this.Controls.Add(this.btnCancel);
            this.Controls.Add(this.btnAccept);
            this.Controls.Add(this.cbReceiveUpdateEmails);
            this.Controls.Add(this.lblInstitution);
            this.Controls.Add(this.tbInstitution);
            this.Controls.Add(this.lblEmail);
            this.Controls.Add(this.tbEmail);
            this.Controls.Add(this.lblName);
            this.Controls.Add(this.tbName);
            this.Controls.Add(this.rtbAgreeToLicense);
            this.Controls.Add(this.cbAgreeToLicense);
            this.Controls.Add(this.tbUsageConditions);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.Name = "MsFraggerDownloadDlg";
            this.ShowIcon = false;
            this.ShowInTaskbar = false;
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.TextBox tbUsageConditions;
        private System.Windows.Forms.CheckBox cbAgreeToLicense;
        private System.Windows.Forms.RichTextBox rtbAgreeToLicense;
        private System.Windows.Forms.TextBox tbName;
        private System.Windows.Forms.Label lblName;
        private System.Windows.Forms.Label lblEmail;
        private System.Windows.Forms.TextBox tbEmail;
        private System.Windows.Forms.Label lblInstitution;
        private System.Windows.Forms.TextBox tbInstitution;
        private System.Windows.Forms.CheckBox cbReceiveUpdateEmails;
        private System.Windows.Forms.Button btnAccept;
        private System.Windows.Forms.Button btnCancel;
    }
}
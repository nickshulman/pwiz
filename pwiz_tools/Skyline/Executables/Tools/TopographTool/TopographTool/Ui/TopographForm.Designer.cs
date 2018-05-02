using DigitalRune.Windows.Docking;

namespace TopographTool.Ui
{
    partial class TopographForm
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
            this.menuStrip1 = new System.Windows.Forms.MenuStrip();
            this.fileToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.importToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.importIsolationSchemeToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.dockPanel = new DigitalRune.Windows.Docking.DockPanel();
            this.panel1 = new System.Windows.Forms.Panel();
            this.btnShowPeptide = new System.Windows.Forms.Button();
            this.comboPeptide = new System.Windows.Forms.ComboBox();
            this.backgroundWorker1 = new System.ComponentModel.BackgroundWorker();
            this.menuStrip1.SuspendLayout();
            this.panel1.SuspendLayout();
            this.SuspendLayout();
            // 
            // menuStrip1
            // 
            this.menuStrip1.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.fileToolStripMenuItem});
            this.menuStrip1.Location = new System.Drawing.Point(0, 0);
            this.menuStrip1.Name = "menuStrip1";
            this.menuStrip1.Size = new System.Drawing.Size(948, 24);
            this.menuStrip1.TabIndex = 0;
            this.menuStrip1.Text = "menuStrip1";
            // 
            // fileToolStripMenuItem
            // 
            this.fileToolStripMenuItem.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.importToolStripMenuItem,
            this.importIsolationSchemeToolStripMenuItem});
            this.fileToolStripMenuItem.Name = "fileToolStripMenuItem";
            this.fileToolStripMenuItem.Size = new System.Drawing.Size(37, 20);
            this.fileToolStripMenuItem.Text = "File";
            // 
            // importToolStripMenuItem
            // 
            this.importToolStripMenuItem.Name = "importToolStripMenuItem";
            this.importToolStripMenuItem.Size = new System.Drawing.Size(212, 22);
            this.importToolStripMenuItem.Text = "Import Results...";
            this.importToolStripMenuItem.Click += new System.EventHandler(this.importToolStripMenuItem_Click);
            // 
            // importIsolationSchemeToolStripMenuItem
            // 
            this.importIsolationSchemeToolStripMenuItem.Name = "importIsolationSchemeToolStripMenuItem";
            this.importIsolationSchemeToolStripMenuItem.Size = new System.Drawing.Size(212, 22);
            this.importIsolationSchemeToolStripMenuItem.Text = "Import Isolation Scheme...";
            this.importIsolationSchemeToolStripMenuItem.Click += new System.EventHandler(this.importIsolationSchemeToolStripMenuItem_Click);
            // 
            // dockPanel
            // 
            this.dockPanel.ActiveAutoHideContent = null;
            this.dockPanel.Dock = System.Windows.Forms.DockStyle.Fill;
            this.dockPanel.Location = new System.Drawing.Point(0, 124);
            this.dockPanel.Name = "dockPanel";
            this.dockPanel.Size = new System.Drawing.Size(948, 499);
            this.dockPanel.TabIndex = 1;
            // 
            // panel1
            // 
            this.panel1.Controls.Add(this.btnShowPeptide);
            this.panel1.Controls.Add(this.comboPeptide);
            this.panel1.Dock = System.Windows.Forms.DockStyle.Top;
            this.panel1.Location = new System.Drawing.Point(0, 24);
            this.panel1.Name = "panel1";
            this.panel1.Size = new System.Drawing.Size(948, 100);
            this.panel1.TabIndex = 8;
            // 
            // btnShowPeptide
            // 
            this.btnShowPeptide.Location = new System.Drawing.Point(561, 8);
            this.btnShowPeptide.Name = "btnShowPeptide";
            this.btnShowPeptide.Size = new System.Drawing.Size(129, 23);
            this.btnShowPeptide.TabIndex = 1;
            this.btnShowPeptide.Text = "Show Peptide";
            this.btnShowPeptide.UseVisualStyleBackColor = true;
            this.btnShowPeptide.Click += new System.EventHandler(this.btnShowPeptide_Click);
            // 
            // comboPeptide
            // 
            this.comboPeptide.DropDownHeight = 300;
            this.comboPeptide.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.comboPeptide.FormattingEnabled = true;
            this.comboPeptide.IntegralHeight = false;
            this.comboPeptide.Location = new System.Drawing.Point(12, 10);
            this.comboPeptide.Name = "comboPeptide";
            this.comboPeptide.Size = new System.Drawing.Size(543, 21);
            this.comboPeptide.TabIndex = 0;
            // 
            // TopographForm
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(948, 623);
            this.Controls.Add(this.dockPanel);
            this.Controls.Add(this.panel1);
            this.Controls.Add(this.menuStrip1);
            this.MainMenuStrip = this.menuStrip1;
            this.Name = "TopographForm";
            this.Text = "TopographForm";
            this.menuStrip1.ResumeLayout(false);
            this.menuStrip1.PerformLayout();
            this.panel1.ResumeLayout(false);
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.MenuStrip menuStrip1;
        private System.Windows.Forms.ToolStripMenuItem fileToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem importToolStripMenuItem;
        private DockPanel dockPanel;
        private System.Windows.Forms.Panel panel1;
        private System.Windows.Forms.ComboBox comboPeptide;
        private System.Windows.Forms.Button btnShowPeptide;
        private System.ComponentModel.BackgroundWorker backgroundWorker1;
        private System.Windows.Forms.ToolStripMenuItem importIsolationSchemeToolStripMenuItem;
    }
}
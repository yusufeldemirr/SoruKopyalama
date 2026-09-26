namespace SoruKopyalama.Forms
{
    partial class BrowserLoginForm
    {
        private System.ComponentModel.IContainer components = null;

        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows Form Designer generated code

        private void InitializeComponent()
        {
            pnlHeader = new System.Windows.Forms.Panel();
            btnManuelYakalayici = new System.Windows.Forms.Button();
            lblDurum = new System.Windows.Forms.Label();
            pnlWeb = new System.Windows.Forms.Panel();
            pnlHeader.SuspendLayout();
            SuspendLayout();
            // 
            // pnlHeader
            // 
            pnlHeader.BackColor = System.Drawing.Color.White;
            pnlHeader.Controls.Add(btnManuelYakalayici);
            pnlHeader.Controls.Add(lblDurum);
            pnlHeader.Dock = System.Windows.Forms.DockStyle.Top;
            pnlHeader.Location = new System.Drawing.Point(0, 0);
            pnlHeader.Name = "pnlHeader";
            pnlHeader.Padding = new System.Windows.Forms.Padding(10);
            pnlHeader.Size = new System.Drawing.Size(984, 52);
            pnlHeader.TabIndex = 0;
            // 
            // btnManuelYakalayici
            // 
            btnManuelYakalayici.Anchor = System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right;
            btnManuelYakalayici.BackColor = System.Drawing.Color.FromArgb(220, 53, 69);
            btnManuelYakalayici.Cursor = System.Windows.Forms.Cursors.Hand;
            btnManuelYakalayici.FlatAppearance.BorderSize = 0;
            btnManuelYakalayici.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            btnManuelYakalayici.Font = new System.Drawing.Font("Segoe UI", 9F, System.Drawing.FontStyle.Bold);
            btnManuelYakalayici.ForeColor = System.Drawing.Color.White;
            btnManuelYakalayici.Location = new System.Drawing.Point(790, 8);
            btnManuelYakalayici.Name = "btnManuelYakalayici";
            btnManuelYakalayici.Size = new System.Drawing.Size(182, 36);
            btnManuelYakalayici.TabIndex = 1;
            btnManuelYakalayici.Text = "⚡ Çerezi Hemen Yakala";
            btnManuelYakalayici.UseVisualStyleBackColor = false;
            btnManuelYakalayici.Click += btnManuelYakalayici_Click;
            // 
            // lblDurum
            // 
            lblDurum.Anchor = System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Right;
            lblDurum.Font = new System.Drawing.Font("Segoe UI Semibold", 10F, System.Drawing.FontStyle.Bold);
            lblDurum.ForeColor = System.Drawing.Color.FromArgb(33, 37, 41);
            lblDurum.Location = new System.Drawing.Point(12, 10);
            lblDurum.Name = "lblDurum";
            lblDurum.Size = new System.Drawing.Size(760, 32);
            lblDurum.TabIndex = 0;
            lblDurum.Text = "Oturum açılıyor...";
            lblDurum.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // pnlWeb
            // 
            pnlWeb.Dock = System.Windows.Forms.DockStyle.Fill;
            pnlWeb.Location = new System.Drawing.Point(0, 52);
            pnlWeb.Name = "pnlWeb";
            pnlWeb.Size = new System.Drawing.Size(984, 609);
            pnlWeb.TabIndex = 1;
            // 
            // BrowserLoginForm
            // 
            AutoScaleDimensions = new System.Drawing.SizeF(7F, 15F);
            AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            BackColor = System.Drawing.Color.FromArgb(244, 246, 249);
            ClientSize = new System.Drawing.Size(984, 661);
            Controls.Add(pnlWeb);
            Controls.Add(pnlHeader);
            Name = "BrowserLoginForm";
            StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen;
            Text = "Panel Otomatik Giriş";
            Load += BrowserLoginForm_Load;
            pnlHeader.ResumeLayout(false);
            ResumeLayout(false);
        }

        #endregion

        private System.Windows.Forms.Panel pnlHeader;
        private System.Windows.Forms.Label lblDurum;
        private System.Windows.Forms.Button btnManuelYakalayici;
        private System.Windows.Forms.Panel pnlWeb;
    }
}

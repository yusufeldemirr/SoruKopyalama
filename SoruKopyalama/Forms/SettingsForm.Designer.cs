namespace SoruKopyalama.Forms
{
    partial class SettingsForm
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
            tabControl1 = new TabControl();
            tabPaneller = new TabPage();
            btnTestLogin = new Button();
            btnKaydet = new Button();
            lblStatus = new Label();
            txtCookie = new TextBox();
            label5 = new Label();
            txtSifre = new TextBox();
            label4 = new Label();
            txtEposta = new TextBox();
            label3 = new Label();
            txtDomain = new TextBox();
            label2 = new Label();
            cmbPanelSec = new ComboBox();
            label1 = new Label();
            tabControl1.SuspendLayout();
            tabPaneller.SuspendLayout();
            SuspendLayout();
            // 
            // tabControl1
            // 
            tabControl1.Controls.Add(tabPaneller);
            tabControl1.Dock = DockStyle.Fill;
            tabControl1.Font = new Font("Segoe UI Semibold", 9.5F, FontStyle.Bold);
            tabControl1.Location = new Point(0, 0);
            tabControl1.Name = "tabControl1";
            tabControl1.SelectedIndex = 0;
            tabControl1.Size = new Size(584, 430);
            tabControl1.TabIndex = 0;
            // 
            // tabPaneller
            // 
            tabPaneller.BackColor = Color.White;
            tabPaneller.Controls.Add(btnTestLogin);
            tabPaneller.Controls.Add(btnKaydet);
            tabPaneller.Controls.Add(lblStatus);
            tabPaneller.Controls.Add(txtCookie);
            tabPaneller.Controls.Add(label5);
            tabPaneller.Controls.Add(txtSifre);
            tabPaneller.Controls.Add(label4);
            tabPaneller.Controls.Add(txtEposta);
            tabPaneller.Controls.Add(label3);
            tabPaneller.Controls.Add(txtDomain);
            tabPaneller.Controls.Add(label2);
            tabPaneller.Controls.Add(cmbPanelSec);
            tabPaneller.Controls.Add(label1);
            tabPaneller.ForeColor = Color.FromArgb(33, 37, 41);
            tabPaneller.Location = new Point(4, 25);
            tabPaneller.Name = "tabPaneller";
            tabPaneller.Padding = new Padding(15);
            tabPaneller.Size = new Size(576, 401);
            tabPaneller.TabIndex = 0;
            tabPaneller.Text = "  🌐 Panel & Oturum Ayarları  ";
            // 
            // btnTestLogin
            // 
            btnTestLogin.BackColor = Color.FromArgb(220, 53, 69);
            btnTestLogin.Cursor = Cursors.Hand;
            btnTestLogin.FlatAppearance.BorderSize = 0;
            btnTestLogin.FlatStyle = FlatStyle.Flat;
            btnTestLogin.Font = new Font("Segoe UI", 9.75F, FontStyle.Bold);
            btnTestLogin.ForeColor = Color.White;
            btnTestLogin.Location = new Point(140, 345);
            btnTestLogin.Name = "btnTestLogin";
            btnTestLogin.Size = new Size(200, 36);
            btnTestLogin.TabIndex = 11;
            btnTestLogin.Text = "⚡ Otomatik Giriş Yap ve Çek";
            btnTestLogin.UseVisualStyleBackColor = false;
            btnTestLogin.Click += btnTestLogin_Click;
            // 
            // btnKaydet
            // 
            btnKaydet.BackColor = Color.FromArgb(39, 174, 96);
            btnKaydet.Cursor = Cursors.Hand;
            btnKaydet.FlatAppearance.BorderSize = 0;
            btnKaydet.FlatStyle = FlatStyle.Flat;
            btnKaydet.Font = new Font("Segoe UI", 9.75F, FontStyle.Bold);
            btnKaydet.ForeColor = Color.White;
            btnKaydet.Location = new Point(355, 345);
            btnKaydet.Name = "btnKaydet";
            btnKaydet.Size = new Size(190, 36);
            btnKaydet.TabIndex = 10;
            btnKaydet.Text = "💾 Ayarları Kaydet";
            btnKaydet.UseVisualStyleBackColor = false;
            btnKaydet.Click += btnKaydet_Click;
            // 
            // lblStatus
            // 
            lblStatus.Font = new Font("Segoe UI", 9F);
            lblStatus.ForeColor = Color.FromArgb(108, 117, 125);
            lblStatus.Location = new Point(140, 305);
            lblStatus.Name = "lblStatus";
            lblStatus.Size = new Size(405, 30);
            lblStatus.TabIndex = 9;
            lblStatus.Text = "Durum: Hazır";
            // 
            // txtCookie
            // 
            txtCookie.BackColor = Color.FromArgb(248, 249, 250);
            txtCookie.BorderStyle = BorderStyle.FixedSingle;
            txtCookie.Font = new Font("Consolas", 8.5F);
            txtCookie.ForeColor = Color.FromArgb(33, 37, 41);
            txtCookie.Location = new Point(140, 195);
            txtCookie.Multiline = true;
            txtCookie.Name = "txtCookie";
            txtCookie.ScrollBars = ScrollBars.Vertical;
            txtCookie.Size = new Size(405, 100);
            txtCookie.TabIndex = 8;
            // 
            // label5
            // 
            label5.AutoSize = true;
            label5.Location = new Point(20, 198);
            label5.Name = "label5";
            label5.Size = new Size(118, 17);
            label5.TabIndex = 7;
            label5.Text = "Aktif Çerez/Cookie:";
            // 
            // txtSifre
            // 
            txtSifre.BackColor = Color.FromArgb(248, 249, 250);
            txtSifre.BorderStyle = BorderStyle.FixedSingle;
            txtSifre.ForeColor = Color.FromArgb(33, 37, 41);
            txtSifre.Location = new Point(140, 150);
            txtSifre.Name = "txtSifre";
            txtSifre.PasswordChar = '●';
            txtSifre.Size = new Size(405, 24);
            txtSifre.TabIndex = 6;
            // 
            // label4
            // 
            label4.AutoSize = true;
            label4.Location = new Point(20, 153);
            label4.Name = "label4";
            label4.Size = new Size(39, 17);
            label4.TabIndex = 5;
            label4.Text = "Şifre:";
            // 
            // txtEposta
            // 
            txtEposta.BackColor = Color.FromArgb(248, 249, 250);
            txtEposta.BorderStyle = BorderStyle.FixedSingle;
            txtEposta.ForeColor = Color.FromArgb(33, 37, 41);
            txtEposta.Location = new Point(140, 107);
            txtEposta.Name = "txtEposta";
            txtEposta.Size = new Size(405, 24);
            txtEposta.TabIndex = 4;
            // 
            // label3
            // 
            label3.AutoSize = true;
            label3.Location = new Point(20, 110);
            label3.Name = "label3";
            label3.Size = new Size(59, 17);
            label3.TabIndex = 3;
            label3.Text = "E-Posta:";
            // 
            // txtDomain
            // 
            txtDomain.BackColor = Color.FromArgb(248, 249, 250);
            txtDomain.BorderStyle = BorderStyle.FixedSingle;
            txtDomain.ForeColor = Color.FromArgb(33, 37, 41);
            txtDomain.Location = new Point(140, 65);
            txtDomain.Name = "txtDomain";
            txtDomain.Size = new Size(405, 24);
            txtDomain.TabIndex = 3;
            // 
            // label2
            // 
            label2.AutoSize = true;
            label2.Location = new Point(20, 68);
            label2.Name = "label2";
            label2.Size = new Size(93, 17);
            label2.TabIndex = 2;
            label2.Text = "Panel Domain:";
            // 
            // cmbPanelSec
            // 
            cmbPanelSec.BackColor = Color.FromArgb(248, 249, 250);
            cmbPanelSec.DropDownStyle = ComboBoxStyle.DropDownList;
            cmbPanelSec.FlatStyle = FlatStyle.Flat;
            cmbPanelSec.ForeColor = Color.FromArgb(33, 37, 41);
            cmbPanelSec.FormattingEnabled = true;
            cmbPanelSec.Location = new Point(140, 23);
            cmbPanelSec.Name = "cmbPanelSec";
            cmbPanelSec.Size = new Size(405, 25);
            cmbPanelSec.TabIndex = 1;
            cmbPanelSec.SelectedIndexChanged += cmbPanelSec_SelectedIndexChanged;
            // 
            // label1
            // 
            label1.AutoSize = true;
            label1.Location = new Point(20, 26);
            label1.Name = "label1";
            label1.Size = new Size(89, 17);
            label1.TabIndex = 0;
            label1.Text = "Yayın / Panel:";
            // 
            // SettingsForm
            // 
            AutoScaleDimensions = new SizeF(7F, 15F);
            AutoScaleMode = AutoScaleMode.Font;
            BackColor = Color.FromArgb(244, 246, 249);
            ClientSize = new Size(584, 430);
            Controls.Add(tabControl1);
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox = false;
            MinimizeBox = false;
            Name = "SettingsForm";
            StartPosition = FormStartPosition.CenterParent;
            Text = "Panel & Oturum Ayarları";
            tabControl1.ResumeLayout(false);
            tabPaneller.ResumeLayout(false);
            tabPaneller.PerformLayout();
            ResumeLayout(false);
        }

        #endregion

        private TabControl tabControl1;
        private TabPage tabPaneller;
        private ComboBox cmbPanelSec;
        private Label label1;
        private TextBox txtDomain;
        private Label label2;
        private TextBox txtEposta;
        private Label label3;
        private TextBox txtSifre;
        private Label label4;
        private TextBox txtCookie;
        private Label label5;
        private Label lblStatus;
        private Button btnTestLogin;
        private Button btnKaydet;
    }
}

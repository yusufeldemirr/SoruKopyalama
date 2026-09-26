namespace SoruKopyalama
{
    partial class Form1
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
            pnlHeader = new Panel();
            picLogo = new PictureBox();
            btnAyarlar = new Button();
            lblSubtitle = new Label();
            lblTitle = new Label();
            pnlMain = new TableLayoutPanel();
            grpIsEmri = new GroupBox();
            lblFen = new Label();
            txtFenId = new TextBox();
            lblMat = new Label();
            txtMatematikId = new TextBox();
            lblSos = new Label();
            txtSosyalId = new TextBox();
            lblTurk = new Label();
            txtTurkceId = new TextBox();
            btnAltKlasorleriGetir = new Button();
            txtAnaKlasorId = new TextBox();
            label2 = new Label();
            btnExcelSec = new Button();
            txtAdreslemeExceli = new TextBox();
            label1 = new Label();
            grpIstatistik = new GroupBox();
            lblKalanSayac = new Label();
            label10 = new Label();
            lblHataliSayac = new Label();
            label8 = new Label();
            lblBasariliSayac = new Label();
            label5 = new Label();
            lblToplamSayac = new Label();
            label3 = new Label();
            grpSorgu = new GroupBox();
            lblPanelSecim = new Label();
            cmbHedefPanel = new ComboBox();
            btnIdSorgula = new Button();
            txtSorguId = new TextBox();
            label4 = new Label();
            pnlKontroller = new Panel();
            btnRaporuIndir = new Button();
            btnIptal = new Button();
            btnDurdurDevam = new Button();
            btnBaslat = new Button();
            lblIlerlemeDurum = new Label();
            prgIlerleme = new ProgressBar();
            rtbLog = new RichTextBox();
            pnlHeader.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)picLogo).BeginInit();
            pnlMain.SuspendLayout();
            grpIsEmri.SuspendLayout();
            grpIstatistik.SuspendLayout();
            grpSorgu.SuspendLayout();
            pnlKontroller.SuspendLayout();
            SuspendLayout();
            // 
            // pnlHeader
            // 
            pnlHeader.BackColor = Color.White;
            pnlHeader.Controls.Add(picLogo);
            pnlHeader.Controls.Add(btnAyarlar);
            pnlHeader.Controls.Add(lblSubtitle);
            pnlHeader.Controls.Add(lblTitle);
            pnlHeader.Dock = DockStyle.Top;
            pnlHeader.Location = new Point(0, 0);
            pnlHeader.Name = "pnlHeader";
            pnlHeader.Padding = new Padding(20, 10, 20, 10);
            pnlHeader.Size = new Size(1220, 80);
            pnlHeader.TabIndex = 0;
            // 
            // picLogo
            // 
            picLogo.Location = new Point(18, 14);
            picLogo.Name = "picLogo";
            picLogo.Size = new Size(52, 52);
            picLogo.SizeMode = PictureBoxSizeMode.Zoom;
            picLogo.TabIndex = 3;
            picLogo.TabStop = false;
            // 
            // btnAyarlar
            // 
            btnAyarlar.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            btnAyarlar.BackColor = Color.FromArgb(248, 249, 250);
            btnAyarlar.Cursor = Cursors.Hand;
            btnAyarlar.FlatAppearance.BorderColor = Color.FromArgb(220, 53, 69);
            btnAyarlar.FlatStyle = FlatStyle.Flat;
            btnAyarlar.Font = new Font("Segoe UI", 9.5F, FontStyle.Bold);
            btnAyarlar.ForeColor = Color.FromArgb(220, 53, 69);
            btnAyarlar.Location = new Point(976, 20);
            btnAyarlar.Name = "btnAyarlar";
            btnAyarlar.Size = new Size(224, 40);
            btnAyarlar.TabIndex = 2;
            btnAyarlar.Text = "⚙️ Panel & Oturum Ayarları";
            btnAyarlar.UseVisualStyleBackColor = false;
            btnAyarlar.Click += btnAyarlar_Click;
            // 
            // lblSubtitle
            // 
            lblSubtitle.AutoSize = true;
            lblSubtitle.Font = new Font("Segoe UI", 9F);
            lblSubtitle.ForeColor = Color.FromArgb(108, 117, 125);
            lblSubtitle.Location = new Point(78, 46);
            lblSubtitle.Name = "lblSubtitle";
            lblSubtitle.Size = new Size(570, 15);
            lblSubtitle.TabIndex = 1;
            lblSubtitle.Text = "Tek Excel ile 4 Branş Otomatik Dağıtım (Türkçe • Sosyal • Matematik • Fen) | Akıllı Puanlama & Otomasyon";
            // 
            // lblTitle
            // 
            lblTitle.AutoSize = true;
            lblTitle.Font = new Font("Segoe UI", 15F, FontStyle.Bold);
            lblTitle.ForeColor = Color.FromArgb(220, 53, 69);
            lblTitle.Location = new Point(76, 14);
            lblTitle.Name = "lblTitle";
            lblTitle.Size = new Size(346, 28);
            lblTitle.TabIndex = 0;
            lblTitle.Text = "Limit Soru Kopyalama Sistemi v2.5";
            // 
            // pnlMain
            // 
            pnlMain.BackColor = Color.FromArgb(244, 246, 249);
            pnlMain.ColumnCount = 3;
            pnlMain.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 54F));
            pnlMain.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 23F));
            pnlMain.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 23F));
            pnlMain.Controls.Add(grpIsEmri, 0, 0);
            pnlMain.Controls.Add(grpIstatistik, 1, 0);
            pnlMain.Controls.Add(grpSorgu, 2, 0);
            pnlMain.Dock = DockStyle.Top;
            pnlMain.Location = new Point(0, 80);
            pnlMain.Name = "pnlMain";
            pnlMain.Padding = new Padding(12, 10, 12, 5);
            pnlMain.RowCount = 1;
            pnlMain.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
            pnlMain.Size = new Size(1220, 215);
            pnlMain.TabIndex = 1;
            // 
            // grpIsEmri
            // 
            grpIsEmri.BackColor = Color.White;
            grpIsEmri.Controls.Add(lblFen);
            grpIsEmri.Controls.Add(txtFenId);
            grpIsEmri.Controls.Add(lblMat);
            grpIsEmri.Controls.Add(txtMatematikId);
            grpIsEmri.Controls.Add(lblSos);
            grpIsEmri.Controls.Add(txtSosyalId);
            grpIsEmri.Controls.Add(lblTurk);
            grpIsEmri.Controls.Add(txtTurkceId);
            grpIsEmri.Controls.Add(btnAltKlasorleriGetir);
            grpIsEmri.Controls.Add(txtAnaKlasorId);
            grpIsEmri.Controls.Add(label2);
            grpIsEmri.Controls.Add(btnExcelSec);
            grpIsEmri.Controls.Add(txtAdreslemeExceli);
            grpIsEmri.Controls.Add(label1);
            grpIsEmri.Dock = DockStyle.Fill;
            grpIsEmri.Font = new Font("Segoe UI Semibold", 9.5F, FontStyle.Bold);
            grpIsEmri.ForeColor = Color.FromArgb(33, 37, 41);
            grpIsEmri.Location = new Point(15, 13);
            grpIsEmri.Name = "grpIsEmri";
            grpIsEmri.Size = new Size(640, 194);
            grpIsEmri.TabIndex = 0;
            grpIsEmri.TabStop = false;
            grpIsEmri.Text = " 📋 1. İş Emri ve Klasör Eşleştirme ";
            // 
            // lblFen
            // 
            lblFen.AutoSize = true;
            lblFen.Font = new Font("Segoe UI", 8.5F, FontStyle.Bold);
            lblFen.ForeColor = Color.FromArgb(217, 119, 6);
            lblFen.Location = new Point(480, 137);
            lblFen.Name = "lblFen";
            lblFen.Size = new Size(69, 15);
            lblFen.TabIndex = 13;
            lblFen.Text = "🟡 Fen ID:";
            // 
            // txtFenId
            // 
            txtFenId.BackColor = Color.FromArgb(248, 249, 250);
            txtFenId.BorderStyle = BorderStyle.FixedSingle;
            txtFenId.ForeColor = Color.FromArgb(33, 37, 41);
            txtFenId.Location = new Point(480, 155);
            txtFenId.Name = "txtFenId";
            txtFenId.PlaceholderText = "Fen Klasör ID";
            txtFenId.Size = new Size(145, 24);
            txtFenId.TabIndex = 12;
            // 
            // lblMat
            // 
            lblMat.AutoSize = true;
            lblMat.Font = new Font("Segoe UI", 8.5F, FontStyle.Bold);
            lblMat.ForeColor = Color.FromArgb(25, 135, 84);
            lblMat.Location = new Point(325, 137);
            lblMat.Name = "lblMat";
            lblMat.Size = new Size(71, 15);
            lblMat.TabIndex = 11;
            lblMat.Text = "🟢 Mat ID:";
            // 
            // txtMatematikId
            // 
            txtMatematikId.BackColor = Color.FromArgb(248, 249, 250);
            txtMatematikId.BorderStyle = BorderStyle.FixedSingle;
            txtMatematikId.ForeColor = Color.FromArgb(33, 37, 41);
            txtMatematikId.Location = new Point(325, 155);
            txtMatematikId.Name = "txtMatematikId";
            txtMatematikId.PlaceholderText = "Mat Klasör ID";
            txtMatematikId.Size = new Size(145, 24);
            txtMatematikId.TabIndex = 10;
            // 
            // lblSos
            // 
            lblSos.AutoSize = true;
            lblSos.Font = new Font("Segoe UI", 8.5F, FontStyle.Bold);
            lblSos.ForeColor = Color.FromArgb(13, 110, 253);
            lblSos.Location = new Point(170, 137);
            lblSos.Name = "lblSos";
            lblSos.Size = new Size(73, 15);
            lblSos.TabIndex = 9;
            lblSos.Text = "🔵 Sosyal ID:";
            // 
            // txtSosyalId
            // 
            txtSosyalId.BackColor = Color.FromArgb(248, 249, 250);
            txtSosyalId.BorderStyle = BorderStyle.FixedSingle;
            txtSosyalId.ForeColor = Color.FromArgb(33, 37, 41);
            txtSosyalId.Location = new Point(170, 155);
            txtSosyalId.Name = "txtSosyalId";
            txtSosyalId.PlaceholderText = "Sosyal Klasör ID";
            txtSosyalId.Size = new Size(145, 24);
            txtSosyalId.TabIndex = 8;
            // 
            // lblTurk
            // 
            lblTurk.AutoSize = true;
            lblTurk.Font = new Font("Segoe UI", 8.5F, FontStyle.Bold);
            lblTurk.ForeColor = Color.FromArgb(220, 53, 69);
            lblTurk.Location = new Point(15, 137);
            lblTurk.Name = "lblTurk";
            lblTurk.Size = new Size(77, 15);
            lblTurk.TabIndex = 7;
            lblTurk.Text = "🔴 Türkçe ID:";
            // 
            // txtTurkceId
            // 
            txtTurkceId.BackColor = Color.FromArgb(248, 249, 250);
            txtTurkceId.BorderStyle = BorderStyle.FixedSingle;
            txtTurkceId.ForeColor = Color.FromArgb(33, 37, 41);
            txtTurkceId.Location = new Point(15, 155);
            txtTurkceId.Name = "txtTurkceId";
            txtTurkceId.PlaceholderText = "Türkçe Klasör ID";
            txtTurkceId.Size = new Size(145, 24);
            txtTurkceId.TabIndex = 6;
            // 
            // btnAltKlasorleriGetir
            // 
            btnAltKlasorleriGetir.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            btnAltKlasorleriGetir.BackColor = Color.FromArgb(220, 53, 69);
            btnAltKlasorleriGetir.Cursor = Cursors.Hand;
            btnAltKlasorleriGetir.FlatAppearance.BorderSize = 0;
            btnAltKlasorleriGetir.FlatStyle = FlatStyle.Flat;
            btnAltKlasorleriGetir.Font = new Font("Segoe UI", 8.5F, FontStyle.Bold);
            btnAltKlasorleriGetir.ForeColor = Color.White;
            btnAltKlasorleriGetir.Location = new Point(480, 95);
            btnAltKlasorleriGetir.Name = "btnAltKlasorleriGetir";
            btnAltKlasorleriGetir.Size = new Size(145, 28);
            btnAltKlasorleriGetir.TabIndex = 5;
            btnAltKlasorleriGetir.Text = "🔍 Alt Klasörleri Getir";
            btnAltKlasorleriGetir.UseVisualStyleBackColor = false;
            btnAltKlasorleriGetir.Click += btnAltKlasorleriGetir_Click;
            // 
            // txtAnaKlasorId
            // 
            txtAnaKlasorId.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
            txtAnaKlasorId.BackColor = Color.FromArgb(248, 249, 250);
            txtAnaKlasorId.BorderStyle = BorderStyle.FixedSingle;
            txtAnaKlasorId.ForeColor = Color.FromArgb(33, 37, 41);
            txtAnaKlasorId.Location = new Point(15, 97);
            txtAnaKlasorId.Name = "txtAnaKlasorId";
            txtAnaKlasorId.PlaceholderText = "Deneme Ana Klasör ID (örn: 186360)";
            txtAnaKlasorId.Size = new Size(455, 24);
            txtAnaKlasorId.TabIndex = 4;
            // 
            // label2
            // 
            label2.AutoSize = true;
            label2.Font = new Font("Segoe UI", 9F);
            label2.ForeColor = Color.FromArgb(73, 80, 87);
            label2.Location = new Point(15, 78);
            label2.Name = "label2";
            label2.Size = new Size(147, 15);
            label2.TabIndex = 3;
            label2.Text = "Deneme Ana Klasör ID:";
            // 
            // btnExcelSec
            // 
            btnExcelSec.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            btnExcelSec.BackColor = Color.FromArgb(233, 236, 239);
            btnExcelSec.Cursor = Cursors.Hand;
            btnExcelSec.FlatAppearance.BorderColor = Color.FromArgb(206, 212, 218);
            btnExcelSec.FlatStyle = FlatStyle.Flat;
            btnExcelSec.Font = new Font("Segoe UI", 8.5F, FontStyle.Bold);
            btnExcelSec.ForeColor = Color.FromArgb(33, 37, 41);
            btnExcelSec.Location = new Point(480, 44);
            btnExcelSec.Name = "btnExcelSec";
            btnExcelSec.Size = new Size(145, 28);
            btnExcelSec.TabIndex = 2;
            btnExcelSec.Text = "📁 Excel Seç...";
            btnExcelSec.UseVisualStyleBackColor = false;
            btnExcelSec.Click += btnExcelSec_Click;
            // 
            // txtAdreslemeExceli
            // 
            txtAdreslemeExceli.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
            txtAdreslemeExceli.BackColor = Color.FromArgb(248, 249, 250);
            txtAdreslemeExceli.BorderStyle = BorderStyle.FixedSingle;
            txtAdreslemeExceli.ForeColor = Color.FromArgb(33, 37, 41);
            txtAdreslemeExceli.Location = new Point(15, 46);
            txtAdreslemeExceli.Name = "txtAdreslemeExceli";
            txtAdreslemeExceli.PlaceholderText = "Seçilen Adresleme Excel Dosya Yolu...";
            txtAdreslemeExceli.Size = new Size(455, 24);
            txtAdreslemeExceli.TabIndex = 1;
            // 
            // label1
            // 
            label1.AutoSize = true;
            label1.Font = new Font("Segoe UI", 9F);
            label1.ForeColor = Color.FromArgb(73, 80, 87);
            label1.Location = new Point(15, 27);
            label1.Name = "label1";
            label1.Size = new Size(139, 15);
            label1.TabIndex = 0;
            label1.Text = "Adresleme Excel Dosyası:";
            // 
            // grpIstatistik
            // 
            grpIstatistik.BackColor = Color.White;
            grpIstatistik.Controls.Add(lblKalanSayac);
            grpIstatistik.Controls.Add(label10);
            grpIstatistik.Controls.Add(lblHataliSayac);
            grpIstatistik.Controls.Add(label8);
            grpIstatistik.Controls.Add(lblBasariliSayac);
            grpIstatistik.Controls.Add(label5);
            grpIstatistik.Controls.Add(lblToplamSayac);
            grpIstatistik.Controls.Add(label3);
            grpIstatistik.Dock = DockStyle.Fill;
            grpIstatistik.Font = new Font("Segoe UI Semibold", 9.5F, FontStyle.Bold);
            grpIstatistik.ForeColor = Color.FromArgb(33, 37, 41);
            grpIstatistik.Location = new Point(661, 13);
            grpIstatistik.Name = "grpIstatistik";
            grpIstatistik.Size = new Size(270, 194);
            grpIstatistik.TabIndex = 1;
            grpIstatistik.TabStop = false;
            grpIstatistik.Text = " 📊 Canlı Sayaç ";
            // 
            // lblKalanSayac
            // 
            lblKalanSayac.AutoSize = true;
            lblKalanSayac.Font = new Font("Segoe UI", 13F, FontStyle.Bold);
            lblKalanSayac.ForeColor = Color.FromArgb(13, 110, 253);
            lblKalanSayac.Location = new Point(145, 125);
            lblKalanSayac.Name = "lblKalanSayac";
            lblKalanSayac.Size = new Size(22, 25);
            lblKalanSayac.TabIndex = 7;
            lblKalanSayac.Text = "0";
            // 
            // label10
            // 
            label10.AutoSize = true;
            label10.Font = new Font("Segoe UI", 8.5F);
            label10.ForeColor = Color.FromArgb(108, 117, 125);
            label10.Location = new Point(145, 105);
            label10.Name = "label10";
            label10.Size = new Size(67, 15);
            label10.TabIndex = 6;
            label10.Text = "Kalan Soru:";
            // 
            // lblHataliSayac
            // 
            lblHataliSayac.AutoSize = true;
            lblHataliSayac.Font = new Font("Segoe UI", 13F, FontStyle.Bold);
            lblHataliSayac.ForeColor = Color.FromArgb(220, 53, 69);
            lblHataliSayac.Location = new Point(145, 55);
            lblHataliSayac.Name = "lblHataliSayac";
            lblHataliSayac.Size = new Size(22, 25);
            lblHataliSayac.TabIndex = 5;
            lblHataliSayac.Text = "0";
            // 
            // label8
            // 
            label8.AutoSize = true;
            label8.Font = new Font("Segoe UI", 8.5F);
            label8.ForeColor = Color.FromArgb(108, 117, 125);
            label8.Location = new Point(145, 35);
            label8.Name = "label8";
            label8.Size = new Size(82, 15);
            label8.TabIndex = 4;
            label8.Text = "Hatalı / Eksik:";
            // 
            // lblBasariliSayac
            // 
            lblBasariliSayac.AutoSize = true;
            lblBasariliSayac.Font = new Font("Segoe UI", 13F, FontStyle.Bold);
            lblBasariliSayac.ForeColor = Color.FromArgb(25, 135, 84);
            lblBasariliSayac.Location = new Point(20, 125);
            lblBasariliSayac.Name = "lblBasariliSayac";
            lblBasariliSayac.Size = new Size(22, 25);
            lblBasariliSayac.TabIndex = 3;
            lblBasariliSayac.Text = "0";
            // 
            // label5
            // 
            label5.AutoSize = true;
            label5.Font = new Font("Segoe UI", 8.5F);
            label5.ForeColor = Color.FromArgb(108, 117, 125);
            label5.Location = new Point(20, 105);
            label5.Name = "label5";
            label5.Size = new Size(50, 15);
            label5.TabIndex = 2;
            label5.Text = "Başarılı:";
            // 
            // lblToplamSayac
            // 
            lblToplamSayac.AutoSize = true;
            lblToplamSayac.Font = new Font("Segoe UI", 13F, FontStyle.Bold);
            lblToplamSayac.ForeColor = Color.FromArgb(33, 37, 41);
            lblToplamSayac.Location = new Point(20, 55);
            lblToplamSayac.Name = "lblToplamSayac";
            lblToplamSayac.Size = new Size(22, 25);
            lblToplamSayac.TabIndex = 1;
            lblToplamSayac.Text = "0";
            // 
            // label3
            // 
            label3.AutoSize = true;
            label3.Font = new Font("Segoe UI", 8.5F);
            label3.ForeColor = Color.FromArgb(108, 117, 125);
            label3.Location = new Point(20, 35);
            label3.Name = "label3";
            label3.Size = new Size(76, 15);
            label3.TabIndex = 0;
            label3.Text = "Toplam Soru:";
            // 
            // grpSorgu
            // 
            grpSorgu.BackColor = Color.White;
            grpSorgu.Controls.Add(lblPanelSecim);
            grpSorgu.Controls.Add(cmbHedefPanel);
            grpSorgu.Controls.Add(btnIdSorgula);
            grpSorgu.Controls.Add(txtSorguId);
            grpSorgu.Controls.Add(label4);
            grpSorgu.Dock = DockStyle.Fill;
            grpSorgu.Font = new Font("Segoe UI Semibold", 9.5F, FontStyle.Bold);
            grpSorgu.ForeColor = Color.FromArgb(33, 37, 41);
            grpSorgu.Location = new Point(937, 13);
            grpSorgu.Name = "grpSorgu";
            grpSorgu.Size = new Size(271, 194);
            grpSorgu.TabIndex = 2;
            grpSorgu.TabStop = false;
            grpSorgu.Text = " 🔍 Hızlı Sorgu & Panel ";
            // 
            // lblPanelSecim
            // 
            lblPanelSecim.AutoSize = true;
            lblPanelSecim.Font = new Font("Segoe UI", 8.5F);
            lblPanelSecim.ForeColor = Color.FromArgb(73, 80, 87);
            lblPanelSecim.Location = new Point(15, 27);
            lblPanelSecim.Name = "lblPanelSecim";
            lblPanelSecim.Size = new Size(73, 15);
            lblPanelSecim.TabIndex = 4;
            lblPanelSecim.Text = "Hedef Panel:";
            // 
            // cmbHedefPanel
            // 
            cmbHedefPanel.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
            cmbHedefPanel.BackColor = Color.FromArgb(248, 249, 250);
            cmbHedefPanel.DropDownStyle = ComboBoxStyle.DropDownList;
            cmbHedefPanel.FlatStyle = FlatStyle.Flat;
            cmbHedefPanel.ForeColor = Color.FromArgb(33, 37, 41);
            cmbHedefPanel.FormattingEnabled = true;
            cmbHedefPanel.Location = new Point(15, 46);
            cmbHedefPanel.Name = "cmbHedefPanel";
            cmbHedefPanel.Size = new Size(241, 25);
            cmbHedefPanel.TabIndex = 3;
            // 
            // btnIdSorgula
            // 
            btnIdSorgula.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
            btnIdSorgula.BackColor = Color.FromArgb(233, 236, 239);
            btnIdSorgula.Cursor = Cursors.Hand;
            btnIdSorgula.FlatAppearance.BorderColor = Color.FromArgb(206, 212, 218);
            btnIdSorgula.FlatStyle = FlatStyle.Flat;
            btnIdSorgula.Font = new Font("Segoe UI", 9F, FontStyle.Bold);
            btnIdSorgula.ForeColor = Color.FromArgb(33, 37, 41);
            btnIdSorgula.Location = new Point(15, 147);
            btnIdSorgula.Name = "btnIdSorgula";
            btnIdSorgula.Size = new Size(241, 32);
            btnIdSorgula.TabIndex = 2;
            btnIdSorgula.Text = "🔎 Hafızada Sorgula";
            btnIdSorgula.UseVisualStyleBackColor = false;
            btnIdSorgula.Click += btnIdSorgula_Click;
            // 
            // txtSorguId
            // 
            txtSorguId.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
            txtSorguId.BackColor = Color.FromArgb(248, 249, 250);
            txtSorguId.BorderStyle = BorderStyle.FixedSingle;
            txtSorguId.ForeColor = Color.FromArgb(33, 37, 41);
            txtSorguId.Location = new Point(15, 110);
            txtSorguId.Name = "txtSorguId";
            txtSorguId.PlaceholderText = "Solution ID veya Source ID";
            txtSorguId.Size = new Size(241, 24);
            txtSorguId.TabIndex = 1;
            // 
            // label4
            // 
            label4.AutoSize = true;
            label4.Font = new Font("Segoe UI", 8.5F);
            label4.ForeColor = Color.FromArgb(73, 80, 87);
            label4.Location = new Point(15, 88);
            label4.Name = "label4";
            label4.Size = new Size(126, 15);
            label4.TabIndex = 0;
            label4.Text = "Soru ID Hafıza Arama:";
            // 
            // pnlKontroller
            // 
            pnlKontroller.BackColor = Color.FromArgb(244, 246, 249);
            pnlKontroller.Controls.Add(btnRaporuIndir);
            pnlKontroller.Controls.Add(btnIptal);
            pnlKontroller.Controls.Add(btnDurdurDevam);
            pnlKontroller.Controls.Add(btnBaslat);
            pnlKontroller.Controls.Add(lblIlerlemeDurum);
            pnlKontroller.Controls.Add(prgIlerleme);
            pnlKontroller.Dock = DockStyle.Top;
            pnlKontroller.Location = new Point(0, 295);
            pnlKontroller.Name = "pnlKontroller";
            pnlKontroller.Padding = new Padding(15, 8, 15, 8);
            pnlKontroller.Size = new Size(1220, 115);
            pnlKontroller.TabIndex = 2;
            // 
            // btnRaporuIndir
            // 
            btnRaporuIndir.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            btnRaporuIndir.BackColor = Color.White;
            btnRaporuIndir.Cursor = Cursors.Hand;
            btnRaporuIndir.Enabled = false;
            btnRaporuIndir.FlatAppearance.BorderColor = Color.FromArgb(25, 135, 84);
            btnRaporuIndir.FlatStyle = FlatStyle.Flat;
            btnRaporuIndir.Font = new Font("Segoe UI", 10F, FontStyle.Bold);
            btnRaporuIndir.ForeColor = Color.FromArgb(25, 135, 84);
            btnRaporuIndir.Location = new Point(976, 52);
            btnRaporuIndir.Name = "btnRaporuIndir";
            btnRaporuIndir.Size = new Size(229, 48);
            btnRaporuIndir.TabIndex = 5;
            btnRaporuIndir.Text = "📊 Raporu İndir (Excel)";
            btnRaporuIndir.UseVisualStyleBackColor = false;
            btnRaporuIndir.Click += btnRaporuIndir_Click;
            // 
            // btnIptal
            // 
            btnIptal.BackColor = Color.FromArgb(220, 53, 69);
            btnIptal.Cursor = Cursors.Hand;
            btnIptal.Enabled = false;
            btnIptal.FlatAppearance.BorderSize = 0;
            btnIptal.FlatStyle = FlatStyle.Flat;
            btnIptal.Font = new Font("Segoe UI", 10F, FontStyle.Bold);
            btnIptal.ForeColor = Color.White;
            btnIptal.Location = new Point(600, 52);
            btnIptal.Name = "btnIptal";
            btnIptal.Size = new Size(155, 48);
            btnIptal.TabIndex = 4;
            btnIptal.Text = "⏹️ İPTAL ET";
            btnIptal.UseVisualStyleBackColor = false;
            btnIptal.Click += btnIptal_Click;
            // 
            // btnDurdurDevam
            // 
            btnDurdurDevam.BackColor = Color.FromArgb(243, 156, 18);
            btnDurdurDevam.Cursor = Cursors.Hand;
            btnDurdurDevam.Enabled = false;
            btnDurdurDevam.FlatAppearance.BorderSize = 0;
            btnDurdurDevam.FlatStyle = FlatStyle.Flat;
            btnDurdurDevam.Font = new Font("Segoe UI", 10F, FontStyle.Bold);
            btnDurdurDevam.ForeColor = Color.White;
            btnDurdurDevam.Location = new Point(410, 52);
            btnDurdurDevam.Name = "btnDurdurDevam";
            btnDurdurDevam.Size = new Size(175, 48);
            btnDurdurDevam.TabIndex = 3;
            btnDurdurDevam.Text = "⏸️ DURAKLAT";
            btnDurdurDevam.UseVisualStyleBackColor = false;
            btnDurdurDevam.Click += btnDurdurDevam_Click;
            // 
            // btnBaslat
            // 
            btnBaslat.BackColor = Color.White;
            btnBaslat.Cursor = Cursors.Hand;
            btnBaslat.FlatAppearance.BorderColor = Color.FromArgb(220, 53, 69);
            btnBaslat.FlatAppearance.BorderSize = 2;
            btnBaslat.FlatStyle = FlatStyle.Flat;
            btnBaslat.Font = new Font("Segoe UI", 11F, FontStyle.Bold);
            btnBaslat.ForeColor = Color.FromArgb(220, 53, 69);
            btnBaslat.Location = new Point(15, 52);
            btnBaslat.Name = "btnBaslat";
            btnBaslat.Size = new Size(380, 48);
            btnBaslat.TabIndex = 2;
            btnBaslat.Text = "🚀 TÜM DENEMEYİ KOPYALA";
            btnBaslat.UseVisualStyleBackColor = false;
            btnBaslat.Click += btnBaslat_Click;
            // 
            // lblIlerlemeDurum
            // 
            lblIlerlemeDurum.AutoSize = true;
            lblIlerlemeDurum.Font = new Font("Segoe UI", 9F);
            lblIlerlemeDurum.ForeColor = Color.FromArgb(73, 80, 87);
            lblIlerlemeDurum.Location = new Point(15, 8);
            lblIlerlemeDurum.Name = "lblIlerlemeDurum";
            lblIlerlemeDurum.Size = new Size(97, 15);
            lblIlerlemeDurum.TabIndex = 1;
            lblIlerlemeDurum.Text = "Durum: Bekliyor.";
            // 
            // prgIlerleme
            // 
            prgIlerleme.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
            prgIlerleme.Location = new Point(15, 27);
            prgIlerleme.Name = "prgIlerleme";
            prgIlerleme.Size = new Size(1190, 16);
            prgIlerleme.TabIndex = 0;
            // 
            // rtbLog
            // 
            rtbLog.BackColor = Color.White;
            rtbLog.BorderStyle = BorderStyle.None;
            rtbLog.Dock = DockStyle.Fill;
            rtbLog.Font = new Font("Consolas", 10F);
            rtbLog.ForeColor = Color.FromArgb(33, 37, 41);
            rtbLog.Location = new Point(0, 410);
            rtbLog.Name = "rtbLog";
            rtbLog.ReadOnly = true;
            rtbLog.Size = new Size(1220, 290);
            rtbLog.TabIndex = 3;
            rtbLog.Text = "";
            // 
            // Form1
            // 
            AutoScaleDimensions = new SizeF(7F, 15F);
            AutoScaleMode = AutoScaleMode.Font;
            BackColor = Color.FromArgb(244, 246, 249);
            ClientSize = new Size(1220, 700);
            Controls.Add(rtbLog);
            Controls.Add(pnlKontroller);
            Controls.Add(pnlMain);
            Controls.Add(pnlHeader);
            Font = new Font("Segoe UI", 9F);
            MinimumSize = new Size(1050, 650);
            Name = "Form1";
            StartPosition = FormStartPosition.CenterScreen;
            Text = "Limit Soru Kopyalama";
            Load += Form1_Load;
            pnlHeader.ResumeLayout(false);
            pnlHeader.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)picLogo).EndInit();
            pnlMain.ResumeLayout(false);
            grpIsEmri.ResumeLayout(false);
            grpIsEmri.PerformLayout();
            grpIstatistik.ResumeLayout(false);
            grpIstatistik.PerformLayout();
            grpSorgu.ResumeLayout(false);
            grpSorgu.PerformLayout();
            pnlKontroller.ResumeLayout(false);
            pnlKontroller.PerformLayout();
            ResumeLayout(false);
        }

        #endregion

        private Panel pnlHeader;
        private PictureBox picLogo;
        private Label lblTitle;
        private Label lblSubtitle;
        private Button btnAyarlar;
        private TableLayoutPanel pnlMain;
        private GroupBox grpIsEmri;
        private Label label1;
        private TextBox txtAdreslemeExceli;
        private Button btnExcelSec;
        private Label label2;
        private TextBox txtAnaKlasorId;
        private Button btnAltKlasorleriGetir;
        private Label lblTurk;
        private TextBox txtTurkceId;
        private Label lblSos;
        private TextBox txtSosyalId;
        private Label lblMat;
        private TextBox txtMatematikId;
        private Label lblFen;
        private TextBox txtFenId;
        private GroupBox grpIstatistik;
        private Label label3;
        private Label lblToplamSayac;
        private Label lblBasariliSayac;
        private Label label5;
        private Label lblHataliSayac;
        private Label label8;
        private Label lblKalanSayac;
        private Label label10;
        private GroupBox grpSorgu;
        private Label label4;
        private TextBox txtSorguId;
        private Button btnIdSorgula;
        private ComboBox cmbHedefPanel;
        private Label lblPanelSecim;
        private Panel pnlKontroller;
        private ProgressBar prgIlerleme;
        private Label lblIlerlemeDurum;
        private Button btnBaslat;
        private Button btnDurdurDevam;
        private Button btnIptal;
        private Button btnRaporuIndir;
        private RichTextBox rtbLog;
    }
}

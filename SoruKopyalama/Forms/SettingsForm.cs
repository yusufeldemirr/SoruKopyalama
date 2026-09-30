using System;
using System.Drawing;
using System.Windows.Forms;
using SoruKopyalama.Services;

namespace SoruKopyalama.Forms
{
    public partial class SettingsForm : Form
    {
        private readonly FernusSessionManager _sessionManager;

        public SettingsForm(FernusSessionManager sessionManager, string? seciliPanel = null)
        {
            InitializeComponent();
            _sessionManager = sessionManager;

            cmbPanelSec.Items.Clear();
            foreach (var panel in _sessionManager.Settings.Panels)
                cmbPanelSec.Items.Add(panel.Name);

            if (cmbPanelSec.Items.Count > 0)
            {
                int idx = cmbPanelSec.FindStringExact(seciliPanel ?? _sessionManager.Settings.DefaultTargetPanel);
                cmbPanelSec.SelectedIndex = idx >= 0 ? idx : 0;
            }
        }

        private void cmbPanelSec_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (cmbPanelSec.SelectedItem == null) return;
            var panel = _sessionManager.GetPanel(cmbPanelSec.SelectedItem.ToString()!);

            txtDomain.Text = panel.Domain;
            txtEposta.Text = panel.Email;
            txtSifre.Text = panel.Password;
            txtCookie.Text = panel.ActiveCookie;
            lblStatus.Text = panel.LastLoginTime != default
                ? $"Son Giriş: {panel.LastLoginTime:dd.MM.yyyy HH:mm:ss}"
                : "Durum: Henüz oturum açılmadı.";
            lblStatus.ForeColor = Color.FromArgb(108, 117, 125);
        }

        private void btnKaydet_Click(object sender, EventArgs e)
        {
            if (cmbPanelSec.SelectedItem == null) return;
            var panel = _sessionManager.GetPanel(cmbPanelSec.SelectedItem.ToString()!);

            panel.Domain = txtDomain.Text.Trim();
            panel.Email = txtEposta.Text.Trim();
            panel.Password = txtSifre.Text.Trim();
            panel.ActiveCookie = txtCookie.Text.Trim();
            _sessionManager.SaveSettings();

            lblStatus.Text = "Ayarlar kaydedildi.";
            lblStatus.ForeColor = Color.FromArgb(39, 174, 96);
        }

        private async void btnTestLogin_Click(object sender, EventArgs e)
        {
            if (cmbPanelSec.SelectedItem == null) return;
            string selectedName = cmbPanelSec.SelectedItem.ToString()!;
            var panel = _sessionManager.GetPanel(selectedName);

            panel.Domain = txtDomain.Text.Trim();
            panel.Email = txtEposta.Text.Trim();
            panel.Password = txtSifre.Text.Trim();
            _sessionManager.SaveSettings();

            using var browserForm = new BrowserLoginForm(_sessionManager, selectedName);
            browserForm.ShowDialog(this);

            if (browserForm.LoginSuccess)
            {
                txtCookie.Text = browserForm.ExtractedCookie;
                lblStatus.Text = "✅ Oturum açıldı, çerez panelde doğrulandı ve kaydedildi.";
                lblStatus.ForeColor = Color.FromArgb(39, 174, 96);
            }
            else
            {
                lblStatus.Text = "Oturum tamamlanamadı veya pencere kapatıldı.";
                lblStatus.ForeColor = Color.FromArgb(211, 84, 0);
            }

            // Mevcut çerezin durumunu da göster
            var durum = await _sessionManager.OturumGecerliMiAsync(selectedName);
            if (durum == OturumDurumu.Gecersiz)
            {
                lblStatus.Text += " (Dikkat: kayıtlı çerez panelde geçerli değil)";
                lblStatus.ForeColor = Color.FromArgb(211, 84, 0);
            }
        }
    }
}

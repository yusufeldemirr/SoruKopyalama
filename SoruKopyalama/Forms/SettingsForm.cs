using System;
using System.Drawing;
using System.Threading.Tasks;
using System.Windows.Forms;
using SoruKopyalama.Models;
using SoruKopyalama.Services;

namespace SoruKopyalama.Forms
{
    public partial class SettingsForm : Form
    {
        private readonly FernusSessionManager _sessionManager;
        private readonly DatabaseManager _dbManager;

        public SettingsForm(FernusSessionManager sessionManager, DatabaseManager dbManager)
        {
            InitializeComponent();
            _sessionManager = sessionManager;
            _dbManager = dbManager;

            LoadPanelList();
        }

        private void LoadPanelList()
        {
            cmbPanelSec.Items.Clear();
            cmbSyncPanel.Items.Clear();

            foreach (var panel in _sessionManager.Settings.Panels)
            {
                cmbPanelSec.Items.Add(panel.Name);
                cmbSyncPanel.Items.Add(panel.Name);
            }

            if (cmbPanelSec.Items.Count > 0)
            {
                int limitIdx = cmbPanelSec.FindStringExact("Limit");
                cmbPanelSec.SelectedIndex = limitIdx >= 0 ? limitIdx : 0;
                cmbSyncPanel.SelectedIndex = limitIdx >= 0 ? limitIdx : 0;
            }
        }

        private void cmbPanelSec_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (cmbPanelSec.SelectedItem == null) return;
            string selectedName = cmbPanelSec.SelectedItem.ToString()!;
            var panel = _sessionManager.GetPanel(selectedName);

            txtDomain.Text = panel.Domain;
            txtEposta.Text = panel.Email;
            txtSifre.Text = panel.Password;
            txtCookie.Text = panel.ActiveCookie;
            lblStatus.Text = panel.LastLoginTime != default 
                ? $"Son Giriş: {panel.LastLoginTime:dd.MM.yyyy HH:mm:ss}" 
                : "Durum: Henüz oturum açılmadı.";
            lblStatus.ForeColor = Color.LightGray;
        }

        private void btnKaydet_Click(object sender, EventArgs e)
        {
            if (cmbPanelSec.SelectedItem == null) return;
            string selectedName = cmbPanelSec.SelectedItem.ToString()!;
            var panel = _sessionManager.GetPanel(selectedName);

            panel.Domain = txtDomain.Text.Trim();
            panel.Email = txtEposta.Text.Trim();
            panel.Password = txtSifre.Text.Trim();
            panel.ActiveCookie = txtCookie.Text.Trim();

            _sessionManager.SaveSettings();

            lblStatus.Text = "Ayarlar başarıyla kaydedildi!";
            lblStatus.ForeColor = Color.LimeGreen;
        }

        private void btnTestLogin_Click(object sender, EventArgs e)
        {
            if (cmbPanelSec.SelectedItem == null) return;
            string selectedName = cmbPanelSec.SelectedItem.ToString()!;
            var panel = _sessionManager.GetPanel(selectedName);

            // Önce güncel bilgileri panele kaydet
            panel.Domain = txtDomain.Text.Trim();
            panel.Email = txtEposta.Text.Trim();
            panel.Password = txtSifre.Text.Trim();
            _sessionManager.SaveSettings();

            using var browserForm = new BrowserLoginForm(_sessionManager, selectedName);
            var dr = browserForm.ShowDialog(this);

            if (browserForm.LoginSuccess)
            {
                txtCookie.Text = browserForm.ExtractedCookie;
                lblStatus.Text = "BAŞARILI: Tarayıcı oturumu açıldı ve tüm çerezler eksiksiz alındı!";
                lblStatus.ForeColor = Color.LimeGreen;
            }
            else
            {
                lblStatus.Text = "Oturum tam tamamlanamadı veya pencere kapatıldı.";
                lblStatus.ForeColor = Color.Orange;
            }
        }

        private async void btnPaneldenCek_Click(object sender, EventArgs e)
        {
            if (cmbSyncPanel.SelectedItem == null)
            {
                MessageBox.Show("Lütfen bir panel seçin.", "Uyarı", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            string panelName = cmbSyncPanel.SelectedItem.ToString()!;
            string folderId = txtSyncKlasorId.Text.Trim();
            string folderTitle = txtSyncBaslik.Text.Trim();

            if (string.IsNullOrEmpty(folderId))
            {
                MessageBox.Show("Lütfen Kaynak Klasör ID'sini girin.", "Uyarı", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            btnPaneldenCek.Enabled = false;
            lblSyncSonuc.Text = "Sorular panelden çekiliyor, lütfen bekleyin...";
            lblSyncSonuc.ForeColor = Color.Yellow;

            var panel = _sessionManager.GetPanel(panelName);
            using var client = _sessionManager.CreateHttpClient(panelName);

            var progress = new Progress<string>(msg =>
            {
                lblSyncSonuc.Text = msg;
            });

            var result = await _dbManager.SyncFolderFromPanelAsync(client, panel.Domain, folderId, folderTitle, progress);

            btnPaneldenCek.Enabled = true;

            if (result.Success)
            {
                lblSyncSonuc.Text = $"🎉 {result.Message}\n\nToplam soru havuzundaki soru sayısı: {_dbManager.SistemVeritabani.Count}";
                lblSyncSonuc.ForeColor = Color.LimeGreen;
            }
            else
            {
                lblSyncSonuc.Text = $"❌ {result.Message}\nİpucu: Panel ayarlarından oturum açtığınızdan veya Cookie girdiğinizden emin olun.";
                lblSyncSonuc.ForeColor = Color.Red;
            }
        }
    }
}

using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;
using Microsoft.Web.WebView2.Core;
using Microsoft.Web.WebView2.WinForms;
using SoruKopyalama.Models;
using SoruKopyalama.Services;

namespace SoruKopyalama.Forms
{
    public partial class BrowserLoginForm : Form
    {
        private readonly FernusSessionManager _sessionManager;
        private readonly PanelConfig _panel;
        private WebView2? _webView;
        private bool _isCompleted = false;

        public bool LoginSuccess { get; private set; } = false;
        public string ExtractedCookie { get; private set; } = "";

        public BrowserLoginForm(FernusSessionManager sessionManager, string panelName)
        {
            InitializeComponent();
            _sessionManager = sessionManager;
            _panel = _sessionManager.GetPanel(panelName);
            this.Text = $"🌐 {_panel.Name} Paneli Otomatik Giriş ({_panel.Domain})";
        }

        private async void BrowserLoginForm_Load(object sender, EventArgs e)
        {
            lblDurum.Text = $"{_panel.Domain} açılıyor ve oturum başlatılıyor...";
            lblDurum.ForeColor = Color.Yellow;

            try
            {
                _webView = new WebView2
                {
                    Dock = DockStyle.Fill
                };
                pnlWeb.Controls.Add(_webView);

                await _webView.EnsureCoreWebView2Async(null);

                _webView.CoreWebView2.NavigationCompleted += WebView_NavigationCompleted;
                _webView.CoreWebView2.SourceChanged += WebView_SourceChanged;

                string targetUrl = $"https://{_panel.Domain}/admin/login.php";
                _webView.CoreWebView2.Navigate(targetUrl);
            }
            catch (Exception ex)
            {
                lblDurum.Text = "Tarayıcı motoru hatası: " + ex.Message;
                lblDurum.ForeColor = Color.Red;
            }
        }

        private async void WebView_NavigationCompleted(object? sender, CoreWebView2NavigationCompletedEventArgs e)
        {
            if (_isCompleted || _webView?.CoreWebView2 == null) return;

            string currentUrl = _webView.Source.ToString().ToLower();

            // Eğer login sayfasındaysak ve panelde kayıtlı e-posta/şifre varsa otomatik doldur
            if (currentUrl.Contains("login"))
            {
                lblDurum.Text = "Giriş formu algılandı, bilgiler otomatik yazılıyor...";
                lblDurum.ForeColor = Color.DeepSkyBlue;

                if (!string.IsNullOrEmpty(_panel.Email) && !string.IsNullOrEmpty(_panel.Password))
                {
                    string script = $@"
                        (function() {{
                            var emailInput = document.querySelector('input[name=email]') || document.querySelector('input[type=email]') || document.querySelector('input[type=text]');
                            var passInput = document.querySelector('input[name=password]') || document.querySelector('input[type=password]');
                            var submitBtn = document.querySelector('button[type=submit]') || document.querySelector('input[type=submit]') || document.querySelector('.btn-login') || document.querySelector('form button');
                            
                            if (emailInput) {{
                                emailInput.value = '{_panel.Email}';
                                emailInput.dispatchEvent(new Event('input', {{ bubbles: true }}));
                            }}
                            if (passInput) {{
                                passInput.value = '{_panel.Password}';
                                passInput.dispatchEvent(new Event('input', {{ bubbles: true }}));
                            }}
                            if (submitBtn && emailInput && emailInput.value) {{
                                setTimeout(function() {{ submitBtn.click(); }}, 500);
                            }}
                        }})();
                    ";

                    await _webView.ExecuteScriptAsync(script);
                }
            }

            // Eğer admin paneline veya ana sayfaya yönlendiyse çerezleri yakala!
            if (currentUrl.Contains("admin") || currentUrl.Contains("index") || currentUrl.Contains("kaynak") || currentUrl.Contains("dashboard") || !currentUrl.Contains("login"))
            {
                await CheckAndExtractCookiesAsync();
            }
        }

        private async void WebView_SourceChanged(object? sender, CoreWebView2SourceChangedEventArgs e)
        {
            if (_isCompleted || _webView?.CoreWebView2 == null) return;
            string currentUrl = _webView.Source.ToString().ToLower();

            if (!currentUrl.Contains("login"))
            {
                await CheckAndExtractCookiesAsync();
            }
        }

        private async Task CheckAndExtractCookiesAsync()
        {
            if (_isCompleted || _webView?.CoreWebView2 == null) return;

            try
            {
                var cookieManager = _webView.CoreWebView2.CookieManager;
                var cookies = await cookieManager.GetCookiesAsync($"https://{_panel.Domain}");

                var cookiePairs = new List<string>();
                bool hasPhpSessId = false;

                foreach (var c in cookies)
                {
                    cookiePairs.Add($"{c.Name}={c.Value}");
                    if (c.Name.Equals("PHPSESSID", StringComparison.OrdinalIgnoreCase))
                    {
                        hasPhpSessId = true;
                    }
                }

                if (hasPhpSessId && cookiePairs.Count > 0)
                {
                    _isCompleted = true;
                    LoginSuccess = true;

                    ExtractedCookie = string.Join("; ", cookiePairs);
                    _panel.ActiveCookie = ExtractedCookie;
                    _panel.LastLoginTime = DateTime.Now;
                    _sessionManager.SaveSettings();

                    lblDurum.Text = "🎉 BAŞARILI: Oturum açıldı ve güncel Cookie yakalandı! Pencere kapatılıyor...";
                    lblDurum.ForeColor = Color.LimeGreen;

                    await Task.Delay(1200);
                    this.DialogResult = DialogResult.OK;
                    this.Close();
                }
            }
            catch (Exception ex)
            {
                lblDurum.Text = "Çerez okuma hatası: " + ex.Message;
            }
        }

        private async void btnManuelYakalayici_Click(object sender, EventArgs e)
        {
            lblDurum.Text = "Çerezler taranıyor...";
            await CheckAndExtractCookiesAsync();

            if (!LoginSuccess)
            {
                MessageBox.Show("Henüz oturum çerezi algılanamadı. Lütfen tarayıcı ekranından giriş yapıp tekrar deneyin.", "Bilgi", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
        }
    }
}

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
    /// <summary>
    /// Panele gömülü tarayıcıyla giriş yapar ve GERÇEK oturum çerezini alır.
    /// Çerez, ancak giriş sayfasından çıkıldıktan ve oturum panelde doğrulandıktan sonra kabul edilir;
    /// giriş yapılmamış boş PHPSESSID alınmaz.
    /// </summary>
    public partial class BrowserLoginForm : Form
    {
        private readonly FernusSessionManager _sessionManager;
        private readonly PanelConfig _panel;
        private WebView2? _webView;
        private bool _isCompleted;
        private bool _formGonderildi;
        private bool _kontrolEdiliyor;

        public bool LoginSuccess { get; private set; }
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
            lblDurum.Text = $"{_panel.Domain} açılıyor...";
            lblDurum.ForeColor = Color.FromArgb(73, 80, 87);

            try
            {
                _webView = new WebView2 { Dock = DockStyle.Fill };
                pnlWeb.Controls.Add(_webView);

                // Profil veri klasörü veri kökünde: oturum bir kez açılınca sonraki girişler anında olur
                var env = await CoreWebView2Environment.CreateAsync(null, VeriYolu.Dosya("WebView2Profil"));
                await _webView.EnsureCoreWebView2Async(env);

                _webView.CoreWebView2.NavigationCompleted += WebView_NavigationCompleted;
                _webView.CoreWebView2.Navigate($"https://{_panel.Domain}/admin/login.php");
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

            string currentUrl = _webView.Source.ToString().ToLowerInvariant();

            if (currentUrl.Contains("login"))
            {
                if (_formGonderildi)
                {
                    lblDurum.Text = "Otomatik giriş kabul edilmedi. Lütfen aşağıdaki formdan kendiniz giriş yapın; çerez otomatik alınacak.";
                    lblDurum.ForeColor = Color.FromArgb(211, 84, 0);
                    return;
                }

                if (string.IsNullOrEmpty(_panel.Email) || string.IsNullOrEmpty(_panel.Password))
                {
                    lblDurum.Text = "Ayarlarda e-posta/şifre yok. Lütfen aşağıdaki formdan giriş yapın; çerez otomatik alınacak.";
                    lblDurum.ForeColor = Color.FromArgb(211, 84, 0);
                    return;
                }

                lblDurum.Text = "Giriş formu algılandı, bilgiler yazılıp gönderiliyor...";
                lblDurum.ForeColor = Color.FromArgb(13, 110, 253);
                _formGonderildi = true;

                string script = $@"
                    (function() {{
                        var emailInput = document.querySelector('input[name=email]') || document.querySelector('input[type=email]') || document.querySelector('input[type=text]');
                        var passInput = document.querySelector('input[name=password]') || document.querySelector('input[type=password]');
                        var submitBtn = document.querySelector('button[type=submit]') || document.querySelector('input[type=submit]') || document.querySelector('.btn-login') || document.querySelector('form button');
                        if (emailInput) {{ emailInput.value = {JsStr(_panel.Email)}; emailInput.dispatchEvent(new Event('input', {{ bubbles: true }})); }}
                        if (passInput) {{ passInput.value = {JsStr(_panel.Password)}; passInput.dispatchEvent(new Event('input', {{ bubbles: true }})); }}
                        if (emailInput && passInput) {{
                            setTimeout(function() {{ if (submitBtn) submitBtn.click(); else if (passInput.form) passInput.form.submit(); }}, 400);
                            return 'ok';
                        }}
                        return 'form-yok';
                    }})();";

                string sonuc = await _webView.ExecuteScriptAsync(script);
                if (sonuc.Contains("form-yok"))
                {
                    lblDurum.Text = "Giriş formu bulunamadı. Lütfen aşağıdan kendiniz giriş yapın; çerez otomatik alınacak.";
                    lblDurum.ForeColor = Color.FromArgb(211, 84, 0);
                }
                return;
            }

            // Giriş sayfasından çıkıldı: çerezi al ve panelde doğrula
            await CheckAndExtractCookiesAsync();
        }

        private static string JsStr(string s) => System.Text.Json.JsonSerializer.Serialize(s ?? "");

        private async Task CheckAndExtractCookiesAsync()
        {
            if (_isCompleted || _kontrolEdiliyor || _webView?.CoreWebView2 == null) return;
            _kontrolEdiliyor = true;

            try
            {
                var cookies = await _webView.CoreWebView2.CookieManager.GetCookiesAsync($"https://{_panel.Domain}");
                var pairs = cookies.Select(c => $"{c.Name}={c.Value}").ToList();

                if (!cookies.Any(c => c.Name.Equals("PHPSESSID", StringComparison.OrdinalIgnoreCase)))
                {
                    lblDurum.Text = "Henüz oturum çerezi yok, bekleniyor...";
                    return;
                }

                if (!pairs.Any(p => p.StartsWith("email=", StringComparison.OrdinalIgnoreCase)) && !string.IsNullOrEmpty(_panel.Email))
                    pairs.Add($"email={_panel.Email}");

                string cookie = string.Join("; ", pairs);

                lblDurum.Text = "Oturum panelde doğrulanıyor...";
                var durum = await _sessionManager.OturumGecerliMiAsync(_panel.Name, cookie);

                if (durum == OturumDurumu.Gecersiz)
                {
                    lblDurum.Text = "Alınan çerez panelde geçerli değil (giriş tamamlanmamış). Lütfen aşağıdan giriş yapın.";
                    lblDurum.ForeColor = Color.FromArgb(211, 84, 0);
                    return;
                }

                _isCompleted = true;
                LoginSuccess = true;
                ExtractedCookie = cookie;
                _sessionManager.UpdatePanelCookie(_panel.Name, cookie);

                lblDurum.Text = "✅ Oturum açıldı ve çerez doğrulandı. Pencere kapatılıyor...";
                lblDurum.ForeColor = Color.FromArgb(39, 174, 96);

                await Task.Delay(600);
                this.DialogResult = DialogResult.OK;
                this.Close();
            }
            catch (Exception ex)
            {
                lblDurum.Text = "Çerez okuma hatası: " + ex.Message;
                lblDurum.ForeColor = Color.Red;
            }
            finally
            {
                _kontrolEdiliyor = false;
            }
        }

        private async void btnManuelYakalayici_Click(object sender, EventArgs e)
        {
            await CheckAndExtractCookiesAsync();
        }
    }
}

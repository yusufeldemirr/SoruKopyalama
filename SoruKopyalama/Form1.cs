using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Net.Http;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using OfficeOpenXml;
using SoruKopyalama.Forms;
using SoruKopyalama.Models;
using SoruKopyalama.Services;

namespace SoruKopyalama
{
    public partial class Form1 : Form
    {
        private readonly FernusSessionManager _sessionManager;
        private readonly DatabaseManager _dbManager;
        private readonly QuestionMatcher _matcher;

        private CancellationTokenSource? _cts;
        private ManualResetEventSlim _pauseEvent = new ManualResetEventSlim(true);
        private bool _isPaused = false;
        private bool _isRunning = false;

        private List<SoruIslemRaporu> _sonIslemRaporlari = new List<SoruIslemRaporu>();

        public Form1()
        {
            InitializeComponent();

            ExcelPackage.License.SetNonCommercialPersonal("FastCopyBot");

            _sessionManager = new FernusSessionManager();
            _dbManager = new DatabaseManager();
            _matcher = new QuestionMatcher();
        }

        private async void Form1_Load(object sender, EventArgs e)
        {
            try
            {
                this.Icon = LogoHelper.CreateLimitIcon();
                picLogo.Image = LogoHelper.GetLimitLogoImage(52, 52);
            }
            catch { }

            InitPanelsUI();
            LogYaz("📚 Limit Soru Kopyalama Sistemi başlatılıyor...", Color.FromArgb(220, 53, 69));

            var progress = new Progress<string>(msg => LogYaz(msg, Color.FromArgb(108, 117, 125)));
            var (fileCount, questionCount, msg) = await _dbManager.LoadExcelDatabaseAsync(progress);

            if (fileCount > 0)
            {
                LogYaz($"✅ BAŞARILI: {msg}", Color.FromArgb(39, 174, 96));
            }
            else
            {
                LogYaz($"⚠️ BİLGİ: {msg}", Color.FromArgb(211, 84, 0));
            }

            LogYaz($"🧠 Hafıza: {_dbManager.KodHafizasi.Count} adet geçmiş soru eşleşmesi hazır.", Color.FromArgb(220, 53, 69));
        }

        private void InitPanelsUI()
        {
            cmbHedefPanel.SelectedIndexChanged -= cmbHedefPanel_SelectedIndexChanged;
            cmbHedefPanel.Items.Clear();

            foreach (var panel in _sessionManager.Settings.Panels)
            {
                cmbHedefPanel.Items.Add(panel.Name);
            }

            if (cmbHedefPanel.Items.Count > 0)
            {
                int limitIdx = cmbHedefPanel.FindStringExact("Limit");
                if (limitIdx >= 0)
                {
                    cmbHedefPanel.SelectedIndex = limitIdx;
                }
                else
                {
                    int tgtIdx = cmbHedefPanel.FindStringExact(_sessionManager.Settings.DefaultTargetPanel);
                    cmbHedefPanel.SelectedIndex = tgtIdx >= 0 ? tgtIdx : 0;
                }
            }

            cmbHedefPanel.SelectedIndexChanged += cmbHedefPanel_SelectedIndexChanged;
        }

        private void cmbHedefPanel_SelectedIndexChanged(object? sender, EventArgs e)
        {
            string secili = cmbHedefPanel.SelectedItem?.ToString() ?? "Limit";
            var panelDb = _dbManager.GetVeritabani(secili);
            var panelMem = _dbManager.GetKodHafizasi(secili);
            LogYaz($"📌 Aktif Veritabanı: [{secili}] ({panelDb.Count} soru havuzu, {panelMem.Count} hafıza kaydı devrede)", Color.FromArgb(41, 128, 185));
        }

        private void LogYaz(string mesaj, Color renk = default)
        {
            if (this.InvokeRequired)
            {
                this.Invoke(new Action<string, Color>(LogYaz), mesaj, renk);
                return;
            }

            if (renk == default(Color))
            {
                if (mesaj.Contains("HATA") || mesaj.Contains("BULUNAMADI") || mesaj.Contains("reddetti") || mesaj.Contains("İptal"))
                    renk = Color.FromArgb(220, 53, 69); // Kırmızı
                else if (mesaj.Contains("BAŞARILI") || mesaj.Contains("Eşleşme Sağlandı") || mesaj.Contains("TAMAMLANDI") || mesaj.Contains("Eşleşti"))
                    renk = Color.FromArgb(39, 174, 96); // Zümrüt Yeşili
                else if (mesaj.Contains("UYARI") || mesaj.Contains("DURAKLATILDI"))
                    renk = Color.FromArgb(211, 84, 0); // Koyu Turuncu
                else if (mesaj.Contains("Sistem:") || mesaj.Contains("Bilgi:") || mesaj.Contains("🧠") || mesaj.Contains("🎯"))
                    renk = Color.FromArgb(220, 53, 69); // Limit Kırmızısı
                else
                    renk = Color.FromArgb(44, 62, 80); // Koyu Antrasit
            }

            rtbLog.SelectionStart = rtbLog.TextLength;
            rtbLog.SelectionLength = 0;
            rtbLog.SelectionColor = renk;
            rtbLog.AppendText($"[{DateTime.Now:HH:mm:ss}] {mesaj}\n");
            rtbLog.ScrollToCaret();
        }

        private void btnExcelSec_Click(object sender, EventArgs e)
        {
            try
            {
                using OpenFileDialog ofd = new OpenFileDialog();
                ofd.Title = "Adresleme (İş Emri) Excel'ini Seçin";
                ofd.Filter = "Excel Dosyaları|*.xlsx;*.xls";
                ofd.InitialDirectory = Environment.GetFolderPath(Environment.SpecialFolder.Desktop);

                if (ofd.ShowDialog() == DialogResult.OK)
                {
                    txtAdreslemeExceli.Text = ofd.FileName;
                    LogYaz($"📁 İş emri dosyası seçildi: {Path.GetFileName(ofd.FileName)}", Color.Cyan);
                }
            }
            catch (Exception ex)
            {
                LogYaz("HATA Dosya Seçimi: " + ex.Message, Color.Red);
            }
        }

        private void btnAyarlar_Click(object sender, EventArgs e)
        {
            using var settingsForm = new SettingsForm(_sessionManager, _dbManager);
            settingsForm.ShowDialog(this);
            InitPanelsUI();
        }

        private async void btnAltKlasorleriGetir_Click(object sender, EventArgs e)
        {
            string anaKlasorId = txtAnaKlasorId.Text.Trim();
            string hedefPanelAdi = cmbHedefPanel.SelectedItem?.ToString() ?? "Limit";

            if (string.IsNullOrEmpty(anaKlasorId))
            {
                MessageBox.Show("Lütfen önce Deneme Ana Klasör ID'sini girin.", "Uyarı", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            btnAltKlasorleriGetir.Enabled = false;
            
            var panel = _sessionManager.GetPanel(hedefPanelAdi);
            LogYaz($"🔍 [{panel.Name}] ({panel.Domain}) panelinden [{anaKlasorId}] alt klasörleri sorgulanıyor...", Color.Yellow);

            var result = await _sessionManager.GetSubFoldersAsync(hedefPanelAdi, anaKlasorId);

            // Eğer seçili panelde bulunamadıysa diğer kayıtlı panellerde de ara (Limit, Final vs.)
            if (!result.Success || result.AllFolders.Count == 0)
            {
                foreach (var otherPanel in _sessionManager.Settings.Panels)
                {
                    if (otherPanel.Name.Equals(hedefPanelAdi, StringComparison.OrdinalIgnoreCase)) continue;

                    LogYaz($"🔍 [{otherPanel.Name}] ({otherPanel.Domain}) panelinde de deneniyor...", Color.Yellow);
                    var otherResult = await _sessionManager.GetSubFoldersAsync(otherPanel.Name, anaKlasorId);
                    if (otherResult.Success && otherResult.AllFolders.Count > 0)
                    {
                        result = otherResult;
                        hedefPanelAdi = otherPanel.Name;
                        int idx = cmbHedefPanel.FindStringExact(hedefPanelAdi);
                        if (idx >= 0) cmbHedefPanel.SelectedIndex = idx;
                        break;
                    }
                }
            }

            btnAltKlasorleriGetir.Enabled = true;

            if (result.Success && result.AllFolders.Count > 0)
            {
                if (result.SubFolders.TryGetValue("Türkçe", out var tId)) txtTurkceId.Text = tId;
                if (result.SubFolders.TryGetValue("Sosyal", out var sId)) txtSosyalId.Text = sId;
                if (result.SubFolders.TryGetValue("Matematik", out var mId)) txtMatematikId.Text = mId;
                if (result.SubFolders.TryGetValue("Fen", out var fId)) txtFenId.Text = fId;

                LogYaz($"✅ {result.Message} (Panel: {hedefPanelAdi})", Color.LimeGreen);
                foreach (var f in result.AllFolders)
                {
                    LogYaz($"   📁 Alt Klasör Bulundu: [{f.Id}] {f.Title}", Color.DeepSkyBlue);
                }
            }
            else
            {
                LogYaz($"⚠️ Panelden alt klasörler alınamadı. Lütfen oturum Cookie'nizi kontrol edin.", Color.Orange);
                MessageBox.Show("Panelden alt klasör listesi alınamadı.\n\nİpucu: 'Panel & Oturum Ayarları'ndan 'Otomatik Giriş Yap ve Çek' butonuna basabilir veya tarayıcınızdaki Cookie'yi yapıştırıp kaydedebilirsiniz.", "Bilgi", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
        }

        private async void btnBaslat_Click(object sender, EventArgs e)
        {
            if (_isRunning) return;

            string excelYolu = txtAdreslemeExceli.Text.Trim();
            string anaKlasorId = txtAnaKlasorId.Text.Trim();
            string turkceId = txtTurkceId.Text.Trim();
            string sosyalId = txtSosyalId.Text.Trim();
            string matId = txtMatematikId.Text.Trim();
            string fenId = txtFenId.Text.Trim();
            string hedefPanelAdi = cmbHedefPanel.SelectedItem?.ToString() ?? "Limit";

            if (string.IsNullOrEmpty(excelYolu) || !File.Exists(excelYolu))
            {
                MessageBox.Show("Lütfen geçerli bir Adresleme Excel dosyası seçin.", "Hata", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            // En az bir klasör ID girilmiş olmalı
            if (string.IsNullOrEmpty(anaKlasorId) && string.IsNullOrEmpty(turkceId) && string.IsNullOrEmpty(sosyalId) && string.IsNullOrEmpty(matId) && string.IsNullOrEmpty(fenId))
            {
                MessageBox.Show("Lütfen Deneme Ana Klasör ID'sini veya alt klasör ID'lerini girin.", "Hata", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            var hedefPanel = _sessionManager.GetPanel(hedefPanelAdi);
            if (string.IsNullOrEmpty(hedefPanel.ActiveCookie) && string.IsNullOrEmpty(hedefPanel.Email))
            {
                var dr = MessageBox.Show($"'{hedefPanelAdi}' paneli için henüz oturum açılmamış. Ayarlar ekranını açmak ister misiniz?", "Oturum Gerekli", MessageBoxButtons.YesNo, MessageBoxIcon.Question);
                if (dr == DialogResult.Yes)
                {
                    btnAyarlar_Click(sender, e);
                }
                return;
            }

            // UI Durum Güncelleme
            SetRunningState(true);
            _cts = new CancellationTokenSource();
            _pauseEvent.Set();
            _isPaused = false;
            btnDurdurDevam.Text = "⏸️ DURAKLAT";
            btnDurdurDevam.BackColor = Color.FromArgb(243, 156, 18);

            _sonIslemRaporlari.Clear();
            btnRaporuIndir.Enabled = false;

            try
            {
                await Task.Run(() => KopyalamaMotorunuCalistirAsync(excelYolu, anaKlasorId, turkceId, sosyalId, matId, fenId, hedefPanelAdi, _cts.Token));
            }
            catch (OperationCanceledException)
            {
                LogYaz("🛑 Otomasyon kullanıcı tarafından İPTAL EDİLDİ.", Color.OrangeRed);
            }
            catch (Exception ex)
            {
                LogYaz("HATA Kritik Motor Hatası: " + ex.Message, Color.Red);
            }
            finally
            {
                SetRunningState(false);
                btnRaporuIndir.Enabled = _sonIslemRaporlari.Count > 0;
            }
        }

        private async Task KopyalamaMotorunuCalistirAsync(
            string excelYolu, 
            string anaKlasorId, 
            string turkceId, 
            string sosyalId, 
            string matId, 
            string fenId, 
            string hedefPanelAdi, 
            CancellationToken ct)
        {
            var hedefPanel = _sessionManager.GetPanel(hedefPanelAdi);
            using var client = _sessionManager.CreateHttpClient(hedefPanelAdi);

            using var package = new ExcelPackage(new FileInfo(excelYolu));
            var worksheet = package.Workbook.Worksheets[0];
            int totalRows = worksheet.Dimension?.Rows ?? 0;

            // İşlenecek geçerli satırları tespit et
            var islenecekSatirlar = new List<int>();
            for (int r = 1; r <= totalRows; r++)
            {
                string kKod = worksheet.Cells[r, 1].Text.Trim();
                string kAcilim = worksheet.Cells[r, 2].Text.Trim();
                string bolum = worksheet.Cells[r, 3].Text.Trim();
                
                if (kKod == "Kod" || kKod == "Kodu" || kKod == "KISA KOD" || kAcilim == "Kod Açılımı") continue;
                
                if (!string.IsNullOrEmpty(kKod) || !string.IsNullOrEmpty(kAcilim) || !string.IsNullOrEmpty(bolum))
                {
                    islenecekSatirlar.Add(r);
                }
            }

            int toplamIslem = islenecekSatirlar.Count;
            int basariliSayisi = 0;
            int hataliSayisi = 0;
            int islemSirasi = 1;

            UpdateProgressUI(0, toplamIslem, 0, 0, toplamIslem, "Otomasyon başladı...");

            LogYaz($"🚀 İŞ EMRİ BAŞLATILDI: Toplam {toplamIslem} soru işlenecek. (Panel: {hedefPanel.Domain})", Color.LimeGreen);
            LogYaz($"📌 Hedef Klasörler -> Türkçe: [{turkceId}] | Sosyal: [{sosyalId}] | Mat: [{matId}] | Fen: [{fenId}]", Color.DeepSkyBlue);

            foreach (int row in islenecekSatirlar)
            {
                // Duraklatma kontrolü
                _pauseEvent.Wait(ct);
                ct.ThrowIfCancellationRequested();

                string kisaKod = worksheet.Cells[row, 1].Text.Trim();
                string kodAcilimi = worksheet.Cells[row, 2].Text.Trim();
                string bolumKodu = worksheet.Cells[row, 3].Text.Trim(); // TÜRK-İÇ, SOS-İÇ, MAT-İÇ, FEN-İÇ
                string hedefSoruNo = worksheet.Cells[row, 4].Text.Trim();
                string cevapAnahtari = worksheet.Cells[row, 5].Text.Trim();

                // Eğer kod açılımı tanımsız veya boşsa üret
                if (string.IsNullOrWhiteSpace(kodAcilimi) || kodAcilimi.Contains("TANIMSIZ"))
                {
                    kodAcilimi = ShortCodeDecoder.Decode(kisaKod, bolumKodu);
                }

                // Branş Tespiti (Türkçe, Sosyal, Matematik, Fen)
                string tespitEdilenBrans = ShortCodeDecoder.GetBranş(kisaKod, bolumKodu);

                // Bu branşa ait hedef alt klasör ID'sini belirle
                string hedefTargetId = anaKlasorId;
                if (tespitEdilenBrans == "Türkçe" && !string.IsNullOrEmpty(turkceId)) hedefTargetId = turkceId;
                else if (tespitEdilenBrans == "Sosyal" && !string.IsNullOrEmpty(sosyalId)) hedefTargetId = sosyalId;
                else if (tespitEdilenBrans == "Matematik" && !string.IsNullOrEmpty(matId)) hedefTargetId = matId;
                else if (tespitEdilenBrans == "Fen" && !string.IsNullOrEmpty(fenId)) hedefTargetId = fenId;

                var rapor = new SoruIslemRaporu
                {
                    SiraNo = islemSirasi,
                    KisaKod = kisaKod,
                    KodAcilimi = kodAcilimi,
                    HedefSoruNo = hedefSoruNo,
                    HedefCevap = cevapAnahtari
                };

                if (string.IsNullOrEmpty(hedefTargetId))
                {
                    LogYaz($"{islemSirasi}-) ❌ HATA: [{tespitEdilenBrans}] için hedef klasör ID bulunamadı!", Color.LightCoral);
                    hataliSayisi++;
                    rapor.Basarili = false;
                    rapor.DurumMesaji = $"{tespitEdilenBrans} için hedef klasör ID boş.";
                    _sonIslemRaporlari.Add(rapor);
                    islemSirasi++;
                    UpdateProgressUI(islemSirasi - 1, toplamIslem, basariliSayisi, hataliSayisi, toplamIslem - (islemSirasi - 1), $"İşleniyor: {islemSirasi - 1}/{toplamIslem}");
                    continue;
                }

                // Panel Bazlı Puanlama Motoru ile eşleştirme yap
                var panelVeritabani = _dbManager.GetVeritabani(hedefPanelAdi);
                var panelHafizasi = _dbManager.GetKodHafizasi(hedefPanelAdi);
                var match = _matcher.Match(kisaKod, kodAcilimi, panelVeritabani, panelHafizasi, bolumKodu);

                if (!match.IsSuccess || string.IsNullOrEmpty(match.SolutionId))
                {
                    LogYaz($"{islemSirasi}-) ❌ BULUNAMADI [{tespitEdilenBrans}]: {match.Message}", Color.LightCoral);
                    hataliSayisi++;
                    rapor.Basarili = false;
                    rapor.DurumMesaji = match.Message;
                    _sonIslemRaporlari.Add(rapor);

                    islemSirasi++;
                    UpdateProgressUI(islemSirasi - 1, toplamIslem, basariliSayisi, hataliSayisi, toplamIslem - (islemSirasi - 1), $"İşleniyor: {islemSirasi - 1}/{toplamIslem}");
                    continue;
                }

                rapor.BulunanSolutionId = match.SolutionId;
                rapor.BulunanSourceId = match.SourceId;

                string eskiNo = !string.IsNullOrEmpty(match.BulunanSoruNo) ? match.BulunanSoruNo.Replace("Soru", "").Replace(".", "").Trim() : match.SafSoruNo;
                if (string.IsNullOrEmpty(eskiNo)) eskiNo = "?";

                LogYaz($"{islemSirasi}-) 🎯 [{tespitEdilenBrans}] Eski Soru No: {eskiNo} -> Yeni Soru No: {hedefSoruNo} (Cevap: {cevapAnahtari})");

                // API İsteği: Kopyalama (action_solution_copy.php)
                string copyPayload = $"{{\"source_id\":\"{match.SourceId}\",\"target_id\":\"{hedefTargetId}\",\"solutions\":[{{\"id\":\"{match.SolutionId}\"}}]}}";
                string copyUrl = $"https://{hedefPanel.Domain}/controller/soru_cozum/action_solution_copy.php";

                var copyData = new List<KeyValuePair<string, string>>
                {
                    new KeyValuePair<string, string>("action", "solution-copy"),
                    new KeyValuePair<string, string>("data", copyPayload)
                };

                bool kopyalandi = false;
                string yeniSoruId = "";

                try
                {
                    var copyContent = new FormUrlEncodedContent(copyData);
                    var response = await client.PostAsync(copyUrl, copyContent, ct);
                    string responseText = await response.Content.ReadAsStringAsync(ct);

                    // Eğer yetki hatası veya user_id null hatası varsa otomatik re-login yap ve tekrar dene
                    if (responseText.Contains("login") || responseText.Contains("user_id") || responseText.Contains("SQLSTATE") || response.StatusCode == System.Net.HttpStatusCode.Unauthorized)
                    {
                        LogYaz($"   ⚠️ Oturum yenileniyor (Panel girişi yapılıyor)...", Color.Orange);
                        var loginRes = await _sessionManager.LoginAsync(hedefPanelAdi);
                        if (loginRes.Success)
                        {
                            client.DefaultRequestHeaders.Remove("Cookie");
                            client.DefaultRequestHeaders.Add("Cookie", loginRes.Cookie);
                            // Tekrar dene
                            response = await client.PostAsync(copyUrl, new FormUrlEncodedContent(copyData), ct);
                            responseText = await response.Content.ReadAsStringAsync(ct);
                        }
                    }

                    if (response.IsSuccessStatusCode)
                    {
                        yeniSoruId = ParseNewQuestionId(responseText);
                        if (!string.IsNullOrEmpty(yeniSoruId))
                        {
                            kopyalandi = true;
                        }
                        else
                        {
                            rapor.DurumMesaji = $"Yeni soru ID alınamadı. Yanıt: {responseText}";
                        }
                    }
                    else
                    {
                        rapor.DurumMesaji = $"Sunucu hatası: HTTP {response.StatusCode}";
                    }
                }
                catch (Exception ex)
                {
                    rapor.DurumMesaji = "Kopyalama Hatası: " + ex.Message;
                }

                // Soru No ve Cevap Anahtarı Güncelleme
                if (kopyalandi && !string.IsNullOrEmpty(yeniSoruId))
                {
                    string updatePayload = $"{{\"id\":\"{yeniSoruId}\",\"subjects\":[],\"source\":\"{hedefTargetId}\",\"answer\":\"{cevapAnahtari}\",\"difficulty\":\"4\",\"order_number\":\"{hedefSoruNo}\"}}";
                    string updateUrl = $"https://{hedefPanel.Domain}/controller/action_questions.php";

                    var updateData = new List<KeyValuePair<string, string>>
                    {
                        new KeyValuePair<string, string>("action", "update_question"),
                        new KeyValuePair<string, string>("data", updatePayload)
                    };

                    try
                    {
                        var updateContent = new FormUrlEncodedContent(updateData);
                        var updateResponse = await client.PostAsync(updateUrl, updateContent, ct);
                        string updateResponseText = await updateResponse.Content.ReadAsStringAsync(ct);

                        if (updateResponse.IsSuccessStatusCode && (updateResponseText.Contains("\"status\":true") || updateResponseText.Contains("success")))
                        {
                            LogYaz($"{islemSirasi}-) ✅ BAŞARILI: Eski Soru No: {eskiNo} -> Yeni Soru No: {hedefSoruNo}, Cevap {cevapAnahtari} olarak güncellendi.", Color.LimeGreen);
                            basariliSayisi++;
                            rapor.Basarili = true;
                            rapor.DurumMesaji = "Başarıyla aktarıldı ve güncellendi.";

                            // Başarılı eşleşmeyi panel bazlı hafızaya kaydet
                            _dbManager.SaveToMemory(hedefPanelAdi, kisaKod, match.SourceId, match.SolutionId);
                        }
                        else
                        {
                            LogYaz($"{islemSirasi}-) ❌ HATA: Panel güncellemeyi reddetti! ({updateResponseText})", Color.LightCoral);
                            hataliSayisi++;
                            rapor.Basarili = false;
                            rapor.DurumMesaji = $"Güncelleme reddedildi: {updateResponseText}";
                        }
                    }
                    catch (Exception ex)
                    {
                        LogYaz($"{islemSirasi}-) ❌ HATA: Güncelleme isteği başarısız: {ex.Message}", Color.LightCoral);
                        hataliSayisi++;
                        rapor.Basarili = false;
                        rapor.DurumMesaji = "Güncelleme hatası: " + ex.Message;
                    }
                }
                else
                {
                    LogYaz($"{islemSirasi}-) ❌ HATA: Hedefe aktarılamadı! ({rapor.DurumMesaji})", Color.LightCoral);
                    hataliSayisi++;
                    rapor.Basarili = false;
                }

                _sonIslemRaporlari.Add(rapor);
                islemSirasi++;

                UpdateProgressUI(islemSirasi - 1, toplamIslem, basariliSayisi, hataliSayisi, toplamIslem - (islemSirasi - 1), $"İşleniyor: {islemSirasi - 1}/{toplamIslem}");

                await Task.Delay(350, ct); // Sunucu koruma beklemesi
            }

            // FİNAL RAPORU
            LogYaz(" ", Color.White);
            LogYaz("=================================================", Color.DodgerBlue);
            LogYaz("       🏁 İŞ EMRİ TAMAMLANDI - ÖZET RAPOR        ", Color.DodgerBlue);
            LogYaz("=================================================", Color.DodgerBlue);
            LogYaz($"   Toplam Taranan Soru : {toplamIslem} Adet", Color.Cyan);
            LogYaz($"   Kusursuz Kopyalanan : {basariliSayisi} Soru", Color.LimeGreen);
            LogYaz($"   Hatalı / Bulunamayan: {hataliSayisi} Soru", Color.LightCoral);
            LogYaz("=================================================", Color.DodgerBlue);
        }

        private string ParseNewQuestionId(string responseText)
        {
            try
            {
                using var doc = JsonDocument.Parse(responseText);
                if (doc.RootElement.TryGetProperty("content", out var contentProp))
                {
                    if (contentProp.ValueKind == JsonValueKind.String)
                        return contentProp.GetString() ?? "";
                    if (contentProp.ValueKind == JsonValueKind.Number)
                        return contentProp.GetInt64().ToString();
                }
            }
            catch { }

            var match = Regex.Match(responseText, @"\""content\""\s*:\s*\""?(\d+)\""?");
            if (match.Success)
            {
                return match.Groups[1].Value;
            }

            return "";
        }

        private void UpdateProgressUI(int current, int total, int basarili, int hatali, int kalan, string durumMesaji)
        {
            if (this.InvokeRequired)
            {
                this.Invoke(new Action(() => UpdateProgressUI(current, total, basarili, hatali, kalan, durumMesaji)));
                return;
            }

            prgIlerleme.Maximum = Math.Max(1, total);
            prgIlerleme.Value = Math.Min(current, prgIlerleme.Maximum);

            lblToplamSayac.Text = total.ToString();
            lblBasariliSayac.Text = basarili.ToString();
            lblHataliSayac.Text = hatali.ToString();
            lblKalanSayac.Text = kalan.ToString();

            int yuzde = total > 0 ? (int)((double)current / total * 100) : 0;
            lblIlerlemeDurum.Text = $"{durumMesaji} (%{yuzde})";
        }

        private void SetRunningState(bool running)
        {
            if (this.InvokeRequired)
            {
                this.Invoke(new Action(() => SetRunningState(running)));
                return;
            }

            _isRunning = running;
            btnBaslat.Enabled = !running;
            btnExcelSec.Enabled = !running;
            cmbHedefPanel.Enabled = !running;
            txtAnaKlasorId.Enabled = !running;
            btnAltKlasorleriGetir.Enabled = !running;
            txtTurkceId.Enabled = !running;
            txtSosyalId.Enabled = !running;
            txtMatematikId.Enabled = !running;
            txtFenId.Enabled = !running;
            btnAyarlar.Enabled = !running;

            btnDurdurDevam.Enabled = running;
            btnIptal.Enabled = running;
        }

        private void btnDurdurDevam_Click(object sender, EventArgs e)
        {
            if (!_isRunning) return;

            if (_isPaused)
            {
                _isPaused = false;
                _pauseEvent.Set();
                btnDurdurDevam.Text = "⏸️ DURAKLAT";
                btnDurdurDevam.BackColor = Color.FromArgb(243, 156, 18);
                LogYaz("▶️ Otomasyon DEVAM ETTİRİLİYOR...", Color.LimeGreen);
            }
            else
            {
                _isPaused = true;
                _pauseEvent.Reset();
                btnDurdurDevam.Text = "▶️ DEVAM ET";
                btnDurdurDevam.BackColor = Color.FromArgb(46, 204, 113);
                LogYaz("⏸️ Otomasyon DURAKLATILDI.", Color.Orange);
            }
        }

        private void btnIptal_Click(object sender, EventArgs e)
        {
            if (!_isRunning) return;

            var dr = MessageBox.Show("Otomasyonu tamamen durdurmak istediğinize emin misiniz?", "İptal Onayı", MessageBoxButtons.YesNo, MessageBoxIcon.Question);
            if (dr == DialogResult.Yes)
            {
                _pauseEvent.Set();
                _cts?.Cancel();
            }
        }

        private void btnRaporuIndir_Click(object sender, EventArgs e)
        {
            if (_sonIslemRaporlari.Count == 0)
            {
                MessageBox.Show("İndirilecek herhangi bir işlem raporu bulunmuyor.", "Bilgi", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            try
            {
                string dosyaYolu = _dbManager.ExportReportToExcel(_sonIslemRaporlari);
                MessageBox.Show($"İşlem raporu başarıyla masaüstüne kaydedildi!\n\nDosya: {dosyaYolu}", "Rapor Kaydedildi", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show("Rapor kaydedilirken hata oluştu: " + ex.Message, "Hata", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void btnIdSorgula_Click(object sender, EventArgs e)
        {
            string arananId = txtSorguId.Text.Trim();

            if (string.IsNullOrEmpty(arananId))
            {
                LogYaz("⚠️ Lütfen aramak için bir Soru ID girin.", Color.Orange);
                return;
            }

            bool bulundu = false;

            foreach (var item in _dbManager.SistemVeritabani.Values)
            {
                if (item.SolutionId == arananId || item.SourceId == arananId)
                {
                    LogYaz(" ");
                    LogYaz($"--- 🔎 SORGULAMA SONUCU ({arananId}) ---", Color.Cyan);
                    LogYaz($"Klasör/Adres : {item.KaynakAdi}");
                    LogYaz($"Soru Numarası: {item.SoruNo}");
                    LogYaz($"Cevap Anahtarı: {item.CevapAnahtari}");
                    LogYaz($"Panel Domain : {item.PanelDomain}");
                    LogYaz("---------------------------------------------", Color.Cyan);
                    LogYaz(" ");

                    bulundu = true;
                    break;
                }
            }

            if (!found(arananId, ref bulundu))
            {
                LogYaz($"❌ {arananId} ID'li soru hafızadaki {_dbManager.SistemVeritabani.Count} soru içinde bulunamadı.", Color.LightCoral);
            }
        }

        private bool found(string id, ref bool b) => b;
    }
}
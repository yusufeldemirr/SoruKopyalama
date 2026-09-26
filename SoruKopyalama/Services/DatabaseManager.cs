using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using ExcelDataReader;
using OfficeOpenXml;
using SoruKopyalama.Models;

namespace SoruKopyalama.Services
{
    public class DatabaseManager
    {
        // Panel bazlı veritabanı: [PanelAdı -> [Anahtar -> SistemSoruReferans]]
        public Dictionary<string, Dictionary<string, SistemSoruReferans>> PanelVeritabanlari { get; private set; } 
            = new(StringComparer.OrdinalIgnoreCase);

        // Panel bazlı kod hafızası: [PanelAdı -> [KısaKod -> SourceId|SolutionId]]
        public Dictionary<string, Dictionary<string, string>> PanelKodHafizalari { get; private set; } 
            = new(StringComparer.OrdinalIgnoreCase);

        // Genel / Birleşik Veritabanı
        public Dictionary<string, SistemSoruReferans> SistemVeritabani { get; private set; } 
            = new(StringComparer.OrdinalIgnoreCase);

        // Genel / Birleşik Hafıza
        public Dictionary<string, string> KodHafizasi { get; private set; } 
            = new(StringComparer.OrdinalIgnoreCase);

        // Yapısal indeks (kısa kod -> soru) ve kalıcı kullanıcı düzeltmeleri
        public SoruIndeksi Indeks { get; } = new SoruIndeksi();
        public DuzeltmeDeposu Duzeltmeler { get; } = new DuzeltmeDeposu();
        private readonly List<DbSoru> _tumSorular = new List<DbSoru>();

        private readonly string _databaseDirectory;

        public DatabaseManager()
        {
            Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);

            _databaseDirectory = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Veritabani");

            // Temel panel alt klasörlerini otomatik oluştur
            EnsureDirectory(_databaseDirectory);
            EnsureDirectory(Path.Combine(_databaseDirectory, "Limit"));
            EnsureDirectory(Path.Combine(_databaseDirectory, "Final"));
            EnsureDirectory(Path.Combine(_databaseDirectory, "Esen"));

            // Temel panel koleksiyonlarını başlat
            InitPanelStorage("Limit");
            InitPanelStorage("Final");
            InitPanelStorage("Esen");
            InitPanelStorage("Genel");
        }

        private void EnsureDirectory(string path)
        {
            if (!Directory.Exists(path)) Directory.CreateDirectory(path);
        }

        private void InitPanelStorage(string panelName)
        {
            if (!PanelVeritabanlari.ContainsKey(panelName))
                PanelVeritabanlari[panelName] = new Dictionary<string, SistemSoruReferans>(StringComparer.OrdinalIgnoreCase);

            if (!PanelKodHafizalari.ContainsKey(panelName))
                PanelKodHafizalari[panelName] = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        }

        public string SourceIdCikar(string url)
        {
            if (string.IsNullOrWhiteSpace(url)) return "";
            var match = Regex.Match(url, @"questions[\\/](\d+)", RegexOptions.IgnoreCase);
            if (match.Success)
            {
                return match.Groups[1].Value;
            }
            return "";
        }

        public IDictionary<string, SistemSoruReferans> GetVeritabani(string? panelName)
        {
            if (!string.IsNullOrWhiteSpace(panelName) && PanelVeritabanlari.TryGetValue(panelName, out var panelDb) && panelDb.Count > 0)
            {
                return panelDb;
            }
            return SistemVeritabani;
        }

        public IDictionary<string, string> GetKodHafizasi(string? panelName)
        {
            if (!string.IsNullOrWhiteSpace(panelName) && PanelKodHafizalari.TryGetValue(panelName, out var panelMem) && panelMem.Count > 0)
            {
                return panelMem;
            }
            return KodHafizasi;
        }

        public void LoadMemory()
        {
            try
            {
                KodHafizasi.Clear();
                foreach (var kv in PanelKodHafizalari) kv.Value.Clear();

                string genMemPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "kod_hafizasi.txt");
                if (File.Exists(genMemPath))
                {
                    foreach (var satir in File.ReadAllLines(genMemPath))
                    {
                        if (string.IsNullOrWhiteSpace(satir)) continue;
                        var bol = satir.Split('|');
                        if (bol.Length >= 3)
                        {
                            string kKod = bol[0];
                            string pName = "Genel";
                            string sId = bol[1];
                            string solId = bol[2];

                            if (bol.Length >= 4)
                            {
                                pName = bol[1];
                                sId = bol[2];
                                solId = bol[3];
                            }

                            KodHafizasi[kKod] = $"{sId}|{solId}";
                            InitPanelStorage(pName);
                            PanelKodHafizalari[pName][kKod] = $"{sId}|{solId}";
                        }
                    }
                }

                foreach (var panelName in new[] { "Limit", "Final", "Esen" })
                {
                    string pMemPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, $"kod_hafizasi_{panelName.ToLower()}.txt");
                    if (File.Exists(pMemPath))
                    {
                        InitPanelStorage(panelName);
                        foreach (var satir in File.ReadAllLines(pMemPath))
                        {
                            if (string.IsNullOrWhiteSpace(satir)) continue;
                            var bol = satir.Split('|');
                            if (bol.Length >= 3)
                            {
                                PanelKodHafizalari[panelName][bol[0]] = $"{bol[1]}|{bol[2]}";
                                KodHafizasi[bol[0]] = $"{bol[1]}|{bol[2]}";
                            }
                        }
                    }
                }
            }
            catch { }
        }

        public void SaveToMemory(string panelName, string kisaKod, string sourceId, string solutionId)
        {
            if (string.IsNullOrWhiteSpace(kisaKod)) return;
            if (string.IsNullOrWhiteSpace(panelName)) panelName = "Limit";

            try
            {
                InitPanelStorage(panelName);

                PanelKodHafizalari[panelName][kisaKod] = $"{sourceId}|{solutionId}";
                KodHafizasi[kisaKod] = $"{sourceId}|{solutionId}";

                string pMemPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, $"kod_hafizasi_{panelName.ToLower()}.txt");
                File.AppendAllText(pMemPath, $"{kisaKod}|{sourceId}|{solutionId}\n");

                string genMemPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "kod_hafizasi.txt");
                File.AppendAllText(genMemPath, $"{kisaKod}|{panelName}|{sourceId}|{solutionId}\n");
            }
            catch { }
        }

        public void SaveToMemory(string kisaKod, string sourceId, string solutionId)
        {
            SaveToMemory("Limit", kisaKod, sourceId, solutionId);
        }

        private string DetectPanel(string filePath, string sampleContent = "")
        {
            string dirName = Path.GetFileName(Path.GetDirectoryName(filePath) ?? "");
            if (dirName.Equals("Limit", StringComparison.OrdinalIgnoreCase)) return "Limit";
            if (dirName.Equals("Final", StringComparison.OrdinalIgnoreCase)) return "Final";
            if (dirName.Equals("Esen", StringComparison.OrdinalIgnoreCase)) return "Esen";

            string fileName = Path.GetFileNameWithoutExtension(filePath).ToLower();
            if (fileName.Contains("limit")) return "Limit";
            if (fileName.Contains("final") || fileName.Contains("finale doğru")) return "Final";
            if (fileName.Contains("esen")) return "Esen";

            string lowerContent = sampleContent.ToLower();
            if (lowerContent.Contains("limit.frns.in")) return "Limit";
            if (lowerContent.Contains("final.frns.in")) return "Final";
            if (lowerContent.Contains("esen.frns.in")) return "Esen";

            return "Genel";
        }

        /// <summary>
        /// Veritabanı klasöründeki tüm Excel (.xlsx ve .xls) dosyalarını asenkron belleğe yükler.
        /// </summary>
        public async Task<(int FileCount, int QuestionCount, string Message)> LoadExcelDatabaseAsync(IProgress<string>? progress = null)
        {
            return await Task.Run(() =>
            {
                try
                {
                    EnsureDirectory(_databaseDirectory);

                    // Hem .xlsx hem de .xls dosyalarını bul
                    string[] excelDosyalari = Directory.GetFiles(_databaseDirectory, "*.*", SearchOption.AllDirectories)
                        .Where(f => (f.EndsWith(".xlsx", StringComparison.OrdinalIgnoreCase) || f.EndsWith(".xls", StringComparison.OrdinalIgnoreCase))
                                    && !Path.GetFileName(f).StartsWith("~$"))
                        .ToArray();

                    if (excelDosyalari.Length == 0)
                    {
                        return (0, 0, "Veritabani klasöründe hiçbir Excel dosyası (.xlsx / .xls) bulunamadı!");
                    }

                    progress?.Report($"Veritabanı taranıyor ({excelDosyalari.Length} Excel dosyası)...");

                    SistemVeritabani.Clear();
                    foreach (var kv in PanelVeritabanlari) kv.Value.Clear();
                    _tumSorular.Clear();
                    LoadMemory();
                    Duzeltmeler.Yukle();

                    int islenenDosyaSayisi = 0;

                    foreach (string dosyaYolu in excelDosyalari)
                    {
                        try
                        {
                            bool isXlsx = dosyaYolu.EndsWith(".xlsx", StringComparison.OrdinalIgnoreCase);

                            if (isXlsx)
                            {
                                try
                                {
                                    using var package = new ExcelPackage(new FileInfo(dosyaYolu));
                                    if (package.Workbook.Worksheets.Count > 0)
                                    {
                                        var worksheet = package.Workbook.Worksheets[0];
                                        if (worksheet.Dimension != null)
                                        {
                                            string detectedPanel = DetectPanel(dosyaYolu);
                                            InitPanelStorage(detectedPanel);

                                            int rowCount = worksheet.Dimension.Rows;
                                            for (int row = 1; row <= rowCount; row++)
                                            {
                                                string kaynakAdi = worksheet.Cells[row, 1].Text.Trim();
                                                string soruNo = worksheet.Cells[row, 2].Text.Trim();
                                                string solutionId = worksheet.Cells[row, 3].Text.Trim();
                                                string cevapAnahtari = worksheet.Cells[row, 4].Text.Trim();

                                                if (kaynakAdi.Equals("Kaynak Adı", StringComparison.OrdinalIgnoreCase) ||
                                                    solutionId.Equals("Solution ID", StringComparison.OrdinalIgnoreCase))
                                                    continue;

                                                string sourceId = "";
                                                string domain = "";

                                                for (int col = 5; col <= Math.Min(16, worksheet.Dimension.Columns); col++)
                                                {
                                                    string cellText = worksheet.Cells[row, col].Text;
                                                    string sId = SourceIdCikar(cellText);
                                                    if (!string.IsNullOrEmpty(sId)) sourceId = sId;
                                                    if (cellText.Contains(".frns.in"))
                                                    {
                                                        var m = Regex.Match(cellText, @"https?:\/\/([a-zA-Z0-9_\-\.]+frns\.in)");
                                                        if (m.Success) domain = m.Groups[1].Value;
                                                    }
                                                }

                                                if (!string.IsNullOrEmpty(kaynakAdi) && !string.IsNullOrEmpty(soruNo))
                                                {
                                                    string anahtar = (kaynakAdi + "_" + soruNo).ToLower();
                                                    var refObj = new SistemSoruReferans
                                                    {
                                                        KaynakAdi = kaynakAdi,
                                                        SoruNo = soruNo,
                                                        SolutionId = solutionId,
                                                        CevapAnahtari = cevapAnahtari,
                                                        SourceId = sourceId,
                                                        PanelDomain = !string.IsNullOrEmpty(domain) ? domain : $"{detectedPanel.ToLower()}.frns.in"
                                                    };

                                                    PanelVeritabanlari[detectedPanel][anahtar] = refObj;
                                                    SistemVeritabani[anahtar] = refObj;
                                                    _tumSorular.Add(new DbSoru { Panel = detectedPanel, KaynakAdi = kaynakAdi, SoruNoMetin = soruNo, SolutionId = solutionId, SourceId = sourceId, CevapAnahtari = cevapAnahtari });
                                                }
                                            }
                                            islenenDosyaSayisi++;
                                            continue;
                                        }
                                    }
                                }
                                catch { }
                            }

                            // .xls (BIFF8 binary) veya okunmayan .xlsx için ExcelDataReader ile oku
                            using var stream = File.Open(dosyaYolu, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
                            using var reader = ExcelReaderFactory.CreateReader(stream);
                            var dataSet = reader.AsDataSet();

                            if (dataSet.Tables.Count > 0)
                            {
                                var table = dataSet.Tables[0];
                                string detectedPanel = DetectPanel(dosyaYolu);
                                InitPanelStorage(detectedPanel);

                                for (int row = 0; row < table.Rows.Count; row++)
                                {
                                    string kaynakAdi = table.Rows[row][0]?.ToString()?.Trim() ?? "";
                                    string soruNo = table.Columns.Count > 1 ? table.Rows[row][1]?.ToString()?.Trim() ?? "" : "";
                                    string solutionId = table.Columns.Count > 2 ? table.Rows[row][2]?.ToString()?.Trim() ?? "" : "";
                                    string cevapAnahtari = table.Columns.Count > 3 ? table.Rows[row][3]?.ToString()?.Trim() ?? "" : "";

                                    if (kaynakAdi.Equals("Kaynak Adı", StringComparison.OrdinalIgnoreCase) ||
                                        solutionId.Equals("Solution ID", StringComparison.OrdinalIgnoreCase))
                                        continue;

                                    string sourceId = "";
                                    string domain = "";

                                    for (int col = 4; col < Math.Min(16, table.Columns.Count); col++)
                                    {
                                        string cellText = table.Rows[row][col]?.ToString() ?? "";
                                        string sId = SourceIdCikar(cellText);
                                        if (!string.IsNullOrEmpty(sId)) sourceId = sId;
                                        if (cellText.Contains(".frns.in"))
                                        {
                                            var m = Regex.Match(cellText, @"https?:\/\/([a-zA-Z0-9_\-\.]+frns\.in)");
                                            if (m.Success) domain = m.Groups[1].Value;
                                        }
                                    }

                                    if (!string.IsNullOrEmpty(kaynakAdi) && !string.IsNullOrEmpty(soruNo))
                                    {
                                        string anahtar = (kaynakAdi + "_" + soruNo).ToLower();
                                        var refObj = new SistemSoruReferans
                                        {
                                            KaynakAdi = kaynakAdi,
                                            SoruNo = soruNo,
                                            SolutionId = solutionId,
                                            CevapAnahtari = cevapAnahtari,
                                            SourceId = sourceId,
                                            PanelDomain = !string.IsNullOrEmpty(domain) ? domain : $"{detectedPanel.ToLower()}.frns.in"
                                        };

                                        PanelVeritabanlari[detectedPanel][anahtar] = refObj;
                                        SistemVeritabani[anahtar] = refObj;
                                        _tumSorular.Add(new DbSoru { Panel = detectedPanel, KaynakAdi = kaynakAdi, SoruNoMetin = soruNo, SolutionId = solutionId, SourceId = sourceId, CevapAnahtari = cevapAnahtari });
                                    }
                                }
                                islenenDosyaSayisi++;
                            }
                        }
                        catch { }
                    }

                    Indeks.Olustur(_tumSorular);

                    var panelOzetleri = PanelVeritabanlari
                        .Where(p => p.Value.Count > 0)
                        .Select(p => $"{p.Key}: {p.Value.Count} soru");

                    string detayMsg = string.Join(" | ", panelOzetleri);

                    return (islenenDosyaSayisi, SistemVeritabani.Count, $"{islenenDosyaSayisi} dosyadan {SistemVeritabani.Count} soru yüklendi. ({detayMsg})");
                }
                catch (Exception ex)
                {
                    return (0, 0, "HATA Veritabanı Yükleme: " + ex.Message);
                }
            });
        }

        public async Task<(bool Success, int AddedCount, string Message)> SyncFolderFromPanelAsync(
            HttpClient client, 
            string panelNameOrDomain, 
            string folderId, 
            string customFolderTitle = "",
            IProgress<string>? progress = null)
        {
            try
            {
                string domain = panelNameOrDomain.Contains(".frns.in") ? panelNameOrDomain : $"{panelNameOrDomain.ToLower()}.frns.in";
                string panelName = panelNameOrDomain.Contains(".") ? panelNameOrDomain.Split('.')[0] : panelNameOrDomain;
                panelName = char.ToUpper(panelName[0]) + panelName[1..].ToLower();

                progress?.Report($"[{panelName}] ({domain}) Klasör ID {folderId} taranıyor...");

                var endpoints = new[]
                {
                    $"https://{domain}/controller/action_questions.php",
                    $"https://{domain}/controller/soru_cozum/action_solutions.php",
                    $"https://{domain}/controller/soru_cozum/action_questions.php"
                };

                var payloadList = new[]
                {
                    new List<KeyValuePair<string, string>> { new("action", "get_questions"), new("source", folderId) },
                    new List<KeyValuePair<string, string>> { new("action", "get_questions"), new("source_id", folderId) },
                    new List<KeyValuePair<string, string>> { new("action", "list"), new("source", folderId) }
                };

                string responseText = "";
                bool requestSuccess = false;

                foreach (var url in endpoints)
                {
                    foreach (var postData in payloadList)
                    {
                        try
                        {
                            var content = new FormUrlEncodedContent(postData);
                            var response = await client.PostAsync(url, content);
                            string text = await response.Content.ReadAsStringAsync();

                            if (response.IsSuccessStatusCode && !string.IsNullOrWhiteSpace(text) && (text.Contains("\"id\"") || text.Contains("\"order_number\"") || text.Contains("\"answer\"")))
                            {
                                responseText = text;
                                requestSuccess = true;
                                break;
                            }
                        }
                        catch { }
                    }
                    if (requestSuccess) break;
                }

                if (!requestSuccess || string.IsNullOrEmpty(responseText))
                {
                    return (false, 0, "Panel yanıt vermedi veya oturum geçersiz/yetkisiz.");
                }

                var yeniEklenenler = new List<SistemSoruReferans>();
                int eklenen = 0;

                InitPanelStorage(panelName);

                try
                {
                    using var doc = JsonDocument.Parse(responseText);
                    var root = doc.RootElement;

                    JsonElement dataArray = root;
                    if (root.TryGetProperty("data", out var dProp) && dProp.ValueKind == JsonValueKind.Array) dataArray = dProp;
                    else if (root.TryGetProperty("questions", out var qProp) && qProp.ValueKind == JsonValueKind.Array) dataArray = qProp;
                    else if (root.TryGetProperty("items", out var iProp) && iProp.ValueKind == JsonValueKind.Array) dataArray = iProp;

                    if (dataArray.ValueKind == JsonValueKind.Array)
                    {
                        foreach (var item in dataArray.EnumerateArray())
                        {
                            string solutionId = item.TryGetProperty("id", out var idProp) ? (idProp.ValueKind == JsonValueKind.String ? idProp.GetString() ?? "" : idProp.GetInt64().ToString()) : "";
                            string soruNo = item.TryGetProperty("order_number", out var onProp) ? (onProp.ValueKind == JsonValueKind.String ? onProp.GetString() ?? "" : onProp.GetInt64().ToString()) : "";
                            string cevap = item.TryGetProperty("answer", out var ansProp) ? ansProp.GetString() ?? "" : "";
                            string kaynakAdi = customFolderTitle;

                            if (string.IsNullOrEmpty(kaynakAdi) && item.TryGetProperty("title", out var tProp))
                            {
                                kaynakAdi = tProp.GetString() ?? "";
                            }
                            if (string.IsNullOrEmpty(kaynakAdi))
                            {
                                kaynakAdi = $"Klasor_{folderId}";
                            }

                            if (!string.IsNullOrEmpty(solutionId) && !string.IsNullOrEmpty(soruNo))
                            {
                                string anahtar = (kaynakAdi + "_" + soruNo).ToLower();
                                var refObj = new SistemSoruReferans
                                {
                                    KaynakAdi = kaynakAdi,
                                    SoruNo = soruNo,
                                    SolutionId = solutionId,
                                    SourceId = folderId,
                                    CevapAnahtari = cevap,
                                    PanelDomain = domain
                                };

                                PanelVeritabanlari[panelName][anahtar] = refObj;
                                SistemVeritabani[anahtar] = refObj;
                                yeniEklenenler.Add(refObj);
                                eklenen++;
                            }
                        }
                    }
                }
                catch
                {
                    var matches = Regex.Matches(responseText, @"\""id\""\s*:\s*\""?(\d+)\""?.*?\""order_number\""\s*:\s*\""?(\d+)\""?", RegexOptions.Singleline);
                    foreach (Match m in matches)
                    {
                        string solId = m.Groups[1].Value;
                        string sNo = m.Groups[2].Value;
                        string kAdi = string.IsNullOrEmpty(customFolderTitle) ? $"Klasor_{folderId}" : customFolderTitle;

                        string anahtar = (kAdi + "_" + sNo).ToLower();
                        var refObj = new SistemSoruReferans
                        {
                            KaynakAdi = kAdi,
                            SoruNo = sNo,
                            SolutionId = solId,
                            SourceId = folderId,
                            PanelDomain = domain
                        };

                        PanelVeritabanlari[panelName][anahtar] = refObj;
                        SistemVeritabani[anahtar] = refObj;
                        yeniEklenenler.Add(refObj);
                        eklenen++;
                    }
                }

                if (eklenen > 0)
                {
                    try
                    {
                        string panelDir = Path.Combine(_databaseDirectory, panelName);
                        EnsureDirectory(panelDir);

                        string dosyaAdi = $"{customFolderTitle ?? ("Klasor_" + folderId)}_{DateTime.Now:yyyyMMdd_HHmmss}.xlsx";
                        dosyaAdi = string.Join("_", dosyaAdi.Split(Path.GetInvalidFileNameChars()));
                        string kayitYolu = Path.Combine(panelDir, dosyaAdi);

                        using (var p = new ExcelPackage())
                        {
                            var ws = p.Workbook.Worksheets.Add("Sorular");
                            for (int i = 0; i < yeniEklenenler.Count; i++)
                            {
                                ws.Cells[i + 1, 1].Value = yeniEklenenler[i].KaynakAdi;
                                ws.Cells[i + 1, 2].Value = yeniEklenenler[i].SoruNo;
                                ws.Cells[i + 1, 3].Value = yeniEklenenler[i].SolutionId;
                                ws.Cells[i + 1, 4].Value = yeniEklenenler[i].CevapAnahtari;
                                ws.Cells[i + 1, 5].Value = $"https://{domain}/questions/{folderId}";
                            }
                            p.SaveAs(new FileInfo(kayitYolu));
                        }
                    }
                    catch { }

                    foreach (var y in yeniEklenenler)
                        _tumSorular.Add(new DbSoru { Panel = panelName, KaynakAdi = y.KaynakAdi, SoruNoMetin = y.SoruNo, SolutionId = y.SolutionId, SourceId = y.SourceId, CevapAnahtari = y.CevapAnahtari });
                    Indeks.Olustur(_tumSorular);

                    return (true, eklenen, $"BAŞARILI: [{panelName}] panelinden {eklenen} soru hafızaya alındı ve '{panelName}' veritabanına kaydedildi!");
                }

                return (false, 0, "Klasörde soru bulunamadı veya oturum geçersiz.");
            }
            catch (Exception ex)
            {
                return (false, 0, "Senkronizasyon Hatası: " + ex.Message);
            }
        }

        public string ExportReportToExcel(List<SoruIslemRaporu> raporlar, string hedefKlasor = "")
        {
            if (string.IsNullOrEmpty(hedefKlasor))
            {
                hedefKlasor = Environment.GetFolderPath(Environment.SpecialFolder.Desktop);
            }

            string dosyaAdi = $"Soru_Kopyalama_Raporu_{DateTime.Now:yyyyMMdd_HHmmss}.xlsx";
            string tamYol = Path.Combine(hedefKlasor, dosyaAdi);

            using (var package = new ExcelPackage())
            {
                var ws = package.Workbook.Worksheets.Add("İşlem Raporu");

                string[] headers = { "Sıra", "Kısa Kod", "Kod Açılımı", "Hedef Soru No", "Hedef Cevap", "Solution ID", "Source ID", "Durum", "Detay", "İşlem Zamanı" };
                for (int i = 0; i < headers.Length; i++)
                {
                    ws.Cells[1, i + 1].Value = headers[i];
                    ws.Cells[1, i + 1].Style.Font.Bold = true;
                    ws.Cells[1, i + 1].Style.Fill.PatternType = OfficeOpenXml.Style.ExcelFillStyle.Solid;
                    ws.Cells[1, i + 1].Style.Fill.BackgroundColor.SetColor(System.Drawing.Color.LightSteelBlue);
                }

                int row = 2;
                foreach (var r in raporlar)
                {
                    ws.Cells[row, 1].Value = r.SiraNo;
                    ws.Cells[row, 2].Value = r.KisaKod;
                    ws.Cells[row, 3].Value = r.KodAcilimi;
                    ws.Cells[row, 4].Value = r.HedefSoruNo;
                    ws.Cells[row, 5].Value = r.HedefCevap;
                    ws.Cells[row, 6].Value = r.BulunanSolutionId;
                    ws.Cells[row, 7].Value = r.BulunanSourceId;
                    ws.Cells[row, 8].Value = r.Basarili ? "BAŞARILI" : "HATALI";
                    ws.Cells[row, 9].Value = r.DurumMesaji;
                    ws.Cells[row, 10].Value = r.IslemZamani.ToString("yyyy-MM-dd HH:mm:ss");

                    if (r.Basarili)
                    {
                        ws.Cells[row, 8].Style.Font.Color.SetColor(System.Drawing.Color.Green);
                    }
                    else
                    {
                        ws.Cells[row, 8].Style.Font.Color.SetColor(System.Drawing.Color.Red);
                    }

                    row++;
                }

                ws.Cells[ws.Dimension.Address].AutoFitColumns();
                package.SaveAs(new FileInfo(tamYol));
            }

            return tamYol;
        }
    }
}

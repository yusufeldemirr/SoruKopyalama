using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;
using SoruKopyalama.Models;

namespace SoruKopyalama.Services
{
    public enum OturumDurumu { Gecerli, Gecersiz, Bilinmiyor }

    public class FernusSessionManager
    {
        private static readonly string SettingsPath = VeriYolu.Dosya("appsettings.json");
        public AppSettings Settings { get; private set; } = new AppSettings();

        public FernusSessionManager()
        {
            LoadSettings();
        }

        public void LoadSettings()
        {
            try
            {
                if (File.Exists(SettingsPath))
                {
                    string json = File.ReadAllText(SettingsPath);
                    var loaded = JsonSerializer.Deserialize<AppSettings>(json);
                    if (loaded != null)
                    {
                        Settings = loaded;
                    }
                }
            }
            catch
            {
                Settings = new AppSettings();
            }

            // Standart paneller eksikse ekle
            EnsureDefaultPanels();
        }

        public void SaveSettings()
        {
            try
            {
                var options = new JsonSerializerOptions { WriteIndented = true };
                string json = JsonSerializer.Serialize(Settings, options);
                File.WriteAllText(SettingsPath, json);
            }
            catch { }
        }

        private void EnsureDefaultPanels()
        {
            var defaultList = new[]
            {
                new { Name = "Final", Domain = "final.frns.in" },
                new { Name = "Limit", Domain = "limit.frns.in" },
                new { Name = "Esen", Domain = "esen.frns.in" }
            };

            foreach (var d in defaultList)
            {
                if (!Settings.Panels.Any(p => p.Domain.Equals(d.Domain, StringComparison.OrdinalIgnoreCase)))
                {
                    Settings.Panels.Add(new PanelConfig { Name = d.Name, Domain = d.Domain });
                }
            }
        }

        public PanelConfig GetPanel(string panelNameOrDomain)
        {
            var panel = Settings.Panels.FirstOrDefault(p =>
                p.Name.Equals(panelNameOrDomain, StringComparison.OrdinalIgnoreCase) ||
                p.Domain.Equals(panelNameOrDomain, StringComparison.OrdinalIgnoreCase));

            if (panel == null)
            {
                panel = new PanelConfig { Name = panelNameOrDomain, Domain = panelNameOrDomain };
                Settings.Panels.Add(panel);
            }

            return panel;
        }

        public void UpdatePanelCookie(string panelNameOrDomain, string cookie)
        {
            var panel = GetPanel(panelNameOrDomain);
            panel.ActiveCookie = cookie;
            panel.LastLoginTime = DateTime.Now;
            SaveSettings();
        }

        public void UpdatePanelCredentials(string panelNameOrDomain, string email, string password)
        {
            var panel = GetPanel(panelNameOrDomain);
            panel.Email = email;
            panel.Password = password;
            SaveSettings();
        }

        /// <summary>
        /// E-posta ve şifre ile Fernus paneline otomatik oturum açar ve Cookie'yi çeker.
        /// </summary>
        public async Task<(bool Success, string Message, string Cookie)> LoginAsync(string panelNameOrDomain, string? email = null, string? password = null)
        {
            var panel = GetPanel(panelNameOrDomain);

            if (!string.IsNullOrEmpty(email)) panel.Email = email;
            if (!string.IsNullOrEmpty(password)) panel.Password = password;

            if (string.IsNullOrEmpty(panel.Email) || string.IsNullOrEmpty(panel.Password))
            {
                // Eğer email/password yoksa ama aktif cookie varsa onu deneyebiliriz
                if (!string.IsNullOrEmpty(panel.ActiveCookie))
                {
                    return (true, "Mevcut kayıtlı Cookie kullanılıyor.", panel.ActiveCookie);
                }
                return (false, "Lütfen panel için E-posta ve Şifre girin.", "");
            }

            try
            {
                var cookieContainer = new CookieContainer();
                var handler = new HttpClientHandler
                {
                    CookieContainer = cookieContainer,
                    UseCookies = true,
                    AllowAutoRedirect = true,
                    ServerCertificateCustomValidationCallback = (sender, cert, chain, sslPolicyErrors) => true
                };

                using var client = new HttpClient(handler);
                client.Timeout = TimeSpan.FromSeconds(20);
                client.DefaultRequestHeaders.Add("User-Agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/120.0.0.0 Safari/537.36");

                // 1. Ana sayfayı ve admin login sayfasını ziyaret edip başlangıç oturum çerezlerini al
                string baseUrl = $"https://{panel.Domain}";
                try
                {
                    await client.GetAsync($"{baseUrl}/admin/login.php");
                }
                catch
                {
                    try { await client.GetAsync(baseUrl); } catch { }
                }

                // 2. Login POST İsteği
                var loginEndpoints = new[]
                {
                    $"{baseUrl}/controller/action_login.php",
                    $"{baseUrl}/admin/controller/action_login.php",
                    $"{baseUrl}/controller/action_auth.php",
                    $"{baseUrl}/login.php",
                    $"{baseUrl}/admin/login.php",
                    $"{baseUrl}/login"
                };

                var postData = new List<KeyValuePair<string, string>>
                {
                    new KeyValuePair<string, string>("action", "login"),
                    new KeyValuePair<string, string>("email", panel.Email),
                    new KeyValuePair<string, string>("password", panel.Password),
                    new KeyValuePair<string, string>("remember", "1")
                };

                string generatedCookie = "";
                bool loginSuccess = false;
                string serverResponse = "";

                foreach (var endpoint in loginEndpoints)
                {
                    try
                    {
                        var content = new FormUrlEncodedContent(postData);
                        var response = await client.PostAsync(endpoint, content);
                        serverResponse = await response.Content.ReadAsStringAsync();

                        if (response.IsSuccessStatusCode && (serverResponse.Contains("success") || serverResponse.Contains("\"status\":true") || serverResponse.Contains("true") || response.Headers.Contains("Set-Cookie")))
                        {
                            // Login sonrası admin sayfasını ziyaret edip tüm çerezleri sabitle
                            try { await client.GetAsync($"{baseUrl}/admin/kaynak_duzenle.php"); } catch { }

                            var uri = new Uri(baseUrl);
                            var cookies = cookieContainer.GetCookies(uri);
                            var cookieList = new List<string>();

                            foreach (Cookie c in cookies)
                            {
                                cookieList.Add($"{c.Name}={c.Value}");
                            }

                            if (!cookieList.Any(c => c.StartsWith("email=")))
                            {
                                cookieList.Add($"email={panel.Email}");
                            }

                            generatedCookie = string.Join("; ", cookieList);
                            if (!string.IsNullOrEmpty(generatedCookie))
                            {
                                loginSuccess = true;
                                break;
                            }
                        }
                    }
                    catch { }
                }

                if (loginSuccess && !string.IsNullOrEmpty(generatedCookie))
                {
                    panel.ActiveCookie = generatedCookie;
                    panel.LastLoginTime = DateTime.Now;
                    SaveSettings();
                    return (true, "Otomatik giriş başarılı! Güncel çerez hafızaya alındı.", generatedCookie);
                }

                // Eğer aktif cookie zaten varsa yedek olarak onu tut
                if (!string.IsNullOrEmpty(panel.ActiveCookie))
                {
                    return (true, "Giriş isteği tamamlandı (Mevcut Cookie devrede).", panel.ActiveCookie);
                }

                return (false, $"Giriş başarısız. Lütfen bilgilerinizi kontrol edin veya tarayıcıdan Cookie kopyalayın. (Yanıt: {serverResponse})", "");
            }
            catch (Exception ex)
            {
                return (false, $"Giriş Hatası: {ex.Message}", "");
            }
        }

        /// <summary>
        /// Kayıtlı (veya verilen) çerezle panelde oturumun açık olup olmadığını sınar.
        /// Yönetici sayfası giriş sayfasına yönlendiriyorsa veya şifre alanı içeriyorsa oturum düşmüştür.
        /// Panel hiç yanıt vermezse Bilinmiyor döner; bu durumda işlem denenir.
        /// </summary>
        public async Task<OturumDurumu> OturumGecerliMiAsync(string panelNameOrDomain, string? cookie = null)
        {
            var panel = GetPanel(panelNameOrDomain);
            cookie ??= panel.ActiveCookie;
            if (string.IsNullOrWhiteSpace(cookie)) return OturumDurumu.Gecersiz;

            var handler = new HttpClientHandler
            {
                ServerCertificateCustomValidationCallback = (sender, cert, chain, sslPolicyErrors) => true,
                AllowAutoRedirect = true,
                UseCookies = false
            };
            using var client = new HttpClient(handler);
            client.Timeout = TimeSpan.FromSeconds(15);
            client.DefaultRequestHeaders.Add("User-Agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/120.0.0.0 Safari/537.36");
            client.DefaultRequestHeaders.Add("Cookie", cookie);

            bool yanitAlindi = false;
            foreach (var yol in new[] { "/admin/", "/admin/index.php", "/admin/kaynak_duzenle.php" })
            {
                try
                {
                    var resp = await client.GetAsync($"https://{panel.Domain}{yol}");
                    string body = await resp.Content.ReadAsStringAsync();
                    string sonUrl = resp.RequestMessage?.RequestUri?.ToString().ToLowerInvariant() ?? "";

                    if (sonUrl.Contains("login")) return OturumDurumu.Gecersiz;
                    if (!resp.IsSuccessStatusCode) continue;
                    yanitAlindi = true;
                    if (System.Text.RegularExpressions.Regex.IsMatch(body, @"type\s*=\s*[""']?password", System.Text.RegularExpressions.RegexOptions.IgnoreCase))
                        return OturumDurumu.Gecersiz;
                    return OturumDurumu.Gecerli;
                }
                catch { }
            }

            return yanitAlindi ? OturumDurumu.Gecerli : OturumDurumu.Bilinmiyor;
        }

        /// <summary>
        /// Panel için yapılandırılmış HttpClient oluşturur.
        /// </summary>
        public HttpClient CreateHttpClient(string panelNameOrDomain)
        {
            var panel = GetPanel(panelNameOrDomain);

            var handler = new HttpClientHandler
            {
                ServerCertificateCustomValidationCallback = (sender, cert, chain, sslPolicyErrors) => true,
                AutomaticDecompression = DecompressionMethods.GZip | DecompressionMethods.Deflate
            };

            var client = new HttpClient(handler);
            client.Timeout = TimeSpan.FromSeconds(30);
            client.DefaultRequestHeaders.Add("User-Agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/120.0.0.0 Safari/537.36");
            client.DefaultRequestHeaders.Add("X-Requested-With", "XMLHttpRequest");
            client.DefaultRequestHeaders.Add("Referer", $"https://{panel.Domain}/admin/");

            if (!string.IsNullOrEmpty(panel.ActiveCookie))
            {
                client.DefaultRequestHeaders.Add("Cookie", panel.ActiveCookie);
            }

            return client;
        }

        /// <summary>
        /// Deneme ana klasörünün altındaki branş klasörlerini bulur.
        /// Lise: Ana / Türkçe, Sosyal, Matematik, Fen.
        /// Ortaokul: Ana / Sözel Bölüm / (Türkçe, Sosyal Bilgiler, Din Kültürü, İngilizce) + Sayısal Bölüm / (Matematik, Fen).
        /// Branş adı taşımayan ara klasörlerin (Sözel/Sayısal Bölüm, A/B Kitapçığı...) içine de bakılır.
        /// </summary>
        public async Task<(bool Success, string Message, Dictionary<string, string> SubFolders, List<(string Id, string Title)> AllFolders)> GetSubFoldersAsync(string panelNameOrDomain, string parentFolderId)
        {
            var resultFolders = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            var allList = new List<(string Id, string Title)>();

            if (string.IsNullOrWhiteSpace(parentFolderId))
            {
                return (false, "Lütfen geçerli bir Ana Klasör ID girin.", resultFolders, allList);
            }

            var panel = GetPanel(panelNameOrDomain);
            using var client = CreateHttpClient(panelNameOrDomain);

            var cocuklar = await KlasorCocuklariniGetirAsync(client, panel, parentFolderId);

            foreach (var c in cocuklar)
            {
                if (KlasorBransi(c.Title) != "")
                {
                    allList.Add(c);
                    continue;
                }

                // Ara klasör: bir seviye daha in
                var torunlar = await KlasorCocuklariniGetirAsync(client, panel, c.Id);
                if (torunlar.Count > 0)
                    allList.AddRange(torunlar.Where(t => !allList.Any(x => x.Id == t.Id)));
                else
                    allList.Add(c);
            }

            foreach (var item in allList)
            {
                string brans = KlasorBransi(item.Title);
                if (brans != "" && !resultFolders.ContainsKey(brans))
                    resultFolders[brans] = item.Id;
            }

            // İsimsiz (Test 1..4 gibi) tam 4 klasör: lise sırası
            if (allList.Count == 4 && resultFolders.Count == 0)
            {
                resultFolders["Türkçe"] = allList[0].Id;
                resultFolders["Sosyal"] = allList[1].Id;
                resultFolders["Matematik"] = allList[2].Id;
                resultFolders["Fen"] = allList[3].Id;
            }

            if (allList.Count > 0)
            {
                return (true, $"{allList.Count} adet alt klasör tespit edildi, {resultFolders.Count} branş eşleştirildi.", resultFolders, allList);
            }

            return (false, "Panelden alt klasör listesi alınamadı. Lütfen oturumunuzu kontrol edin veya ID'leri manuel girin.", resultFolders, allList);
        }

        /// <summary>Klasör adından branş: Türkçe, Sosyal, Din, İngilizce, Matematik, Fen. Tanınmazsa boş.</summary>
        public static string KlasorBransi(string title)
        {
            string n = (title ?? "").ToUpper(new System.Globalization.CultureInfo("tr-TR"))
                .Replace("İ", "I").Replace("Ğ", "G").Replace("Ü", "U").Replace("Ş", "S").Replace("Ö", "O").Replace("Ç", "C");

            if (n.Contains("TURK") || n.Contains("EDEB")) return "Türkçe";
            if (n.Contains("DIN")) return "Din";
            if (n.Contains("INGILIZ") || n.Contains("ENGLISH")) return "İngilizce";
            if (n.Contains("SOS") || n.Contains("TARIH") || n.Contains("COG") || n.Contains("FELSEFE")) return "Sosyal";
            if (n.Contains("MAT") || n.Contains("GEO")) return "Matematik";
            if (n.Contains("FEN") || n.Contains("FIZIK") || n.Contains("KIMYA") || n.Contains("BIYOLOJI")) return "Fen";

            var t = System.Text.RegularExpressions.Regex.Match(n, @"\b(?:TEST|T)\s*-?\s*([1-4])\b");
            if (t.Success) return t.Groups[1].Value switch { "1" => "Türkçe", "2" => "Sosyal", "3" => "Matematik", _ => "Fen" };
            return "";
        }

        private async Task<List<(string Id, string Title)>> KlasorCocuklariniGetirAsync(HttpClient client, PanelConfig panel, string parentFolderId)
        {
            var allList = new List<(string Id, string Title)>();

            // Fernus JSTree standartlarındaki endpoint'ler
            var getUrls = new[]
            {
                $"https://{panel.Domain}/jstree/process.php?tree_struct=source_tree_struct&tree_data=source_tree_data&operation=get_node&id={parentFolderId}",
                $"https://{panel.Domain}/admin/jstree/process.php?tree_struct=source_tree_struct&tree_data=source_tree_data&operation=get_node&id={parentFolderId}",
                $"https://{panel.Domain}/controller/process.php?tree_struct=source_tree_struct&tree_data=source_tree_data&operation=get_node&id={parentFolderId}",
                $"https://{panel.Domain}/process.php?tree_struct=source_tree_struct&tree_data=source_tree_data&operation=get_node&id={parentFolderId}",
                $"https://{panel.Domain}/jstree/process.php?tree_struct=sources&id={parentFolderId}",
                $"https://{panel.Domain}/controller/process.php?tree_struct=sources&id={parentFolderId}",
                $"https://{panel.Domain}/process.php?tree_struct=sources&id={parentFolderId}",
                $"https://{panel.Domain}/controller/process.php?tree_struct=sources&operation=get_children&id={parentFolderId}",
                $"https://{panel.Domain}/controller/action_sources.php?action=get_sources&parent={parentFolderId}"
            };

            foreach (var url in getUrls)
            {
                try
                {
                    var response = await client.GetAsync(url);
                    string resText = await response.Content.ReadAsStringAsync();

                    if (response.IsSuccessStatusCode && !string.IsNullOrWhiteSpace(resText) && (resText.Contains("\"id\"") || resText.Contains("\"text\"") || resText.Contains("\"title\"")))
                    {
                        ParseFoldersFromJson(resText, allList);
                        if (allList.Count > 0) break;
                    }
                }
                catch { }
            }

            if (allList.Count == 0)
            {
                var postEndpoints = new[]
                {
                    $"https://{panel.Domain}/jstree/process.php",
                    $"https://{panel.Domain}/controller/process.php",
                    $"https://{panel.Domain}/process.php",
                    $"https://{panel.Domain}/controller/action_sources.php",
                    $"https://{panel.Domain}/controller/soru_cozum/action_sources.php"
                };

                var requestPayloads = new[]
                {
                    new List<KeyValuePair<string, string>> { new("tree_struct", "source_tree_struct"), new("tree_data", "source_tree_data"), new("operation", "get_node"), new("id", parentFolderId) },
                    new List<KeyValuePair<string, string>> { new("id", parentFolderId) },
                    new List<KeyValuePair<string, string>> { new("action", "get_sources"), new("parent", parentFolderId) },
                    new List<KeyValuePair<string, string>> { new("action", "get_sources"), new("source_id", parentFolderId) }
                };

                foreach (var url in postEndpoints)
                {
                    foreach (var payload in requestPayloads)
                    {
                        try
                        {
                            var content = new FormUrlEncodedContent(payload);
                            var response = await client.PostAsync(url, content);
                            string resText = await response.Content.ReadAsStringAsync();

                            if (response.IsSuccessStatusCode && !string.IsNullOrWhiteSpace(resText) && (resText.Contains("\"id\"") || resText.Contains("\"text\"") || resText.Contains("\"title\"")))
                            {
                                ParseFoldersFromJson(resText, allList);
                                if (allList.Count > 0) break;
                            }
                        }
                        catch { }
                    }
                    if (allList.Count > 0) break;
                }
            }

            // Sorgulanan klasörün kendisi de yanıtta dönebiliyor; onu çocuk sayma
            allList.RemoveAll(x => x.Id == parentFolderId);
            return allList;
        }

        private void ParseFoldersFromJson(string json, List<(string Id, string Title)> list)
        {
            try
            {
                using var doc = JsonDocument.Parse(json);
                var root = doc.RootElement;

                void Traverse(JsonElement element)
                {
                    if (element.ValueKind == JsonValueKind.Array)
                    {
                        foreach (var child in element.EnumerateArray())
                        {
                            Traverse(child);
                        }
                    }
                    else if (element.ValueKind == JsonValueKind.Object)
                    {
                        string id = "";
                        string title = "";

                        if (element.TryGetProperty("id", out var idProp))
                        {
                            if (idProp.ValueKind == JsonValueKind.String) id = idProp.GetString() ?? "";
                            else if (idProp.ValueKind == JsonValueKind.Number) id = idProp.GetInt64().ToString();
                        }
                        else if (element.TryGetProperty("source_id", out var sIdProp))
                        {
                            if (sIdProp.ValueKind == JsonValueKind.String) id = sIdProp.GetString() ?? "";
                            else if (sIdProp.ValueKind == JsonValueKind.Number) id = sIdProp.GetInt64().ToString();
                        }

                        if (element.TryGetProperty("text", out var txProp))
                            title = txProp.GetString() ?? "";
                        else if (element.TryGetProperty("title", out var tProp))
                            title = tProp.GetString() ?? "";
                        else if (element.TryGetProperty("name", out var nProp))
                            title = nProp.GetString() ?? "";

                        if (!string.IsNullOrEmpty(id) && !string.IsNullOrEmpty(title))
                        {
                            if (!list.Any(x => x.Id == id))
                            {
                                list.Add((id, title));
                            }
                        }

                        // Alt dallar (children / nodes / sub / data)
                        if (element.TryGetProperty("children", out var cProp) && cProp.ValueKind == JsonValueKind.Array) Traverse(cProp);
                        if (element.TryGetProperty("nodes", out var ndProp) && ndProp.ValueKind == JsonValueKind.Array) Traverse(ndProp);
                        if (element.TryGetProperty("data", out var dProp)) Traverse(dProp);
                        if (element.TryGetProperty("sources", out var scProp)) Traverse(scProp);
                    }
                }

                Traverse(root);
            }
            catch
            {
                // Regex fallback 1: id önce, text/title sonra
                var matches1 = System.Text.RegularExpressions.Regex.Matches(json, @"\""id\""\s*:\s*\""?(\d+)\""?.*?\""(?:text|title|name)\""\s*:\s*\""([^\""]+)\""", System.Text.RegularExpressions.RegexOptions.Singleline);
                foreach (System.Text.RegularExpressions.Match m in matches1)
                {
                    string id = m.Groups[1].Value;
                    string title = m.Groups[2].Value;
                    if (!list.Any(x => x.Id == id))
                    {
                        list.Add((id, title));
                    }
                }

                // Regex fallback 2: text/title önce, id sonra
                var matches2 = System.Text.RegularExpressions.Regex.Matches(json, @"\""(?:text|title|name)\""\s*:\s*\""([^\""]+)\"".*?\""id\""\s*:\s*\""?(\d+)\""?", System.Text.RegularExpressions.RegexOptions.Singleline);
                foreach (System.Text.RegularExpressions.Match m in matches2)
                {
                    string title = m.Groups[1].Value;
                    string id = m.Groups[2].Value;
                    if (!list.Any(x => x.Id == id))
                    {
                        list.Add((id, title));
                    }
                }
            }
        }
    }
}

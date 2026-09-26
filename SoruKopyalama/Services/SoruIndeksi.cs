using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text.RegularExpressions;
using SoruKopyalama.Models;

namespace SoruKopyalama.Services
{
    /// <summary>
    /// Veritabanı sorularını yapısal anahtarla (yayın, seri, sınav, deneme, test, soru no) indeksler.
    /// Kelime puanlaması yapmaz: bir soru ya birebir bulunur ya da bulunamaz.
    /// </summary>
    public class SoruIndeksi
    {
        private static readonly CultureInfo Tr = new CultureInfo("tr-TR");

        // Panel -> Anahtar -> Adaylar
        private readonly Dictionary<string, Dictionary<SoruAnahtari, List<DbSoru>>> _panelIndeksi = new(StringComparer.OrdinalIgnoreCase);
        // Panel -> SolutionId -> Soru
        private readonly Dictionary<string, Dictionary<string, DbSoru>> _idIndeksi = new(StringComparer.OrdinalIgnoreCase);

        public int ToplamSoru => _idIndeksi.Values.Sum(d => d.Count);
        public int AyristirilamayanSayisi { get; private set; }

        public static string Normalize(string s)
        {
            return Regex.Replace((s ?? "").ToLower(Tr), @"\s+", " ").Trim();
        }

        public void Olustur(IEnumerable<DbSoru> sorular)
        {
            _panelIndeksi.Clear();
            _idIndeksi.Clear();
            AyristirilamayanSayisi = 0;

            var liste = sorular.Where(s => !string.IsNullOrWhiteSpace(s.SolutionId)).ToList();

            // Görseli olmayan satırlarda kaynak klasör ID'si boş kalıyor. Aynı klasördeki diğer
            // sorulardan en sık görülen klasör ID'sini alarak boşlukları doldur.
            foreach (var grup in liste.GroupBy(s => (s.Panel, s.KaynakAdi)))
            {
                string? enSik = grup.Where(s => !string.IsNullOrEmpty(s.SourceId))
                                    .GroupBy(s => s.SourceId)
                                    .OrderByDescending(g => g.Count())
                                    .Select(g => g.Key)
                                    .FirstOrDefault();
                if (enSik == null) continue;
                foreach (var s in grup) s.SourceId = enSik;
            }

            foreach (var soru in liste)
            {
                if (!_idIndeksi.TryGetValue(soru.Panel, out var idler))
                    _idIndeksi[soru.Panel] = idler = new Dictionary<string, DbSoru>();

                // Aynı dosya iki kez yüklendiyse (örn. kök klasörde ve panel klasöründe) tekrar ekleme
                if (idler.ContainsKey(soru.SolutionId)) continue;
                idler[soru.SolutionId] = soru;

                soru.Anahtar = YoldanAnahtar(soru.KaynakAdi, soru.SoruNoMetin);
                if (soru.Anahtar == null)
                {
                    AyristirilamayanSayisi++;
                    continue;
                }

                if (!_panelIndeksi.TryGetValue(soru.Panel, out var indeks))
                    _panelIndeksi[soru.Panel] = indeks = new Dictionary<SoruAnahtari, List<DbSoru>>();
                if (!indeks.TryGetValue(soru.Anahtar, out var adaylar))
                    indeks[soru.Anahtar] = adaylar = new List<DbSoru>();
                adaylar.Add(soru);
            }
        }

        public List<DbSoru> Bul(string panel, SoruAnahtari anahtar)
        {
            if (_panelIndeksi.TryGetValue(panel, out var indeks) && indeks.TryGetValue(anahtar, out var adaylar))
                return adaylar;
            return new List<DbSoru>();
        }

        public DbSoru? IdIleBul(string panel, string solutionId)
        {
            solutionId = (solutionId ?? "").Trim();
            if (_idIndeksi.TryGetValue(panel, out var idler) && idler.TryGetValue(solutionId, out var soru))
                return soru;
            return null;
        }

        public DbSoru? TumPanellerdeIdIleBul(string solutionId)
        {
            foreach (var panel in _idIndeksi.Keys)
            {
                var s = IdIleBul(panel, solutionId);
                if (s != null) return s;
            }
            return null;
        }

        /// <summary>
        /// Örnek yol: ".../Final Yayınları Finale Doğru TYTAYT Kurs Deneme Sınavı1/Final Yayınları Finale Doğru TYT Kurs Deneme Sınavı1/Türkçe Testi"
        /// Son 3 klasör: [deneme grubu] / [TYT veya AYT kitapçığı] / [test].
        /// </summary>
        public static SoruAnahtari? YoldanAnahtar(string kaynakAdi, string soruNoMetin)
        {
            var seg = (kaynakAdi ?? "").Split('/').Where(s => !string.IsNullOrWhiteSpace(s)).ToArray();
            if (seg.Length < 3) return null;

            string grup = Normalize(seg[^3]);
            string kitapcik = Normalize(seg[^2]);
            string test = Normalize(seg[^1]);
            string ikisi = grup + " " + kitapcik;

            var (yayin, seri) = YayinVeSeri(ikisi);
            if (yayin == "") return null;

            // "...Deneme Sınavı1" veya "...Deneme Sınavı1 (Başarı Serisi)"
            var dMatch = Regex.Match(grup, @"(\d+)\s*$");
            if (!dMatch.Success) dMatch = Regex.Match(grup, @"(?:sınavı|deneme)\s*(\d+)");
            if (!dMatch.Success) dMatch = Regex.Match(kitapcik, @"(?:sınavı|deneme)\s*(\d+)");
            if (!dMatch.Success) return null;

            string kitapcikSinav = kitapcik.Replace("tytayt", "");
            string sinav = kitapcikSinav.Contains("ayt") ? "AYT" : kitapcikSinav.Contains("tyt") ? "TYT" : "";
            if (sinav == "") return null;

            int testNo = TestNo(test);
            if (testNo == 0) return null;

            var nMatch = Regex.Match(soruNoMetin ?? "", @"^\s*(\d+)");
            if (!nMatch.Success) return null;

            return new SoruAnahtari(yayin, seri, sinav, int.Parse(dMatch.Groups[1].Value), testNo, int.Parse(nMatch.Groups[1].Value));
        }

        /// <summary>Normalize edilmiş (küçük harf) metinden yayın ve seriyi bulur. Kod açılımı için de kullanılır.</summary>
        public static (string Yayin, string Seri) YayinVeSeri(string metin)
        {
            if (metin.Contains("esen"))
            {
                if (metin.Contains("başarı")) return ("Esen", "Başarı");
                return ("", "");
            }
            if (metin.Contains("finale doğru"))
            {
                if (metin.Contains("okul")) return ("Final", "Okul");
                if (metin.Contains("kurs")) return ("Final", "Kurs");
                return ("", "");
            }
            if (metin.Contains("aktör")) return ("Limit", "Aktör");
            if (metin.Contains("dublör")) return ("Limit", "Dublör");
            return ("", "");
        }

        /// <summary>TYT: 1 Türkçe, 2 Sosyal, 3 Temel Mat, 4 Fen. AYT: 1 Edebiyat-Sosyal1, 2 Sosyal2, 3 Mat, 4 Fen.</summary>
        public static int TestNo(string test)
        {
            // "Türk Dili ve Edebiyatı Sosyal Bilimler1" hem edebiyat hem sosyal içerir, önce edebiyata bak
            if (test.Contains("edebiyat") || test.Contains("türkçe")) return 1;
            if (test.Contains("sosyal")) return 2;
            if (test.Contains("matematik")) return 3;
            if (test.Contains("fen")) return 4;
            return 0;
        }

        public static string TestBransi(int test) => test switch
        {
            1 => "Türkçe",
            2 => "Sosyal",
            3 => "Matematik",
            4 => "Fen",
            _ => ""
        };
    }
}

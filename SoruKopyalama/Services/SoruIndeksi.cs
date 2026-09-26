using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text.RegularExpressions;
using SoruKopyalama.Models;

namespace SoruKopyalama.Services
{
    /// <summary>
    /// Veritabanı sorularını yapısal anahtarla (sınav, deneme, test, soru no) indeksler ve her sorunun
    /// klasör yolundaki ayırt edici kelimeleri (yayın/seri: "final", "okul", "aktör", "lek"...) saklar.
    /// Yayın/seri listesi yoktur: hangi kelimelerin önemli olduğu iş emrindeki açıklamadan gelir.
    /// </summary>
    public class SoruIndeksi
    {
        private static readonly CultureInfo Tr = new CultureInfo("tr-TR");

        /// <summary>Her deneme klasöründe geçen, seriyi ayırt etmeyen kelimeler.</summary>
        private static readonly HashSet<string> GenelKelimeler = new(StringComparer.Ordinal)
        {
            "yayınları", "yayınevi", "yayın", "yayınlari", "kurumsal", "deneme", "denemesi", "denemeleri",
            "sınavı", "sınavları", "sınav", "sinavi", "testi", "test", "tytayt", "tyt", "ayt", "sınıf", "sınıflar",
            "sezonu", "sezon", "grubu", "grup", "ve", "ile", "serisi", "seri", "soru", "kds", "yks"
        };

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

        /// <summary>Metni kelimelere böler; sayıları ve genel kelimeleri atar. Kalanlar seriyi ayırt eden kelimelerdir.</summary>
        public static HashSet<string> AyirtEdiciKelimeler(string metin)
        {
            var sonuc = new HashSet<string>(StringComparer.Ordinal);
            foreach (var ham in Regex.Split(Normalize(metin), @"[^\p{L}\p{Nd}'’`]+"))
            {
                string k = ham.Replace("'", "").Replace("’", "").Replace("`", ""); // "20'li" → "20li"
                if (k.Length < 2 || k.All(char.IsDigit) || GenelKelimeler.Contains(k)) continue;
                // "20252026", "2025-2026" gibi sezon parçaları
                if (Regex.IsMatch(k, @"^\d")) continue;
                sonuc.Add(k);
            }
            return sonuc;
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
                soru.GrupKelimeleri = YoldanGrupKelimeleri(soru.KaynakAdi);
                soru.Sezon = SezonBul(soru.KaynakAdi) ?? SezonBul(soru.DosyaAdi) ?? "";
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

        /// <summary>Yapısal anahtarı tutan tüm sorular (seri ayrımı yapılmadan).</summary>
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

        /// <summary>Son üç klasör: [deneme grubu] / [kitapçık] / [test]. Test klasörü hariç tutulur.</summary>
        private static string[] GrupSegmentleri(string kaynakAdi)
        {
            var seg = (kaynakAdi ?? "").Split('/').Where(s => !string.IsNullOrWhiteSpace(s)).ToArray();
            if (seg.Length < 3) return Array.Empty<string>();
            return new[] { seg[^3], seg[^2] };
        }

        public static HashSet<string> YoldanGrupKelimeleri(string kaynakAdi)
        {
            var kelimeler = new HashSet<string>(StringComparer.Ordinal);
            foreach (var seg in GrupSegmentleri(kaynakAdi))
                kelimeler.UnionWith(AyirtEdiciKelimeler(seg));
            return kelimeler;
        }

        /// <summary>
        /// Örnek yollar:
        ///   ".../Finale Doğru TYTAYT Kurs Deneme Sınavı1/Finale Doğru TYT Kurs Deneme Sınavı1/Türkçe Testi"
        ///   ".../Final 9. Sınıf Kurumsal Deneme Sınavları/Final 9. Sınıf Kurs Deneme1/Türk Dili ve Edebiyatı Testi"
        /// </summary>
        public static SoruAnahtari? YoldanAnahtar(string kaynakAdi, string soruNoMetin)
        {
            var seg = (kaynakAdi ?? "").Split('/').Where(s => !string.IsNullOrWhiteSpace(s)).ToArray();
            if (seg.Length < 3) return null;

            string grup = Normalize(seg[^3]);
            string kitapcik = Normalize(seg[^2]);
            string test = Normalize(seg[^1]);

            string sinav = SinavBul(grup + " " + kitapcik);
            if (sinav == "") return null;

            // Deneme no: önce grup klasörü (LEK D2'de kitapçık yanlışlıkla "Sınavı1" yazıyor), sonra kitapçık
            int deneme = DenemeNoBul(grup) ?? DenemeNoBul(kitapcik) ?? 0;
            if (deneme == 0) return null;

            int testNo = TestNo(test);
            if (testNo == 0) return null;

            var nMatch = Regex.Match(soruNoMetin ?? "", @"^\s*(\d+)");
            if (!nMatch.Success) return null;

            return new SoruAnahtari(sinav, deneme, testNo, int.Parse(nMatch.Groups[1].Value));
        }

        /// <summary>"20252026", "2025-2026", "2025 2026" → "2025-2026". Yoksa null.</summary>
        public static string? SezonBul(string metin)
        {
            var m = Regex.Match(metin ?? "", @"(20\d{2})\s*[-–_ ]?\s*(20\d{2})");
            return m.Success ? $"{m.Groups[1].Value}-{m.Groups[2].Value}" : null;
        }

        /// <summary>"9" / "10" / "11" / "12" (ara sınıf) veya "AYT" / "TYT".</summary>
        public static string SinavBul(string normMetin)
        {
            var sinif = Regex.Match(normMetin, @"\b(9|10|11|12)\s*\.?\s*sınıf");
            if (sinif.Success) return sinif.Groups[1].Value;

            string m = normMetin.Replace("tytayt", " ").Replace("tyt-ayt", " ");
            bool ayt = Regex.IsMatch(m, @"\bayt\b"), tyt = Regex.IsMatch(m, @"\btyt\b");
            if (ayt && !tyt) return "AYT";
            if (tyt && !ayt) return "TYT";
            return "";
        }

        public static int? DenemeNoBul(string normMetin)
        {
            // "9. sınıf" içindeki 9 deneme numarası değildir
            string m = Regex.Replace(normMetin, @"\b(9|10|11|12)\s*\.?\s*sınıf", " ");
            m = Regex.Replace(m, @"\b(20\d{2})\b", " ");
            var son = Regex.Match(m, @"(\d+)\s*$");
            if (son.Success) return int.Parse(son.Groups[1].Value);
            var ic = Regex.Match(m, @"(?:sınavı|deneme|kds)\s*(\d+)");
            if (ic.Success) return int.Parse(ic.Groups[1].Value);
            return null;
        }

        /// <summary>TYT: 1 Türkçe, 2 Sosyal, 3 Temel Mat, 4 Fen. AYT: 1 Edebiyat-Sosyal1, 2 Sosyal2, 3 Mat, 4 Fen.</summary>
        public static int TestNo(string test)
        {
            // "Türk Dili ve Edebiyatı Sosyal Bilimler1" hem edebiyat hem sosyal içerir, önce edebiyata bak
            if (test.Contains("edebiyat") || test.Contains("türkçe") || test.Contains("turkce")) return 1;
            if (test.Contains("sosyal")) return 2;
            if (test.Contains("matematik")) return 3;
            if (test.Contains("fen")) return 4;
            var t = Regex.Match(test, @"\btest\s*([1-4])\b");
            if (t.Success) return int.Parse(t.Groups[1].Value);
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

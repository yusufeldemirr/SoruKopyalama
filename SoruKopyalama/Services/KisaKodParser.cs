using System.Text.RegularExpressions;
using SoruKopyalama.Models;

namespace SoruKopyalama.Services
{
    /// <summary>
    /// Kısa kodu yapısal anahtara çevirir. Sadece anlamı kesin bilinen kod ailelerini tanır;
    /// tanımadığı kodlar için null döner (tahmin yürütmez).
    /// Yeni bir kod ailesi eklemek için buraya bir kural eklemek yeterlidir.
    /// </summary>
    public static class KisaKodParser
    {
        // Final - Finale Doğru. Örnek: 56FNOKFDD1TT1S1
        //   56   : sezon (2025-2026), isteğe bağlı
        //   FN   : Final Yayınları
        //   OK   : Okul serisi  (KR/K: Kurs serisi)
        //   FDD  : Finale Doğru Deneme (FD, FDD, FDDD hepsi kabul)
        //   1    : deneme no
        //   T/A  : TYT / AYT
        //   T1   : test no (1-4)
        //   S1   : soru no
        private static readonly Regex FinaleDogru = new(
            @"^(?<sezon>\d{2})?FN(?<seri>OK|KR|K)(?:FD+|D)(?<deneme>\d+)(?<sinav>[AT])T(?<test>[1-4])S(?<soru>\d+)$",
            RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);

        // Esen - Başarı Serisi. Örnek: 56EBD3TT1S1 = 2025-2026 Esen Başarı Deneme 3, TYT Test 1 (Türkçe), Soru 1
        private static readonly Regex EsenBasari = new(
            @"^(?<sezon>\d{2})?EBD(?<deneme>\d+)(?<sinav>[AT])T(?<test>[1-4])S(?<soru>\d+)$",
            RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);

        // Limit Aktör / Dublör. Örnek: 56LAAYTD6T1S1 = Limit Aktör AYT Deneme 6, Test 1, Soru 1 (LD = Dublör)
        private static readonly Regex LimitAktorDublor = new(
            @"^(?<sezon>\d{2})?L(?<seri>[AD])(?<sinav>AYT|TYT)D(?<deneme>\d+)T(?<test>[1-4])S(?<soru>\d+)$",
            RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);

        // Limit Eğitim Kurumları. Örnek: 56LEKAD8T1S2 = LEK AYT Deneme 8, Test 1, Soru 2 (LEKT... = TYT)
        private static readonly Regex LimitLek = new(
            @"^(?<sezon>\d{2})?LEK(?<sinav>[AT])D(?<deneme>\d+)T(?<test>[1-4])S(?<soru>\d+)$",
            RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);

        public static SoruAnahtari? Coz(string kisaKod)
        {
            string kod = (kisaKod ?? "").Trim().ToUpperInvariant();
            if (kod.StartsWith("TANIMSIZ:")) kod = kod.Substring(9).Trim();

            var m = FinaleDogru.Match(kod);
            if (m.Success)
            {
                return new SoruAnahtari(
                    "Final",
                    m.Groups["seri"].Value == "OK" ? "Okul" : "Kurs",
                    m.Groups["sinav"].Value == "A" ? "AYT" : "TYT",
                    int.Parse(m.Groups["deneme"].Value),
                    int.Parse(m.Groups["test"].Value),
                    int.Parse(m.Groups["soru"].Value));
            }

            m = EsenBasari.Match(kod);
            if (m.Success)
            {
                return new SoruAnahtari(
                    "Esen",
                    "Başarı",
                    m.Groups["sinav"].Value == "A" ? "AYT" : "TYT",
                    int.Parse(m.Groups["deneme"].Value),
                    int.Parse(m.Groups["test"].Value),
                    int.Parse(m.Groups["soru"].Value));
            }

            m = LimitAktorDublor.Match(kod);
            if (m.Success)
            {
                return new SoruAnahtari(
                    "Limit",
                    m.Groups["seri"].Value == "A" ? "Aktör" : "Dublör",
                    m.Groups["sinav"].Value.ToUpperInvariant(),
                    int.Parse(m.Groups["deneme"].Value),
                    int.Parse(m.Groups["test"].Value),
                    int.Parse(m.Groups["soru"].Value));
            }

            m = LimitLek.Match(kod);
            if (m.Success)
            {
                return new SoruAnahtari(
                    "Limit",
                    "LEK",
                    m.Groups["sinav"].Value == "A" ? "AYT" : "TYT",
                    int.Parse(m.Groups["deneme"].Value),
                    int.Parse(m.Groups["test"].Value),
                    int.Parse(m.Groups["soru"].Value));
            }

            return null;
        }

        /// <summary>
        /// İş emrindeki "Kod Açılımı" metnini anahtara çevirir.
        /// Örnek: "2025 2026 FİNAL OKUL FİNALE DOĞRU TYT DENEME 1 - TÜRKÇE - SORU 1"
        ///        "2025 2026 FİNAL OKUL FİNALE DOĞRU TYT DENEME 3 - TEST 3 - SORU 1"
        /// Test: 1 Türkçe/Edebiyat, 2 Sosyal, 3 Matematik, 4 Fen (2025-2026 kodlaması).
        /// </summary>
        public static SoruAnahtari? AcilimdanCoz(string kodAcilimi)
        {
            string a = SoruIndeksi.Normalize(kodAcilimi);
            if (a == "") return null;

            var (yayin, seri) = SoruIndeksi.YayinVeSeri(a);
            if (yayin == "") return null;

            string sinav = Regex.IsMatch(a, @"\bayt\b") ? "AYT" : Regex.IsMatch(a, @"\btyt\b") ? "TYT" : "";
            if (sinav == "") return null;

            var d = Regex.Match(a, @"deneme(?:si| sınavı)?\s*(\d+)");
            if (!d.Success) d = Regex.Match(a, @"(\d+)\s*\.?\s*deneme");
            var s = Regex.Match(a, @"soru\s*(\d+)");
            if (!d.Success || !s.Success) return null;

            // Branş: "- TÜRKÇE -" gibi soru no'dan önceki parça veya "TEST 3"
            int test = 0;
            var t = Regex.Match(a, @"test\s*([1-4])\b");
            if (t.Success) test = int.Parse(t.Groups[1].Value);
            else
            {
                var parcalar = a.Split(" - ");
                if (parcalar.Length >= 2) test = SoruIndeksi.TestNo(parcalar[^2]);
            }
            if (test == 0) return null;

            return new SoruAnahtari(yayin, seri, sinav, int.Parse(d.Groups[1].Value), test, int.Parse(s.Groups[1].Value));
        }
    }
}

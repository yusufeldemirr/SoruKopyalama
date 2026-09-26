using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using SoruKopyalama.Models;

namespace SoruKopyalama.Services
{
    /// <summary>
    /// Kısa kodu ve kod açılımını okur. Yayın/seri listesi yoktur; bütün kod aileleri tek kalıba uyar:
    ///   [sezon 2 rakam] [harf öneki] [sınıf 9/10/11/12]? [AYT/TYT/A/T]? D[deneme] [A/T]? T[test] S[soru]
    /// Örnekler: 56FNOKFDD1TT1S1, 56EBD3TT1S1, 56LAAYTD6T1S1, 56LEKAD8T1S2, 56FK9D8T1S1
    /// Harf öneki (FNOKFD, EB, LA, LEK, FK...) hangi seriyi gösterdiği açıklamadan öğrenilir.
    /// </summary>
    public static class KisaKodParser
    {
        private static readonly Regex Kalip = new(
            @"^(?<sezon>\d{2})?(?<onek>[A-ZÇĞİÖŞÜ]+?)(?<sinif>9|10|11|12)?(?<sinav>AYT|TYT|A|T)?D(?<deneme>\d+)(?<sinav2>[AT])?T(?<test>[1-4])S(?<soru>\d+)$",
            RegexOptions.CultureInvariant);

        public class KodBilgisi
        {
            public string Onek { get; set; } = "";
            /// <summary>"56" → "2025-2026". Kodda yoksa boş.</summary>
            public string Sezon { get; set; } = "";
            /// <summary>"AYT", "TYT", "9"... veya kodda belirtilmemişse "".</summary>
            public string Sinav { get; set; } = "";
            public int Deneme { get; set; }
            public int Test { get; set; }
            public int SoruNo { get; set; }
        }

        public static KodBilgisi? Coz(string kisaKod)
        {
            string kod = (kisaKod ?? "").Trim().ToUpperInvariant().Replace("İ", "I");
            if (kod.StartsWith("TANIMSIZ:")) kod = kod.Substring(9).Trim();

            var m = Kalip.Match(kod);
            if (!m.Success) return null;

            string sinav = "";
            if (m.Groups["sinif"].Success) sinav = m.Groups["sinif"].Value;
            else
            {
                string s = m.Groups["sinav"].Success ? m.Groups["sinav"].Value : m.Groups["sinav2"].Value;
                if (s.StartsWith("A")) sinav = "AYT";
                else if (s.StartsWith("T")) sinav = "TYT";
            }

            return new KodBilgisi
            {
                Onek = m.Groups["onek"].Value,
                Sezon = SezonKoddan(kod),
                Sinav = sinav,
                Deneme = int.Parse(m.Groups["deneme"].Value),
                Test = int.Parse(m.Groups["test"].Value),
                SoruNo = int.Parse(m.Groups["soru"].Value)
            };
        }

        /// <summary>Kodun başındaki iki rakam sezonu verir: 56 → 2025-2026, 45 → 2024-2025. Kalıba uymayan kodlarda da çalışır.</summary>
        public static string SezonKoddan(string kisaKod)
        {
            string kod = (kisaKod ?? "").Trim().ToUpperInvariant();
            if (kod.StartsWith("TANIMSIZ:")) kod = kod.Substring(9).Trim();
            var m = Regex.Match(kod, @"^(\d)(\d)(?=[A-ZÇĞİÖŞÜ])");
            if (!m.Success) return "";
            int a = int.Parse(m.Groups[1].Value), b = int.Parse(m.Groups[2].Value);
            return b == a + 1 ? $"202{a}-202{b}" : "";
        }

        public class AcilimBilgisi
        {
            public string Sezon { get; set; } = "";
            public string Sinav { get; set; } = "";
            public int? Deneme { get; set; }
            public int? SoruNo { get; set; }
            public int Test { get; set; }
            /// <summary>Seriyi ayırt eden kelimeler: {final, okul, finale, doğru}, {lek}, {esen, başarı}...</summary>
            public HashSet<string> GrupKelimeleri { get; set; } = new();
        }

        /// <summary>
        /// "2025 2026 FİNAL KURS 9. SINIF DENEME 8 - TÜRKÇE - SORU 1"
        /// İlk " - " öncesi seriyi ve denemeyi, sonrası branşı ve soru numarasını verir.
        /// </summary>
        public static AcilimBilgisi? AcilimdanCoz(string kodAcilimi)
        {
            string a = SoruIndeksi.Normalize(kodAcilimi);
            if (a == "") return null;

            // İki yazım var:
            //   "2025 2026 FİNAL KURS 9. SINIF DENEME 8 - TÜRKÇE - SORU 1"   (grup - branş - soru)
            //   "FİNALE DOĞRU - TYT - DENEME 7 - FEN - SORU 3"                (her parça ayrı)
            // Son parça soru no, ondan önceki branş; geri kalan her şey seri/deneme bilgisidir.
            var parcalar = a.Split(" - ");
            var s = Regex.Match(a, @"soru\s*(\d+)");
            int grupParcaSayisi = parcalar.Length;
            if (s.Success && Regex.IsMatch(parcalar[^1], @"soru\s*\d+")) grupParcaSayisi--;
            if (grupParcaSayisi >= 2) grupParcaSayisi--; // branş parçası
            string grupMetni = string.Join(" ", parcalar.Take(Math.Max(1, grupParcaSayisi)));
            grupMetni = Regex.Replace(grupMetni, @"soru\s*\d+", " ");

            var bilgi = new AcilimBilgisi
            {
                Sezon = SoruIndeksi.SezonBul(a) ?? "",
                Sinav = SoruIndeksi.SinavBul(a),
                Deneme = SoruIndeksi.DenemeNoBul(grupMetni),
                GrupKelimeleri = SoruIndeksi.AyirtEdiciKelimeler(grupMetni)
            };

            if (s.Success) bilgi.SoruNo = int.Parse(s.Groups[1].Value);
            if (parcalar.Length >= 2) bilgi.Test = SoruIndeksi.TestNo(parcalar[^2]);

            return bilgi;
        }
    }
}

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

            return null;
        }
    }
}

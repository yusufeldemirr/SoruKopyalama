using System;
using System.Collections.Generic;
using System.Linq;

namespace SoruKopyalama.Models
{
    public class PanelConfig
    {
        public string Name { get; set; } = "";
        public string Domain { get; set; } = "";
        public string Email { get; set; } = "";
        public string Password { get; set; } = "";
        public string ActiveCookie { get; set; } = "";
        public DateTime LastLoginTime { get; set; }
    }

    public class AppSettings
    {
        public List<PanelConfig> Panels { get; set; } = new List<PanelConfig>
        {
            new PanelConfig { Name = "Final", Domain = "final.frns.in" },
            new PanelConfig { Name = "Limit", Domain = "limit.frns.in" },
            new PanelConfig { Name = "Esen", Domain = "esen.frns.in" }
        };

        public string DefaultSourcePanel { get; set; } = "Final";
        public string DefaultTargetPanel { get; set; } = "Final";
    }

    public class SistemSoruReferans
    {
        public string KaynakAdi { get; set; } = "";
        public string SoruNo { get; set; } = "";
        public string SolutionId { get; set; } = "";
        public string SourceId { get; set; } = "";
        public string CevapAnahtari { get; set; } = "";
        public string PanelDomain { get; set; } = "final.frns.in";
    }

    /// <summary>
    /// Bir sorunun yapısal kimliği. Hem kısa koddan hem de veritabanındaki klasör yolundan üretilir;
    /// iki taraf aynı anahtarı üretiyorsa eşleşme kesindir.
    /// </summary>
    public record SoruAnahtari(string Sinav, int Deneme, int Test, int SoruNo)
    {
        public override string ToString() => $"{(Sinav.All(char.IsDigit) ? Sinav + ".Sınıf" : Sinav)} Deneme {Deneme} Test {Test} Soru {SoruNo}";
    }

    /// <summary>Veritabanı Excel'lerindeki tek bir soru satırı.</summary>
    public class DbSoru
    {
        public string Panel { get; set; } = "";
        public string DosyaAdi { get; set; } = "";
        /// <summary>"2025-2026" gibi; klasör yolundan veya dosya adından. Bilinmiyorsa boş.</summary>
        public string Sezon { get; set; } = "";
        public string KaynakAdi { get; set; } = "";
        public string SoruNoMetin { get; set; } = "";
        public string SolutionId { get; set; } = "";
        public string SourceId { get; set; } = "";
        public string CevapAnahtari { get; set; } = "";
        public string Zorluk { get; set; } = "";
        public string KazanimId { get; set; } = "";
        public string Kazanim { get; set; } = "";
        public SoruAnahtari? Anahtar { get; set; }
        /// <summary>Klasör yolundaki seriyi ayırt eden kelimeler: {final, finale, doğru, okul}, {limit, aktör}...</summary>
        public HashSet<string> GrupKelimeleri { get; set; } = new();

        /// <summary>Kaynak yolunun son 3 klasörü (ekranda göstermek için).</summary>
        public string KisaYol
        {
            get
            {
                var seg = KaynakAdi.Split('/', StringSplitOptions.RemoveEmptyEntries);
                return string.Join(" / ", seg.Skip(Math.Max(0, seg.Length - 3))) + " - " + SoruNoMetin.Trim();
            }
        }
    }

    public enum EslesmeDurumu
    {
        Kesin,          // Kısa kod veritabanında tek bir soruya birebir denk geldi
        Duzeltme,       // Kullanıcının kalıcı düzeltme tablosundan geldi
        ElleGirildi,    // Ön kontrol ekranında kullanıcı Soru ID yazdı
        CevapFarkli,    // Bulundu ama iş emrindeki cevap veritabanındakinden farklı
        Coklu,          // Aynı anahtara birden fazla soru denk geliyor
        KodAcilimCelisiyor, // Kısa kod ile kod açılımı farklı soruları gösteriyor
        SeriBelirsiz,   // Deneme/test/soru tuttu ama açıklamadaki seri kelimeleri hiçbir klasörle tam örtüşmedi
        Bulunamadi,
        HedefNoBos      // İş emrinde hedef soru numarası yok
    }

    /// <summary>İş emrinin bir satırı ve bu satır için bulunan kaynak soru.</summary>
    public class IsEmriSatiri
    {
        public int SiraNo { get; set; }
        public string KisaKod { get; set; } = "";
        public string KodAcilimi { get; set; } = "";
        public string BolumKodu { get; set; } = "";
        public string HedefSoruNo { get; set; } = "";
        public string Cevap { get; set; } = "";

        public string Brans { get; set; } = "";
        public string Sezon { get; set; } = "";
        public SoruAnahtari? Anahtar { get; set; }
        /// <summary>Açıklamadan (veya önek hafızasından) gelen seri kelimeleri.</summary>
        public HashSet<string> GrupKelimeleri { get; set; } = new();

        public EslesmeDurumu Durum { get; set; } = EslesmeDurumu.Bulunamadi;
        public string Aciklama { get; set; } = "";
        public DbSoru? Kaynak { get; set; }
        public List<DbSoru> Adaylar { get; set; } = new();
        public bool Secili { get; set; }
    }

    public class SoruIslemRaporu
    {
        public int SiraNo { get; set; }
        public string KisaKod { get; set; } = "";
        public string KodAcilimi { get; set; } = "";
        public string HedefSoruNo { get; set; } = "";
        public string HedefCevap { get; set; } = "";
        public string BulunanSolutionId { get; set; } = "";
        public string BulunanSourceId { get; set; } = "";
        public bool Basarili { get; set; }
        public string DurumMesaji { get; set; } = "";
        public DateTime IslemZamani { get; set; } = DateTime.Now;
    }
}

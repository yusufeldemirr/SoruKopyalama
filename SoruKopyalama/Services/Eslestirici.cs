using System;
using System.Collections.Generic;
using System.Linq;
using SoruKopyalama.Models;

namespace SoruKopyalama.Services
{
    /// <summary>
    /// İş emri satırları için kaynak soruyu belirler. Öncelik sırası:
    ///   1. Kalıcı düzeltme tablosu (kullanıcının onayladığı eşleşmeler)
    ///   2. Kısa kodun yapısal anahtarla birebir bulunması
    ///   3. Eski kelime puanlama motoru (sadece öneri; varsayılan olarak kopyalanmaz)
    /// </summary>
    public class Eslestirici
    {
        private readonly DatabaseManager _db;
        private readonly QuestionMatcher _eskiMotor = new QuestionMatcher();

        public Eslestirici(DatabaseManager db)
        {
            _db = db;
        }

        public void Coz(IEnumerable<IsEmriSatiri> satirlar, string panel)
        {
            foreach (var s in satirlar) Coz(s, panel);
        }

        public void Coz(IsEmriSatiri s, string panel)
        {
            s.Kaynak = null;
            s.Adaylar = new List<DbSoru>();
            s.Anahtar = KisaKodParser.Coz(s.KisaKod);
            s.Brans = s.Anahtar != null
                ? SoruIndeksi.TestBransi(s.Anahtar.Test)
                : ShortCodeDecoder.GetBranş(s.KisaKod, s.BolumKodu);

            // 1. Kalıcı düzeltme
            if (_db.Duzeltmeler.TryGet(panel, s.KisaKod, out var dSolId, out var dSrcId))
            {
                var dbSoru = _db.Indeks.IdIleBul(panel, dSolId);
                s.Kaynak = dbSoru ?? new DbSoru { Panel = panel, SolutionId = dSolId, SourceId = dSrcId, KaynakAdi = "(düzeltme tablosu)" };
                if (string.IsNullOrEmpty(s.Kaynak.SourceId)) s.Kaynak.SourceId = dSrcId;
                s.Durum = EslesmeDurumu.Duzeltme;
                s.Aciklama = "Kalıcı düzeltmeden alındı";
            }
            // 2. Yapısal birebir eşleşme
            else if (s.Anahtar != null)
            {
                var adaylar = _db.Indeks.Bul(panel, s.Anahtar);
                s.Adaylar = adaylar;

                if (adaylar.Count == 1)
                {
                    s.Kaynak = adaylar[0];
                    s.Durum = EslesmeDurumu.Kesin;
                    s.Aciklama = "Birebir eşleşti";
                }
                else if (adaylar.Count > 1)
                {
                    // Cevap anahtarı sadece birinde tutuyorsa o doğrudur
                    var cevabiTutan = adaylar.Where(a => CevapAyni(a.CevapAnahtari, s.Cevap)).ToList();
                    if (cevabiTutan.Count == 1 && !string.IsNullOrEmpty(s.Cevap))
                    {
                        s.Kaynak = cevabiTutan[0];
                        s.Durum = EslesmeDurumu.Kesin;
                        s.Aciklama = $"{adaylar.Count} aday vardı, cevap anahtarı ile ayırt edildi";
                    }
                    else
                    {
                        s.Durum = EslesmeDurumu.Coklu;
                        s.Aciklama = $"{adaylar.Count} aday: " + string.Join(" / ", adaylar.Select(a => $"{a.SolutionId} ({a.CevapAnahtari})")) + " → doğru olanı Soru ID'ye yazın";
                    }
                }
                else
                {
                    s.Durum = EslesmeDurumu.Bulunamadi;
                    s.Aciklama = $"[{panel}] veritabanında yok: {s.Anahtar}";
                }
            }
            // 3. Kod tanınmadı: eski motorun tahmini
            else
            {
                var m = _eskiMotor.Match(s.KisaKod, s.KodAcilimi, _db.GetVeritabani(panel), new Dictionary<string, string>(), s.BolumKodu);
                if (m.IsSuccess && !string.IsNullOrEmpty(m.SolutionId))
                {
                    s.Kaynak = _db.Indeks.IdIleBul(panel, m.SolutionId)
                               ?? new DbSoru { Panel = panel, SolutionId = m.SolutionId, SourceId = m.SourceId, KaynakAdi = m.BulunanKaynakAdi, SoruNoMetin = m.BulunanSoruNo, CevapAnahtari = m.BulunanCevapAnahtari };
                    s.Durum = EslesmeDurumu.Tahmini;
                    s.Aciklama = "Kod tanınmadı, kelime benzerliğiyle TAHMİN edildi - kontrol edin";
                }
                else
                {
                    s.Durum = EslesmeDurumu.Bulunamadi;
                    s.Aciklama = "Kod tanınmadı ve tahmin de bulunamadı";
                }
            }

            // Cevap anahtarı kontrolü: iş emrindeki cevap veritabanındakiyle tutmuyorsa şüpheli
            if (s.Kaynak != null && s.Durum == EslesmeDurumu.Kesin &&
                !string.IsNullOrEmpty(s.Cevap) && !string.IsNullOrEmpty(s.Kaynak.CevapAnahtari) &&
                !CevapAyni(s.Kaynak.CevapAnahtari, s.Cevap))
            {
                s.Durum = EslesmeDurumu.CevapFarkli;
                s.Aciklama = $"Bulundu ama cevap farklı! İş emri: {s.Cevap}, veritabanı: {s.Kaynak.CevapAnahtari}";
            }

            if (s.Kaynak != null && string.IsNullOrEmpty(s.Kaynak.SourceId))
            {
                s.Aciklama += " | UYARI: kaynak klasör ID bilinmiyor";
            }

            if (string.IsNullOrWhiteSpace(s.HedefSoruNo))
            {
                s.Durum = EslesmeDurumu.HedefNoBos;
                s.Aciklama = "İş emrinde hedef soru numarası boş" + (s.Kaynak != null ? $" (kaynak bulundu: {s.Kaynak.SolutionId})" : "");
            }

            s.Secili = VarsayilanSecili(s);
        }

        public static bool VarsayilanSecili(IsEmriSatiri s) =>
            s.Kaynak != null &&
            !string.IsNullOrEmpty(s.Kaynak.SourceId) &&
            (s.Durum == EslesmeDurumu.Kesin || s.Durum == EslesmeDurumu.Duzeltme || s.Durum == EslesmeDurumu.ElleGirildi);

        private static bool CevapAyni(string a, string b) =>
            string.Equals((a ?? "").Trim(), (b ?? "").Trim(), StringComparison.OrdinalIgnoreCase);
    }
}

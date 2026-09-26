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
            // Anahtarı önce kısa koddan, olmazsa kod açılımından çıkar. İkisi de varsa birbirini doğrulamalı.
            var koddan = KisaKodParser.Coz(s.KisaKod);
            var acilimdan = KisaKodParser.AcilimdanCoz(s.KodAcilimi);
            s.Anahtar = koddan ?? acilimdan;
            string anahtarKaynagi = koddan != null ? "koddan" : "kod açılımından";
            // Açılımdaki branş adı (TÜRKÇE/SOSYAL) sorunun gideceği branşı gösterir, kaynak testi değil
            // (ör. AYT Test 1'in sosyal kısmı "SOSYAL" yazar). Bu yüzden test numarası karşılaştırılmaz.
            bool celiski = koddan != null && acilimdan != null && koddan with { Test = 0 } != acilimdan with { Test = 0 };

            // Hedef klasör: Master sütunu (TÜRK-İÇ, SOS-İÇ...) varsa ona göre, yoksa kodun test numarasına göre
            if (!string.IsNullOrWhiteSpace(s.BolumKodu))
                s.Brans = ShortCodeDecoder.ResolveBrans("", s.BolumKodu);
            else if (s.Anahtar != null)
                s.Brans = SoruIndeksi.TestBransi(s.Anahtar.Test);
            else
                s.Brans = ShortCodeDecoder.GetBranş(s.KisaKod, s.BolumKodu);

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
                    s.Aciklama = $"Birebir eşleşti ({anahtarKaynagi})";
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
            // 3. Ne kısa kod ne de kod açılımı tanındı. Tahmin yürütülmez: yanlış soru kopyalamaktansa boş bırakmak iyidir.
            else
            {
                s.Durum = EslesmeDurumu.Bulunamadi;
                s.Aciklama = $"Kısa kod ({s.KisaKod}) ve kod açılımı tanınmadı - bu kod ailesi programa eklenmeli";
            }

            // Cevap anahtarı kontrolü: iş emrindeki cevap veritabanındakiyle tutmuyorsa şüpheli
            if (s.Kaynak != null && s.Durum == EslesmeDurumu.Kesin &&
                !string.IsNullOrEmpty(s.Cevap) && !string.IsNullOrEmpty(s.Kaynak.CevapAnahtari) &&
                !CevapAyni(s.Kaynak.CevapAnahtari, s.Cevap))
            {
                s.Durum = EslesmeDurumu.CevapFarkli;
                s.Aciklama = $"Bulundu ama cevap farklı! İş emri: {s.Cevap}, veritabanı: {s.Kaynak.CevapAnahtari}";
            }

            if (celiski && s.Durum == EslesmeDurumu.Kesin)
            {
                s.Durum = EslesmeDurumu.KodAcilimCelisiyor;
                s.Aciklama = $"Kısa kod ({koddan}) ile kod açılımı ({acilimdan}) farklı soruyu gösteriyor - kontrol edin";
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

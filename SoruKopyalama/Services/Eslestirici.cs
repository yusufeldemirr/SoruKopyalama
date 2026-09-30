using System;
using System.Collections.Generic;
using System.Linq;
using SoruKopyalama.Models;

namespace SoruKopyalama.Services
{
    /// <summary>
    /// İş emri satırları için kaynak soruyu belirler. Öncelik sırası:
    ///   1. Kalıcı düzeltme tablosu (kullanıcının onayladığı eşleşmeler)
    ///   2. Yapısal eşleşme: kısa koddan deneme/test/soru, açıklamadan seri kelimeleri;
    ///      veritabanında hem numaraları hem kelimeleri tutan tek soru varsa kesindir
    /// Tahmin yürütülmez: yanlış soru kopyalamaktansa boş bırakmak tercih edilir.
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
            s.Anahtar = null;
            s.GrupKelimeleri = new HashSet<string>(StringComparer.Ordinal);

            var kod = KisaKodParser.Coz(s.KisaKod);
            var acilim = KisaKodParser.AcilimdanCoz(s.KodAcilimi);
            s.Sezon = KisaKodParser.SezonKoddan(s.KisaKod);
            if (s.Sezon == "") s.Sezon = acilim?.Sezon ?? "";

            // Yapısal anahtar: numaralar öncelikle koddan, sınav bilgisi kodda yoksa açıklamadan
            string anahtarKaynagi;
            if (kod != null)
            {
                string sinav = kod.Sinav != "" ? kod.Sinav : (acilim?.Sinav ?? "");
                if (sinav != "") s.Anahtar = new SoruAnahtari(sinav, kod.Deneme, kod.Test, kod.SoruNo);
                anahtarKaynagi = "koddan";
            }
            else
            {
                anahtarKaynagi = "kod açılımından";
                if (acilim != null && acilim.Sinav != "" && acilim.Deneme != null && acilim.SoruNo != null && acilim.Test != 0)
                    s.Anahtar = new SoruAnahtari(acilim.Sinav, acilim.Deneme.Value, acilim.Test, acilim.SoruNo.Value);
            }

            // Seri kelimeleri: açıklamadan; açıklama yoksa daha önce öğrenilmiş önekten
            if (acilim != null && acilim.GrupKelimeleri.Count > 0)
                s.GrupKelimeleri = acilim.GrupKelimeleri;
            else if (kod != null && _db.Onekler.Getir(kod.Onek) is { } ogrenilmis)
                s.GrupKelimeleri = ogrenilmis;

            // Kod ile açıklama farklı deneme/soru gösteriyorsa güvenilmez
            bool celiski = kod != null && acilim != null &&
                           ((acilim.Deneme != null && acilim.Deneme != kod.Deneme) ||
                            (acilim.SoruNo != null && acilim.SoruNo != kod.SoruNo) ||
                            (acilim.Sinav != "" && kod.Sinav != "" && acilim.Sinav != kod.Sinav));

            // Hedef klasör: Master sütunu branş söylüyorsa (TÜRK-İÇ, SOS-İÇ...) ona göre; "A-Master" gibi
            // sadece kitapçık belirten değerlerde veya boşsa kodun test numarasına göre
            string masterBrans = SoruIndeksi.MasterdanBrans(s.BolumKodu);
            if (masterBrans != "")
                s.Brans = masterBrans;
            else if (s.Anahtar != null)
                s.Brans = SoruIndeksi.TestBransi(s.Anahtar.Test, s.Anahtar.Sinav);
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
            // 2. Yapısal eşleşme
            else if (s.Anahtar != null)
            {
                var ayniNumara = _db.Indeks.Bul(panel, s.Anahtar);
                YapisalEslestir(s, panel, kod, ayniNumara);
            }
            else
            {
                s.Durum = EslesmeDurumu.Bulunamadi;
                s.Aciklama = kod == null
                    ? $"Kısa kod ({s.KisaKod}) kalıba uymuyor ve açıklamadan da deneme/soru çıkarılamadı"
                    : "Kodda ve açıklamada sınav türü (TYT/AYT/sınıf) belirtilmemiş";
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
                s.Aciklama = $"Kısa kod ile kod açılımı farklı deneme/soru gösteriyor - kontrol edin";
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

            // Kesin eşleşmeden önek -> seri ilişkisini öğren
            if (s.Durum == EslesmeDurumu.Kesin && kod != null && acilim != null && acilim.GrupKelimeleri.Count > 0)
                _db.Onekler.Ogren(kod.Onek, acilim.GrupKelimeleri);

            s.Secili = VarsayilanSecili(s);
        }

        private void YapisalEslestir(IsEmriSatiri s, string panel, KisaKodParser.KodBilgisi? kod, List<DbSoru> ayniNumara)
        {
            if (ayniNumara.Count == 0)
            {
                s.Durum = EslesmeDurumu.Bulunamadi;
                s.Aciklama = $"[{panel}] veritabanında {s.Anahtar} yok (bu deneme veritabanına eklenmemiş olabilir)";
                return;
            }

            // Sezon: 2024-2025 kodu 2025-2026 sorusuna eşlenmemeli
            if (s.Sezon != "")
            {
                var ayniSezon = ayniNumara.Where(a => a.Sezon == "" || a.Sezon == s.Sezon).ToList();
                if (ayniSezon.Count == 0)
                {
                    s.Durum = EslesmeDurumu.Bulunamadi;
                    s.Aciklama = $"Numaraları tutan soru var ama sezonu farklı (iş emri {s.Sezon}, veritabanı {string.Join("/", ayniNumara.Select(a => a.Sezon).Distinct())}); {s.Sezon} dosyası veritabanına eklenmemiş";
                    return;
                }
                ayniNumara = ayniSezon;
            }

            // Açıklamadaki seri kelimelerinin hepsi klasör yolunda geçmeli
            var tamOrtusen = s.GrupKelimeleri.Count == 0
                ? new List<DbSoru>()
                : ayniNumara.Where(a => s.GrupKelimeleri.IsSubsetOf(a.GrupKelimeleri)).ToList();

            if (tamOrtusen.Count == 0 && s.GrupKelimeleri.Count == 0)
            {
                // Seri bilgisi hiç yok: numaralar tek soruya denk geliyorsa yine de kabul et
                if (ayniNumara.Count == 1)
                {
                    tamOrtusen = ayniNumara;
                }
                else
                {
                    s.Adaylar = ayniNumara;
                    s.Durum = EslesmeDurumu.Coklu;
                    s.Aciklama = "Açıklama yok ve önek daha önce görülmedi; " + AdayMetni(ayniNumara);
                    return;
                }
            }

            if (tamOrtusen.Count == 1)
            {
                s.Kaynak = tamOrtusen[0];
                s.Durum = EslesmeDurumu.Kesin;
                s.Aciklama = $"Birebir eşleşti ({SeriMetni(s.GrupKelimeleri)})";
                return;
            }

            if (tamOrtusen.Count > 1)
            {
                // Seri ayrımı yetmedi: en fazla ortak kelimesi olan tek bir aday varsa onu al, yoksa cevaba bak
                var enIyi = tamOrtusen.GroupBy(a => a.GrupKelimeleri.Intersect(s.GrupKelimeleri).Count() - a.GrupKelimeleri.Count)
                                      .OrderByDescending(g => g.Key).First().ToList();
                var cevabiTutan = enIyi.Where(a => CevapAyni(a.CevapAnahtari, s.Cevap)).ToList();

                if (enIyi.Count == 1 || (cevabiTutan.Count == 1 && !string.IsNullOrEmpty(s.Cevap)))
                {
                    s.Kaynak = enIyi.Count == 1 ? enIyi[0] : cevabiTutan[0];
                    s.Durum = EslesmeDurumu.Kesin;
                    s.Aciklama = $"{tamOrtusen.Count} aday vardı, {(enIyi.Count == 1 ? "seri kelimeleri" : "cevap anahtarı")} ile ayırt edildi";
                }
                else
                {
                    s.Adaylar = tamOrtusen;
                    s.Durum = EslesmeDurumu.Coklu;
                    s.Aciklama = AdayMetni(tamOrtusen) + " → doğru olanı Soru ID'ye yazın";
                }
                return;
            }

            // Numaralar tuttu ama seri kelimeleri hiçbir klasörle tam örtüşmedi: kullanıcıya adayları göster
            var eksikler = ayniNumara.Select(a => (Soru: a, Eksik: s.GrupKelimeleri.Except(a.GrupKelimeleri).ToList()))
                                     .OrderBy(x => x.Eksik.Count).ToList();
            s.Adaylar = ayniNumara;
            s.Durum = EslesmeDurumu.SeriBelirsiz;
            s.Aciklama = $"Numaralar tutan {ayniNumara.Count} soru var ama açıklamadaki '{string.Join(" ", eksikler[0].Eksik)}' kelimesi klasör adında geçmiyor. "
                       + AdayMetni(ayniNumara) + " → doğruysa Soru ID'ye yazın";
        }

        private static string SeriMetni(HashSet<string> kelimeler) =>
            kelimeler.Count == 0 ? "seri bilgisi olmadan, tek aday" : "seri: " + string.Join(" ", kelimeler);

        private static string AdayMetni(List<DbSoru> adaylar) =>
            $"{adaylar.Count} aday: " + string.Join(" / ", adaylar.Take(4).Select(a => $"{a.SolutionId} [{string.Join(" ", a.GrupKelimeleri)}] cevap {a.CevapAnahtari}"));

        public static bool VarsayilanSecili(IsEmriSatiri s) =>
            s.Kaynak != null &&
            !string.IsNullOrEmpty(s.Kaynak.SourceId) &&
            (s.Durum == EslesmeDurumu.Kesin || s.Durum == EslesmeDurumu.Duzeltme || s.Durum == EslesmeDurumu.ElleGirildi);

        private static bool CevapAyni(string a, string b) =>
            string.Equals((a ?? "").Trim(), (b ?? "").Trim(), StringComparison.OrdinalIgnoreCase);
    }
}

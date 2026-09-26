using System;
using System.Collections.Generic;
using SoruKopyalama.Models;

namespace SoruKopyalama.Services
{
    public class QuestionMatcher
    {
        public class MatchResult
        {
            public bool IsSuccess { get; set; }
            public string SolutionId { get; set; } = "";
            public string SourceId { get; set; } = "";
            public string BulunanKaynakAdi { get; set; } = "";
            public string BulunanSoruNo { get; set; } = "";
            public string BulunanCevapAnahtari { get; set; } = "";
            public string SinavTuru { get; set; } = "TYT";
            public string SafSoruNo { get; set; } = "";
            public string Message { get; set; } = "";
            public int Score { get; set; } = -1;
        }

        /// <summary>
        /// Orijinal Kelime Puanlama Motoru (Birebir korunmuş ve Final/Esen/Limit çoklu format desteği eklenmiştir)
        /// </summary>
        public MatchResult Match(string kisaKod, string kodAcilimi, IDictionary<string, SistemSoruReferans> veritabani, IDictionary<string, string> kodHafizasi, string? bolumKodu = null)
        {
            var result = new MatchResult();

            // Eğer kod açılımı boş, tanımsız veya geçersizse Kısa Koddan ve Bölüm Kodundan otomatik üret
            if (string.IsNullOrWhiteSpace(kodAcilimi) || kodAcilimi.Contains("TANIMSIZ") || kodAcilimi == "Kod Açılımı")
            {
                string decoded = ShortCodeDecoder.Decode(kisaKod, bolumKodu);
                if (!string.IsNullOrEmpty(decoded))
                {
                    kodAcilimi = decoded;
                }
            }

            if (kodAcilimi == "Kod Açılımı" || string.IsNullOrWhiteSpace(kodAcilimi))
            {
                result.Message = "Başlık veya boş satır atlandı.";
                return result;
            }

            var parcalar = kodAcilimi.Split(new string[] { " - " }, StringSplitOptions.RemoveEmptyEntries);

            if (parcalar.Length < 5)
            {
                string decoded = ShortCodeDecoder.Decode(kisaKod, bolumKodu);
                if (!string.IsNullOrEmpty(decoded) && decoded != kodAcilimi)
                {
                    kodAcilimi = decoded;
                    parcalar = kodAcilimi.Split(new string[] { " - " }, StringSplitOptions.RemoveEmptyEntries);
                }

                if (parcalar.Length < 5)
                {
                    result.Message = $"Geçersiz format veya TANIMSIZ satır! (Parça sayısı: {parcalar.Length})";
                    return result;
                }
            }

            // SÖZEL = EŞİT AĞIRLIK ÇEVİRMENİ
            for (int i = 0; i < parcalar.Length; i++)
            {
                if (parcalar[i].ToUpper().Contains("SÖZEL"))
                    parcalar[i] = parcalar[i].ToUpper().Replace("SÖZEL", "EŞİT AĞIRLIK");
            }

            // =================================================================
            // KELİME PUANLAMA MOTORU (ANA BEYİN)
            // =================================================================
            string safSoruNo = parcalar[parcalar.Length - 1].ToUpper().Replace("SORU", "").Replace("-", "").Trim();
            string bransHam = parcalar[parcalar.Length - 2].Trim();
            string brans = ShortCodeDecoder.ResolveBrans(bransHam, bolumKodu).Replace("İ", "i").Replace("I", "ı").ToLower();
            result.SafSoruNo = safSoruNo;

            // DENEME NO BULUCU
            string denemeNo = "";
            for (int i = parcalar.Length - 1; i >= 0; i--)
            {
                if (parcalar[i].ToUpper().Contains("DENEME") && !parcalar[i].ToUpper().Contains("Lİ"))
                {
                    denemeNo = parcalar[i].ToUpper().Replace("DENEME", "").Replace(".", "").Trim();
                    break;
                }
            }

            string yayinHam = parcalar[1].ToUpper().Replace("YAYINLARI", "").Replace("'", "").Replace("’", "").Replace("`", "").Trim().Replace("İ", "i").Replace("I", "ı").ToLower();
            string yayin = yayinHam.Split(new char[] { ' ' }, StringSplitOptions.RemoveEmptyEntries)[0];

            string sinavTuru = "TYT";
            string araSinifSeviyesi = "";
            string temizKodAcilimi = kodAcilimi.ToUpper().Replace(" ", "");

            if (temizKodAcilimi.Contains("AYT")) sinavTuru = "AYT";
            else if (temizKodAcilimi.Contains("9.SINIF") || temizKodAcilimi.Contains("9SINIF")) { sinavTuru = "ARA_SINIF"; araSinifSeviyesi = "9"; }
            else if (temizKodAcilimi.Contains("10.SINIF") || temizKodAcilimi.Contains("10SINIF")) { sinavTuru = "ARA_SINIF"; araSinifSeviyesi = "10"; }
            else if (temizKodAcilimi.Contains("11.SINIF") || temizKodAcilimi.Contains("11SINIF")) { sinavTuru = "ARA_SINIF"; araSinifSeviyesi = "11"; }

            result.SinavTuru = sinavTuru;

            string aranacakDers = brans;
            bool temelOlamaz = false;
            bool aytSosyalSarti = false;

            if (sinavTuru == "TYT")
            {
                if (brans == "türkçe" || brans == "edebiyat") aranacakDers = "türkçe";
                else if (brans == "matematik") aranacakDers = "matematik";
                else if (brans == "sosyal") aranacakDers = "sosyal bilimler";
                else if (brans == "fen") aranacakDers = "fen bilimleri";
            }
            else if (sinavTuru == "AYT")
            {
                if (brans == "türkçe" || brans == "edebiyat") aranacakDers = "edebiyat";
                else if (brans == "matematik") { aranacakDers = "matematik"; temelOlamaz = true; }
                else if (brans == "sosyal") { aranacakDers = "sosyal bilimler"; aytSosyalSarti = true; }
                else if (brans == "fen") aranacakDers = "fen bilimleri";
            }

            string bulunanSolutionId = "";
            string bulunanSourceId = "";
            string bulunanKaynakAdi = "";
            string bulunanSoruNo = "";
            string bulunanCevap = "";
            int enYuksekSkor = -1;

            // Veritabanında Klasik Kelime Araması
            foreach (var item in veritabani)
            {
                string dbKey = item.Key;
                string normKey = dbKey.Replace("-", " ").Replace("  ", " ").Replace("'", "").Replace("’", "").Replace("`", "").Replace("(", "").Replace(")", "").Replace("[", "").Replace("]", "").Replace("İ", "i").Replace("I", "ı").ToLower();

                bool soruNoEslesti = normKey.EndsWith("_" + safSoruNo) || normKey.EndsWith("_" + safSoruNo + ". soru") || normKey.EndsWith("_" + safSoruNo + ".");

                if (soruNoEslesti)
                {
                    bool denemeEslesti = string.IsNullOrEmpty(denemeNo) ||
                        normKey.Contains("sınavı " + denemeNo) || normKey.Contains("sınavı" + denemeNo) || normKey.Contains("sınavı-" + denemeNo) ||
                        normKey.Contains("deneme " + denemeNo) || normKey.Contains("deneme" + denemeNo) || normKey.Contains("deneme-" + denemeNo) ||
                        normKey.Contains("denemesi " + denemeNo) || normKey.Contains("denemesi-" + denemeNo) ||
                        normKey.Contains("d" + denemeNo + " ") || normKey.Contains("d" + denemeNo + "/");

                    bool dersEslesti = normKey.Contains(aranacakDers) ||
                        (aranacakDers == "edebiyat" && (normKey.Contains("türk dili") || normKey.Contains("edebiyatı") || normKey.Contains("edebiyat"))) ||
                        (aranacakDers == "sosyal bilimler" && (normKey.Contains("sosyal") || normKey.Contains("tarih") || normKey.Contains("coğrafya"))) ||
                        (aranacakDers == "türkçe" && (normKey.Contains("türkçe") || normKey.Contains("turkce"))) ||
                        (aranacakDers == "matematik" && (normKey.Contains("matematik") || normKey.Contains("temel matematik"))) ||
                        (aranacakDers == "fen bilimleri" && (normKey.Contains("fen") || normKey.Contains("fizik")));

                    if (normKey.Contains(yayin) && denemeEslesti && dersEslesti)
                    {
                        if (temelOlamaz && normKey.Contains("temel matematik")) continue;
                        if (sinavTuru == "TYT" && brans == "sosyal" && (normKey.Contains("bilimler 2") || normKey.Contains("bilimler2") || normKey.Contains("bilimler-2"))) continue;
                        if (aytSosyalSarti && !(normKey.Contains("bilimler 2") || normKey.Contains("bilimler2") || normKey.Contains("bilimler-2") || normKey.Contains("sosyal 2")))
                        {
                            // AYT Sosyal-1 Edebiyat testinde olabilir, Sosyal-2 ise Sosyal Bilimler-2 testidir
                        }

                        string safKontrol = normKey.Replace("tytayt", "").Replace("tyt ayt", "");
                        if (sinavTuru == "TYT" && (safKontrol.Contains(" ayt ") || safKontrol.Contains("/ayt ") || safKontrol.Contains("ayt "))) continue;
                        if (sinavTuru == "AYT" && (safKontrol.Contains(" tyt ") || safKontrol.Contains("/tyt ") || safKontrol.Contains("tyt "))) continue;

                        if (sinavTuru == "ARA_SINIF")
                        {
                            bool dogruSinif = normKey.Contains(araSinifSeviyesi + ".sınıf") || normKey.Contains(araSinifSeviyesi + ". sınıf") || normKey.Contains(araSinifSeviyesi + " sınıf") || normKey.Contains(araSinifSeviyesi + "sınıf");
                            if (!dogruSinif) continue;
                        }
                        else
                        {
                            if (normKey.Contains("9.sınıf") || normKey.Contains("9. sınıf") || normKey.Contains("9 sınıf") || normKey.Contains("9sınıf") ||
                                normKey.Contains("10.sınıf") || normKey.Contains("10. sınıf") || normKey.Contains("10 sınıf") || normKey.Contains("10sınıf") ||
                                normKey.Contains("11.sınıf") || normKey.Contains("11. sınıf") || normKey.Contains("11 sınıf") || normKey.Contains("11sınıf"))
                            { continue; }
                        }

                        int anlikSkor = 0;
                        foreach (string parca in parcalar)
                        {
                            var kelimeler = parca.Replace("'", "").Replace("’", "").Replace("`", "").Replace("(", "").Replace(")", "").Replace("[", "").Replace("]", "").Replace(".", "").Replace("İ", "i").Replace("I", "ı").ToLower().Split(new char[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);

                            foreach (string kelime in kelimeler)
                            {
                                if (kelime.Length > 2 && kelime != "tyt" && kelime != "ayt" && kelime != "deneme" && kelime != "soru")
                                {
                                    if (normKey.Contains(kelime)) { anlikSkor++; }
                                }
                            }
                        }

                        if (anlikSkor > enYuksekSkor)
                        {
                            enYuksekSkor = anlikSkor;
                            bulunanSolutionId = item.Value.SolutionId;
                            bulunanSourceId = item.Value.SourceId;
                            bulunanKaynakAdi = item.Value.KaynakAdi;
                            bulunanSoruNo = item.Value.SoruNo;
                            bulunanCevap = item.Value.CevapAnahtari;
                        }
                    }
                }
            }

            // =================================================================
            // YEDEK MOTOR: HAFIZA KONTROLÜ
            // =================================================================
            if (string.IsNullOrEmpty(bulunanSolutionId))
            {
                if (!string.IsNullOrEmpty(kisaKod) && kodHafizasi.ContainsKey(kisaKod))
                {
                    var hafizaVerisi = kodHafizasi[kisaKod].Split('|');
                    bulunanSourceId = hafizaVerisi[0];
                    bulunanSolutionId = hafizaVerisi.Length > 1 ? hafizaVerisi[1] : "";
                    result.IsSuccess = true;
                    result.SolutionId = bulunanSolutionId;
                    result.SourceId = bulunanSourceId;
                    result.Score = 999;
                    result.Message = $"Kelimelerle bulunamadı, [{kisaKod}] hafızasından kurtarıldı!";
                    return result;
                }
            }

            if (!string.IsNullOrEmpty(bulunanSolutionId))
            {
                result.IsSuccess = true;
                result.SolutionId = bulunanSolutionId;
                result.SourceId = bulunanSourceId;
                result.BulunanKaynakAdi = bulunanKaynakAdi;
                result.BulunanSoruNo = bulunanSoruNo;
                result.BulunanCevapAnahtari = bulunanCevap;
                result.Score = enYuksekSkor;
                result.Message = $"[{sinavTuru}] Eşleşme Sağlandı -> Solution ID: {bulunanSolutionId}";
                return result;
            }

            result.IsSuccess = false;
            result.Message = $"Bu soru veritabanında veya hafızada eşleşmedi! (Kod: {kisaKod})";
            return result;
        }
    }
}

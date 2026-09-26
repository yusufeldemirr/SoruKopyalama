using System;
using System.Text.RegularExpressions;

namespace SoruKopyalama.Services
{
    public static class ShortCodeDecoder
    {
        public static string Decode(string kisaKod, string? bolumKodu = null)
        {
            if (string.IsNullOrWhiteSpace(kisaKod)) return "";

            string code = kisaKod.Trim().ToUpper();
            if (code.StartsWith("TANIMSIZ:")) code = code.Replace("TANIMSIZ:", "").Trim();

            // 1. Sezon (56 -> 2025-2026)
            string sezon = "2025-2026";
            if (code.StartsWith("56")) { sezon = "2025-2026"; code = code.Substring(2); }
            else if (code.StartsWith("45")) { sezon = "2024-2025"; code = code.Substring(2); }
            else if (code.StartsWith("34")) { sezon = "2023-2024"; code = code.Substring(2); }

            // =========================================================================
            // FİNAL KODLARI (Örn: FNOKFDDD1AT4S24, FNOKFDD2AT1S1, FNOKFDD3TT3S8, FNKD1AT1S1)
            // =========================================================================
            if (code.StartsWith("FN") || code.StartsWith("FINAL"))
            {
                string yayin = "Final Yayınları";
                string seri = "Finale Doğru";
                if (code.Contains("OK")) seri = "Finale Doğru Okul Deneme Sınavı";
                else if (code.Contains("KURS") || code.Contains("KR")) seri = "Finale Doğru Kurs Deneme Sınavı";

                // Regex: (FNOK|FN)(FDDD|FDD|FD|D|KR|K)?(\d+)?([AT])(T[1-4]|TT[1-4])S?(\d+)$
                var fMatch = Regex.Match(code, @"^(?:FN[A-Z]*?)(?:FDDD|FDD|FD|D)?(\d+)?([AT])(T[1-4]|TT[1-4])S?(\d+)$", RegexOptions.IgnoreCase);
                if (fMatch.Success)
                {
                    string denemeNo = !string.IsNullOrEmpty(fMatch.Groups[1].Value) ? fMatch.Groups[1].Value : "1";
                    string aytTyt = fMatch.Groups[2].Value.ToUpper() == "A" ? "AYT" : "TYT";
                    string testKod = fMatch.Groups[3].Value.ToUpper();
                    string soruNo = fMatch.Groups[4].Value;

                    string brans = ResolveBrans(testKod, bolumKodu);
                    if (aytTyt == "AYT" && (testKod == "T1" || testKod == "TT1" || brans == "Türkçe"))
                    {
                        brans = "Edebiyat";
                    }

                    return $"{sezon} - {yayin} - {aytTyt} - {seri} - {denemeNo}. Deneme - {brans} - Soru {soruNo}";
                }
            }

            // =========================================================================
            // LİMİT & ESEN & DİĞERLERİ
            // =========================================================================
            string genYayin = "Limit Yayınları";
            string genSeri = "";

            if (code.StartsWith("LDTYT") || code.StartsWith("LDAYT") || code.StartsWith("LD"))
            {
                genYayin = "Limit Yayınları";
                genSeri = "Dublör";
                code = code.StartsWith("LD") ? code.Substring(2) : code;
            }
            else if (code.StartsWith("LATYT") || code.StartsWith("LAAYT") || code.StartsWith("LA"))
            {
                genYayin = "Limit Yayınları";
                genSeri = "Aktör";
                code = code.StartsWith("LA") ? code.Substring(2) : code;
            }
            else if (code.StartsWith("LP5") || code.StartsWith("L5"))
            {
                genYayin = "Limit Yayınları";
                genSeri = "5'li Deneme";
                code = code.StartsWith("LP5") ? code.Substring(3) : code.Substring(2);
            }
            else if (code.StartsWith("LEK"))
            {
                genYayin = "Limit Yayınları";
                genSeri = "Kurumsal";
                code = code.Substring(3);
            }
            else if (code.StartsWith("EB"))
            {
                genYayin = "Esen Yayınları";
                genSeri = "Başarı";
                code = code.Substring(2);
            }
            else if (code.StartsWith("EM"))
            {
                genYayin = "Esen Yayınları";
                genSeri = "Matematik";
                code = code.Substring(2);
            }
            else if (code.StartsWith("EYA"))
            {
                genYayin = "Esen Yayınları";
                genSeri = "AYT";
                code = code.Substring(3);
            }
            else if (code.StartsWith("EPT"))
            {
                genYayin = "Esen Yayınları";
                genSeri = "TYT";
                code = code.Substring(3);
            }
            else if (code.StartsWith("EC"))
            {
                genYayin = "Esen Yayınları";
                genSeri = "ÜTS";
                code = code.Substring(2);
            }

            // Sınav Türü
            string genSinavTuru = "TYT";
            if (code.StartsWith("TYT")) { genSinavTuru = "TYT"; code = code.Substring(3); }
            else if (code.StartsWith("AYT")) { genSinavTuru = "AYT"; code = code.Substring(3); }
            else if (code.StartsWith("11S") || code.StartsWith("11ÜTS")) { genSinavTuru = "11.SINIF"; code = Regex.Replace(code, @"^11(S|ÜTS)", ""); }
            else if (code.StartsWith("10S") || code.StartsWith("10ÜTS")) { genSinavTuru = "10.SINIF"; code = Regex.Replace(code, @"^10(S|ÜTS)", ""); }
            else if (code.StartsWith("9S") || code.StartsWith("9ÜTS")) { genSinavTuru = "9.SINIF"; code = Regex.Replace(code, @"^9(S|ÜTS)", ""); }

            // Deneme No
            string genDenemeNo = "1";
            var denemeMatch = Regex.Match(code, @"^D(\d+)");
            if (denemeMatch.Success)
            {
                genDenemeNo = denemeMatch.Groups[1].Value;
                code = code.Substring(denemeMatch.Length);
            }
            else
            {
                var numMatch = Regex.Match(code, @"^(\d+)");
                if (numMatch.Success)
                {
                    genDenemeNo = numMatch.Groups[1].Value;
                    code = code.Substring(numMatch.Length);
                }
            }

            // Soru No ve Branş
            string genSoruNo = "1";
            string bransKodu = code;

            var soruMatch = Regex.Match(code, @"S(\d+)$");
            if (soruMatch.Success)
            {
                genSoruNo = soruMatch.Groups[1].Value;
                bransKodu = code.Substring(0, code.Length - soruMatch.Length);
            }
            else
            {
                var endNumMatch = Regex.Match(code, @"(\d+)$");
                if (endNumMatch.Success)
                {
                    genSoruNo = endNumMatch.Groups[1].Value;
                    bransKodu = code.Substring(0, code.Length - endNumMatch.Length);
                }
            }

            string genBrans = ResolveBrans(bransKodu, bolumKodu);

            string acilim = $"{sezon} - {genYayin} - {genSinavTuru}";
            if (!string.IsNullOrEmpty(genSeri)) acilim += $" - {genSeri}";
            acilim += $" - {genDenemeNo}. Deneme - {genBrans} - Soru {genSoruNo}";

            return acilim;
        }

        public static string ResolveBrans(string bransKodu, string? bolumKodu = null)
        {
            if (!string.IsNullOrWhiteSpace(bolumKodu))
            {
                string bNorm = bolumKodu.Trim().ToUpper();
                if (bNorm.Contains("TÜRK") || bNorm.Contains("TURK") || bNorm.Contains("EDEB") || bNorm == "T1") return "Türkçe";
                if (bNorm.Contains("SOS") || bNorm.Contains("TAR") || bNorm.Contains("COĞ") || bNorm.Contains("COG") || bNorm.Contains("FEL") || bNorm.Contains("DİN") || bNorm == "T2") return "Sosyal";
                if (bNorm.Contains("MAT") || bNorm.Contains("GEO") || bNorm == "T3") return "Matematik";
                if (bNorm.Contains("FEN") || bNorm.Contains("FİZ") || bNorm.Contains("FIZ") || bNorm.Contains("KİM") || bNorm.Contains("KIM") || bNorm.Contains("BİY") || bNorm.Contains("BIY") || bNorm == "T4") return "Fen";
            }

            string bk = (bransKodu ?? "").Trim().ToUpper();

            if (bk == "T1" || bk == "AT1" || bk == "TT1" || bk.StartsWith("T1") || bk.StartsWith("AT1") || bk.StartsWith("TT1")) return "Türkçe";
            if (bk == "T2" || bk == "AT2" || bk == "TT2" || bk.StartsWith("T2") || bk.StartsWith("AT2") || bk.StartsWith("TT2")) return "Sosyal";
            if (bk == "T3" || bk == "AT3" || bk == "TT3" || bk.StartsWith("T3") || bk.StartsWith("AT3") || bk.StartsWith("TT3")) return "Matematik";
            if (bk == "T4" || bk == "AT4" || bk == "TT4" || bk.StartsWith("T4") || bk.StartsWith("AT4") || bk.StartsWith("TT4")) return "Fen";

            if (bk.Contains("EDB") || bk.Contains("ED") || bk == "AT") return "Türkçe";
            if (bk.StartsWith("M") || bk == "TM" || bk == "MS" || bk == "MAT" || bk == "AM") return "Matematik";
            if (bk.StartsWith("S") || bk == "SS" || bk == "SB" || bk == "SOSYAL" || bk == "AS") return "Sosyal";
            if (bk.StartsWith("F") || bk == "FS" || bk == "FB" || bk == "FEN" || bk == "AF") return "Fen";
            if (bk.StartsWith("T") || bk == "TT" || bk == "TS" || bk == "TURKCE") return "Türkçe";

            return "Türkçe";
        }

        public static string GetBranş(string kisaKod, string? bolumKodu = null)
        {
            if (!string.IsNullOrWhiteSpace(bolumKodu))
            {
                string b = ResolveBrans("", bolumKodu);
                if (!string.IsNullOrEmpty(b)) return b;
            }

            if (string.IsNullOrWhiteSpace(kisaKod)) return "Türkçe";

            string code = kisaKod.Trim().ToUpper();
            if (code.StartsWith("TANIMSIZ:")) code = code.Replace("TANIMSIZ:", "").Trim();

            var match = Regex.Match(code, @"(?:[A-Z0-9]+)?(?:[AT])?(T[1-4]|TT[1-4])S\d+", RegexOptions.IgnoreCase);
            if (match.Success)
            {
                string tKod = match.Groups[1].Value.ToUpper();
                if (tKod == "T1" || tKod == "TT1") return "Türkçe";
                if (tKod == "T2" || tKod == "TT2") return "Sosyal";
                if (tKod == "T3" || tKod == "TT3") return "Matematik";
                if (tKod == "T4" || tKod == "TT4") return "Fen";
            }

            string decoded = Decode(kisaKod, bolumKodu);
            var parts = decoded.Split(new string[] { " - " }, StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length >= 2)
            {
                string pBrans = parts[parts.Length - 2].Trim();
                if (pBrans.Equals("Edebiyat", StringComparison.OrdinalIgnoreCase)) return "Türkçe";
                return pBrans;
            }

            return "Türkçe";
        }
    }
}

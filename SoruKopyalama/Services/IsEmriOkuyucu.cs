using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;
using System.Text;
using ExcelDataReader;
using OfficeOpenXml;
using SoruKopyalama.Models;

namespace SoruKopyalama.Services
{
    /// <summary>
    /// İş emri dosyasını (.csv, .xlsx, .xls) okur.
    /// Sütunlar: Kod | Kod Açılımı | Master (bölüm kodu) | SoruNo (hedef) | Cevap Anahtarı
    /// </summary>
    public static class IsEmriOkuyucu
    {
        public static List<IsEmriSatiri> Oku(string yol)
        {
            var satirlar = HamSatirlariOku(yol);
            var sonuc = new List<IsEmriSatiri>();
            int sira = 1;

            foreach (var h in satirlar)
            {
                string Al(int i) => i < h.Length ? (h[i] ?? "").Trim() : "";

                string kod = Al(0), acilim = Al(1), bolum = Al(2), no = Al(3), cevap = Al(4);

                if (kod.Equals("Kod", StringComparison.OrdinalIgnoreCase) || kod.Equals("Kodu", StringComparison.OrdinalIgnoreCase) ||
                    kod.Equals("KISA KOD", StringComparison.OrdinalIgnoreCase) || acilim.Equals("Kod Açılımı", StringComparison.OrdinalIgnoreCase))
                    continue;
                if (kod == "" && acilim == "" && bolum == "" && no == "") continue;

                sonuc.Add(new IsEmriSatiri
                {
                    SiraNo = sira++,
                    KisaKod = kod,
                    KodAcilimi = acilim,
                    BolumKodu = bolum,
                    HedefSoruNo = no,
                    Cevap = cevap.ToUpperInvariant()
                });
            }

            return sonuc;
        }

        private static List<string[]> HamSatirlariOku(string yol)
        {
            string uzanti = Path.GetExtension(yol).ToLowerInvariant();

            if (uzanti == ".csv" || uzanti == ".txt")
                return CsvOku(yol);

            if (uzanti == ".xlsx")
            {
                using var package = new ExcelPackage(new FileInfo(yol));
                var ws = package.Workbook.Worksheets[0];
                var liste = new List<string[]>();
                if (ws.Dimension == null) return liste;
                for (int r = 1; r <= ws.Dimension.Rows; r++)
                {
                    var satir = new string[5];
                    for (int c = 1; c <= 5; c++) satir[c - 1] = ws.Cells[r, c].Text;
                    liste.Add(satir);
                }
                return liste;
            }

            // .xls
            using var stream = File.Open(yol, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
            using var reader = ExcelReaderFactory.CreateReader(stream);
            var table = reader.AsDataSet().Tables[0];
            return table.Rows.Cast<DataRow>()
                .Select(r => Enumerable.Range(0, Math.Min(5, table.Columns.Count)).Select(i => r[i]?.ToString() ?? "").ToArray())
                .ToList();
        }

        private static List<string[]> CsvOku(string yol)
        {
            var bytes = File.ReadAllBytes(yol);
            string metin;
            try
            {
                metin = new UTF8Encoding(false, true).GetString(bytes).TrimStart('﻿');
            }
            catch (DecoderFallbackException)
            {
                // UTF-8 değilse Excel'in Türkçe Windows kodlaması
                metin = Encoding.GetEncoding(1254).GetString(bytes);
            }

            var satirlar = metin.Split(new[] { "\r\n", "\n" }, StringSplitOptions.None);
            string ilk = satirlar.FirstOrDefault(s => s.Trim() != "") ?? "";
            char ayrac = ilk.Count(c => c == ';') >= ilk.Count(c => c == ',') ? ';' : ',';

            return satirlar.Where(s => s.Trim() != "")
                           .Select(s => s.Split(ayrac).Select(x => x.Trim().Trim('"')).ToArray())
                           .ToList();
        }
    }
}

using System;
using System.Collections.Generic;
using System.IO;

namespace SoruKopyalama.Services
{
    /// <summary>
    /// Kullanıcının "bu kodun gerçek sorusu budur" dediği kalıcı düzeltmeler.
    /// Eşleştirmede her şeyden önce buraya bakılır.
    /// Dosya formatı (satır başına): Panel|KisaKod|SolutionId|SourceId
    /// </summary>
    public class DuzeltmeDeposu
    {
        private readonly string _dosyaYolu = VeriYolu.Dosya("duzeltmeler.txt");
        private readonly Dictionary<string, (string SolutionId, string SourceId)> _kayitlar = new(StringComparer.OrdinalIgnoreCase);

        public int Sayi => _kayitlar.Count;

        private static string Anahtar(string panel, string kod) => $"{panel.Trim()}|{kod.Trim()}";

        public void Yukle()
        {
            _kayitlar.Clear();
            if (!File.Exists(_dosyaYolu)) return;

            foreach (var satir in File.ReadAllLines(_dosyaYolu))
            {
                var p = satir.Split('|');
                if (p.Length < 4 || string.IsNullOrWhiteSpace(p[1]) || string.IsNullOrWhiteSpace(p[2])) continue;
                // Sonraki satırlar öncekileri ezer: en son düzeltme geçerlidir
                _kayitlar[Anahtar(p[0], p[1])] = (p[2].Trim(), p[3].Trim());
            }
        }

        public bool TryGet(string panel, string kod, out string solutionId, out string sourceId)
        {
            if (_kayitlar.TryGetValue(Anahtar(panel, kod), out var k))
            {
                solutionId = k.SolutionId;
                sourceId = k.SourceId;
                return true;
            }
            solutionId = sourceId = "";
            return false;
        }

        public void Kaydet(string panel, string kod, string solutionId, string sourceId)
        {
            if (string.IsNullOrWhiteSpace(kod) || string.IsNullOrWhiteSpace(solutionId)) return;
            _kayitlar[Anahtar(panel, kod)] = (solutionId.Trim(), sourceId.Trim());
            File.AppendAllText(_dosyaYolu, $"{panel.Trim()}|{kod.Trim()}|{solutionId.Trim()}|{sourceId.Trim()}{Environment.NewLine}");
        }
    }
}

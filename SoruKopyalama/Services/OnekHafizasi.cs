using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace SoruKopyalama.Services
{
    /// <summary>
    /// Kısa kod önekinin (FNOKFD, LEK, FK...) hangi seriyi gösterdiğini kesin eşleşmelerden öğrenir.
    /// Açıklaması eksik veya bozuk gelen satırlarda bu bilgi kullanılır.
    /// Dosya formatı: Onek|kelime1 kelime2 ...
    /// </summary>
    public class OnekHafizasi
    {
        private readonly string _dosyaYolu = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "kod_onekleri.txt");
        private readonly Dictionary<string, HashSet<string>> _kayitlar = new(StringComparer.OrdinalIgnoreCase);

        public int Sayi => _kayitlar.Count;

        public void Yukle()
        {
            _kayitlar.Clear();
            if (!File.Exists(_dosyaYolu)) return;
            foreach (var satir in File.ReadAllLines(_dosyaYolu))
            {
                var p = satir.Split('|');
                if (p.Length < 2 || p[0].Trim() == "") continue;
                _kayitlar[p[0].Trim()] = new HashSet<string>(p[1].Split(' ', StringSplitOptions.RemoveEmptyEntries), StringComparer.Ordinal);
            }
        }

        public HashSet<string>? Getir(string onek)
        {
            return _kayitlar.TryGetValue(onek, out var k) ? k : null;
        }

        public void Ogren(string onek, HashSet<string> kelimeler)
        {
            if (string.IsNullOrWhiteSpace(onek) || kelimeler.Count == 0) return;
            if (_kayitlar.TryGetValue(onek, out var mevcut) && mevcut.SetEquals(kelimeler)) return;
            _kayitlar[onek] = new HashSet<string>(kelimeler, StringComparer.Ordinal);
            try
            {
                File.AppendAllText(_dosyaYolu, $"{onek}|{string.Join(" ", kelimeler.OrderBy(k => k))}{Environment.NewLine}");
            }
            catch { }
        }
    }
}

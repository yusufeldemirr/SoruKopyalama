using System;
using System.IO;

namespace SoruKopyalama.Services
{
    /// <summary>
    /// Veri klasörünü bulur: veritabanı Excel'leri, düzeltmeler, öğrenilen önekler ve panel ayarları burada durur.
    /// Önce exe'nin yanına, sonra üst klasörlere bakar; "Veritabani" klasörünü içeren ilk klasör veri köküdür.
    /// Böylece proje git'ten klonlandığında (Veritabani proje kökünde) bin klasörüne hiçbir şey kopyalamak gerekmez.
    /// </summary>
    public static class VeriYolu
    {
        private static string? _kok;

        public static string Kok
        {
            get
            {
                if (_kok != null) return _kok;

                var dir = new DirectoryInfo(AppDomain.CurrentDomain.BaseDirectory);
                for (int i = 0; i < 6 && dir != null; i++, dir = dir.Parent)
                {
                    if (Directory.Exists(Path.Combine(dir.FullName, "Veritabani")))
                    {
                        _kok = dir.FullName;
                        return _kok;
                    }
                }

                _kok = AppDomain.CurrentDomain.BaseDirectory;
                return _kok;
            }
        }

        public static string Dosya(string ad) => Path.Combine(Kok, ad);
        public static string Veritabani => Path.Combine(Kok, "Veritabani");
    }
}

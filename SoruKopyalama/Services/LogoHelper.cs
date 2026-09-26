using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.IO;

namespace SoruKopyalama.Services
{
    public static class LogoHelper
    {
        private static string LogoPath => Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Resources", "limit_bird_logo.png");

        public static Image GetLimitLogoImage(int width = 48, int height = 48)
        {
            try
            {
                string path = LogoPath;
                if (!File.Exists(path))
                {
                    path = Path.Combine(Directory.GetCurrentDirectory(), "Resources", "limit_bird_logo.png");
                }

                if (File.Exists(path))
                {
                    using var original = Image.FromFile(path);
                    var bmp = new Bitmap(width, height, PixelFormat.Format32bppArgb);
                    using var g = Graphics.FromImage(bmp);
                    g.SmoothingMode = SmoothingMode.AntiAlias;
                    g.InterpolationMode = InterpolationMode.HighQualityBicubic;
                    g.PixelOffsetMode = PixelOffsetMode.HighQuality;
                    
                    // Şık yuvarlak köşeli (rounded rectangle) maske ile kırpma
                    using var gp = new GraphicsPath();
                    int radius = Math.Max(4, width / 6);
                    gp.AddArc(0, 0, radius, radius, 180, 90);
                    gp.AddArc(width - radius, 0, radius, radius, 270, 90);
                    gp.AddArc(width - radius, height - radius, radius, radius, 0, 90);
                    gp.AddArc(0, height - radius, radius, radius, 90, 90);
                    gp.CloseFigure();

                    g.SetClip(gp);
                    g.DrawImage(original, 0, 0, width, height);

                    return bmp;
                }
            }
            catch { }

            return new Bitmap(width, height);
        }

        public static Icon CreateLimitIcon()
        {
            try
            {
                using var bmp = (Bitmap)GetLimitLogoImage(64, 64);
                IntPtr hIcon = bmp.GetHicon();
                return Icon.FromHandle(hIcon);
            }
            catch
            {
                return SystemIcons.Application;
            }
        }
    }
}

using System;
using System.IO;
using System.Windows.Media.Imaging;

namespace EReceiptApp.Services
{
    public static class LogoService
    {
        private static readonly string LogoPath = Path.Combine(
            Environment.GetFolderPath(
                Environment.SpecialFolder.ApplicationData),
            "EReceiptApp", "logo.png");

        public static bool HasLogo => File.Exists(LogoPath);

        public static BitmapImage? LoadLogo()
        {
            if (!HasLogo) return null;

            try
            {
                var bitmap = new BitmapImage();
                bitmap.BeginInit();
                bitmap.UriSource = new Uri(LogoPath);
                bitmap.CacheOption = BitmapCacheOption.OnLoad;
                bitmap.DecodePixelWidth = 120;
                bitmap.EndInit();
                bitmap.Freeze();
                return bitmap;
            }
            catch { return null; }
        }

        public static string LogoFilePath => LogoPath;
    }
}
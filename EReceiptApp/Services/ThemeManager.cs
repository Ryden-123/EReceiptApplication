using System.Windows;
using System.Windows.Media;

namespace EReceiptApp.Services
{
    public enum AppTheme { Light, Dark }

    public static class ThemeManager
    {
        public static AppTheme CurrentTheme { get; private set; }
            = AppTheme.Light;
        public static Color CurrentAccent { get; private set; }
            = Color.FromRgb(124, 111, 205);

        public static void Initialize()
        {
            ApplyTheme(AppTheme.Light, CurrentAccent);
        }

        public static void ApplyTheme(AppTheme theme, Color accent)
        {
            CurrentTheme = theme;
            CurrentAccent = accent;

            var res = Application.Current.Resources;

            if (theme == AppTheme.Dark)
            {
                res["AppBackground"] = Brush(30, 26, 50);
                res["AppSurface"] = Brush(40, 36, 65);
                res["AppSurfaceAlt"] = Brush(48, 44, 75);
                res["AppText"] = Brush(235, 232, 250);
                res["AppTextMuted"] = Brush(160, 152, 200);
                res["AppBorder"] = Brush(65, 58, 98);
                res["AppSidebar"] = Brush(22, 18, 40);
                res["AppSidebarText"] = Brush(185, 178, 220);
                res["AppInputBackground"] = Brush(45, 40, 70);
                res["AppCardShadow"] = Brush(20, 16, 40);
            }
            else
            {
                res["AppBackground"] = Brush(249, 247, 255);
                res["AppSurface"] = Brush(255, 255, 255);
                res["AppSurfaceAlt"] = Brush(244, 241, 255);
                res["AppText"] = Brush(45, 37, 69);
                res["AppTextMuted"] = Brush(139, 129, 184);
                res["AppBorder"] = Brush(232, 228, 248);
                res["AppSidebar"] = Brush(45, 37, 69);
                res["AppSidebarText"] = Brush(185, 178, 220);
                res["AppInputBackground"] = Brush(255, 255, 255);
                res["AppCardShadow"] = Brush(200, 195, 230);
            }

            // Accent
            res["AppAccent"] = new SolidColorBrush(accent);
            res["AppAccentDark"] = new SolidColorBrush(
                DarkenColor(accent, 0.12f));
            res["AppAccentLight"] = new SolidColorBrush(
                LightenColor(accent, 0.88f));
            res["AppAccentText"] = Brush(255, 255, 255);

            // Semantic colors — soft pastels
            res["AppSuccess"] = Brush(82, 164, 122);
            res["AppSuccessLight"] = Brush(232, 245, 238);
            res["AppDanger"] = Brush(198, 80, 80);
            res["AppDangerLight"] = Brush(255, 240, 240);
            res["AppWarning"] = Brush(210, 155, 50);
            res["AppWarningLight"] = Brush(255, 248, 230);
        }

        private static SolidColorBrush Brush(byte r, byte g, byte b)
            => new SolidColorBrush(Color.FromRgb(r, g, b));

        private static Color DarkenColor(Color c, float f)
            => Color.FromRgb(
                (byte)(c.R * (1 - f)),
                (byte)(c.G * (1 - f)),
                (byte)(c.B * (1 - f)));

        private static Color LightenColor(Color c, float f)
            => Color.FromRgb(
                (byte)(c.R + (255 - c.R) * f),
                (byte)(c.G + (255 - c.G) * f),
                (byte)(c.B + (255 - c.B) * f));
    }
}
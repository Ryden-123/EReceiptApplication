using System;
using System.IO;
using System.Text.Json;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using EReceiptApp.Services;
using System.Windows.Media.Imaging;

namespace EReceiptApp.Views.Pages
{
    public partial class SettingsPage : Page
    {
        private static readonly string LogoPath = Path.Combine(
            Environment.GetFolderPath(
                Environment.SpecialFolder.ApplicationData),
            "EReceiptApp", "logo.png");
        private bool _isLoaded = false;

        public SettingsPage()
        {
            InitializeComponent();
            Loaded += SettingsPage_Loaded;
        }

        // Call this in SettingsPage_Loaded
        private void LoadLogoPreview()
        {
            if (!File.Exists(LogoPath)) return;

            try
            {
                var bitmap = new BitmapImage();
                bitmap.BeginInit();
                bitmap.UriSource = new Uri(LogoPath);
                bitmap.CacheOption = BitmapCacheOption.OnLoad;
                bitmap.EndInit();

                ImgLogoPreview.Source = bitmap;
                ImgLogoPreview.Visibility = Visibility.Visible;
                TxtLogoPlaceholder.Visibility = Visibility.Collapsed;
                BtnRemoveLogo.Visibility = Visibility.Visible;
            }
            catch { }
        }

        private void UploadLogo_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new Microsoft.Win32.OpenFileDialog
            {
                Title = "Choose a logo image",
                Filter = "Image Files|*.png;*.jpg;*.jpeg;*.bmp"
            };

            if (dialog.ShowDialog() != true) return;

            try
            {
                var dir = Path.GetDirectoryName(LogoPath)!;
                if (!Directory.Exists(dir))
                    Directory.CreateDirectory(dir);

                // Copy and resize to a standard size
                var source = new BitmapImage(
                    new Uri(dialog.FileName));

                // Save as PNG to AppData
                var encoder = new PngBitmapEncoder();
                encoder.Frames.Add(BitmapFrame.Create(source));

                using var stream = File.Create(LogoPath);
                encoder.Save(stream);

                LoadLogoPreview();

                MessageBox.Show(
                    "Logo uploaded successfully!",
                    "Logo Saved",
                    MessageBoxButton.OK,
                    MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    $"Could not upload logo: {ex.Message}",
                    "Error",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
        }

        private void RemoveLogo_Click(object sender, RoutedEventArgs e)
        {
            var result = MessageBox.Show(
                "Remove the organization logo?",
                "Remove Logo",
                MessageBoxButton.YesNo,
                MessageBoxImage.Question);

            if (result != MessageBoxResult.Yes) return;

            try
            {
                if (File.Exists(LogoPath))
                    File.Delete(LogoPath);

                ImgLogoPreview.Source = null;
                ImgLogoPreview.Visibility = Visibility.Collapsed;
                TxtLogoPlaceholder.Visibility = Visibility.Visible;
                BtnRemoveLogo.Visibility = Visibility.Collapsed;
            }
            catch { }
        }

        private void SettingsPage_Loaded(object sender, RoutedEventArgs e)
        {
            RadioLight.Checked -= Theme_Changed;
            RadioDark.Checked -= Theme_Changed;

            RadioLight.IsChecked =
                ThemeManager.CurrentTheme == AppTheme.Light;
            RadioDark.IsChecked =
                ThemeManager.CurrentTheme == AppTheme.Dark;

            RadioLight.Checked += Theme_Changed;
            RadioDark.Checked += Theme_Changed;

            RefreshAccentCheckmarks(ThemeManager.CurrentAccent);
            LoadEmailSettings();  // ← called here instead

            LoadLogoPreview();
            _isLoaded = true;
        }

        private void Theme_Changed(object sender, RoutedEventArgs e)
        {
            if (!_isLoaded) return;

            var theme = RadioDark.IsChecked == true
                ? AppTheme.Dark
                : AppTheme.Light;

            ThemeManager.ApplyTheme(theme, ThemeManager.CurrentAccent);
        }

        // Hides all checkmarks then shows the correct one
        private void RefreshAccentCheckmarks(Color accent)
        {
            CheckPurple.Visibility = Visibility.Collapsed;
            CheckBlue.Visibility = Visibility.Collapsed;
            CheckGreen.Visibility = Visibility.Collapsed;
            CheckRed.Visibility = Visibility.Collapsed;
            CheckTeal.Visibility = Visibility.Collapsed;

            if (accent == Color.FromRgb(92, 74, 187)) CheckPurple.Visibility = Visibility.Visible;
            else if (accent == Color.FromRgb(25, 118, 210)) CheckBlue.Visibility = Visibility.Visible;
            else if (accent == Color.FromRgb(46, 125, 50)) CheckGreen.Visibility = Visibility.Visible;
            else if (accent == Color.FromRgb(198, 40, 40)) CheckRed.Visibility = Visibility.Visible;
            else if (accent == Color.FromRgb(0, 105, 92)) CheckTeal.Visibility = Visibility.Visible;
        }

        private void AccentPurple_Click(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            var color = Color.FromRgb(92, 74, 187);
            ThemeManager.ApplyTheme(ThemeManager.CurrentTheme, color);
            RefreshAccentCheckmarks(color);
        }

        private void AccentBlue_Click(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            var color = Color.FromRgb(25, 118, 210);
            ThemeManager.ApplyTheme(ThemeManager.CurrentTheme, color);
            RefreshAccentCheckmarks(color);
        }

        private void AccentGreen_Click(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            var color = Color.FromRgb(46, 125, 50);
            ThemeManager.ApplyTheme(ThemeManager.CurrentTheme, color);
            RefreshAccentCheckmarks(color);
        }

        private void AccentRed_Click(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            var color = Color.FromRgb(198, 40, 40);
            ThemeManager.ApplyTheme(ThemeManager.CurrentTheme, color);
            RefreshAccentCheckmarks(color);
        }

        private void AccentTeal_Click(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            var color = Color.FromRgb(0, 105, 92);
            ThemeManager.ApplyTheme(ThemeManager.CurrentTheme, color);
            RefreshAccentCheckmarks(color);
        }

        // ── Email settings ────────────────────────────────────────────────
        private static readonly string SettingsPath = Path.Combine(
            Environment.GetFolderPath(
                Environment.SpecialFolder.ApplicationData),
            "EReceiptApp", "settings.json");

        protected override void OnInitialized(EventArgs e)
        {
            base.OnInitialized(e);
            LoadEmailSettings();
        }

        private void LoadEmailSettings()
        {
            try
            {
                if (!File.Exists(SettingsPath)) return;

                var json = File.ReadAllText(SettingsPath);
                var settings = JsonSerializer
                    .Deserialize<AppSettings>(json);

                if (settings == null) return;

                TxtGmail.Text = settings.GmailAddress ?? "";
                TxtAppPassword.Password = settings.AppPassword ?? "";
            }
            catch { }
        }

        private void SaveEmailSettings_Click(
            object sender, RoutedEventArgs e)
        {
            try
            {
                var dir = Path.GetDirectoryName(SettingsPath)!;
                if (!Directory.Exists(dir))
                    Directory.CreateDirectory(dir);

                var settings = new AppSettings
                {
                    GmailAddress = TxtGmail.Text.Trim(),
                    AppPassword = TxtAppPassword.Password.Trim()
                };

                File.WriteAllText(SettingsPath,
                    JsonSerializer.Serialize(settings));

                TxtEmailSaved.Visibility = Visibility.Visible;
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    $"Could not save settings: {ex.Message}",
                    "Error", MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
        }

        // Settings model
        public class AppSettings
        {
            public string GmailAddress { get; set; } = "";
            public string AppPassword { get; set; } = "";

            // Remember last used fields
            public string LastOrganization { get; set; } = "";
            public string LastClubName { get; set; } = "";
            public string LastCashier { get; set; } = "";
            public string LastAcademicYear { get; set; } = "";

            // Sequential receipt counters
            public int StandardCounter { get; set; } = 0;
            public int MembershipCounter { get; set; } = 0;
        }

        private void BackupDatabase_Click(
    object sender, RoutedEventArgs e)
        {
            var dialog = new Microsoft.Win32.SaveFileDialog
            {
                FileName = $"receipts_backup_{DateTime.Now:yyyyMMdd}",
                DefaultExt = ".db",
                Filter = "Database File|*.db"
            };

            if (dialog.ShowDialog() != true) return;

            try
            {
                string sourcePath = "receipts.db";

                if (!File.Exists(sourcePath))
                {
                    MessageBox.Show(
                        "Database file not found. " +
                        "Create a receipt first.",
                        "No Database",
                        MessageBoxButton.OK,
                        MessageBoxImage.Warning);
                    return;
                }

                File.Copy(sourcePath, dialog.FileName, overwrite: true);

                TxtBackupStatus.Text =
                    $"✓ Backed up to {dialog.FileName}";
                TxtBackupStatus.Visibility = Visibility.Visible;
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    $"Backup failed: {ex.Message}",
                    "Error",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
        }

        private void RestoreDatabase_Click(
            object sender, RoutedEventArgs e)
        {
            var dialog = new Microsoft.Win32.OpenFileDialog
            {
                Filter = "Database File|*.db",
                Title = "Select a backup database file"
            };

            if (dialog.ShowDialog() != true) return;

            var confirm = MessageBox.Show(
                "Restoring a backup will replace your current database.\n\n" +
                "All receipts not in the backup will be lost.\n\n" +
                "Are you sure you want to continue?",
                "Restore Database",
                MessageBoxButton.YesNo,
                MessageBoxImage.Warning);

            if (confirm != MessageBoxResult.Yes) return;

            try
            {
                File.Copy(dialog.FileName,
                    "receipts.db", overwrite: true);

                TxtBackupStatus.Text =
                    "✓ Database restored successfully. " +
                    "Please restart the app.";
                TxtBackupStatus.Foreground =
                    new SolidColorBrush(Color.FromRgb(46, 125, 50));
                TxtBackupStatus.Visibility = Visibility.Visible;

                MessageBox.Show(
                    "Database restored! Please restart the app " +
                    "for changes to take effect.",
                    "Restore Complete",
                    MessageBoxButton.OK,
                    MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    $"Restore failed: {ex.Message}",
                    "Error",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
        }
    }
}
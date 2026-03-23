using System;
using System.IO;
using System.Text.Json;
using static EReceiptApp.Views.Pages.SettingsPage;

namespace EReceiptApp.Services
{
    public static class SettingsService
    {
        private static readonly string SettingsPath = Path.Combine(
            Environment.GetFolderPath(
                Environment.SpecialFolder.ApplicationData),
            "EReceiptApp", "settings.json");

        public static AppSettings Load()
        {
            try
            {
                if (!File.Exists(SettingsPath))
                    return new AppSettings();

                var json = File.ReadAllText(SettingsPath);
                return JsonSerializer.Deserialize<AppSettings>(json)
                       ?? new AppSettings();
            }
            catch
            {
                return new AppSettings();
            }
        }

        public static void Save(AppSettings settings)
        {
            try
            {
                var dir = Path.GetDirectoryName(SettingsPath)!;
                if (!Directory.Exists(dir))
                    Directory.CreateDirectory(dir);

                File.WriteAllText(SettingsPath,
                    JsonSerializer.Serialize(settings,
                        new JsonSerializerOptions
                        {
                            WriteIndented = true
                        }));
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine(
                    $"Settings save error: {ex.Message}");
            }
        }

        // ── Shortcut helpers ──────────────────────────────────────────

        public static void SaveLastUsedFields(
            string organization, string clubName,
            string cashier, string academicYear)
        {
            var settings = Load();
            settings.LastOrganization = organization;
            settings.LastClubName = clubName;
            settings.LastCashier = cashier;
            settings.LastAcademicYear = academicYear;
            Save(settings);
        }

        public static int GetNextReceiptNumber(bool isMembership)
        {
            var settings = Load();

            if (isMembership)
            {
                settings.MembershipCounter++;
                Save(settings);
                return settings.MembershipCounter;
            }
            else
            {
                settings.StandardCounter++;
                Save(settings);
                return settings.StandardCounter;
            }
        }
    }
}
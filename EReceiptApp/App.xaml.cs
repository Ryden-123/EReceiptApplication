using System;
using System.IO;
using System.Windows;
using EReceiptApp.Services;

namespace EReceiptApp
{
    public partial class App : Application
    {
        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);
            ThemeManager.Initialize();
            CleanupOldDraftFiles();
        }

        private void CleanupOldDraftFiles()
        {
            try
            {
                string draftPath = Path.Combine(
                    Environment.GetFolderPath(
                        Environment.SpecialFolder.ApplicationData),
                    "EReceiptApp", "draft.json");

                if (File.Exists(draftPath))
                    File.Delete(draftPath);
            }
            catch { }
        }
    }
}
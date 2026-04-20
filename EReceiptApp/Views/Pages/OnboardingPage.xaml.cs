using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using EReceiptApp.Services;

namespace EReceiptApp.Views.Pages
{
    public partial class OnboardingPage : Page
    {
        private readonly MainWindow _mainWindow;
        private int _currentStep = 0;

        private readonly (string Icon, string Title, string Desc)[]
            _steps = new[]
        {
            (
                 "👋",
                "Welcome to E-Bidensya!",
                "E-Bidensya helps you create, manage, and share " +
                "digital receipts quickly and easily. " +
                "This short tutorial will walk you through " +
                "the key features in just a minute."
            ),
            (
                "🧾",
                "Creating a receipt",
                "Click New Receipt in the sidebar. " +
                "Fill in who it is issued to, " +
                "add your items and prices, " +
                "then click Preview Receipt when done."
            ),
            (
                "📱",
                "Sharing with a QR code",
                "Every receipt gets a QR code. " +
                "The recipient scans it with their phone " +
                "to save the details. " +
                "You can also export as PDF or image."
            ),
            (
                "📋",
                "Managing your receipts",
                "All receipts are saved automatically. " +
                "Use View All Receipts to search, edit, " +
                "or duplicate any receipt. " +
                "Deleted receipts go to the Trash first."
            ),
            (
                "✅",
                "You are all set!",
                "Explore the Dashboard for stats, " +
                "Settings to upload your logo and set your theme, " +
                "and Verify Receipt to confirm any receipt is authentic. " +
                "Enjoy the app!"
            )
        };

        public OnboardingPage(MainWindow mainWindow)
        {
            InitializeComponent();
            _mainWindow = mainWindow;
            ShowStep(0);
        }

        private void ShowStep(int step)
        {
            _currentStep = step;
            var (icon, title, desc) = _steps[step];

            TxtStepIcon.Text = icon;
            TxtStepTitle.Text = title;
            TxtStepDesc.Text = desc;

            // Update dots
            var dots = new[] { Dot1, Dot2, Dot3, Dot4, Dot5 };
            var accent = (SolidColorBrush)Application.Current
                .Resources["AppAccent"];
            var border = (SolidColorBrush)Application.Current
                .Resources["AppBorder"];

            for (int i = 0; i < dots.Length; i++)
                dots[i].Fill = i == step ? accent : border;

            // Update button text on last step
            BtnNext.Content = step == _steps.Length - 1
                ? "Get Started! 🚀" : "Next →";
        }

        private void Next_Click(object sender, RoutedEventArgs e)
        {
            if (_currentStep < _steps.Length - 1)
            {
                ShowStep(_currentStep + 1);
            }
            else
            {
                CompleteOnboarding();
            }
        }

        private void Skip_Click(object sender, RoutedEventArgs e)
        {
            CompleteOnboarding();
        }

        private void CompleteOnboarding()
        {
            // Mark onboarding as done so it never shows again
            var settings = SettingsService.Load();
            settings.HasCompletedOnboarding = true;
            SettingsService.Save(settings);

            // Navigate to dashboard
            _mainWindow.ShowSidebar();
        }
    }
}
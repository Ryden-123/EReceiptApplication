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
                "Welcome to E-Receipt System!",
                "This app lets you create, manage, and share " +
                "digital receipts quickly and easily. " +
                "This short tutorial will walk you through " +
                "the key features in just a minute."
            ),
            (
                "🧾",
                "Creating a receipt",
                "Click New Receipt in the sidebar to open " +
                "the receipt form. Fill in who it's issued to, " +
                "add your items with prices, and click " +
                "Preview Receipt when done."
            ),
            (
                "📱",
                "Sharing with a QR code",
                "Every receipt gets a QR code automatically. " +
                "The recipient can scan it with their phone " +
                "camera to save the receipt details. " +
                "You can also export it as a PDF or image."
            ),
            (
                "📋",
                "Managing receipts",
                "All receipts are saved automatically. " +
                "Go to View All Receipts to search, " +
                "filter, edit, or duplicate any receipt. " +
                "Deleted receipts go to the Trash first."
            ),
            (
                "✅",
                "You are all set!",
                "That covers the basics. Explore the " +
                "Dashboard for stats, Settings to customize " +
                "your theme and upload your logo, and " +
                "Verify Receipt to confirm any receipt is authentic."
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
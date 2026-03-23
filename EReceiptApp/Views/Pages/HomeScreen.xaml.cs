using System.Windows.Controls;
using System.Windows.Input;
using EReceiptApp;

namespace EReceiptApp.Views.Pages
{
    public partial class HomeScreen : Page
    {
        private readonly MainWindow _mainWindow;

        public HomeScreen(MainWindow mainWindow)
        {
            InitializeComponent();
            _mainWindow = mainWindow;
            WireUpCards();
        }

        private void WireUpCards()
        {
            StartCard.MouseLeftButtonUp += (s, e) => _mainWindow.ShowSidebar();
            SettingsCard.MouseLeftButtonUp += (s, e) =>
            {
                _mainWindow.ShowSidebar();
                _mainWindow.GoToSettings();
            };
            AboutCard.MouseLeftButtonUp += (s, e) =>
            {
                _mainWindow.ShowSidebar();
                _mainWindow.GoToAbout();
            };
        }
    }
}
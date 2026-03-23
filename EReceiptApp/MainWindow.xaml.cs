using EReceiptApp.Models;
using EReceiptApp.Views.Pages;
using System.Configuration;
using System.Windows;

namespace EReceiptApp
{
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();

            // Hook fade transition on every navigation
            MainFrame.Navigated += MainFrame_Navigated;

            MainFrame.Navigate(new Views.Pages.HomeScreen(this));
        }

        private void MainFrame_Navigated(object sender,
            System.Windows.Navigation.NavigationEventArgs e)
        {
            if (MainFrame.Content is UIElement page)
            {
                page.Opacity = 0;

                var fade = new System.Windows.Media.Animation
                    .DoubleAnimation
                {
                    From = 0,
                    To = 1,
                    Duration = new Duration(
                        TimeSpan.FromMilliseconds(200)),
                    EasingFunction =
                        new System.Windows.Media.Animation
                            .CubicEase
                        {
                            EasingMode = System.Windows.Media
                                .Animation.EasingMode.EaseOut
                        }
                };

                page.BeginAnimation(UIElement.OpacityProperty, fade);
            }
        }

        // Called by HomeScreen when Start is clicked
        public void ShowSidebar()
        {
            SidebarColumn.Width = new System.Windows.GridLength(220);
            SidebarPanel.Visibility = Visibility.Visible;
            MainFrame.Navigate(new Views.Pages.DashboardPage());
        }

        // Called by HomeScreen when Settings is clicked
        public void GoToSettings()
        {
            MainFrame.Navigate(new SettingsPage());
        }

        // Called by HomeScreen when About is clicked
        public void GoToAbout()
        {
            MainFrame.Navigate(new AboutPage());
        }

        private void Home_Click(object sender, RoutedEventArgs e)
        {
            SidebarColumn.Width = new System.Windows.GridLength(0);
            SidebarPanel.Visibility = Visibility.Collapsed;
            MainFrame.Navigate(new HomeScreen(this));
        }

        private void NewStandardReceipt_Click(object sender, RoutedEventArgs e)
        {
            MainFrame.Navigate(new ReceiptBuilderPage(ReceiptType.Standard));
        }

        private void NewMembershipReceipt_Click(object sender, RoutedEventArgs e)
        {
            MainFrame.Navigate(new ReceiptBuilderPage(ReceiptType.Membership));
        }

        private void ViewReceipts_Click(object sender, RoutedEventArgs e)
        {
            MainFrame.Navigate(new ReceiptsListPage());
        }

        private void Settings_Click(object sender, RoutedEventArgs e)
        {
            MainFrame.Navigate(new SettingsPage());
        }

        private void About_Click(object sender, RoutedEventArgs e)
        {
            MainFrame.Navigate(new AboutPage());
        }

        private void Trash_Click(object sender, RoutedEventArgs e)
        {
            MainFrame.Navigate(new Views.Pages.TrashPage());
        }

        private void MainWindow_KeyDown(object sender,
    System.Windows.Input.KeyEventArgs e)
        {
            // Only fire if Ctrl is held
            if (e.KeyboardDevice.Modifiers !=
                System.Windows.Input.ModifierKeys.Control) return;

            switch (e.Key)
            {
                // Ctrl+N — New Standard Receipt
                case System.Windows.Input.Key.N:
                    if (SidebarPanel.Visibility == Visibility.Visible)
                    {
                        MainFrame.Navigate(
                            new Views.Pages.ReceiptBuilderPage(
                                Models.ReceiptType.Standard));
                        e.Handled = true;
                    }
                    break;

                // Ctrl+M — New Membership Receipt
                case System.Windows.Input.Key.M:
                    if (SidebarPanel.Visibility == Visibility.Visible)
                    {
                        MainFrame.Navigate(
                            new Views.Pages.ReceiptBuilderPage(
                                Models.ReceiptType.Membership));
                        e.Handled = true;
                    }
                    break;

                // Ctrl+H — View Receipt History
                case System.Windows.Input.Key.H:
                    if (SidebarPanel.Visibility == Visibility.Visible)
                    {
                        MainFrame.Navigate(
                            new Views.Pages.ReceiptsListPage());
                        e.Handled = true;
                    }
                    break;

                // Ctrl+Home — Go to Home Screen
                case System.Windows.Input.Key.Home:
                    Home_Click(sender, new RoutedEventArgs());
                    e.Handled = true;
                    break;
            }
        }
        private void Verify_Click(object sender, RoutedEventArgs e)
        {
            MainFrame.Navigate(new Views.Pages.VerifyReceiptPage());
        }

        private void Dashboard_Click(object sender, RoutedEventArgs e)
        {
            MainFrame.Navigate(new Views.Pages.DashboardPage());
        }
    }
}
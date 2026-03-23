using System.Text.RegularExpressions;
using System.Windows;

namespace EReceiptApp.Views.Dialogs
{
    public partial class SendEmailDialog : Window
    {
        public string RecipientName { get; private set; } = "";
        public string RecipientEmail { get; private set; } = "";

        public SendEmailDialog(string defaultName = "")
        {
            InitializeComponent();
            TxtName.Text = defaultName;
        }

        private void Send_Click(object sender, RoutedEventArgs e)
        {
            TxtError.Visibility = Visibility.Collapsed;

            // Validate name
            if (string.IsNullOrWhiteSpace(TxtName.Text))
            {
                ShowError("Please enter the recipient's name.");
                return;
            }

            // Validate email format
            string email = TxtEmail.Text.Trim();
            if (string.IsNullOrWhiteSpace(email))
            {
                ShowError("Please enter an email address.");
                return;
            }

            if (!IsValidEmail(email))
            {
                ShowError("Please enter a valid email address.");
                return;
            }

            RecipientName = TxtName.Text.Trim();
            RecipientEmail = email;

            // Signal success
            DialogResult = true;
            Close();
        }

        private void Cancel_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }

        private void ShowError(string message)
        {
            TxtError.Text = message;
            TxtError.Visibility = Visibility.Visible;
        }

        private bool IsValidEmail(string email)
        {
            return Regex.IsMatch(email,
                @"^[^@\s]+@[^@\s]+\.[^@\s]+$");
        }
    }
}
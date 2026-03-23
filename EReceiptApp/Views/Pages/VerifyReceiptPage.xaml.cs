using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using EReceiptApp.Models;
using EReceiptApp.Services;

namespace EReceiptApp.Views.Pages
{
    public partial class VerifyReceiptPage : Page
    {
        private readonly DatabaseService _db = new DatabaseService();
        private Receipt? _foundReceipt;

        public VerifyReceiptPage()
        {
            InitializeComponent();
        }

        // ── Search by receipt number ──────────────────────────────────
        private void SearchByNumber_Click(
            object sender, RoutedEventArgs e)
        {
            string number = TxtReceiptNumber.Text.Trim().ToUpper();

            if (string.IsNullOrWhiteSpace(number))
            {
                MessageBox.Show(
                    "Please enter a receipt number.",
                    "Missing Input",
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning);
                return;
            }

            var receipt = _db.GetReceiptByNumber(number);
            ShowResult(receipt, number);
        }

        // ── Verify QR data ────────────────────────────────────────────
        private void VerifyQr_Click(object sender, RoutedEventArgs e)
        {
            string qrData = TxtQrData.Text.Trim();

            if (string.IsNullOrWhiteSpace(qrData))
            {
                MessageBox.Show(
                    "Please paste the QR code data first.",
                    "Missing Input",
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning);
                return;
            }

            // Extract receipt number from QR payload
            // Format: RECEIPT#MEM-2024-0001\nType:...
            string receiptNumber = ExtractReceiptNumber(qrData);

            if (string.IsNullOrWhiteSpace(receiptNumber))
            {
                ShowInvalidResult(
                    "Could not read receipt number from QR data.\n" +
                    "Make sure you copied the full QR code text.");
                return;
            }

            var receipt = _db.GetReceiptByNumber(receiptNumber);
            ShowResult(receipt, receiptNumber);
        }

        // ── Extract receipt number from QR payload ────────────────────
        private string ExtractReceiptNumber(string qrData)
        {
            // QR format starts with: RECEIPT#MEM-2024-0001
            foreach (var line in qrData.Split('\n'))
            {
                if (line.StartsWith("RECEIPT#"))
                    return line.Replace("RECEIPT#", "").Trim();
            }
            return string.Empty;
        }

        // ── Show result ───────────────────────────────────────────────
        private void ShowResult(Receipt? receipt, string searchedNumber)
        {
            ResultPanel.Visibility = Visibility.Visible;

            if (receipt != null)
            {
                _foundReceipt = receipt;

                // Valid receipt
                ResultPanel.Background = new SolidColorBrush(
                    Color.FromRgb(232, 245, 233));
                ResultPanel.BorderBrush = new SolidColorBrush(
                    Color.FromRgb(165, 214, 167));

                TxtResultIcon.Text = "✅";
                TxtResultTitle.Text = "Receipt Verified!";
                TxtResultTitle.Foreground = new SolidColorBrush(
                    Color.FromRgb(27, 94, 32));

                TxtResultDetail.Text =
                    $"Receipt {receipt.ReceiptNumber} is authentic.\n" +
                    $"Issued to: {receipt.IssuedTo}\n" +
                    $"Date: {receipt.DateIssued:MMMM dd, yyyy}\n" +
                    $"Total: ₱{receipt.TotalAmount:F2}";
                TxtResultDetail.Foreground = new SolidColorBrush(
                    Color.FromRgb(27, 94, 32));

                BtnViewReceipt.Visibility = Visibility.Visible;
            }
            else
            {
                ShowInvalidResult(
                    $"No receipt found with number:\n{searchedNumber}\n\n" +
                    "This receipt may be invalid or was not issued " +
                    "by this system.");
            }
        }

        private void ShowInvalidResult(string message)
        {
            _foundReceipt = null;

            ResultPanel.Background = new SolidColorBrush(
                Color.FromRgb(255, 235, 238));
            ResultPanel.BorderBrush = new SolidColorBrush(
                Color.FromRgb(239, 154, 154));

            TxtResultIcon.Text = "❌";
            TxtResultTitle.Text = "Receipt Not Found";
            TxtResultTitle.Foreground = new SolidColorBrush(
                Color.FromRgb(183, 28, 28));

            TxtResultDetail.Text = message;
            TxtResultDetail.Foreground = new SolidColorBrush(
                Color.FromRgb(183, 28, 28));

            BtnViewReceipt.Visibility = Visibility.Collapsed;
        }

        // ── View found receipt ────────────────────────────────────────
        private void ViewFound_Click(object sender, RoutedEventArgs e)
        {
            if (_foundReceipt != null)
                NavigationService?.Navigate(
                    new ReceiptPreviewPage(
                        _foundReceipt, fromHistory: true));
        }
    }
}
using System;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using EReceiptApp.Models;
using EReceiptApp.Services;
using QRCoder;
using System.Windows.Shapes;
using EReceiptApp.Views.Dialogs;
using System.Threading.Tasks;


namespace EReceiptApp.Views.Pages
{
    public partial class ReceiptPreviewPage : Page
    {
        private readonly Receipt _receipt;
        private readonly QRService _qrService = new QRService();
        private readonly bool _fromHistory;
        private readonly bool _fromDashboard;
        private readonly bool _isEdit;


        public ReceiptPreviewPage(Receipt receipt,
            bool fromHistory = false,
            bool isEdit = false,
            bool fromDashboard = false)
        {
            InitializeComponent();
            _receipt = receipt;
            _fromHistory = fromHistory;
            _fromDashboard = fromDashboard;
            _isEdit = isEdit;
            Loaded += Page_Loaded;
        }

        private void Page_Loaded(object sender, RoutedEventArgs e)
        {
            PopulateReceipt();
            GenerateQRCode();

            if (!_fromHistory)
                SaveOrUpdateDatabase();
        }

        private void SaveOrUpdateDatabase()
        {
            try
            {
                var db = new Services.DatabaseService();
                if (_isEdit)
                    db.UpdateReceipt(_receipt);
                else
                    db.SaveReceipt(_receipt);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine(
                    $"DB error: {ex.Message}");
            }
        }


        // ── Fill all receipt fields ───────────────────────────────────
        private void PopulateReceipt()
        {
            // Load logo if available
            var logo = LogoService.LoadLogo();
            if (logo != null)
            {
                ImgReceiptLogo.Source = logo;
                ImgReceiptLogo.Visibility = Visibility.Visible;
            }
            // Header
            TxtOrgName.Text = string.IsNullOrWhiteSpace(_receipt.OrganizationName)
                ? "E-Receipt System"
                : _receipt.OrganizationName;

            TxtClubName.Text = _receipt.ClubName;
            TxtClubName.Visibility = string.IsNullOrWhiteSpace(_receipt.ClubName)
                ? Visibility.Collapsed
                : Visibility.Visible;

            // Type badge
            if (_receipt.Type == ReceiptType.Membership)
            {
                TxtTypeBadge.Text = "🪪  Membership Receipt";
                TypeBadge.Background = new SolidColorBrush(
                    Color.FromRgb(237, 231, 246));
                TxtTypeBadge.Foreground = new SolidColorBrush(
                    Color.FromRgb(81, 45, 168));
            }
            else
            {
                TxtTypeBadge.Text = "🧾  Standard Receipt";
                TypeBadge.Background = new SolidColorBrush(
                    Color.FromRgb(232, 245, 233));
                TxtTypeBadge.Foreground = new SolidColorBrush(
                    Color.FromRgb(46, 125, 50));
            }

            // Receipt info
            TxtReceiptNumber.Text = _receipt.ReceiptNumber;
            TxtDate.Text = _receipt.DateIssued.ToString("MMMM dd, yyyy");
            TxtCashier.Text = _receipt.CashierName;

            // Recipient
            TxtIssuedTo.Text = _receipt.IssuedTo;

            // ID Number (optional)
            if (!string.IsNullOrWhiteSpace(_receipt.IdNumber))
            {
                TxtIdNumber.Text = _receipt.IdNumber;
                IdNumberRow.Visibility = Visibility.Visible;
            }

            // Membership-only fields
            if (_receipt.Type == ReceiptType.Membership)
            {
                if (!string.IsNullOrWhiteSpace(_receipt.ClubName))
                {
                    TxtMembershipType.Text = _receipt.ClubName;
                    MembershipRow.Visibility = Visibility.Visible;
                }
                if (!string.IsNullOrWhiteSpace(_receipt.OrganizationName))
                {
                    TxtAcadYear.Text = _receipt.OrganizationName;
                    AcadYearRow.Visibility = Visibility.Visible;
                }
            }

            // Items
            foreach (var item in _receipt.Items)
            {
                var grid = new Grid { Margin = new Thickness(0, 0, 0, 6) };
                grid.ColumnDefinitions.Add(new ColumnDefinition
                { Width = new GridLength(1, GridUnitType.Star) });
                grid.ColumnDefinitions.Add(new ColumnDefinition
                { Width = new GridLength(40) });
                grid.ColumnDefinitions.Add(new ColumnDefinition
                { Width = new GridLength(80) });
                grid.ColumnDefinitions.Add(new ColumnDefinition
                { Width = new GridLength(80) });

                var desc = MakeText(item.Description, 12, false, 0);
                var qty = MakeText(item.Quantity.ToString(), 12, false, 1,
                    TextAlignment.Center);
                var price = MakeText($"₱{item.UnitPrice:F2}", 12, false, 2,
                    TextAlignment.Right);
                var total = MakeText($"₱{item.Total:F2}", 12, true, 3,
                    TextAlignment.Right);

                grid.Children.Add(desc);
                grid.Children.Add(qty);
                grid.Children.Add(price);
                grid.Children.Add(total);

                ItemsPanel.Children.Add(grid);
            }

            // Total
            TxtTotal.Text = $"₱{_receipt.TotalAmount:F2}";

            // Notes
            if (!string.IsNullOrWhiteSpace(_receipt.Notes))
            {
                TxtNotes.Text = $"📝 {_receipt.Notes}";
                TxtNotes.Visibility = Visibility.Visible;
            }
        }

        // Helper to create a TextBlock for item rows
        private TextBlock MakeText(string text, int fontSize,
            bool bold, int column,
            TextAlignment align = TextAlignment.Left)
        {
            var tb = new TextBlock
            {
                Text = text,
                FontSize = fontSize,
                FontWeight = bold ? FontWeights.SemiBold : FontWeights.Normal,
                Foreground = (SolidColorBrush)Application.Current.Resources["AppText"],
                TextAlignment = align,
                TextWrapping = TextWrapping.Wrap
            };
            Grid.SetColumn(tb, column);
            return tb;
        }

        // ── Generate QR Code ─────────────────────────────────────────
        private void GenerateQRCode()
        {
            try
            {
                QrCodeImage.Source = _qrService.GenerateQR(_receipt);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Could not generate QR code: {ex.Message}",
                    "QR Error", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }

        // ── Print ────────────────────────────────────────────────────
        private void Print_Click(object sender, RoutedEventArgs e)
        {
            var printDialog = new System.Windows.Controls.PrintDialog();
            if (printDialog.ShowDialog() == true)
            {
                printDialog.PrintVisual(ReceiptContent,
                    $"Receipt {_receipt.ReceiptNumber}");
            }
        }

        // ── Save as PNG Image ────────────────────────────────────────
        private void SaveImage_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new Microsoft.Win32.SaveFileDialog
            {
                FileName = $"Receipt_{_receipt.ReceiptNumber}",
                DefaultExt = ".png",
                Filter = "PNG Image|*.png"
            };

            if (dialog.ShowDialog() == true)
            {
                try
                {
                    // Build a standalone receipt visual for rendering
                    var visual = BuildReceiptVisualForExport();

                    // Force layout pass so everything measures correctly
                    visual.Measure(new Size(double.PositiveInfinity,
                        double.PositiveInfinity));
                    visual.Arrange(new Rect(visual.DesiredSize));
                    visual.UpdateLayout();

                    // Render at 2x scale for a sharper image
                    double scale = 2.0;
                    int width = (int)(visual.DesiredSize.Width * scale);
                    int height = (int)(visual.DesiredSize.Height * scale);

                    var renderBitmap = new RenderTargetBitmap(
                        width, height,
                        96 * scale, 96 * scale,
                        PixelFormats.Pbgra32);

                    renderBitmap.Render(visual);

                    var encoder = new PngBitmapEncoder();
                    encoder.Frames.Add(BitmapFrame.Create(renderBitmap));

                    using var stream = File.Create(dialog.FileName);
                    encoder.Save(stream);

                    MessageBox.Show(
                        "Receipt saved as image successfully!",
                        "Saved", MessageBoxButton.OK,
                        MessageBoxImage.Information);
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Could not save image: {ex.Message}",
                        "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        private Border BuildReceiptVisualForExport()
        {
            // Outer card with explicit padding — this is what was missing
            var card = new Border
            {
                Background = new SolidColorBrush(Colors.White),
                BorderBrush = new SolidColorBrush(Color.FromRgb(220, 220, 230)),
                BorderThickness = new Thickness(1),
                CornerRadius = new CornerRadius(12),
                Padding = new Thickness(48, 40, 48, 40),
                MinWidth = 520
            };

            var stack = new StackPanel();

            // ── Header ──────────────────────────────────────────────────

            // Add logo if it exists
            if (LogoService.HasLogo)
            {
                var logo = LogoService.LoadLogo();
                if (logo != null)
                {
                    var logoImg = new System.Windows.Controls.Image
                    {
                        Source = logo,
                        Height = 70,
                        Width = 70,
                        Stretch = System.Windows.Media
                                                  .Stretch.Uniform,
                        HorizontalAlignment = HorizontalAlignment.Center,
                        Margin = new Thickness(0, 0, 0, 12)
                    };
                    stack.Children.Add(logoImg);
                }
            }
            stack.Children.Add(MakeExportText(
                _receipt.OrganizationName, 18, true,
                TextAlignment.Center, Colors.Black, 0, 4));

            if (!string.IsNullOrWhiteSpace(_receipt.ClubName))
                stack.Children.Add(MakeExportText(
                    _receipt.ClubName, 13, false,
                    TextAlignment.Center,
                    Color.FromRgb(120, 120, 140), 0, 0));

            // Type badge
            var badgeColor = _receipt.Type == ReceiptType.Membership
                ? Color.FromRgb(237, 231, 246)
                : Color.FromRgb(232, 245, 233);
            var badgeTextColor = _receipt.Type == ReceiptType.Membership
                ? Color.FromRgb(81, 45, 168)
                : Color.FromRgb(46, 125, 50);
            var badgeText = _receipt.Type == ReceiptType.Membership
                ? "Membership Receipt"
                : "Standard Receipt";

            var badge = new Border
            {
                Background = new SolidColorBrush(badgeColor),
                CornerRadius = new CornerRadius(20),
                Padding = new Thickness(14, 5, 14, 5),
                HorizontalAlignment = HorizontalAlignment.Center,
                Margin = new Thickness(0, 12, 0, 20)
            };
            badge.Child = new TextBlock
            {
                Text = badgeText,
                FontSize = 11,
                FontWeight = FontWeights.SemiBold,
                Foreground = new SolidColorBrush(badgeTextColor)
            };
            stack.Children.Add(badge);

            // ── Receipt Info ─────────────────────────────────────────────
            stack.Children.Add(MakeExportRow("Receipt No.", _receipt.ReceiptNumber));
            stack.Children.Add(MakeExportRow("Date",
                _receipt.DateIssued.ToString("MMMM dd, yyyy")));
            stack.Children.Add(MakeExportRow("Cashier", _receipt.CashierName));

            stack.Children.Add(MakeExportDivider());

            // ── Recipient ────────────────────────────────────────────────
            stack.Children.Add(MakeExportSectionLabel("RECIPIENT"));
            stack.Children.Add(MakeExportRow("Name", _receipt.IssuedTo));

            if (!string.IsNullOrWhiteSpace(_receipt.IdNumber))
                stack.Children.Add(MakeExportRow("ID Number", _receipt.IdNumber));

            if (_receipt.Type == ReceiptType.Membership)
            {
                if (!string.IsNullOrWhiteSpace(_receipt.ClubName))
                    stack.Children.Add(MakeExportRow(
                        "Membership Type", _receipt.ClubName));
                if (!string.IsNullOrWhiteSpace(_receipt.OrganizationName))
                    stack.Children.Add(MakeExportRow(
                        "School Year", _receipt.OrganizationName));
            }

            stack.Children.Add(MakeExportDivider());

            // ── Items ────────────────────────────────────────────────────
            stack.Children.Add(MakeExportSectionLabel("ITEMS"));
            stack.Children.Add(MakeExportItemHeader());

            foreach (var item in _receipt.Items)
                stack.Children.Add(MakeExportItemRow(item));

            // Total line
            var totalLine = new Rectangle
            {
                Height = 1,
                Fill = new SolidColorBrush(Color.FromRgb(220, 220, 230)),
                Margin = new Thickness(0, 8, 0, 12)
            };
            stack.Children.Add(totalLine);

            stack.Children.Add(MakeExportTotalRow(_receipt.TotalAmount));

            // Notes
            if (!string.IsNullOrWhiteSpace(_receipt.Notes))
                stack.Children.Add(MakeExportText(
                    $"Note: {_receipt.Notes}", 11, false,
                    TextAlignment.Left,
                    Color.FromRgb(120, 120, 140), 0, 12,
                    FontStyles.Italic));

            stack.Children.Add(MakeExportDivider());

            // ── Footer ───────────────────────────────────────────────────
            stack.Children.Add(MakeExportText(
                "This is an official digital receipt.",
                10, false, TextAlignment.Center,
                Color.FromRgb(150, 150, 165), 0, 2));
            stack.Children.Add(MakeExportText(
                "E-Receipt System v1.0",
                10, false, TextAlignment.Center,
                Color.FromRgb(150, 150, 165), 0, 0));

            card.Child = stack;
            return card;
        }

        // ── Export helper methods ─────────────────────────────────────────

        private TextBlock MakeExportText(
            string text, int fontSize, bool bold,
            TextAlignment align, Color color,
            double marginTop, double marginBottom,
            FontStyle fontStyle = default)
        {
            return new TextBlock
            {
                Text = text,
                FontSize = fontSize,
                FontWeight = bold ? FontWeights.Bold : FontWeights.Normal,
                FontStyle = fontStyle,
                Foreground = new SolidColorBrush(color),
                TextAlignment = align,
                TextWrapping = TextWrapping.Wrap,
                Margin = new Thickness(0, marginTop, 0, marginBottom)
            };
        }

        private Grid MakeExportRow(string label, string value)
        {
            var grid = new Grid { Margin = new Thickness(0, 0, 0, 8) };
            grid.ColumnDefinitions.Add(new ColumnDefinition
            { Width = new GridLength(1, GridUnitType.Star) });
            grid.ColumnDefinitions.Add(new ColumnDefinition
            { Width = new GridLength(1, GridUnitType.Star) });

            var labelTb = new TextBlock
            {
                Text = label,
                FontSize = 12,
                Foreground = new SolidColorBrush(Color.FromRgb(120, 120, 140)),
                VerticalAlignment = VerticalAlignment.Center
            };

            var valueTb = new TextBlock
            {
                Text = value,
                FontSize = 12,
                FontWeight = FontWeights.SemiBold,
                Foreground = new SolidColorBrush(Colors.Black),
                HorizontalAlignment = HorizontalAlignment.Right,
                TextAlignment = TextAlignment.Right,
                TextWrapping = TextWrapping.Wrap
            };

            Grid.SetColumn(labelTb, 0);
            Grid.SetColumn(valueTb, 1);
            grid.Children.Add(labelTb);
            grid.Children.Add(valueTb);
            return grid;
        }

        private TextBlock MakeExportDivider()
        {
            return new TextBlock
            {
                Text = "- - - - - - - - - - - - - - - - - - - - - - - - - - - -",
                FontSize = 11,
                Foreground = new SolidColorBrush(Color.FromRgb(200, 200, 210)),
                TextAlignment = TextAlignment.Center,
                Margin = new Thickness(0, 14, 0, 14),
                Opacity = 0.8
            };
        }

        private TextBlock MakeExportSectionLabel(string text)
        {
            return new TextBlock
            {
                Text = text,
                FontSize = 10,
                FontWeight = FontWeights.Bold,
                Foreground = new SolidColorBrush(Color.FromRgb(120, 120, 140)),
                Margin = new Thickness(0, 0, 0, 10)
            };
        }

        private Grid MakeExportItemHeader()
        {
            var grid = new Grid { Margin = new Thickness(0, 0, 0, 8) };
            grid.ColumnDefinitions.Add(new ColumnDefinition
            { Width = new GridLength(1, GridUnitType.Star) });
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(36) });
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(72) });
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(72) });

            AddItemCell(grid, "Description", 0, TextAlignment.Left, bold: true);
            AddItemCell(grid, "Qty", 1, TextAlignment.Center, bold: true);
            AddItemCell(grid, "Price", 2, TextAlignment.Right, bold: true);
            AddItemCell(grid, "Total", 3, TextAlignment.Right, bold: true);

            return grid;
        }

        private Grid MakeExportItemRow(ReceiptItem item)
        {
            var grid = new Grid { Margin = new Thickness(0, 0, 0, 6) };
            grid.ColumnDefinitions.Add(new ColumnDefinition
            { Width = new GridLength(1, GridUnitType.Star) });
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(36) });
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(72) });
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(72) });

            AddItemCell(grid, item.Description, 0, TextAlignment.Left);
            AddItemCell(grid, item.Quantity.ToString(), 1, TextAlignment.Center);
            AddItemCell(grid, $"₱{item.UnitPrice:F2}", 2, TextAlignment.Right);
            AddItemCell(grid, $"₱{item.Total:F2}", 3, TextAlignment.Right,
                bold: true);

            return grid;
        }

        private void AddItemCell(Grid grid, string text, int column,
            TextAlignment align, bool bold = false)
        {
            var tb = new TextBlock
            {
                Text = text,
                FontSize = 12,
                FontWeight = bold ? FontWeights.SemiBold : FontWeights.Normal,
                Foreground = new SolidColorBrush(Colors.Black),
                TextAlignment = align,
                HorizontalAlignment = column == 0
                    ? HorizontalAlignment.Left
                    : HorizontalAlignment.Right,
                TextWrapping = TextWrapping.Wrap
            };
            Grid.SetColumn(tb, column);
            grid.Children.Add(tb);
        }

        private Grid MakeExportTotalRow(decimal total)
        {
            var grid = new Grid();
            grid.ColumnDefinitions.Add(new ColumnDefinition
            { Width = new GridLength(1, GridUnitType.Star) });
            grid.ColumnDefinitions.Add(new ColumnDefinition
            { Width = GridLength.Auto });

            var label = new TextBlock
            {
                Text = "TOTAL",
                FontSize = 15,
                FontWeight = FontWeights.Bold,
                Foreground = new SolidColorBrush(Colors.Black),
                VerticalAlignment = VerticalAlignment.Center
            };

            var value = new TextBlock
            {
                Text = $"₱{total:F2}",
                FontSize = 15,
                FontWeight = FontWeights.Bold,
                Foreground = new SolidColorBrush(
                    Color.FromRgb(92, 74, 187)),
                HorizontalAlignment = HorizontalAlignment.Right
            };

            Grid.SetColumn(label, 0);
            Grid.SetColumn(value, 1);
            grid.Children.Add(label);
            grid.Children.Add(value);
            return grid;
        }

        // ── Save as PDF (placeholder for now) ───────────────────────
        // ── Save as PDF ───────────────────────────────────────────────────
        private void SavePdf_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new Microsoft.Win32.SaveFileDialog
            {
                FileName = $"Receipt_{_receipt.ReceiptNumber}",
                DefaultExt = ".pdf",
                Filter = "PDF Document|*.pdf"
            };

            if (dialog.ShowDialog() != true) return;

            try
            {
                var pdfService = new Services.PdfService();
                pdfService.SaveReceiptAsPdf(_receipt, dialog.FileName);

                MessageBox.Show(
                    "Receipt exported as PDF successfully!",
                    "Saved", MessageBoxButton.OK,
                    MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    $"Could not export PDF: {ex.Message}",
                    "Error", MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
        }

        // ── Send via Email ────────────────────────────────────────────────
        private async void SendEmail_Click(object sender, RoutedEventArgs e)
        {
            // Check Gmail settings first
            var settings = Services.SettingsService.Load();

            if (string.IsNullOrWhiteSpace(settings.GmailAddress) ||
                string.IsNullOrWhiteSpace(settings.AppPassword))
            {
                MessageBox.Show(
                    "Please configure your Gmail address and App Password " +
                    "in Settings before sending emails.",
                    "Email Not Configured",
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning);
                return;
            }

            // Show the dialog
            var emailDialog = new SendEmailDialog(_receipt.IssuedTo)
            {
                Owner = Window.GetWindow(this)
            };

            if (emailDialog.ShowDialog() != true) return;

            // Capture all values we need before any async work
            string recipientEmail = emailDialog.RecipientEmail;
            string recipientName = emailDialog.RecipientName;
            string receiptNumber = _receipt.ReceiptNumber;
            string gmailAddress = settings.GmailAddress;
            string appPassword = settings.AppPassword;

            // Disable button while working
            var sendBtn = sender as Button;
            if (sendBtn != null)
            {
                sendBtn.IsEnabled = false;
                sendBtn.Content = "📧  Preparing...";
            }

            string pngPath = string.Empty;
            string pdfPath = string.Empty;

            try
            {
                // ── Step 1: Generate files on UI thread ───────────────────
                // MUST be on UI thread because PdfService builds WPF visuals
                sendBtn!.Content = "📧  Generating files...";

                var pdfService = new Services.PdfService();
                pngPath = pdfService.SaveReceiptAsTempPng(_receipt);
                pdfPath = pdfService.SaveReceiptAsTempPdf(_receipt);

                // ── Step 2: Send email on background thread ───────────────
                // Safe now because we only pass plain strings, no WPF objects
                sendBtn.Content = "📧  Sending...";

                await Task.Run(() =>
                {
                    var emailService = new Services.EmailService(
                        gmailAddress,
                        appPassword);

                    emailService.SendReceipt(
                        recipientEmail,
                        recipientName,
                        receiptNumber,
                        pngPath,
                        pdfPath);
                });

                // ── Step 3: Show success ──────────────────────────────────
                MessageBox.Show(
                    $"Receipt sent successfully to {recipientEmail}!",
                    "Email Sent",
                    MessageBoxButton.OK,
                    MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    GetFriendlyEmailError(ex),
                    "Email Error",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
            finally
            {
                // Always re-enable button
                if (sendBtn != null)
                {
                    sendBtn.IsEnabled = true;
                    sendBtn.Content = "📧  Send via Email";
                }

                // Clean up temp files
                try
                {
                    if (!string.IsNullOrEmpty(pngPath) &&
                        File.Exists(pngPath))
                        File.Delete(pngPath);

                    if (!string.IsNullOrEmpty(pdfPath) &&
                        File.Exists(pdfPath))
                        File.Delete(pdfPath);
                }
                catch { }
            }
        }

        // ── Translate technical errors into friendly messages ─────────────
        private string GetFriendlyEmailError(Exception ex)
        {
            string msg = ex.Message.ToLower();

            if (msg.Contains("authentication") ||
                msg.Contains("535") ||
                msg.Contains("username and password"))
                return "Authentication failed.\n\n" +
                       "Your Gmail address or App Password is incorrect.\n" +
                       "Please check your Settings and try again.";

            if (msg.Contains("network") ||
                msg.Contains("connection") ||
                msg.Contains("timeout") ||
                msg.Contains("socket"))
                return "Could not connect to Gmail.\n\n" +
                       "Please check your internet connection and try again.";

            if (msg.Contains("recipient") ||
                msg.Contains("address") ||
                msg.Contains("550"))
                return "The recipient email address appears to be invalid.\n" +
                       "Please double-check the email address and try again.";

            if (msg.Contains("tls") ||
                msg.Contains("ssl") ||
                msg.Contains("secure"))
                return "A secure connection to Gmail could not be established.\n" +
                       "Please check your internet connection and try again.";

            // Generic fallback with the actual error for debugging
            return $"Could not send email:\n\n{ex.Message}\n\n" +
                   "Make sure your Gmail App Password is correct " +
                   "and your internet connection is working.";
        }

        // ── Back ─────────────────────────────────────────────────────
        private void Back_Click(object sender, RoutedEventArgs e)
        {
            if (_fromDashboard)
                NavigationService?.Navigate(
                    new DashboardPage());
            else if (_fromHistory)
                NavigationService?.Navigate(
                    new ReceiptsListPage());
            else if (NavigationService?.CanGoBack == true)
                NavigationService.GoBack();
        }
    }
}
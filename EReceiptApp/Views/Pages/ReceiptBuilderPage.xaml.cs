using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Media;
using EReceiptApp.Models;
using EReceiptApp.Services;

namespace EReceiptApp.Views.Pages
{
    public partial class ReceiptBuilderPage : Page
    {
        private readonly Receipt? _editingReceipt;
        private readonly bool _isEditMode;
        private readonly bool _isDuplicateMode;
        private readonly List<(TextBox Desc, TextBox Qty,
            TextBox Price)> _itemRows =
            new List<(TextBox, TextBox, TextBox)>();

        private readonly DatabaseService _db = new DatabaseService();
        private System.Windows.Threading.DispatcherTimer? _typingTimer;
        private bool _isTyping = false;

        // ── New receipt ───────────────────────────────────────────────
        public ReceiptBuilderPage()
        {
            InitializeComponent();
            _isEditMode = false;
            _isDuplicateMode = false;
            SetupPage();
            AutoFillLastUsedFields();
            AddItemRow();
        }

        // ── Edit existing receipt ─────────────────────────────────────
        public ReceiptBuilderPage(Receipt receipt)
        {
            InitializeComponent();
            _editingReceipt = receipt;
            _isEditMode = true;
            _isDuplicateMode = false;
            SetupPage();
            PopulateFromReceipt(receipt, keepNumber: true);
        }

        // ── Duplicate receipt ─────────────────────────────────────────
        public ReceiptBuilderPage(Receipt receipt, bool isDuplicate)
        {
            InitializeComponent();
            _editingReceipt = null;
            _isEditMode = false;
            _isDuplicateMode = true;
            SetupPage();
            PopulateFromReceipt(receipt, keepNumber: false);
        }

        // ── Page setup ────────────────────────────────────────────────
        private void SetupPage()
        {
            DpDateIssued.SelectedDate = DateTime.Today;

            if (_isEditMode && _editingReceipt != null)
            {
                PageTitle.Text = "Edit Receipt";
                PageSubtitle.Text =
                    "Update the details of this receipt.";
                // Show existing number — don't generate new one
                TxtReceiptNumber.Text = _editingReceipt.ReceiptNumber;
            }
            else if (_isDuplicateMode)
            {
                PageTitle.Text = "Duplicate Receipt";
                PageSubtitle.Text =
                    "A new receipt pre-filled from the original.";
                // Number will be generated on submit
                TxtReceiptNumber.Text = "(generated on submit)";
            }
            else
            {
                PageTitle.Text = "New Receipt";
                PageSubtitle.Text =
                    "Fill in the details below. " +
                    "Only ✱ marked fields are required.";
                // Number will be generated on submit
                TxtReceiptNumber.Text = "(generated on submit)";
            }

            // Init typing timer
            InitTypingTimer();

            // Wire TextChanged on required fields
            TxtIssuedTo.TextChanged += OnFieldChanged;
            TxtCashier.TextChanged += OnFieldChanged;

            // Initial button state
            UpdatePreviewButton();

            Unloaded += (s, e) => _typingTimer?.Stop();
        }

        // ── Auto-fill from last used ──────────────────────────────────
        private void AutoFillLastUsedFields()
        {
            var s = SettingsService.Load();
            if (!string.IsNullOrWhiteSpace(s.LastOrganization))
                TxtOrganization.Text = s.LastOrganization;
            if (!string.IsNullOrWhiteSpace(s.LastCashier))
                TxtCashier.Text = s.LastCashier;
        }

        // ── Generate receipt number (called on submit only) ───────────
        private string GenerateReceiptNumber()
        {
            int counter = SettingsService.GetNextReceiptNumber(false);
            return $"REC-{DateTime.Now:yyyy}-{counter:D4}";
        }

        // ── Pre-fill from existing receipt ────────────────────────────
        private void PopulateFromReceipt(
            Receipt receipt, bool keepNumber)
        {
            if (keepNumber)
                TxtReceiptNumber.Text = receipt.ReceiptNumber;

            TxtIssuedTo.Text = receipt.IssuedTo;
            TxtCashier.Text = receipt.CashierName;
            TxtOrganization.Text = receipt.OrganizationName;
            TxtNotes.Text = receipt.Notes;
            TxtIdNumber.Text = receipt.IdNumber;
            DpDateIssued.SelectedDate = keepNumber
                ? receipt.DateIssued : DateTime.Today;

            ItemsPanel.Children.Clear();
            _itemRows.Clear();

            foreach (var item in receipt.Items)
                AddItemRow(item.Description,
                    item.Quantity.ToString(),
                    item.UnitPrice.ToString("F2"));

            RecalculateTotal();
        }

        // ── Add item row ──────────────────────────────────────────────
        private void AddItemRow(
            string desc = "",
            string qty = "1",
            string price = "0.00")
        {
            var grid = new Grid
            { Margin = new Thickness(0, 0, 0, 6) };
            grid.ColumnDefinitions.Add(new ColumnDefinition
            { Width = new GridLength(1, GridUnitType.Star) });
            grid.ColumnDefinitions.Add(new ColumnDefinition
            { Width = new GridLength(56) });
            grid.ColumnDefinitions.Add(new ColumnDefinition
            { Width = new GridLength(76) });
            grid.ColumnDefinitions.Add(new ColumnDefinition
            { Width = new GridLength(76) });
            grid.ColumnDefinitions.Add(new ColumnDefinition
            { Width = new GridLength(30) });

            var descBox = MakeInputBox(desc);
            var qtyBox = MakeInputBox(qty, TextAlignment.Center);
            var priceBox = MakeInputBox(price, TextAlignment.Right);
            var totalBox = MakeInputBox("0.00", TextAlignment.Right,
                readOnly: true);

            var removeBtn = new Button
            {
                Content = "✕",
                Padding = new Thickness(4),
                FontSize = 11,
                Background = new SolidColorBrush(
                    Color.FromRgb(198, 80, 80)),
                Foreground = new SolidColorBrush(Colors.White),
                BorderThickness = new Thickness(0),
                Cursor = System.Windows.Input.Cursors.Hand
            };

            qtyBox.PreviewTextInput += NumericOnly_Input;
            priceBox.PreviewTextInput += DecimalOnly_Input;

            descBox.LostFocus += (s, e) =>
                descBox.Text =
                    InputSanitizer.SanitizeText(descBox.Text);

            qtyBox.TextChanged += (s, e) =>
                UpdateRowTotal(qtyBox, priceBox, totalBox);
            priceBox.TextChanged += (s, e) =>
                UpdateRowTotal(qtyBox, priceBox, totalBox);

            removeBtn.Click += (s, e) =>
            {
                ItemsPanel.Children.Remove(grid);
                _itemRows.RemoveAll(r => r.Desc == descBox);
                RecalculateTotal();
            };

            Grid.SetColumn(descBox, 0);
            Grid.SetColumn(qtyBox, 1);
            Grid.SetColumn(priceBox, 2);
            Grid.SetColumn(totalBox, 3);
            Grid.SetColumn(removeBtn, 4);

            grid.Children.Add(descBox);
            grid.Children.Add(qtyBox);
            grid.Children.Add(priceBox);
            grid.Children.Add(totalBox);
            grid.Children.Add(removeBtn);

            ItemsPanel.Children.Add(grid);
            _itemRows.Add((descBox, qtyBox, priceBox));

            UpdateRowTotal(qtyBox, priceBox, totalBox);
        }

        private TextBox MakeInputBox(
            string text = "",
            TextAlignment align = TextAlignment.Left,
            bool readOnly = false)
        {
            return new TextBox
            {
                Text = text,
                Padding = new Thickness(6, 5, 6, 5),
                FontSize = 12,
                BorderBrush = new SolidColorBrush(
                    Color.FromRgb(232, 228, 248)),
                BorderThickness = new Thickness(1),
                Margin = new Thickness(0, 0, 4, 0),
                TextAlignment = align,
                IsReadOnly = readOnly,
                Background = readOnly
                    ? (SolidColorBrush)Application.Current
                        .Resources["AppSurfaceAlt"]
                    : new SolidColorBrush(Colors.White),
                MaxLength = 200
            };
        }

        private void NumericOnly_Input(object sender,
            System.Windows.Input.TextCompositionEventArgs e)
        {
            e.Handled = !System.Text.RegularExpressions
                .Regex.IsMatch(e.Text, @"^\d+$");
        }

        private void DecimalOnly_Input(object sender,
            System.Windows.Input.TextCompositionEventArgs e)
        {
            var box = sender as TextBox;
            bool isDigit = System.Text.RegularExpressions
                .Regex.IsMatch(e.Text, @"^\d+$");
            bool isDot = e.Text == "." &&
                           box != null &&
                           !box.Text.Contains(".");
            e.Handled = !(isDigit || isDot);
        }

        private void UpdateRowTotal(
            TextBox qty, TextBox price, TextBox total)
        {
            bool qOk = int.TryParse(qty.Text, out int q);
            bool pOk = decimal.TryParse(price.Text, out decimal p);
            total.Text = (qOk && pOk)
                ? (q * p).ToString("F2") : "0.00";
            RecalculateTotal();
        }

        private void RecalculateTotal()
        {
            decimal sub = 0;
            foreach (var row in _itemRows)
            {
                if (int.TryParse(row.Qty.Text, out int q) &&
                    decimal.TryParse(row.Price.Text, out decimal p))
                    sub += q * p;
            }
            TxtSubtotal.Text = $"₱{sub:F2}";
            TxtTotal.Text = $"₱{sub:F2}";

            // Update button when items change
            UpdatePreviewButton();
        }

        // ── Preset items picker ───────────────────────────────────────
        private void PickPreset_Click(object sender, RoutedEventArgs e)
        {
            var presets = _db.GetPresetItems();

            if (presets.Count == 0)
            {
                MessageBox.Show(
                    "No preset items found.\n\n" +
                    "Go to Manage Items in the sidebar to add some.",
                    "No Presets",
                    MessageBoxButton.OK,
                    MessageBoxImage.Information);
                return;
            }

            var menu = new System.Windows.Controls.ContextMenu();

            // Group by category
            var grouped = presets
                .GroupBy(p => string.IsNullOrWhiteSpace(p.Category)
                    ? "Other" : p.Category)
                .OrderBy(g => g.Key);

            foreach (var group in grouped)
            {
                // Category header (not clickable)
                var header = new MenuItem
                {
                    Header = group.Key.ToUpper(),
                    IsEnabled = false,
                    FontWeight = FontWeights.Bold,
                    Foreground = (SolidColorBrush)Application.Current
                                     .Resources["AppTextMuted"]
                };
                menu.Items.Add(header);

                // Items in this category
                foreach (var preset in group)
                {
                    string displayName = preset.Name;
                    double displayPrice = preset.DefaultPrice;
                    string desc = string.IsNullOrWhiteSpace(
                        preset.Description)
                        ? "" : $" — {preset.Description}";

                    var item = new MenuItem
                    {
                        Header = $"{displayName}  ₱{displayPrice:F2}{desc}"
                    };

                    item.Click += (s, ev) =>
                        AddItemRow(displayName, "1",
                            displayPrice.ToString("F2"));

                    menu.Items.Add(item);
                }

                menu.Items.Add(new Separator());
            }

            menu.PlacementTarget = sender as UIElement;
            menu.Placement =
                System.Windows.Controls.Primitives.PlacementMode.Bottom;
            menu.IsOpen = true;
        }

        // ── Add Item button ───────────────────────────────────────────
        private void AddItem_Click(object sender, RoutedEventArgs e)
        {
            if (_itemRows.Count >= 20)
            {
                ShowValidation("You can only add up to 20 items.");
                return;
            }
            AddItemRow();
        }

        // ── Clear form ────────────────────────────────────────────────
        private void ClearForm_Click(object sender, RoutedEventArgs e)
        {
            var r = MessageBox.Show(
                "Clear all fields?",
                "Clear Form",
                MessageBoxButton.YesNo,
                MessageBoxImage.Warning);

            if (r != MessageBoxResult.Yes) return;

            TxtIssuedTo.Text = "";
            TxtCashier.Text = "";
            TxtOrganization.Text = "";
            TxtNotes.Text = "";
            TxtIdNumber.Text = "";
            TxtReceiptNumber.Text = _isEditMode && _editingReceipt != null
                ? _editingReceipt.ReceiptNumber
                : "(generated on submit)";
            DpDateIssued.SelectedDate = DateTime.Today;

            ItemsPanel.Children.Clear();
            _itemRows.Clear();
            AddItemRow();
            RecalculateTotal();
            HideValidation();
        }

        // ── Preview / Submit ──────────────────────────────────────────
        private void PreviewReceipt_Click(
            object sender, RoutedEventArgs e)
        {
            if (!ValidateForm()) return;

            // Generate receipt number HERE — not on page load
            if (!_isEditMode)
                TxtReceiptNumber.Text = GenerateReceiptNumber();

            // Save last used fields
            SettingsService.SaveLastUsedFields(
                TxtOrganization.Text.Trim(), "",
                TxtCashier.Text.Trim(), "");

            var receipt = BuildReceipt();
            NavigationService?.Navigate(
                new ReceiptPreviewPage(receipt,
                    fromHistory: false,
                    isEdit: _isEditMode));
        }

        // ── Validation ────────────────────────────────────────────────
        private bool ValidateForm()
        {
            HideValidation();
            var errors = new List<string>();

            // Sanitize
            TxtIssuedTo.Text =
                InputSanitizer.SanitizeName(TxtIssuedTo.Text);
            TxtCashier.Text =
                InputSanitizer.SanitizeName(TxtCashier.Text);
            TxtOrganization.Text =
                InputSanitizer.SanitizeText(TxtOrganization.Text);
            TxtNotes.Text =
                InputSanitizer.SanitizeText(TxtNotes.Text);
            TxtIdNumber.Text =
                InputSanitizer.SanitizeIdNumber(TxtIdNumber.Text);

            // Required fields
            var nameCheck = InputSanitizer.ValidateName(
                TxtIssuedTo.Text, "Issued To");
            if (!nameCheck.IsValid)
                errors.Add($"• {nameCheck.Error}");

            var cashierCheck = InputSanitizer.ValidateName(
                TxtCashier.Text, "Cashier / Issued By");
            if (!cashierCheck.IsValid)
                errors.Add($"• {cashierCheck.Error}");

            // Optional field length checks
            if (TxtOrganization.Text.Length > 150)
                errors.Add("• Organization name is too long.");
            if (TxtNotes.Text.Length > 300)
                errors.Add("• Notes are too long (max 300 chars).");

            var idCheck =
                InputSanitizer.ValidateIdNumber(TxtIdNumber.Text);
            if (!idCheck.IsValid)
                errors.Add($"• {idCheck.Error}");

            // Items
            if (_itemRows.Count == 0)
            {
                errors.Add("• Please add at least one item.");
            }
            else
            {
                int i = 1;
                foreach (var row in _itemRows)
                {
                    row.Desc.Text =
                        InputSanitizer.SanitizeText(row.Desc.Text);

                    if (string.IsNullOrWhiteSpace(row.Desc.Text))
                        errors.Add(
                            $"• Item {i}: description is required.");

                    var qCheck =
                        InputSanitizer.ValidateQuantity(row.Qty.Text);
                    if (!qCheck.IsValid)
                        errors.Add($"• Item {i}: {qCheck.Error}");

                    var pCheck =
                        InputSanitizer.ValidateAmount(row.Price.Text);
                    if (!pCheck.IsValid)
                        errors.Add($"• Item {i}: {pCheck.Error}");
                    i++;
                }
            }

            // Duplicate number check (edit mode only)
            if (_isEditMode && _editingReceipt != null)
            {
                if (_db.ReceiptNumberExists(
                    TxtReceiptNumber.Text.Trim(),
                    _editingReceipt.Id))
                    errors.Add("• Receipt number already exists.");
            }

            if (errors.Count > 0)
            {
                ShowValidation(string.Join("\n", errors));
                return false;
            }

            return true;
        }

        private void ShowValidation(string msg)
        {
            TxtValidation.Text = msg;
            ValidationPanel.Visibility = Visibility.Visible;
        }

        private void HideValidation()
        {
            ValidationPanel.Visibility = Visibility.Collapsed;
        }

        // ── Build Receipt model ───────────────────────────────────────
        private Receipt BuildReceipt()
        {
            var items = new List<ReceiptItem>();
            decimal total = 0;

            foreach (var row in _itemRows)
            {
                int.TryParse(row.Qty.Text, out int q);
                decimal.TryParse(row.Price.Text, out decimal p);

                var item = new ReceiptItem
                {
                    Description = row.Desc.Text,
                    Quantity = q,
                    UnitPrice = p
                };
                items.Add(item);
                total += item.Total;
            }

            return new Receipt
            {
                Id = _isEditMode && _editingReceipt != null
                    ? _editingReceipt.Id : 0,
                ReceiptNumber = TxtReceiptNumber.Text,
                IssuedTo = TxtIssuedTo.Text.Trim(),
                IdNumber = TxtIdNumber.Text.Trim(),
                OrganizationName = TxtOrganization.Text.Trim(),
                DateIssued = DpDateIssued.SelectedDate
                                   ?? DateTime.Today,
                Items = items,
                TotalAmount = total,
                Notes = TxtNotes.Text.Trim(),
                CashierName = TxtCashier.Text.Trim()
            };
        }

        // ── Typing timer setup ────────────────────────────────────────────
        private void InitTypingTimer()
        {
            _typingTimer = new System.Windows.Threading.DispatcherTimer
            {
                Interval = TimeSpan.FromMilliseconds(800)
            };
            _typingTimer.Tick += TypingTimer_Tick;
        }

        private void TypingTimer_Tick(object? sender, EventArgs e)
        {
            _typingTimer?.Stop();
            _isTyping = false;
            UpdatePreviewButton();
        }

        // Called whenever any key is pressed in a field
        private void OnFieldChanged(object sender,
            System.Windows.Controls.TextChangedEventArgs e)
        {
            _isTyping = true;
            UpdatePreviewButton();
            _typingTimer?.Stop();
            _typingTimer?.Start();
        }

        // Enable/disable preview button based on required fields
        private void UpdatePreviewButton()
        {
            bool requiredFilled =
                !string.IsNullOrWhiteSpace(TxtIssuedTo.Text) &&
                !string.IsNullOrWhiteSpace(TxtCashier.Text) &&
                _itemRows.Count > 0;

            // Disable if still typing OR required fields not filled
            BtnPreview.IsEnabled = !_isTyping && requiredFilled;

            // Update button appearance
            BtnPreview.Opacity = BtnPreview.IsEnabled ? 1.0 : 0.5;
        }
    }
}
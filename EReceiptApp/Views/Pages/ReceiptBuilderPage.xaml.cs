using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using EReceiptApp.Models;
using EReceiptApp.Services;

namespace EReceiptApp.Views.Pages
{
    public partial class ReceiptBuilderPage : Page
    {
        
        private readonly ReceiptType _receiptType;
        private readonly Receipt? _editingReceipt;
        private readonly bool _isEditMode;
        private readonly bool _isDuplicateMode;
        private readonly List<(TextBox Desc, TextBox Qty, TextBox Price)>
            _itemRows = new List<(TextBox, TextBox, TextBox)>();

        // ── New receipt ───────────────────────────────────────────────
        public ReceiptBuilderPage(ReceiptType receiptType)
        {
            InitializeComponent();
            _receiptType = receiptType;
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
            _receiptType = receipt.Type;
            _editingReceipt = receipt;
            _isEditMode = true;
            _isDuplicateMode = false;
            SetupPage();
            PopulateFromReceipt(receipt, keepReceiptNumber: true);
        }

        // ── Duplicate existing receipt ────────────────────────────────
        public ReceiptBuilderPage(Receipt receipt, bool isDuplicate)
        {
            InitializeComponent();
            _receiptType = receipt.Type;
            _editingReceipt = null;
            _isEditMode = false;
            _isDuplicateMode = true;
            SetupPage();
            PopulateFromReceipt(receipt, keepReceiptNumber: false);
        }

        // ── Page setup ────────────────────────────────────────────────
        private void SetupPage()
        {
            DpDateIssued.SelectedDate = DateTime.Today;

            

            if (_isEditMode && _editingReceipt != null)
            {
                PageTitle.Text = "Edit Receipt";
                PageSubtitle.Text = "Update the details of this receipt.";
                TxtReceiptNumber.Text = _editingReceipt.ReceiptNumber;
            }
            else if (_isDuplicateMode)
            {
                PageTitle.Text = "Duplicate Receipt";
                PageSubtitle.Text =
                    "A new receipt pre-filled from the original. " +
                    "A new receipt number has been assigned.";
                TxtReceiptNumber.Text = GenerateReceiptNumber();
            }
            else if (_receiptType == ReceiptType.Standard)
            {
                PageTitle.Text = "New Standard Receipt";
                PageSubtitle.Text =
                    "Fill in the details to create a standard sales receipt.";
                TxtReceiptNumber.Text = GenerateReceiptNumber();
            }
            else
            {
                PageTitle.Text = "New Membership Receipt";
                PageSubtitle.Text =
                    "Issue a digital membership proof receipt.";
                TxtReceiptNumber.Text = GenerateReceiptNumber();
            }

            if (_receiptType == ReceiptType.Standard)
            {
                TypeBadge.Background = new SolidColorBrush(
                    Color.FromRgb(232, 245, 233));
                TypeBadgeText.Text = "Standard Receipt";
                TypeBadgeText.Foreground = new SolidColorBrush(
                    Color.FromRgb(46, 125, 50));
                MembershipFields.Visibility = Visibility.Collapsed;
            }
            else
            {
                TypeBadge.Background = new SolidColorBrush(
                    Color.FromRgb(237, 231, 246));
                TypeBadgeText.Text = "Membership Receipt";
                TypeBadgeText.Foreground = new SolidColorBrush(
                    Color.FromRgb(81, 45, 168));
                MembershipFields.Visibility = Visibility.Visible;
            }
        }

        

        // ── Auto-fill from last used fields ───────────────────────────
        private void AutoFillLastUsedFields()
        {
            var settings = SettingsService.Load();

            if (!string.IsNullOrWhiteSpace(settings.LastOrganization))
                TxtOrganization.Text = settings.LastOrganization;

            if (!string.IsNullOrWhiteSpace(settings.LastCashier))
                TxtCashier.Text = settings.LastCashier;

            if (_receiptType == ReceiptType.Membership)
            {
                if (!string.IsNullOrWhiteSpace(settings.LastClubName))
                    TxtClubName.Text = settings.LastClubName;

                if (!string.IsNullOrWhiteSpace(settings.LastAcademicYear))
                    TxtAcademicYear.Text = settings.LastAcademicYear;
            }
        }

        // ── Sequential receipt number ─────────────────────────────────
        private string GenerateReceiptNumber()
        {
            string prefix = _receiptType == ReceiptType.Standard
                ? "STD" : "MEM";
            int counter = SettingsService.GetNextReceiptNumber(
                _receiptType == ReceiptType.Membership);
            return $"{prefix}-{DateTime.Now:yyyy}-{counter:D4}";
        }

        // ── Pre-fill from existing receipt ────────────────────────────
        private void PopulateFromReceipt(
            Receipt receipt, bool keepReceiptNumber)
        {
            if (keepReceiptNumber)
                TxtReceiptNumber.Text = receipt.ReceiptNumber;

            TxtIssuedTo.Text = receipt.IssuedTo;
            TxtCashier.Text = receipt.CashierName;
            TxtOrganization.Text = receipt.OrganizationName;
            TxtNotes.Text = receipt.Notes;
            TxtIdNumber.Text = receipt.IdNumber;
            DpDateIssued.SelectedDate = keepReceiptNumber
                ? receipt.DateIssued
                : DateTime.Today;

            if (receipt.Type == ReceiptType.Membership)
            {
                TxtClubName.Text = receipt.ClubName;
                TxtAcademicYear.Text = receipt.OrganizationName;

                foreach (ComboBoxItem item in CmbMembershipType.Items)
                {
                    if (item.Content?.ToString() == receipt.ClubName)
                    {
                        CmbMembershipType.SelectedItem = item;
                        break;
                    }
                }
            }

            ItemsPanel.Children.Clear();
            _itemRows.Clear();

            foreach (var item in receipt.Items)
                AddItemRow(
                    item.Description,
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
            var grid = new Grid { Margin = new Thickness(0, 0, 0, 6) };
            grid.ColumnDefinitions.Add(new ColumnDefinition
            { Width = new GridLength(1, GridUnitType.Star) });
            grid.ColumnDefinitions.Add(new ColumnDefinition
            { Width = new GridLength(60) });
            grid.ColumnDefinitions.Add(new ColumnDefinition
            { Width = new GridLength(80) });
            grid.ColumnDefinitions.Add(new ColumnDefinition
            { Width = new GridLength(80) });
            grid.ColumnDefinitions.Add(new ColumnDefinition
            { Width = new GridLength(36) });

            var descBox = MakeInputBox(desc);
            var qtyBox = MakeInputBox(qty, TextAlignment.Center);
            var priceBox = MakeInputBox(price, TextAlignment.Right);
            var totalBox = MakeInputBox("0.00", TextAlignment.Right,
                readOnly: true);

            var removeBtn = new Button
            {
                Content = "✕",
                Padding = new Thickness(6, 4, 6, 4),
                FontSize = 12,
                Background = new SolidColorBrush(
                    Color.FromRgb(231, 76, 60)),
                Foreground = new SolidColorBrush(Colors.White),
                BorderThickness = new Thickness(0),
                Cursor = System.Windows.Input.Cursors.Hand
            };

            qtyBox.PreviewTextInput += NumericOnly_PreviewTextInput;
            priceBox.PreviewTextInput += DecimalOnly_PreviewTextInput;

            descBox.LostFocus += (s, e) =>
                descBox.Text = InputSanitizer.SanitizeText(descBox.Text);

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
            string defaultText = "",
            TextAlignment align = TextAlignment.Left,
            bool readOnly = false)
        {
            return new TextBox
            {
                Text = defaultText,
                Padding = new Thickness(6, 5, 6, 5),
                FontSize = 13,
                BorderBrush = new SolidColorBrush(Colors.LightGray),
                Margin = new Thickness(0, 0, 4, 0),
                TextAlignment = align,
                IsReadOnly = readOnly,
                Background = readOnly
                    ? new SolidColorBrush(Color.FromRgb(245, 245, 245))
                    : new SolidColorBrush(Colors.White),
                MaxLength = 200
            };
        }

        private void NumericOnly_PreviewTextInput(object sender,
            System.Windows.Input.TextCompositionEventArgs e)
        {
            e.Handled = !System.Text.RegularExpressions.Regex
                .IsMatch(e.Text, @"^\d+$");
        }

        private void DecimalOnly_PreviewTextInput(object sender,
            System.Windows.Input.TextCompositionEventArgs e)
        {
            var box = sender as TextBox;
            bool isDigit = System.Text.RegularExpressions.Regex
                .IsMatch(e.Text, @"^\d+$");
            bool isDot = e.Text == "." &&
                           box != null &&
                           !box.Text.Contains(".");
            e.Handled = !(isDigit || isDot);
        }

        private void UpdateRowTotal(
            TextBox qtyBox, TextBox priceBox, TextBox totalBox)
        {
            bool qtyOk = int.TryParse(qtyBox.Text, out int qty);
            bool priceOk = decimal.TryParse(priceBox.Text, out decimal price);
            totalBox.Text = (qtyOk && priceOk)
                ? (qty * price).ToString("F2") : "0.00";
            RecalculateTotal();
        }

        private void RecalculateTotal()
        {
            decimal subtotal = 0;
            foreach (var row in _itemRows)
            {
                if (int.TryParse(row.Qty.Text, out int qty) &&
                    decimal.TryParse(row.Price.Text, out decimal price))
                    subtotal += qty * price;
            }
            TxtSubtotal.Text = $"PHP {subtotal:F2}";
            TxtTotal.Text = $"PHP {subtotal:F2}";
        }

        private void AddItem_Click(object sender, RoutedEventArgs e)
        {
            if (_itemRows.Count >= 20)
            {
                ShowValidation("You can only add up to 20 items.");
                return;
            }
            AddItemRow();
        }

        private void ClearForm_Click(object sender, RoutedEventArgs e)
        {
            var result = MessageBox.Show(
                "Are you sure you want to clear all fields?",
                "Clear Form",
                MessageBoxButton.YesNo,
                MessageBoxImage.Warning);

            if (result == MessageBoxResult.Yes)
            {
                TxtIssuedTo.Text = "";
                TxtCashier.Text = "";
                TxtOrganization.Text = "";
                TxtNotes.Text = "";
                TxtIdNumber.Text = "";
                TxtReceiptNumber.Text = _isEditMode && _editingReceipt != null
                    ? _editingReceipt.ReceiptNumber
                    : GenerateReceiptNumber();
                DpDateIssued.SelectedDate = DateTime.Today;

                if (_receiptType == ReceiptType.Membership)
                {
                    TxtClubName.Text = "";
                    TxtStudentId.Text = "";
                    TxtAcademicYear.Text = "";
                    CmbMembershipType.SelectedIndex = -1;
                }

                ItemsPanel.Children.Clear();
                _itemRows.Clear();
                AddItemRow();
                RecalculateTotal();
                HideValidation();
            }
        }

        private void PreviewReceipt_Click(object sender, RoutedEventArgs e)
        {
            if (!ValidateAndSanitizeForm()) return;


            SettingsService.SaveLastUsedFields(
                TxtOrganization.Text.Trim(),
                _receiptType == ReceiptType.Membership
                    ? TxtClubName.Text.Trim() : "",
                TxtCashier.Text.Trim(),
                _receiptType == ReceiptType.Membership
                    ? TxtAcademicYear.Text.Trim() : "");

            var receipt = BuildReceiptFromForm();
            NavigationService?.Navigate(
                new ReceiptPreviewPage(receipt,
                    fromHistory: false,
                    isEdit: _isEditMode));
        }

        private bool ValidateAndSanitizeForm()
        {
            HideValidation();
            var errors = new List<string>();

            TxtIssuedTo.Text = InputSanitizer.SanitizeName(TxtIssuedTo.Text);
            TxtCashier.Text = InputSanitizer.SanitizeName(TxtCashier.Text);
            TxtOrganization.Text = InputSanitizer.SanitizeText(TxtOrganization.Text);
            TxtNotes.Text = InputSanitizer.SanitizeText(TxtNotes.Text);
            TxtIdNumber.Text = InputSanitizer.SanitizeIdNumber(TxtIdNumber.Text);

            if (_receiptType == ReceiptType.Membership)
            {
                TxtClubName.Text =
                    InputSanitizer.SanitizeText(TxtClubName.Text);
                TxtStudentId.Text =
                    InputSanitizer.SanitizeIdNumber(TxtStudentId.Text);
                TxtAcademicYear.Text =
                    InputSanitizer.SanitizeText(TxtAcademicYear.Text);
            }

            var nameCheck = InputSanitizer.ValidateName(
                TxtIssuedTo.Text, "Issued To");
            if (!nameCheck.IsValid) errors.Add($"• {nameCheck.Error}");

            var cashierCheck = InputSanitizer.ValidateName(
                TxtCashier.Text, "Cashier / Issued By");
            if (!cashierCheck.IsValid) errors.Add($"• {cashierCheck.Error}");

            if (_receiptType == ReceiptType.Membership &&
                string.IsNullOrWhiteSpace(TxtClubName.Text))
                errors.Add("• Club / Organization Name is required.");

            var idCheck = InputSanitizer.ValidateIdNumber(TxtIdNumber.Text);
            if (!idCheck.IsValid) errors.Add($"• {idCheck.Error}");

            var notesCheck = InputSanitizer.ValidateText(
                TxtNotes.Text, "Notes", false, 300);
            if (!notesCheck.IsValid) errors.Add($"• {notesCheck.Error}");

            if (_itemRows.Count == 0)
            {
                errors.Add("• Please add at least one item.");
            }
            else
            {
                int rowNum = 1;
                foreach (var row in _itemRows)
                {
                    row.Desc.Text =
                        InputSanitizer.SanitizeText(row.Desc.Text);

                    if (string.IsNullOrWhiteSpace(row.Desc.Text))
                        errors.Add(
                            $"• Item {rowNum}: Description is required.");

                    var qtyCheck =
                        InputSanitizer.ValidateQuantity(row.Qty.Text);
                    if (!qtyCheck.IsValid)
                        errors.Add($"• Item {rowNum}: {qtyCheck.Error}");

                    var priceCheck =
                        InputSanitizer.ValidateAmount(row.Price.Text);
                    if (!priceCheck.IsValid)
                        errors.Add($"• Item {rowNum}: {priceCheck.Error}");

                    rowNum++;
                }
            }

            // Check for duplicate receipt number
            var db = new DatabaseService();
            int excludeId = _isEditMode && _editingReceipt != null
                ? _editingReceipt.Id : 0;

            if (db.ReceiptNumberExists(
                TxtReceiptNumber.Text.Trim(), excludeId))
            {
                errors.Add(
                    "• Receipt number already exists. " +
                    "A new one has been generated.");

                // Auto-generate a new unique number
                TxtReceiptNumber.Text = GenerateReceiptNumber();
            }

            if (errors.Count > 0)
            {
                ShowValidation(string.Join("\n", errors));
                return false;
            }

            return true;
        }

        private void ShowValidation(string message)
        {
            TxtValidation.Text = message;
            TxtValidation.Visibility = Visibility.Visible;
        }

        private void HideValidation()
        {
            TxtValidation.Visibility = Visibility.Collapsed;
        }

        private Receipt BuildReceiptFromForm()
        {
            var items = new List<ReceiptItem>();
            decimal total = 0;

            foreach (var row in _itemRows)
            {
                int.TryParse(row.Qty.Text, out int qty);
                decimal.TryParse(row.Price.Text, out decimal price);

                var item = new ReceiptItem
                {
                    Description = row.Desc.Text,
                    Quantity = qty,
                    UnitPrice = price
                };
                items.Add(item);
                total += item.Total;
            }

            return new Receipt
            {
                Id = _isEditMode && _editingReceipt != null
                                    ? _editingReceipt.Id : 0,
                ReceiptNumber = TxtReceiptNumber.Text,
                Type = _receiptType,
                IssuedTo = TxtIssuedTo.Text.Trim(),
                IdNumber = TxtIdNumber.Text.Trim(),
                OrganizationName = TxtOrganization.Text.Trim(),
                ClubName = _receiptType == ReceiptType.Membership
                                    ? TxtClubName.Text.Trim() : "",
                DateIssued = DpDateIssued.SelectedDate ?? DateTime.Today,
                Items = items,
                TotalAmount = total,
                Notes = TxtNotes.Text.Trim(),
                CashierName = TxtCashier.Text.Trim()
            };
        }
    }

}
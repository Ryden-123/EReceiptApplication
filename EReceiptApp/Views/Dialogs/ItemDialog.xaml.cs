using System;
using System.Collections.Generic;
using System.Windows;
using EReceiptApp.Services;
using static EReceiptApp.Services.DatabaseService;

namespace EReceiptApp.Views.Dialogs
{
    public partial class ItemDialog : Window
    {
        public string ItemName { get; private set; } = "";
        public double ItemPrice { get; private set; }
        public string ItemCategory { get; private set; } = "";
        public string ItemDescription { get; private set; } = "";

        private readonly PresetItem? _editingItem;
        private readonly bool _isEditMode;

        // ── New item ──────────────────────────────────────────────────
        public ItemDialog(List<string> categories)
        {
            InitializeComponent();
            _isEditMode = false;
            TxtDialogTitle.Text = "Add New Item";
            LoadCategories(categories);
            TxtPrice.Text = "0.00";

            TxtPrice.PreviewTextInput +=
                DecimalOnly_PreviewTextInput;
        }

        // ── Edit existing item ────────────────────────────────────────
        public ItemDialog(List<string> categories,
            PresetItem item)
        {
            InitializeComponent();
            _isEditMode = true;
            _editingItem = item;
            TxtDialogTitle.Text = "Edit Item";
            LoadCategories(categories);

            TxtName.Text = item.Name;
            TxtPrice.Text = item.DefaultPrice.ToString("F2");
            TxtDescription.Text = item.Description;

            // Set category in combobox
            CmbCategory.Text = item.Category;

            TxtPrice.PreviewTextInput +=
                DecimalOnly_PreviewTextInput;
        }

        private void LoadCategories(List<string> categories)
        {
            CmbCategory.Items.Clear();
            foreach (var cat in categories)
                CmbCategory.Items.Add(cat);
        }

        private void DecimalOnly_PreviewTextInput(
            object sender,
            System.Windows.Input.TextCompositionEventArgs e)
        {
            bool isDigit = System.Text.RegularExpressions
                .Regex.IsMatch(e.Text, @"^\d+$");
            bool isDot = e.Text == "." &&
                           !TxtPrice.Text.Contains(".");
            e.Handled = !(isDigit || isDot);
        }

        private void Save_Click(object sender, RoutedEventArgs e)
        {
            TxtError.Visibility = Visibility.Collapsed;

            // Validate name
            if (string.IsNullOrWhiteSpace(TxtName.Text))
            {
                ShowError("Item name is required.");
                return;
            }

            if (TxtName.Text.Trim().Length > 100)
            {
                ShowError("Item name must be under 100 characters.");
                return;
            }

            // Validate price
            if (!double.TryParse(TxtPrice.Text,
                out double price) || price < 0)
            {
                ShowError("Please enter a valid price.");
                return;
            }

            if (price > 999999)
            {
                ShowError("Price cannot exceed 999,999.");
                return;
            }

            ItemName = TxtName.Text.Trim();
            ItemPrice = price;
            ItemCategory = CmbCategory.Text.Trim();
            ItemDescription = TxtDescription.Text.Trim();

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
    }
}
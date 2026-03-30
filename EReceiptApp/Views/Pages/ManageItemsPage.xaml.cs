using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using EReceiptApp.Services;
using static EReceiptApp.Services.DatabaseService;

namespace EReceiptApp.Views.Pages
{
    public partial class ManageItemsPage : Page
    {
        private readonly DatabaseService _db =
            new DatabaseService();
        private List<PresetItem> _allItems =
            new List<PresetItem>();
        private List<PresetItem> _filteredItems =
            new List<PresetItem>();
        private string _currentCategory = "All";

        public ManageItemsPage()
        {
            InitializeComponent();
            Loaded += Page_Loaded;
        }

        private void Page_Loaded(object sender, RoutedEventArgs e)
        {
            LoadItems();
            LoadCategories();
        }

        // ── Load all items ────────────────────────────────────────────
        private void LoadItems()
        {
            ItemsPanel.Children.Clear();
            _allItems = _db.GetPresetItems();
            ApplyFilter();
            UpdateStats();
        }

        // ── Load category dropdown ────────────────────────────────────
        private void LoadCategories()
        {
            CmbCategory.Items.Clear();
            CmbCategory.Items.Add("All Categories");

            var categories = _db.GetPresetCategories();
            foreach (var cat in categories)
                CmbCategory.Items.Add(cat);

            CmbCategory.SelectedIndex = 0;
        }

        // ── Apply search + category filter ────────────────────────────
        private void ApplyFilter()
        {
            string query = TxtSearch.Text.Trim().ToLower();

            _filteredItems = _allItems.Where(item =>
            {
                bool catMatch = _currentCategory == "All" ||
                    item.Category == _currentCategory;

                bool searchMatch =
                    string.IsNullOrWhiteSpace(query) ||
                    item.Name.ToLower().Contains(query) ||
                    item.Category.ToLower().Contains(query) ||
                    item.Description.ToLower().Contains(query);

                return catMatch && searchMatch;
            }).ToList();

            RenderItems();
            TxtShowingCount.Text = _filteredItems.Count.ToString();
        }

        // ── Update stats ──────────────────────────────────────────────
        private void UpdateStats()
        {
            TxtTotalCount.Text = _allItems.Count.ToString();
            TxtCategoryCount.Text =
                _allItems.Select(i => i.Category)
                         .Where(c => !string.IsNullOrWhiteSpace(c))
                         .Distinct()
                         .Count()
                         .ToString();
            TxtShowingCount.Text = _filteredItems.Count.ToString();
        }

        // ── Render item rows ──────────────────────────────────────────
        private void RenderItems()
        {
            ItemsPanel.Children.Clear();

            if (_filteredItems.Count == 0)
            {
                EmptyPanel.Visibility = Visibility.Visible;
                return;
            }

            EmptyPanel.Visibility = Visibility.Collapsed;

            foreach (var item in _filteredItems)
                ItemsPanel.Children.Add(BuildRow(item));
        }

        // ── Build a single item row ───────────────────────────────────
        private Border BuildRow(PresetItem item)
        {
            var border = new Border
            {
                BorderBrush = new SolidColorBrush(
                    Color.FromRgb(232, 228, 248)),
                BorderThickness = new Thickness(0, 0, 0, 1)
            };

            border.MouseEnter += (s, e) =>
                border.Background = (SolidColorBrush)Application
                    .Current.Resources["AppSurfaceAlt"];
            border.MouseLeave += (s, e) =>
                border.Background = null;

            var grid = new Grid
            { Margin = new Thickness(0, 2, 0, 2) };
            grid.ColumnDefinitions.Add(new ColumnDefinition
            { Width = new GridLength(1, GridUnitType.Star) });
            grid.ColumnDefinitions.Add(new ColumnDefinition
            { Width = new GridLength(120) });
            grid.ColumnDefinitions.Add(new ColumnDefinition
            { Width = new GridLength(100) });
            grid.ColumnDefinitions.Add(new ColumnDefinition
            { Width = new GridLength(200) });
            grid.ColumnDefinitions.Add(new ColumnDefinition
            { Width = new GridLength(100) });

            // Name
            grid.Children.Add(MakeCell(
                item.Name, 0,
                new Thickness(16, 12, 8, 12),
                bold: true));

            // Category badge
            var badge = new Border
            {
                CornerRadius = new CornerRadius(8),
                Padding = new Thickness(8, 3, 8, 3),
                HorizontalAlignment = HorizontalAlignment.Left,
                VerticalAlignment = VerticalAlignment.Center,
                Background = GetCategoryColor(item.Category)
            };
            badge.Child = new TextBlock
            {
                Text = string.IsNullOrWhiteSpace(item.Category)
                    ? "Uncategorized" : item.Category,
                FontSize = 11,
                FontWeight = FontWeights.SemiBold,
                Foreground = GetCategoryTextColor(item.Category)
            };
            Grid.SetColumn(badge, 1);
            grid.Children.Add(badge);

            // Price
            grid.Children.Add(MakeCell(
                $"₱{item.DefaultPrice:F2}", 2, bold: true));

            // Description
            grid.Children.Add(MakeCell(
                string.IsNullOrWhiteSpace(item.Description)
                    ? "—" : item.Description,
                3, muted: string.IsNullOrWhiteSpace(
                    item.Description)));

            // Action buttons
            var actions = new StackPanel
            {
                Orientation = Orientation.Horizontal,
                VerticalAlignment = VerticalAlignment.Center,
                Margin = new Thickness(4, 0, 8, 0)
            };

            var editBtn = MakeActionButton("✏", "outline", "Edit");
            var delBtn = MakeActionButton("🗑", "danger", "Delete");
            delBtn.Margin = new Thickness(0);

            editBtn.Click += (s, e) => EditItem(item);
            delBtn.Click += (s, e) => DeleteItem(item, border);

            actions.Children.Add(editBtn);
            actions.Children.Add(delBtn);

            Grid.SetColumn(actions, 4);
            grid.Children.Add(actions);

            border.Child = grid;
            return border;
        }

        // ── Category badge colors ─────────────────────────────────────
        private SolidColorBrush GetCategoryColor(string category)
        {
            return category switch
            {
                "Food" => new SolidColorBrush(
                    Color.FromRgb(232, 245, 233)),
                "Fees" => new SolidColorBrush(
                    Color.FromRgb(240, 236, 255)),
                "Merchandise" => new SolidColorBrush(
                    Color.FromRgb(255, 243, 224)),
                _ => new SolidColorBrush(
                    Color.FromRgb(240, 240, 245))
            };
        }

        private SolidColorBrush GetCategoryTextColor(string category)
        {
            return category switch
            {
                "Food" => new SolidColorBrush(
                    Color.FromRgb(46, 125, 50)),
                "Fees" => new SolidColorBrush(
                    Color.FromRgb(92, 74, 187)),
                "Merchandise" => new SolidColorBrush(
                    Color.FromRgb(230, 81, 0)),
                _ => new SolidColorBrush(
                    Color.FromRgb(100, 100, 120))
            };
        }

        // ── Add item ──────────────────────────────────────────────────
        private void AddItem_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new Views.Dialogs.ItemDialog(
                _db.GetPresetCategories())
            {
                Owner = Window.GetWindow(this)
            };

            if (dialog.ShowDialog() != true) return;

            var newItem = new PresetItem
            {
                Name = dialog.ItemName,
                DefaultPrice = dialog.ItemPrice,
                Category = dialog.ItemCategory,
                Description = dialog.ItemDescription
            };

            if (_db.PresetItemNameExists(newItem.Name))
            {
                MessageBox.Show(
                    $"An item named '{newItem.Name}' already exists.",
                    "Duplicate Item",
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning);
                return;
            }

            _db.AddPresetItem(newItem);
            LoadItems();
            LoadCategories();

            MessageBox.Show(
                $"'{newItem.Name}' added successfully!",
                "Item Added",
                MessageBoxButton.OK,
                MessageBoxImage.Information);
        }

        // ── Edit item ─────────────────────────────────────────────────
        private void EditItem(PresetItem item)
        {
            var dialog = new Views.Dialogs.ItemDialog(
                _db.GetPresetCategories(), item)
            {
                Owner = Window.GetWindow(this)
            };

            if (dialog.ShowDialog() != true) return;

            item.Name = dialog.ItemName;
            item.DefaultPrice = dialog.ItemPrice;
            item.Category = dialog.ItemCategory;
            item.Description = dialog.ItemDescription;

            if (_db.PresetItemNameExists(item.Name, item.Id))
            {
                MessageBox.Show(
                    $"An item named '{item.Name}' already exists.",
                    "Duplicate Item",
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning);
                return;
            }

            _db.UpdatePresetItem(item);
            LoadItems();
            LoadCategories();
        }

        // ── Delete item ───────────────────────────────────────────────
        private void DeleteItem(PresetItem item, Border row)
        {
            var result = MessageBox.Show(
                $"Delete '{item.Name}' from the preset list?\n\n" +
                "This will not affect existing receipts.",
                "Delete Item",
                MessageBoxButton.YesNo,
                MessageBoxImage.Warning);

            if (result != MessageBoxResult.Yes) return;

            _db.DeletePresetItem(item.Id);
            ItemsPanel.Children.Remove(row);
            _allItems.RemoveAll(i => i.Id == item.Id);
            _filteredItems.RemoveAll(i => i.Id == item.Id);
            UpdateStats();

            if (_filteredItems.Count == 0)
                EmptyPanel.Visibility = Visibility.Visible;
        }

        // ── Search ────────────────────────────────────────────────────
        private void Search_Changed(
            object sender, TextChangedEventArgs e)
        {
            ApplyFilter();
        }

        // ── Category filter ───────────────────────────────────────────
        private void Category_Changed(
            object sender, SelectionChangedEventArgs e)
        {
            if (CmbCategory.SelectedItem == null) return;

            string selected =
                CmbCategory.SelectedItem.ToString() ?? "All";
            _currentCategory = selected == "All Categories"
                ? "All" : selected;

            ApplyFilter();
        }

        // ── Helpers ───────────────────────────────────────────────────
        private TextBlock MakeCell(
            string text, int column,
            Thickness? margin = null,
            bool bold = false,
            bool muted = false)
        {
            var tb = new TextBlock
            {
                Text = text,
                FontSize = 13,
                FontWeight = bold
                    ? FontWeights.SemiBold : FontWeights.Normal,
                Foreground = muted
                    ? (SolidColorBrush)Application.Current
                        .Resources["AppTextMuted"]
                    : (SolidColorBrush)Application.Current
                        .Resources["AppText"],
                VerticalAlignment = VerticalAlignment.Center,
                TextTrimming = TextTrimming.CharacterEllipsis,
                Margin = margin ?? new Thickness(0, 12, 8, 12)
            };
            Grid.SetColumn(tb, column);
            return tb;
        }

        private Button MakeActionButton(
            string emoji, string style, string tooltip)
        {
            var btn = new Button
            {
                Content = emoji,
                Width = 30,
                Height = 30,
                FontSize = 14,
                BorderThickness = new Thickness(1),
                Cursor = Cursors.Hand,
                VerticalAlignment = VerticalAlignment.Center,
                Margin = new Thickness(0, 0, 4, 0),
                Padding = new Thickness(0),
                ToolTip = tooltip
            };

            switch (style)
            {
                case "danger":
                    btn.Background = new SolidColorBrush(
                        Color.FromRgb(255, 240, 240));
                    btn.Foreground = new SolidColorBrush(
                        Color.FromRgb(198, 40, 40));
                    btn.BorderBrush = new SolidColorBrush(
                        Color.FromRgb(255, 205, 210));
                    break;
                default:
                    btn.Background = (SolidColorBrush)Application
                        .Current.Resources["AppSurface"];
                    btn.Foreground = (SolidColorBrush)Application
                        .Current.Resources["AppText"];
                    btn.BorderBrush = (SolidColorBrush)Application
                        .Current.Resources["AppBorder"];
                    break;
            }

            return btn;
        }
    }
}
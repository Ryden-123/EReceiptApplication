using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using EReceiptApp.Models;
using EReceiptApp.Services;

namespace EReceiptApp.Views.Pages
{
    public partial class ReceiptsListPage : Page
    {
        private readonly DatabaseService _db = new DatabaseService();
        private List<Receipt> _allReceipts = new List<Receipt>();
        private List<Receipt> _filteredReceipts = new List<Receipt>();
        private readonly List<(CheckBox Chk, Receipt Receipt, Border Row)>
    _selectableRows =
    new List<(CheckBox, Receipt, Border)>();

        public ReceiptsListPage()
        {
            InitializeComponent();
            Loaded += Page_Loaded;
        }

        private void Page_Loaded(object sender, RoutedEventArgs e)
        {
            ReceiptsPanel.Children.Clear();
            LoadingPanel.Visibility = Visibility.Visible;
            TxtEmpty.Visibility = Visibility.Collapsed;
            LoadReceipts();
        }

        private void LoadReceipts()
        {
            try
            {
                _allReceipts = _db.GetAllReceipts();
                ApplyFilter();
                UpdateStats();
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    $"Could not load receipts: {ex.Message}",
                    "Error", MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
            finally
            {
                LoadingPanel.Visibility = Visibility.Collapsed;
            }
        }


        // ── Update stat cards ─────────────────────────────────────────
        private void UpdateStats()
        {
            StatTotal.Text = _allReceipts.Count.ToString();
            StatStandard.Text = _allReceipts.Count.ToString();
            StatMembership.Text = "—";
            StatAmount.Text =
                $"₱{_allReceipts.Sum(r => r.TotalAmount):F2}";
        }

        // ── Apply current filter + search ─────────────────────────────
        private void ApplyFilter()
        {
            var query = TxtSearch.Text.Trim().ToLower();

            _filteredReceipts = _allReceipts.Where(r =>
            {
                // Type filter
                bool typeMatch = true;

                // Search filter
                bool searchMatch = string.IsNullOrWhiteSpace(query)
                    || r.IssuedTo.ToLower().Contains(query)
                    || r.ReceiptNumber.ToLower().Contains(query)
                    || r.IdNumber.ToLower().Contains(query)
                    || r.OrganizationName.ToLower().Contains(query);

                return typeMatch && searchMatch;
            }).ToList();

            RenderRows();
        }

        // ── Render receipt rows ───────────────────────────────────────
        private void RenderRows()
        {
            ReceiptsPanel.Children.Clear();
            _selectableRows.Clear();          // ← add this
            ChkSelectAll.IsChecked = false;   // ← and this
            BulkActionBar.Visibility = Visibility.Collapsed;

            if (_filteredReceipts.Count == 0)
            {
                TxtEmpty.Visibility = Visibility.Visible;
                return;
            }
            TxtEmpty.Visibility = Visibility.Collapsed;
            foreach (var r in _filteredReceipts)
                ReceiptsPanel.Children.Add(BuildRow(r));
        }

        // ── Build a single row ────────────────────────────────────────
        private Border BuildRow(Receipt receipt)
        {
            var border = new Border
            {
                BorderBrush = new SolidColorBrush(
                    Color.FromRgb(232, 228, 248)),
                BorderThickness = new Thickness(0, 0, 0, 1),
                Cursor = System.Windows.Input.Cursors.Hand
            };

            border.MouseEnter += (s, e) =>
                border.Background = (SolidColorBrush)Application
                    .Current.Resources["AppSurfaceAlt"];
            border.MouseLeave += (s, e) =>
                border.Background = null;

            // Click on row opens receipt (but not on buttons/checkbox)
            border.MouseLeftButtonUp += (s, e) =>
            {
                if (e.OriginalSource is CheckBox ||
                    e.OriginalSource is Button) return;
                ViewReceipt(receipt);
            };

            var grid = new Grid { Margin = new Thickness(0, 2, 0, 2) };
            grid.ColumnDefinitions.Add(new ColumnDefinition
            { Width = new GridLength(36) });
            grid.ColumnDefinitions.Add(new ColumnDefinition
            { Width = new GridLength(165) });
            grid.ColumnDefinitions.Add(new ColumnDefinition
            { Width = new GridLength(1, GridUnitType.Star) });
            grid.ColumnDefinitions.Add(new ColumnDefinition
            { Width = new GridLength(110) });
            grid.ColumnDefinitions.Add(new ColumnDefinition
            { Width = new GridLength(90) });
            grid.ColumnDefinitions.Add(new ColumnDefinition
            { Width = new GridLength(150) });

            // Checkbox
            var chk = new CheckBox
            {
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center,
                Margin = new Thickness(0)
            };
            chk.Checked += (s, e) => UpdateBulkBar();
            chk.Unchecked += (s, e) => UpdateBulkBar();
            Grid.SetColumn(chk, 0);
            grid.Children.Add(chk);

            // Track this row for bulk selection
            _selectableRows.Add((chk, receipt, border));

            // Receipt number
            grid.Children.Add(MakeCell(
                receipt.ReceiptNumber, 1,
                new Thickness(8, 10, 4, 10), bold: true));

            // Name + ID
            var nameStack = new StackPanel
            {
                VerticalAlignment = VerticalAlignment.Center,
                Margin = new Thickness(0, 8, 8, 8)
            };
            nameStack.Children.Add(new TextBlock
            {
                Text = receipt.IssuedTo,
                FontSize = 13,
                FontWeight = FontWeights.SemiBold,
                Foreground = (SolidColorBrush)Application.Current
                                   .Resources["AppText"],
                TextTrimming = TextTrimming.CharacterEllipsis
            });
            if (!string.IsNullOrWhiteSpace(receipt.IdNumber))
                nameStack.Children.Add(new TextBlock
                {
                    Text = receipt.IdNumber,
                    FontSize = 11,
                    Foreground = (SolidColorBrush)Application.Current
                                     .Resources["AppTextMuted"]
                });
            Grid.SetColumn(nameStack, 2);
            grid.Children.Add(nameStack);

            grid.Children.Add(MakeCell(
                receipt.DateIssued.ToString("MMM dd, yyyy"), 3));
            grid.Children.Add(MakeCell(
                $"₱{receipt.TotalAmount:F2}", 4, bold: true));

            // Action buttons
            var actions = new StackPanel
            {
                Orientation = Orientation.Horizontal,
                VerticalAlignment = VerticalAlignment.Center,
                Margin = new Thickness(4, 0, 4, 0)
            };

            var viewBtn = MakeIconButton("👁", "accent", "View");
            var dupBtn = MakeIconButton("⧉", "outline", "Duplicate");
            var editBtn = MakeIconButton("✏", "outline", "Edit");
            var delBtn = MakeIconButton("🗑", "danger", "Delete");
            delBtn.Margin = new Thickness(0);

            viewBtn.Click += (s, e) => ViewReceipt(receipt);
            dupBtn.Click += (s, e) => DuplicateReceipt(receipt);
            editBtn.Click += (s, e) => EditReceipt(receipt);
            delBtn.Click += (s, e) => DeleteReceipt(receipt, border);

            actions.Children.Add(viewBtn);
            actions.Children.Add(dupBtn);
            actions.Children.Add(editBtn);
            actions.Children.Add(delBtn);

            Grid.SetColumn(actions, 5);
            grid.Children.Add(actions);

            border.Child = grid;
            return border;
        }

        // ── Action button factory ─────────────────────────────────────────
        private Button MakeIconButton(
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
                case "accent":
                    btn.Background = (SolidColorBrush)
                        Application.Current.Resources["AppAccent"];
                    btn.Foreground = new SolidColorBrush(Colors.White);
                    btn.BorderBrush = (SolidColorBrush)
                        Application.Current.Resources["AppAccent"];
                    break;

                case "danger":
                    btn.Background = new SolidColorBrush(
                        Color.FromRgb(255, 240, 240));
                    btn.Foreground = new SolidColorBrush(
                        Color.FromRgb(198, 40, 40));
                    btn.BorderBrush = new SolidColorBrush(
                        Color.FromRgb(255, 205, 210));
                    break;

                default: // outline
                    btn.Background = (SolidColorBrush)
                        Application.Current.Resources["AppSurface"];
                    btn.Foreground = (SolidColorBrush)
                        Application.Current.Resources["AppText"];
                    btn.BorderBrush = (SolidColorBrush)
                        Application.Current.Resources["AppBorder"];
                    break;
            }

            return btn;
        }

        // ── Helper: make a table cell TextBlock ───────────────────────
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

        // ── Navigate to receipt preview ───────────────────────────────
        private void ViewReceipt(Receipt receipt)
        {
            NavigationService?.Navigate(
                new ReceiptPreviewPage(receipt, fromHistory: true));
        }

        private void EditReceipt(Receipt receipt)
        {
            NavigationService?.Navigate(
                new ReceiptBuilderPage(receipt));
        }

        private void DuplicateReceipt(Receipt receipt)
        {
            var result = MessageBox.Show(
                $"Create a duplicate of receipt {receipt.ReceiptNumber}\n" +
                $"for {receipt.IssuedTo}?\n\n" +
                $"A new receipt number will be assigned.",
                "Duplicate Receipt",
                MessageBoxButton.YesNo,
                MessageBoxImage.Question);

            if (result == MessageBoxResult.Yes)
                NavigationService?.Navigate(
                    new ReceiptBuilderPage(receipt, isDuplicate: true));
        }

        private void DeleteReceipt(Receipt receipt, Border row)
        {
            var result = MessageBox.Show(
                $"Move receipt {receipt.ReceiptNumber} to Trash?\n\n" +
                $"You can restore it later from the Trash page.",
                "Move to Trash",
                MessageBoxButton.YesNo,
                MessageBoxImage.Warning);

            if (result != MessageBoxResult.Yes) return;

            try
            {
                _db.SoftDeleteReceipt(receipt.Id);

                ReceiptsPanel.Children.Remove(row);
                _allReceipts.RemoveAll(r => r.Id == receipt.Id);
                _filteredReceipts.RemoveAll(r => r.Id == receipt.Id);

                UpdateStats();

                if (_filteredReceipts.Count == 0)
                    TxtEmpty.Visibility = Visibility.Visible;
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    $"Could not move to trash: {ex.Message}",
                    "Error", MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
        }

        // ── Search ────────────────────────────────────────────────────
        private void TxtSearch_TextChanged(object sender,
            TextChangedEventArgs e)
        {
            ApplyFilter();
        }

        // ── Filter buttons ────────────────────────────────────────────
        private void FilterAll_Click(object sender, RoutedEventArgs e)
        {
            UpdateFilterButtons(BtnAll);
            ApplyFilter();
        }

        private void Refresh_Click(object sender, RoutedEventArgs e)
        {
            TxtSearch.Text = "";

            UpdateFilterButtons(BtnAll);
            LoadReceipts();
        }

        private void UpdateFilterButtons(Button active)
        {
            // Only BtnAll remains
            BtnAll.Background = (SolidColorBrush)
                Application.Current.Resources["AppAccent"];
            BtnAll.Foreground =
                new SolidColorBrush(Colors.White);
            BtnAll.BorderBrush = (SolidColorBrush)
                Application.Current.Resources["AppAccent"];
        }

        private void ViewTrash_Click(object sender, RoutedEventArgs e)
        {
            NavigationService?.Navigate(new TrashPage());
        }

        private void UpdateBulkBar()
        {
            int selected = _selectableRows
                .Count(r => r.Chk.IsChecked == true);

            if (selected > 0)
            {
                BulkActionBar.Visibility = Visibility.Visible;
                TxtSelectedCount.Text =
                    $"{selected} receipt{(selected == 1 ? "" : "s")} selected";
            }
            else
            {
                BulkActionBar.Visibility = Visibility.Collapsed;
            }
        }

        private void ChkSelectAll_Changed(
            object sender, RoutedEventArgs e)
        {
            bool check = ChkSelectAll.IsChecked == true;
            foreach (var row in _selectableRows)
                row.Chk.IsChecked = check;
            UpdateBulkBar();
        }

        private void BulkDelete_Click(object sender, RoutedEventArgs e)
        {
            var selected = _selectableRows
                .Where(r => r.Chk.IsChecked == true)
                .ToList();

            if (selected.Count == 0) return;

            var result = MessageBox.Show(
                $"Move {selected.Count} receipt(s) to Trash?",
                "Bulk Delete",
                MessageBoxButton.YesNo,
                MessageBoxImage.Warning);

            if (result != MessageBoxResult.Yes) return;

            var ids = selected.Select(r => r.Receipt.Id).ToList();
            _db.SoftDeleteMultiple(ids);

            foreach (var row in selected)
            {
                ReceiptsPanel.Children.Remove(row.Row);
                _allReceipts.RemoveAll(r => r.Id == row.Receipt.Id);
                _filteredReceipts.RemoveAll(r => r.Id == row.Receipt.Id);
                _selectableRows.RemoveAll(r => r.Receipt.Id == row.Receipt.Id);
            }

            UpdateStats();
            UpdateBulkBar();
            ChkSelectAll.IsChecked = false;

            if (_filteredReceipts.Count == 0)
                TxtEmpty.Visibility = Visibility.Visible;
        }

        private void ClearSelection_Click(
            object sender, RoutedEventArgs e)
        {
            foreach (var row in _selectableRows)
                row.Chk.IsChecked = false;
            ChkSelectAll.IsChecked = false;
            UpdateBulkBar();
        }


    }
}
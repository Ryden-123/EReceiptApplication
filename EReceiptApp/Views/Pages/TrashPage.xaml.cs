using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using EReceiptApp.Models;
using EReceiptApp.Services;

namespace EReceiptApp.Views.Pages
{
    public partial class TrashPage : Page
    {
        private readonly DatabaseService _db = new DatabaseService();
        private List<Receipt> _deletedReceipts = new List<Receipt>();
        private readonly List<(CheckBox Chk, Receipt Receipt,
    StackPanel Row)> _trashSelectableRows =
    new List<(CheckBox, Receipt, StackPanel)>();

        public TrashPage()
        {
            InitializeComponent();
            Loaded += Page_Loaded;
        }

        private void Page_Loaded(object sender, RoutedEventArgs e)
        {
            TrashPanel.Children.Clear();
            LoadTrash();
        }

        private void LoadTrash()
        {
            _trashSelectableRows.Clear();
            _deletedReceipts = _db.GetDeletedReceipts();

            if (_deletedReceipts.Count == 0)
            {
                EmptyPanel.Visibility = Visibility.Visible;
                TablePanel.Visibility = Visibility.Collapsed;
                return;
            }

            EmptyPanel.Visibility = Visibility.Collapsed;
            TablePanel.Visibility = Visibility.Visible;

            foreach (var receipt in _deletedReceipts)
                TrashPanel.Children.Add(BuildRow(receipt));
        }

        private Border BuildRow(Receipt receipt)
        {
            var outerStack = new StackPanel();

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

            // Build the row grid (same as before)
            var grid = new Grid { Margin = new Thickness(0, 2, 0, 2) };
            grid.ColumnDefinitions.Add(new ColumnDefinition
            { Width = new GridLength(155) });
            grid.ColumnDefinitions.Add(new ColumnDefinition
            { Width = new GridLength(1, GridUnitType.Star) });
            grid.ColumnDefinitions.Add(new ColumnDefinition
            { Width = new GridLength(140) });
            grid.ColumnDefinitions.Add(new ColumnDefinition
            { Width = new GridLength(90) });
            grid.ColumnDefinitions.Add(new ColumnDefinition
            { Width = new GridLength(160) });

            grid.Children.Add(MakeCell(
                receipt.ReceiptNumber, 0,
                new Thickness(16, 10, 4, 10), bold: true));

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
            Grid.SetColumn(nameStack, 1);
            grid.Children.Add(nameStack);

            grid.Children.Add(MakeCell(
                receipt.DateIssued.ToString("MMM dd, yyyy"), 2,
                muted: true));
            grid.Children.Add(MakeCell(
                $"₱{receipt.TotalAmount:F2}", 3, bold: true));

            // Action buttons
            var actions = new StackPanel
            {
                Orientation = Orientation.Horizontal,
                VerticalAlignment = VerticalAlignment.Center,
                Margin = new Thickness(4, 0, 8, 0)
            };

            var restoreBtn = MakeActionButton("↩ Restore", "restore");
            var deleteBtn = MakeActionButton("🗑 Delete", "danger");
            restoreBtn.ToolTip = "Restore";
            deleteBtn.ToolTip = "Permanently delete";

            // ── Inline preview panel (hidden by default) ──────────────
            var previewPanel = new Border
            {
                Background = (SolidColorBrush)Application.Current
                                      .Resources["AppSurfaceAlt"],
                BorderBrush = (SolidColorBrush)Application.Current
                                      .Resources["AppBorder"],
                BorderThickness = new Thickness(0, 1, 0, 0),
                Padding = new Thickness(16, 12, 16, 12),
                Visibility = Visibility.Collapsed
            };

            var previewContent = BuildPreviewContent(receipt);
            previewPanel.Child = previewContent;

            // Toggle preview on row click
            bool expanded = false;
            border.MouseLeftButtonUp += (s, e) =>
            {
                if (e.OriginalSource is Button) return;
                expanded = !expanded;
                previewPanel.Visibility = expanded
                    ? Visibility.Visible
                    : Visibility.Collapsed;
            };

            restoreBtn.Click += (s, e) =>
                RestoreReceipt(receipt, outerStack);
            deleteBtn.Click += (s, e) =>
                PermanentlyDelete(receipt, outerStack);

            actions.Children.Add(restoreBtn);
            actions.Children.Add(deleteBtn);

            Grid.SetColumn(actions, 4);
            grid.Children.Add(actions);

            border.Child = grid;
            outerStack.Children.Add(border);
            outerStack.Children.Add(previewPanel);

            return new Border
            {
                Child = outerStack,
                BorderBrush = new SolidColorBrush(
                    Color.FromRgb(232, 228, 248)),
                BorderThickness = new Thickness(0, 0, 0, 1)
            };
        }

        private StackPanel BuildPreviewContent(Receipt receipt)
        {
            var stack = new StackPanel();

            void AddRow(string label, string value)
            {
                if (string.IsNullOrWhiteSpace(value)) return;
                var g = new Grid { Margin = new Thickness(0, 3, 0, 3) };
                g.ColumnDefinitions.Add(new ColumnDefinition
                { Width = new GridLength(160) });
                g.ColumnDefinitions.Add(new ColumnDefinition
                { Width = new GridLength(1, GridUnitType.Star) });

                var lbl = new TextBlock
                {
                    Text = label,
                    FontSize = 12,
                    Foreground = (SolidColorBrush)Application.Current
                                     .Resources["AppTextMuted"]
                };
                var val = new TextBlock
                {
                    Text = value,
                    FontSize = 12,
                    FontWeight = FontWeights.SemiBold,
                    Foreground = (SolidColorBrush)Application.Current
                                     .Resources["AppText"]
                };
                Grid.SetColumn(lbl, 0);
                Grid.SetColumn(val, 1);
                g.Children.Add(lbl);
                g.Children.Add(val);
                stack.Children.Add(g);
            }

            AddRow("Cashier:", receipt.CashierName);
            AddRow("Organization:", receipt.OrganizationName);
            AddRow("ID Number:", receipt.IdNumber);
            AddRow("Date:", receipt.DateIssued.ToString("MMMM dd, yyyy"));
            AddRow("Total:", $"₱{receipt.TotalAmount:F2}");

            if (receipt.Items.Count > 0)
            {
                stack.Children.Add(new TextBlock
                {
                    Text = "Items:",
                    FontSize = 12,
                    FontWeight = FontWeights.SemiBold,
                    Foreground = (SolidColorBrush)Application.Current
                                     .Resources["AppTextMuted"],
                    Margin = new Thickness(0, 6, 0, 4)
                });

                foreach (var item in receipt.Items)
                {
                    stack.Children.Add(new TextBlock
                    {
                        Text = $"  • {item.Description} " +
                                     $"x{item.Quantity} — " +
                                     $"₱{item.Total:F2}",
                        FontSize = 12,
                        Foreground = (SolidColorBrush)Application.Current
                                         .Resources["AppText"],
                        Margin = new Thickness(0, 2, 0, 2)
                    });
                }
            }

            if (!string.IsNullOrWhiteSpace(receipt.Notes))
                AddRow("Notes:", receipt.Notes);

            return stack;
        }

        private void RestoreReceipt(Receipt receipt, StackPanel row)
        {
            var result = MessageBox.Show(
                $"Restore receipt {receipt.ReceiptNumber}?",
                "Restore Receipt",
                MessageBoxButton.YesNo,
                MessageBoxImage.Question);

            if (result != MessageBoxResult.Yes) return;

            try
            {
                _db.RestoreReceipt(receipt.Id);
                TrashPanel.Children.Remove(row);
                _deletedReceipts.RemoveAll(r => r.Id == receipt.Id);

                if (_deletedReceipts.Count == 0)
                {
                    EmptyPanel.Visibility = Visibility.Visible;
                    TablePanel.Visibility = Visibility.Collapsed;
                }

                MessageBox.Show("Receipt restored successfully!",
                    "Restored", MessageBoxButton.OK,
                    MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Could not restore: {ex.Message}",
                    "Error", MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
        }

        private void PermanentlyDelete(Receipt receipt, StackPanel row)
        {
            var result = MessageBox.Show(
                $"Permanently delete {receipt.ReceiptNumber}?\n\n" +
                "This CANNOT be undone.",
                "Permanent Delete",
                MessageBoxButton.YesNo,
                MessageBoxImage.Warning);

            if (result != MessageBoxResult.Yes) return;

            try
            {
                _db.PermanentlyDeleteReceipt(receipt.Id);
                TrashPanel.Children.Remove(row);
                _deletedReceipts.RemoveAll(r => r.Id == receipt.Id);

                if (_deletedReceipts.Count == 0)
                {
                    EmptyPanel.Visibility = Visibility.Visible;
                    TablePanel.Visibility = Visibility.Collapsed;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Could not delete: {ex.Message}",
                    "Error", MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
        }

        private void EmptyTrash_Click(object sender, RoutedEventArgs e)
        {
            if (_deletedReceipts.Count == 0)
            {
                MessageBox.Show(
                    "Trash is already empty!",
                    "Empty Trash",
                    MessageBoxButton.OK,
                    MessageBoxImage.Information);
                return;
            }

            var result = MessageBox.Show(
                $"Permanently delete all " +
                $"{_deletedReceipts.Count} receipts in trash?\n\n" +
                "This CANNOT be undone.",
                "Empty Trash",
                MessageBoxButton.YesNo,
                MessageBoxImage.Warning);

            if (result != MessageBoxResult.Yes) return;

            try
            {
                _db.EmptyTrash();
                TrashPanel.Children.Clear();
                _deletedReceipts.Clear();
                EmptyPanel.Visibility = Visibility.Visible;
                TablePanel.Visibility = Visibility.Collapsed;
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    $"Could not empty trash: {ex.Message}",
                    "Error",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
        }

        private void Back_Click(object sender, RoutedEventArgs e)
        {
            NavigationService?.Navigate(new ReceiptsListPage());
        }

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
                Margin = margin ?? new Thickness(0, 10, 8, 10)
            };
            Grid.SetColumn(tb, column);
            return tb;
        }

        private Button MakeActionButton(string text, string style)
        {
            var btn = new Button
            {
                Content = text,
                Padding = new Thickness(10, 5, 10, 5),
                FontSize = 11,
                BorderThickness = new Thickness(1),
                Cursor = Cursors.Hand,
                VerticalAlignment = VerticalAlignment.Center,
                Margin = new Thickness(0, 0, 6, 0)
            };

            switch (style)
            {
                case "restore":
                    btn.Background = (SolidColorBrush)Application
                                          .Current.Resources["AppSurface"];
                    btn.Foreground = (SolidColorBrush)Application
                                          .Current.Resources["AppText"];
                    btn.BorderBrush = (SolidColorBrush)Application
                                          .Current.Resources["AppBorder"];
                    break;
                case "danger":
                    btn.Background = new SolidColorBrush(
                        Color.FromRgb(255, 240, 240));
                    btn.Foreground = new SolidColorBrush(
                        Color.FromRgb(198, 40, 40));
                    btn.BorderBrush = new SolidColorBrush(
                        Color.FromRgb(255, 205, 210));
                    break;
            }

            return btn;
        }

        private void UpdateTrashBulkBar()
        {
            int selected = _trashSelectableRows
                .Count(r => r.Chk.IsChecked == true);

            if (selected > 0)
            {
                TrashBulkBar.Visibility = Visibility.Visible;
                TxtTrashSelected.Text =
                    $"{selected} item{(selected == 1 ? "" : "s")} selected";
            }
            else
            {
                TrashBulkBar.Visibility = Visibility.Collapsed;
            }
        }

        private void BulkRestore_Click(object sender, RoutedEventArgs e)
        {
            var selected = _trashSelectableRows
                .Where(r => r.Chk.IsChecked == true).ToList();
            if (selected.Count == 0) return;

            var result = MessageBox.Show(
                $"Restore {selected.Count} receipt(s)?",
                "Bulk Restore",
                MessageBoxButton.YesNo,
                MessageBoxImage.Question);

            if (result != MessageBoxResult.Yes) return;

            _db.RestoreMultiple(
                selected.Select(r => r.Receipt.Id).ToList());

            foreach (var row in selected)
            {
                TrashPanel.Children.Remove(row.Row);
                _deletedReceipts.RemoveAll(r => r.Id == row.Receipt.Id);
                _trashSelectableRows.RemoveAll(
                    r => r.Receipt.Id == row.Receipt.Id);
            }

            if (_deletedReceipts.Count == 0)
            {
                EmptyPanel.Visibility = Visibility.Visible;
                TablePanel.Visibility = Visibility.Collapsed;
            }

            UpdateTrashBulkBar();
        }

        private void BulkPermanentDelete_Click(
            object sender, RoutedEventArgs e)
        {
            var selected = _trashSelectableRows
                .Where(r => r.Chk.IsChecked == true).ToList();
            if (selected.Count == 0) return;

            var result = MessageBox.Show(
                $"Permanently delete {selected.Count} " +
                $"receipt(s)?\n\nThis CANNOT be undone.",
                "Bulk Delete",
                MessageBoxButton.YesNo,
                MessageBoxImage.Warning);

            if (result != MessageBoxResult.Yes) return;

            _db.PermanentlyDeleteMultiple(
                selected.Select(r => r.Receipt.Id).ToList());

            foreach (var row in selected)
            {
                TrashPanel.Children.Remove(row.Row);
                _deletedReceipts.RemoveAll(r => r.Id == row.Receipt.Id);
                _trashSelectableRows.RemoveAll(
                    r => r.Receipt.Id == row.Receipt.Id);
            }

            if (_deletedReceipts.Count == 0)
            {
                EmptyPanel.Visibility = Visibility.Visible;
                TablePanel.Visibility = Visibility.Collapsed;
            }

            UpdateTrashBulkBar();
        }

        private void ClearTrashSelection_Click(
            object sender, RoutedEventArgs e)
        {
            foreach (var row in _trashSelectableRows)
                row.Chk.IsChecked = false;
            UpdateTrashBulkBar();
        }
    }
}
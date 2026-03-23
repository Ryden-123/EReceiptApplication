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
            var border = new Border
            {
                BorderBrush = new SolidColorBrush(
                    Color.FromRgb(235, 235, 240)),
                BorderThickness = new Thickness(0, 0, 0, 1)
            };

            var grid = new Grid { Margin = new Thickness(0, 2, 0, 2) };
            grid.ColumnDefinitions.Add(new ColumnDefinition
            { Width = new GridLength(155) });
            grid.ColumnDefinitions.Add(new ColumnDefinition
            { Width = new GridLength(80) });
            grid.ColumnDefinitions.Add(new ColumnDefinition
            { Width = new GridLength(1, GridUnitType.Star) });
            grid.ColumnDefinitions.Add(new ColumnDefinition
            { Width = new GridLength(140) });
            grid.ColumnDefinitions.Add(new ColumnDefinition
            { Width = new GridLength(90) });
            grid.ColumnDefinitions.Add(new ColumnDefinition
            { Width = new GridLength(160) });

            // Receipt number
            grid.Children.Add(MakeCell(
                receipt.ReceiptNumber, 0,
                new Thickness(16, 10, 4, 10), true));

            // Type badge
            var badge = new Border
            {
                CornerRadius = new CornerRadius(10),
                Padding = new Thickness(8, 3, 8, 3),
                HorizontalAlignment = HorizontalAlignment.Left,
                VerticalAlignment = VerticalAlignment.Center
            };

            if (receipt.Type == ReceiptType.Membership)
            {
                badge.Background = new SolidColorBrush(
                    Color.FromRgb(237, 231, 246));
                badge.Child = new TextBlock
                {
                    Text = "Memb.",
                    FontSize = 11,
                    FontWeight = FontWeights.SemiBold,
                    Foreground = new SolidColorBrush(
                        Color.FromRgb(81, 45, 168))
                };
            }
            else
            {
                badge.Background = new SolidColorBrush(
                    Color.FromRgb(232, 245, 233));
                badge.Child = new TextBlock
                {
                    Text = "Std.",
                    FontSize = 11,
                    FontWeight = FontWeights.SemiBold,
                    Foreground = new SolidColorBrush(
                        Color.FromRgb(46, 125, 50))
                };
            }
            Grid.SetColumn(badge, 1);
            grid.Children.Add(badge);

            // Issued to
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
                Foreground = (SolidColorBrush)Application
                                   .Current.Resources["AppText"],
                TextTrimming = TextTrimming.CharacterEllipsis
            });
            Grid.SetColumn(nameStack, 2);
            grid.Children.Add(nameStack);

            // Deleted date — shown in red
            grid.Children.Add(MakeCell(
                receipt.DateIssued.ToString("MMM dd, yyyy"),
                3, muted: true));

            // Total
            grid.Children.Add(MakeCell(
                $"₱{receipt.TotalAmount:F2}", 4, bold: true));

            // Actions
            var actions = new StackPanel
            {
                Orientation = Orientation.Horizontal,
                VerticalAlignment = VerticalAlignment.Center,
                Margin = new Thickness(4, 0, 8, 0)
            };

            var restoreBtn = MakeActionButton(
                "↩ Restore", "restore");
            var deleteBtn = MakeActionButton(
                "🗑 Delete", "danger");

            restoreBtn.ToolTip = "Restore this receipt";
            deleteBtn.ToolTip = "Permanently delete";

            restoreBtn.Click += (s, e) =>
                RestoreReceipt(receipt, border);
            deleteBtn.Click += (s, e) =>
                PermanentlyDelete(receipt, border);

            actions.Children.Add(restoreBtn);
            actions.Children.Add(deleteBtn);

            Grid.SetColumn(actions, 5);
            grid.Children.Add(actions);

            border.Child = grid;
            return border;
        }

        private void RestoreReceipt(Receipt receipt, Border row)
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

                MessageBox.Show(
                    "Receipt restored successfully!",
                    "Restored",
                    MessageBoxButton.OK,
                    MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    $"Could not restore: {ex.Message}",
                    "Error",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
        }

        private void PermanentlyDelete(Receipt receipt, Border row)
        {
            var result = MessageBox.Show(
                $"Permanently delete receipt " +
                $"{receipt.ReceiptNumber}?\n\n" +
                $"This CANNOT be undone.",
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
                MessageBox.Show(
                    $"Could not delete: {ex.Message}",
                    "Error",
                    MessageBoxButton.OK,
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
    }
}
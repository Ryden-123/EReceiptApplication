using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Shapes;
using EReceiptApp.Models;
using EReceiptApp.Services;

namespace EReceiptApp.Views.Pages
{
    public partial class DashboardPage : Page
    {
        private readonly DatabaseService _db = new DatabaseService();
        private readonly ExportService _export = new ExportService();

        public DashboardPage()
        {
            InitializeComponent();
            Loaded += Page_Loaded;
        }

        private void Page_Loaded(object sender, RoutedEventArgs e)
        {
            // Clear all panels first to prevent duplication on back-navigate
            RecentReceiptsPanel.Children.Clear();
            TopRecipientsPanel.Children.Clear();
            ChartCanvas.Children.Clear();

            SetGreeting();
            LoadStats();
            LoadChart();
            LoadTopRecipients();
            LoadRecentReceipts();
        }

        // ── Greeting ──────────────────────────────────────────────────
        private void SetGreeting()
        {
            int hour = DateTime.Now.Hour;
            string greeting = hour < 12 ? "Good morning" :
                              hour < 17 ? "Good afternoon" :
                                          "Good evening";

            TxtGreeting.Text = $"{greeting}! 👋";
            TxtDate.Text = DateTime.Now.ToString(
                "dddd, MMMM dd, yyyy");
        }

        // ── Stat cards ────────────────────────────────────────────────
        private void LoadStats()
        {
            var all = _db.GetAllReceipts();
            var thisMonth = _db.GetReceiptsThisMonth();

            // Replace with — no types, just use all receipts
            var standard = all;
            var membership = new List<Receipt>();

            // This month
            StatMonthTotal.Text =
                $"₱{thisMonth.Sum(r => r.TotalAmount):F2}";
            StatMonthCount.Text =
                $"{thisMonth.Count} receipt{(thisMonth.Count == 1 ? "" : "s")}";

            // Replace with
            StatStandard.Text = all.Count.ToString();
            StatStandardAmt.Text = $"all time";

            // Average receipt value
            decimal avg = all.Count > 0
                ? all.Sum(r => r.TotalAmount) / all.Count : 0;
            StatMembership.Text = $"₱{avg:F0}";
            StatMembershipAmt.Text = "average receipt";

            // Removed to show actual value
            // StatMembership.Text = "—"; --Removed 
            // StatMembershipAmt.Text = "No longer used"; --Removed

            // All time
            StatAllTime.Text =
                $"₱{all.Sum(r => r.TotalAmount):F2}";
            StatAllTimeCount.Text =
                $"{all.Count} total receipt{(all.Count == 1 ? "" : "s")}";
        }

        // ── Bar chart ─────────────────────────────────────────────────
        private void LoadChart()
        {
            var data = _db.GetDailyTotals(30);

            if (data.Count == 0)
            {
                TxtNoChartData.Visibility = Visibility.Visible;
                return;
            }

            ChartCanvas.Children.Clear();

            // Wait for canvas to measure
            ChartCanvas.Loaded += (s, e) => DrawChart(data);

            // If already loaded draw immediately
            if (ChartCanvas.ActualWidth > 0)
                DrawChart(data);
        }

        private void DrawChart(List<(string Date, decimal Total)> data)
        {
            ChartCanvas.Children.Clear();

            double canvasW = ChartCanvas.ActualWidth;
            double canvasH = ChartCanvas.ActualHeight;

            if (canvasW <= 0 || canvasH <= 0) return;

            double maxVal = (double)data.Max(d => d.Total);
            if (maxVal == 0) maxVal = 1;

            double padding = 8;
            double barWidth = (canvasW - padding * 2) / data.Count
                               - 4;
            double maxBarH = canvasH - 30;

            // Get accent color
            var accentBrush = Application.Current.Resources["AppAccent"]
                as SolidColorBrush
                ?? new SolidColorBrush(Color.FromRgb(92, 74, 187));

            for (int i = 0; i < data.Count; i++)
            {
                double barH = ((double)data[i].Total / maxVal) * maxBarH;
                double x = padding + i * (barWidth + 4);
                double y = canvasH - 24 - barH;

                // Bar
                var rect = new Rectangle
                {
                    Width = barWidth,
                    Height = Math.Max(barH, 2),
                    Fill = accentBrush,
                    Opacity = 0.85,
                    RadiusX = 3,
                    RadiusY = 3
                };
                Canvas.SetLeft(rect, x);
                Canvas.SetTop(rect, y);
                ChartCanvas.Children.Add(rect);

                // Date label — only show every 5th to avoid crowding
                if (i % 5 == 0 && data[i].Date.Length >= 10)
                {
                    var label = new TextBlock
                    {
                        Text = data[i].Date.Substring(5), // MM-dd
                        FontSize = 9,
                        Foreground = (SolidColorBrush)Application
                            .Current.Resources["AppTextMuted"]
                    };
                    Canvas.SetLeft(label, x);
                    Canvas.SetTop(label, canvasH - 18);
                    ChartCanvas.Children.Add(label);
                }
            }

            // Baseline
            var baseline = new Line
            {
                X1 = padding,
                Y1 = canvasH - 24,
                X2 = canvasW - padding,
                Y2 = canvasH - 24,
                Stroke = (SolidColorBrush)Application
                                      .Current.Resources["AppBorder"],
                StrokeThickness = 1
            };
            ChartCanvas.Children.Add(baseline);
        }

        // ── Top recipients ────────────────────────────────────────────
        private void LoadTopRecipients()
        {
            var top = _db.GetTopRecipients(5);

            if (top.Count == 0)
            {
                TxtNoRecipients.Visibility = Visibility.Visible;
                return;
            }

            int maxCount = top.Max(t => t.Count);

            foreach (var (name, count) in top)
            {
                var row = new StackPanel
                { Margin = new Thickness(0, 0, 0, 10) };

                // Name + count
                var header = new Grid();
                header.ColumnDefinitions.Add(new ColumnDefinition
                { Width = new GridLength(1, GridUnitType.Star) });
                header.ColumnDefinitions.Add(new ColumnDefinition
                { Width = GridLength.Auto });

                var nameTb = new TextBlock
                {
                    Text = name,
                    FontSize = 12,
                    FontWeight = FontWeights.SemiBold,
                    Foreground = (SolidColorBrush)Application
                                     .Current.Resources["AppText"],
                    TextTrimming = TextTrimming.CharacterEllipsis
                };
                var countTb = new TextBlock
                {
                    Text = $"{count}x",
                    FontSize = 12,
                    Foreground = (SolidColorBrush)Application
                                     .Current.Resources["AppTextMuted"]
                };

                Grid.SetColumn(nameTb, 0);
                Grid.SetColumn(countTb, 1);
                header.Children.Add(nameTb);
                header.Children.Add(countTb);
                row.Children.Add(header);

                // Progress bar
                var barTrack = new Border
                {
                    Height = 5,
                    CornerRadius = new CornerRadius(3),
                    Background = (SolidColorBrush)Application
                                          .Current.Resources["AppBorder"],
                    Margin = new Thickness(0, 4, 0, 0)
                };

                var barFill = new Border
                {
                    Height = 5,
                    CornerRadius = new CornerRadius(3),
                    Background = (SolidColorBrush)Application
                                          .Current.Resources["AppAccent"],
                    HorizontalAlignment = HorizontalAlignment.Left,
                    Width = double.NaN // set after layout
                };

                // Use a Grid to overlay fill on track
                var barGrid = new Grid();
                barGrid.Children.Add(barTrack);

                double pct = maxCount > 0
                    ? (double)count / maxCount : 0;

                barFill.Width = 0;
                barGrid.Children.Add(barFill);
                row.Children.Add(barGrid);

                // Set width after layout
                barGrid.Loaded += (s, e) =>
                {
                    barFill.Width =
                        Math.Max(barGrid.ActualWidth * pct, 4);
                };

                TopRecipientsPanel.Children.Add(row);
            }
        }

        // ── Recent receipts ───────────────────────────────────────────
        private void LoadRecentReceipts()
        {
            var recent = _db.GetRecentReceipts(5);

            if (recent.Count == 0)
            {
                TxtNoRecent.Visibility = Visibility.Visible;
                return;
            }

            foreach (var r in recent)
            {
                var border = new Border
                {
                    BorderBrush = (SolidColorBrush)Application
                                          .Current.Resources["AppBorder"],
                    BorderThickness = new Thickness(0, 0, 0, 1),
                    Padding = new Thickness(0, 8, 0, 8),
                    Cursor = System.Windows.Input.Cursors.Hand
                };

                border.MouseEnter += (s, e) =>
                    border.Background = (SolidColorBrush)Application
                        .Current.Resources["AppSurfaceAlt"];
                border.MouseLeave += (s, e) =>
                    border.Background = null;
                border.MouseLeftButtonUp += (s, e) =>
                    NavigationService?.Navigate(
                        new ReceiptPreviewPage(r, fromHistory: true,
                          fromDashboard: true));

                var grid = new Grid();
                grid.ColumnDefinitions.Add(new ColumnDefinition
                { Width = new GridLength(1, GridUnitType.Star) });
                grid.ColumnDefinitions.Add(new ColumnDefinition
                { Width = GridLength.Auto });
                grid.ColumnDefinitions.Add(new ColumnDefinition
                { Width = GridLength.Auto });

                // Left: name + receipt number
                var left = new StackPanel();
                left.Children.Add(new TextBlock
                {
                    Text = r.IssuedTo,
                    FontSize = 13,
                    FontWeight = FontWeights.SemiBold,
                    Foreground = (SolidColorBrush)Application
                                     .Current.Resources["AppText"]
                });
                left.Children.Add(new TextBlock
                {
                    Text = r.ReceiptNumber,
                    FontSize = 11,
                    Foreground = (SolidColorBrush)Application
                                     .Current.Resources["AppTextMuted"],
                    Margin = new Thickness(0, 2, 0, 0)
                });
                Grid.SetColumn(left, 0);

                // Middle: date
                var dateTb = new TextBlock
                {
                    Text = r.DateIssued.ToString("MMM dd"),
                    FontSize = 12,
                    Foreground = (SolidColorBrush)Application
                                            .Current.Resources["AppTextMuted"],
                    VerticalAlignment = VerticalAlignment.Center,
                    Margin = new Thickness(12, 0, 12, 0)
                };
                Grid.SetColumn(dateTb, 1);

                // Right: total
                var totalTb = new TextBlock
                {
                    Text = $"₱{r.TotalAmount:F2}",
                    FontSize = 13,
                    FontWeight = FontWeights.Bold,
                    Foreground = (SolidColorBrush)Application
                                            .Current.Resources["AppAccent"],
                    VerticalAlignment = VerticalAlignment.Center
                };
                Grid.SetColumn(totalTb, 2);

                grid.Children.Add(left);
                grid.Children.Add(dateTb);
                grid.Children.Add(totalTb);

                border.Child = grid;
                RecentReceiptsPanel.Children.Add(border);
            }
        }

        // ── Export Excel ──────────────────────────────────────────────
        private void ExportExcel_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new Microsoft.Win32.SaveFileDialog
            {
                FileName = $"Receipts_{DateTime.Now:yyyyMMdd}",
                DefaultExt = ".xlsx",
                Filter = "Excel Workbook|*.xlsx"
            };

            if (dialog.ShowDialog() != true) return;

            try
            {
                var receipts = _db.GetAllReceipts();
                _export.ExportToExcel(receipts, dialog.FileName);
                MessageBox.Show(
                    $"Exported {receipts.Count} receipts to Excel!",
                    "Export Complete",
                    MessageBoxButton.OK,
                    MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    $"Export failed: {ex.Message}",
                    "Error",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
        }

        // ── Export CSV ────────────────────────────────────────────────
        private void ExportCsv_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new Microsoft.Win32.SaveFileDialog
            {
                FileName = $"Receipts_{DateTime.Now:yyyyMMdd}",
                DefaultExt = ".csv",
                Filter = "CSV File|*.csv"
            };

            if (dialog.ShowDialog() != true) return;

            try
            {
                var receipts = _db.GetAllReceipts();
                _export.ExportToCsv(receipts, dialog.FileName);
                MessageBox.Show(
                    $"Exported {receipts.Count} receipts to CSV!",
                    "Export Complete",
                    MessageBoxButton.OK,
                    MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    $"Export failed: {ex.Message}",
                    "Error",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
        }

        // ── Verify Receipt ────────────────────────────────────────────
        private void VerifyReceipt_Click(object sender, RoutedEventArgs e)
        {
            NavigationService?.Navigate(new VerifyReceiptPage());
        }

        private void NewReceipt_Click(object sender, RoutedEventArgs e)
        {
            NavigationService?.Navigate(
                new ReceiptBuilderPage());
        }
    }
}
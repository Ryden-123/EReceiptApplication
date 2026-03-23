using System;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using EReceiptApp.Models;

namespace EReceiptApp.Services
{
    public class PdfService
    {
        // Generates a PDF from the receipt and saves it to filePath
        public void SaveReceiptAsPdf(Receipt receipt, string filePath)
        {
            // We render the receipt to a high-res bitmap first
            // then embed it into a PDF page using PdfSharp
            var visual = BuildReceiptVisual(receipt);

            // Force layout
            visual.Measure(new Size(
                double.PositiveInfinity,
                double.PositiveInfinity));
            visual.Arrange(new Rect(visual.DesiredSize));
            visual.UpdateLayout();

            // Render to bitmap at 2x for sharpness
            double scale = 2.0;
            int width = (int)(visual.DesiredSize.Width * scale);
            int height = (int)(visual.DesiredSize.Height * scale);

            var renderBitmap = new RenderTargetBitmap(
                width, height,
                96 * scale, 96 * scale,
                PixelFormats.Pbgra32);
            renderBitmap.Render(visual);

            // Save bitmap to a temp PNG first
            string tempPng = Path.Combine(
                Path.GetTempPath(),
                $"receipt_temp_{Guid.NewGuid()}.png");

            var pngEncoder = new PngBitmapEncoder();
            pngEncoder.Frames.Add(BitmapFrame.Create(renderBitmap));
            using (var pngStream = File.Create(tempPng))
                pngEncoder.Save(pngStream);

            // Now embed that PNG into a PdfSharp PDF page
            WritePdf(tempPng, filePath,
                visual.DesiredSize.Width,
                visual.DesiredSize.Height);

            // Clean up temp file
            try { File.Delete(tempPng); } catch { }
        }

        // Saves receipt as PNG and returns the file path
        // Used by EmailService to attach the image
        public string SaveReceiptAsTempPng(Receipt receipt)
        {
            var visual = BuildReceiptVisual(receipt);

            visual.Measure(new Size(
                double.PositiveInfinity,
                double.PositiveInfinity));
            visual.Arrange(new Rect(visual.DesiredSize));
            visual.UpdateLayout();

            double scale = 2.0;
            int width = (int)(visual.DesiredSize.Width * scale);
            int height = (int)(visual.DesiredSize.Height * scale);

            var renderBitmap = new RenderTargetBitmap(
                width, height,
                96 * scale, 96 * scale,
                PixelFormats.Pbgra32);
            renderBitmap.Render(visual);

            string tempPng = Path.Combine(
                Path.GetTempPath(),
                $"receipt_{receipt.ReceiptNumber}.png");

            var encoder = new PngBitmapEncoder();
            encoder.Frames.Add(BitmapFrame.Create(renderBitmap));
            using var stream = File.Create(tempPng);
            encoder.Save(stream);

            return tempPng;
        }

        // Saves receipt as PDF to temp folder and returns path
        // Used by EmailService to attach the PDF
        public string SaveReceiptAsTempPdf(Receipt receipt)
        {
            string tempPdf = Path.Combine(
                Path.GetTempPath(),
                $"receipt_{receipt.ReceiptNumber}.pdf");

            SaveReceiptAsPdf(receipt, tempPdf);
            return tempPdf;
        }

        // ── Write PDF using PdfSharp ──────────────────────────────────
        private void WritePdf(
            string pngPath, string pdfPath,
            double imgWidthPx, double imgHeightPx)
        {
            var document = new PdfSharp.Pdf.PdfDocument();
            document.Info.Title = "E-Receipt";
            document.Info.Author = "E-Receipt System";

            var page = document.AddPage();

            // Convert pixels to points (1 point = 1/72 inch, 96dpi)
            double pxToPoint = 72.0 / 96.0;
            page.Width = new PdfSharp.Drawing.XUnit(
                imgWidthPx * pxToPoint,
                PdfSharp.Drawing.XGraphicsUnit.Point);
            page.Height = new PdfSharp.Drawing.XUnit(
                imgHeightPx * pxToPoint,
                PdfSharp.Drawing.XGraphicsUnit.Point);

            using var gfx = PdfSharp.Drawing.XGraphics
                .FromPdfPage(page);
            using var img = PdfSharp.Drawing.XImage
                .FromFile(pngPath);

            gfx.DrawImage(img, 0, 0, page.Width.Point, page.Height.Point);

            document.Save(pdfPath);
        }

        // ── Build the receipt visual (same as in ReceiptPreviewPage) ──
        private Border BuildReceiptVisual(Receipt receipt)
        {
            var card = new Border
            {
                Background = new SolidColorBrush(Colors.White),
                BorderBrush = new SolidColorBrush(
                    Color.FromRgb(220, 220, 230)),
                BorderThickness = new Thickness(1),
                CornerRadius = new CornerRadius(12),
                Padding = new Thickness(48, 40, 48, 40),
                MinWidth = 520
            };

            var stack = new StackPanel();

            // Header
            stack.Children.Add(MakeText(
                string.IsNullOrWhiteSpace(receipt.OrganizationName)
                    ? "E-Receipt System"
                    : receipt.OrganizationName,
                18, true, TextAlignment.Center,
                Colors.Black, 0, 4));

            if (!string.IsNullOrWhiteSpace(receipt.ClubName))
                stack.Children.Add(MakeText(
                    receipt.ClubName, 13, false,
                    TextAlignment.Center,
                    Color.FromRgb(120, 120, 140), 0, 0));

            // Badge
            var badgeColor = receipt.Type == ReceiptType.Membership
                ? Color.FromRgb(237, 231, 246)
                : Color.FromRgb(232, 245, 233);
            var badgeTextColor = receipt.Type == ReceiptType.Membership
                ? Color.FromRgb(81, 45, 168)
                : Color.FromRgb(46, 125, 50);

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
                Text = receipt.Type == ReceiptType.Membership
                    ? "Membership Receipt" : "Standard Receipt",
                FontSize = 11,
                FontWeight = FontWeights.SemiBold,
                Foreground = new SolidColorBrush(badgeTextColor)
            };
            stack.Children.Add(badge);

            // Receipt info rows
            stack.Children.Add(MakeRow("Receipt No.",
                receipt.ReceiptNumber));
            stack.Children.Add(MakeRow("Date",
                receipt.DateIssued.ToString("MMMM dd, yyyy")));
            stack.Children.Add(MakeRow("Cashier",
                receipt.CashierName));

            stack.Children.Add(MakeDivider());

            // Recipient
            stack.Children.Add(MakeSectionLabel("RECIPIENT"));
            stack.Children.Add(MakeRow("Name", receipt.IssuedTo));

            if (!string.IsNullOrWhiteSpace(receipt.IdNumber))
                stack.Children.Add(MakeRow("ID Number",
                    receipt.IdNumber));

            if (receipt.Type == ReceiptType.Membership)
            {
                if (!string.IsNullOrWhiteSpace(receipt.ClubName))
                    stack.Children.Add(MakeRow("Membership Type",
                        receipt.ClubName));
                if (!string.IsNullOrWhiteSpace(receipt.OrganizationName))
                    stack.Children.Add(MakeRow("School Year",
                        receipt.OrganizationName));
            }

            stack.Children.Add(MakeDivider());

            // Items
            stack.Children.Add(MakeSectionLabel("ITEMS"));
            stack.Children.Add(MakeItemHeader());

            foreach (var item in receipt.Items)
                stack.Children.Add(MakeItemRow(item));

            var line = new System.Windows.Shapes.Rectangle
            {
                Height = 1,
                Fill = new SolidColorBrush(
                    Color.FromRgb(220, 220, 230)),
                Margin = new Thickness(0, 8, 0, 12)
            };
            stack.Children.Add(line);
            stack.Children.Add(MakeTotalRow(receipt.TotalAmount));

            if (!string.IsNullOrWhiteSpace(receipt.Notes))
                stack.Children.Add(MakeText(
                    $"Note: {receipt.Notes}",
                    11, false, TextAlignment.Left,
                    Color.FromRgb(120, 120, 140),
                    12, 0, FontStyles.Italic));

            stack.Children.Add(MakeDivider());

            stack.Children.Add(MakeText(
                "This is an official digital receipt.",
                10, false, TextAlignment.Center,
                Color.FromRgb(150, 150, 165), 0, 2));
            stack.Children.Add(MakeText(
                "E-Receipt System v1.0",
                10, false, TextAlignment.Center,
                Color.FromRgb(150, 150, 165), 0, 0));

            card.Child = stack;
            return card;
        }

        // ── Visual builder helpers ────────────────────────────────────

        private TextBlock MakeText(
            string text, int fontSize, bool bold,
            TextAlignment align, Color color,
            double marginTop, double marginBottom,
            FontStyle fontStyle = default)
        {
            return new TextBlock
            {
                Text = text,
                FontSize = fontSize,
                FontWeight = bold
                    ? FontWeights.Bold : FontWeights.Normal,
                FontStyle = fontStyle,
                Foreground = new SolidColorBrush(color),
                TextAlignment = align,
                TextWrapping = TextWrapping.Wrap,
                Margin = new Thickness(0, marginTop, 0, marginBottom)
            };
        }

        private Grid MakeRow(string label, string value)
        {
            var grid = new Grid { Margin = new Thickness(0, 0, 0, 8) };
            grid.ColumnDefinitions.Add(new ColumnDefinition
            { Width = new GridLength(1, GridUnitType.Star) });
            grid.ColumnDefinitions.Add(new ColumnDefinition
            { Width = new GridLength(1, GridUnitType.Star) });

            var lbl = new TextBlock
            {
                Text = label,
                FontSize = 12,
                Foreground = new SolidColorBrush(
                    Color.FromRgb(120, 120, 140)),
                VerticalAlignment = VerticalAlignment.Center
            };
            var val = new TextBlock
            {
                Text = value,
                FontSize = 12,
                FontWeight = FontWeights.SemiBold,
                Foreground = new SolidColorBrush(Colors.Black),
                HorizontalAlignment = HorizontalAlignment.Right,
                TextAlignment = TextAlignment.Right,
                TextWrapping = TextWrapping.Wrap
            };

            Grid.SetColumn(lbl, 0);
            Grid.SetColumn(val, 1);
            grid.Children.Add(lbl);
            grid.Children.Add(val);
            return grid;
        }

        private TextBlock MakeDivider() => new TextBlock
        {
            Text = "- - - - - - - - - - - - - - - - - - - - - - - - - - - -",
            FontSize = 11,
            Foreground = new SolidColorBrush(
                Color.FromRgb(200, 200, 210)),
            TextAlignment = TextAlignment.Center,
            Margin = new Thickness(0, 14, 0, 14),
            Opacity = 0.8
        };

        private TextBlock MakeSectionLabel(string text) => new TextBlock
        {
            Text = text,
            FontSize = 10,
            FontWeight = FontWeights.Bold,
            Foreground = new SolidColorBrush(
                Color.FromRgb(120, 120, 140)),
            Margin = new Thickness(0, 0, 0, 10)
        };

        private Grid MakeItemHeader()
        {
            var grid = new Grid { Margin = new Thickness(0, 0, 0, 8) };
            grid.ColumnDefinitions.Add(new ColumnDefinition
            { Width = new GridLength(1, GridUnitType.Star) });
            grid.ColumnDefinitions.Add(new ColumnDefinition
            { Width = new GridLength(36) });
            grid.ColumnDefinitions.Add(new ColumnDefinition
            { Width = new GridLength(72) });
            grid.ColumnDefinitions.Add(new ColumnDefinition
            { Width = new GridLength(72) });

            AddCell(grid, "Description", 0, TextAlignment.Left, true);
            AddCell(grid, "Qty", 1, TextAlignment.Center, true);
            AddCell(grid, "Price", 2, TextAlignment.Right, true);
            AddCell(grid, "Total", 3, TextAlignment.Right, true);
            return grid;
        }

        private Grid MakeItemRow(ReceiptItem item)
        {
            var grid = new Grid { Margin = new Thickness(0, 0, 0, 6) };
            grid.ColumnDefinitions.Add(new ColumnDefinition
            { Width = new GridLength(1, GridUnitType.Star) });
            grid.ColumnDefinitions.Add(new ColumnDefinition
            { Width = new GridLength(36) });
            grid.ColumnDefinitions.Add(new ColumnDefinition
            { Width = new GridLength(72) });
            grid.ColumnDefinitions.Add(new ColumnDefinition
            { Width = new GridLength(72) });

            AddCell(grid, item.Description, 0, TextAlignment.Left);
            AddCell(grid, item.Quantity.ToString(), 1, TextAlignment.Center);
            AddCell(grid, $"₱{item.UnitPrice:F2}", 2, TextAlignment.Right);
            AddCell(grid, $"₱{item.Total:F2}", 3, TextAlignment.Right,
                bold: true);
            return grid;
        }

        private void AddCell(Grid grid, string text, int col,
            TextAlignment align, bool bold = false)
        {
            var tb = new TextBlock
            {
                Text = text,
                FontSize = 12,
                FontWeight = bold
                    ? FontWeights.SemiBold : FontWeights.Normal,
                Foreground = new SolidColorBrush(Colors.Black),
                TextAlignment = align,
                HorizontalAlignment = col == 0
                    ? HorizontalAlignment.Left
                    : HorizontalAlignment.Right,
                TextWrapping = TextWrapping.Wrap
            };
            Grid.SetColumn(tb, col);
            grid.Children.Add(tb);
        }

        private Grid MakeTotalRow(decimal total)
        {
            var grid = new Grid();
            grid.ColumnDefinitions.Add(new ColumnDefinition
            { Width = new GridLength(1, GridUnitType.Star) });
            grid.ColumnDefinitions.Add(new ColumnDefinition
            { Width = GridLength.Auto });

            var lbl = new TextBlock
            {
                Text = "TOTAL",
                FontSize = 15,
                FontWeight = FontWeights.Bold,
                Foreground = new SolidColorBrush(Colors.Black),
                VerticalAlignment = VerticalAlignment.Center
            };
            var val = new TextBlock
            {
                Text = $"₱{total:F2}",
                FontSize = 15,
                FontWeight = FontWeights.Bold,
                Foreground = new SolidColorBrush(
                    Color.FromRgb(92, 74, 187)),
                HorizontalAlignment = HorizontalAlignment.Right
            };

            Grid.SetColumn(lbl, 0);
            Grid.SetColumn(val, 1);
            grid.Children.Add(lbl);
            grid.Children.Add(val);
            return grid;
        }
    }
}
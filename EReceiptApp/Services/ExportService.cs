using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using ClosedXML.Excel;
using EReceiptApp.Models;

namespace EReceiptApp.Services
{
    public class ExportService
    {
        // ── Export to CSV ─────────────────────────────────────────────
        public void ExportToCsv(List<Receipt> receipts, string filePath)
        {
            var sb = new StringBuilder();

            // Header row
            sb.AppendLine(
                "Receipt Number,Type,Issued To,ID Number," +
                "Organization,Club,Date,Total,Cashier,Notes");

            // Data rows
            foreach (var r in receipts)
            {
                sb.AppendLine(
                    $"{Escape(r.ReceiptNumber)}," +
                    $"{r.Type}," +
                    $"{Escape(r.IssuedTo)}," +
                    $"{Escape(r.IdNumber)}," +
                    $"{Escape(r.OrganizationName)}," +
                    $"{Escape(r.ClubName)}," +
                    $"{r.DateIssued:yyyy-MM-dd}," +
                    $"{r.TotalAmount:F2}," +
                    $"{Escape(r.CashierName)}," +
                    $"{Escape(r.Notes)}");
            }

            File.WriteAllText(filePath, sb.ToString(),
                Encoding.UTF8);
        }

        // ── Export to Excel ───────────────────────────────────────────
        public void ExportToExcel(List<Receipt> receipts, string filePath)
        {
            using var workbook = new XLWorkbook();

            // ── Sheet 1: All Receipts ─────────────────────────────────
            var ws = workbook.Worksheets.Add("Receipts");

            // Header styling
            var headers = new[]
            {
                "Receipt Number", "Type", "Issued To",
                "ID Number", "Organization", "Club",
                "Date", "Total (₱)", "Cashier", "Notes"
            };

            for (int i = 0; i < headers.Length; i++)
            {
                var cell = ws.Cell(1, i + 1);
                cell.Value = headers[i];
                cell.Style.Font.Bold = true;
                cell.Style.Fill.BackgroundColor =
                    XLColor.FromHtml("#5C4ABB");
                cell.Style.Font.FontColor =
                    XLColor.White;
                cell.Style.Alignment.Horizontal =
                    XLAlignmentHorizontalValues.Center;
            }

            // Data rows
            for (int i = 0; i < receipts.Count; i++)
            {
                var r = receipts[i];
                int row = i + 2;

                ws.Cell(row, 1).Value = r.ReceiptNumber;
                ws.Cell(row, 2).Value = r.Type.ToString();
                ws.Cell(row, 3).Value = r.IssuedTo;
                ws.Cell(row, 4).Value = r.IdNumber;
                ws.Cell(row, 5).Value = r.OrganizationName;
                ws.Cell(row, 6).Value = r.ClubName;
                ws.Cell(row, 7).Value = r.DateIssued.ToString("yyyy-MM-dd");
                ws.Cell(row, 8).Value = (double)r.TotalAmount;
                ws.Cell(row, 8).Style.NumberFormat.Format = "#,##0.00";
                ws.Cell(row, 9).Value = r.CashierName;
                ws.Cell(row, 10).Value = r.Notes;

                // Alternate row color
                if (i % 2 == 0)
                {
                    ws.Row(row).Style.Fill.BackgroundColor =
                        XLColor.FromHtml("#F5F5FA");
                }
            }

            // Auto-fit columns
            ws.Columns().AdjustToContents();

            // Add totals row
            int totalRow = receipts.Count + 2;
            ws.Cell(totalRow, 7).Value = "TOTAL";
            ws.Cell(totalRow, 7).Style.Font.Bold = true;
            ws.Cell(totalRow, 8).FormulaA1 =
                $"=SUM(H2:H{receipts.Count + 1})";
            ws.Cell(totalRow, 8).Style.Font.Bold = true;
            ws.Cell(totalRow, 8).Style.NumberFormat.Format = "#,##0.00";

            // ── Sheet 2: Summary ──────────────────────────────────────
            var summary = workbook.Worksheets.Add("Summary");

            summary.Cell(1, 1).Value = "E-Receipt System — Export Summary";
            summary.Cell(1, 1).Style.Font.Bold = true;
            summary.Cell(1, 1).Style.Font.FontSize = 14;

            summary.Cell(2, 1).Value =
                $"Generated: {DateTime.Now:MMMM dd, yyyy hh:mm tt}";
            summary.Cell(2, 1).Style.Font.FontColor = XLColor.Gray;

            summary.Cell(4, 1).Value = "Metric";
            summary.Cell(4, 2).Value = "Value";
            summary.Cell(4, 1).Style.Font.Bold = true;
            summary.Cell(4, 2).Style.Font.Bold = true;

            int std = receipts.FindAll(
                r => r.Type == ReceiptType.Standard).Count;
            int mem = receipts.FindAll(
                r => r.Type == ReceiptType.Membership).Count;
            decimal total = 0;
            receipts.ForEach(r => total += r.TotalAmount);

            var summaryData = new[]
            {
                ("Total Receipts",    receipts.Count.ToString()),
                ("Standard Receipts", std.ToString()),
                ("Membership Receipts", mem.ToString()),
                ("Total Amount Collected", $"₱{total:F2}"),
                ("Export Date", DateTime.Now.ToString("MMMM dd, yyyy"))
            };

            for (int i = 0; i < summaryData.Length; i++)
            {
                summary.Cell(i + 5, 1).Value = summaryData[i].Item1;
                summary.Cell(i + 5, 2).Value = summaryData[i].Item2;
            }

            summary.Columns().AdjustToContents();

            workbook.SaveAs(filePath);
        }

        // Escape CSV special characters
        private string Escape(string value)
        {
            if (string.IsNullOrEmpty(value)) return "";
            if (value.Contains(",") || value.Contains("\"") ||
                value.Contains("\n"))
                return $"\"{value.Replace("\"", "\"\"")}\"";
            return value;
        }
    }
}
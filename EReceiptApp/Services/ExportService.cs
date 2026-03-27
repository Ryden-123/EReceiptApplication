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

            // Replace header with
            sb.AppendLine(
                "Receipt Number,Issued To,ID Number," +
                "Organization,Date,Total,Cashier,Notes");

            // Data rows
            foreach (var r in receipts)
            {
                // Replace data row with
                sb.AppendLine(
                    $"{Escape(r.ReceiptNumber)}," +
                    $"{Escape(r.IssuedTo)}," +
                    $"{Escape(r.IdNumber)}," +
                    $"{Escape(r.OrganizationName)}," +
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
            // Replace with
            var headers = new[]
            {
                "Receipt Number", "Issued To",
                "ID Number", "Organization",
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

                // Replace with
                ws.Cell(row, 1).Value = r.ReceiptNumber;
                ws.Cell(row, 2).Value = r.IssuedTo;
                ws.Cell(row, 3).Value = r.IdNumber;
                ws.Cell(row, 4).Value = r.OrganizationName;
                ws.Cell(row, 5).Value = r.DateIssued.ToString("yyyy-MM-dd");
                ws.Cell(row, 6).Value = (double)r.TotalAmount;
                ws.Cell(row, 6).Style.NumberFormat.Format = "#,##0.00";
                ws.Cell(row, 7).Value = r.CashierName;
                ws.Cell(row, 8).Value = r.Notes;

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
            // Replace with
            ws.Cell(totalRow, 5).Value = "TOTAL";
            ws.Cell(totalRow, 5).Style.Font.Bold = true;
            ws.Cell(totalRow, 6).FormulaA1 =
                $"=SUM(F2:F{receipts.Count + 1})";
            ws.Cell(totalRow, 6).Style.Font.Bold = true;
            ws.Cell(totalRow, 6).Style.NumberFormat.Format = "#,##0.00";

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

            // Replace with — no types anymore, just show total
            int std = receipts.Count;
            int mem = 0;
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
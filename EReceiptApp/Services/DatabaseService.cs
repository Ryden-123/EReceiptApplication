using Microsoft.Data.Sqlite;
using EReceiptApp.Models;
using System;
using System.Collections.Generic;
using System.Text.Json;

namespace EReceiptApp.Services
{
    public class DatabaseService
    {
        private readonly string _connectionString = "Data Source=receipts.db";

        public DatabaseService()
        {
            InitializeDatabase();
        }

        private void InitializeDatabase()
        {
            using var conn = new SqliteConnection(_connectionString);
            conn.Open();
            var cmd = conn.CreateCommand();
            cmd.CommandText = @"
                CREATE TABLE IF NOT EXISTS Receipts (
                    Id               INTEGER PRIMARY KEY AUTOINCREMENT,
                    ReceiptNumber    TEXT NOT NULL,
                    Type             INTEGER NOT NULL,
                    IssuedTo         TEXT,
                    IdNumber         TEXT,
                    OrganizationName TEXT,
                    ClubName         TEXT,
                    DateIssued       TEXT,
                    ItemsJson        TEXT,
                    TotalAmount      REAL,
                    Notes            TEXT,
                    CashierName      TEXT,
                    IsDeleted        INTEGER DEFAULT 0,
                    DeletedAt        TEXT
                );";
            cmd.ExecuteNonQuery();

            // Safely add new columns if upgrading from old DB
            var alterCmds = new[]
            {
                "ALTER TABLE Receipts ADD COLUMN IdNumber TEXT;",
                "ALTER TABLE Receipts ADD COLUMN IsDeleted INTEGER DEFAULT 0;",
                "ALTER TABLE Receipts ADD COLUMN DeletedAt TEXT;"
            };

            foreach (var sql in alterCmds)
            {
                try
                {
                    var alter = conn.CreateCommand();
                    alter.CommandText = sql;
                    alter.ExecuteNonQuery();
                }
                catch { } // Column already exists — safe to ignore
            }
        }

        public void SaveReceipt(Receipt receipt)
        {
            using var conn = new SqliteConnection(_connectionString);
            conn.Open();
            var cmd = conn.CreateCommand();
            cmd.CommandText = @"
                INSERT INTO Receipts
                (ReceiptNumber, Type, IssuedTo, IdNumber, OrganizationName,
                 ClubName, DateIssued, ItemsJson, TotalAmount, Notes, CashierName)
                VALUES
                ($num, $type, $to, $id, $org,
                 $club, $date, $items, $total, $notes, $cashier)";

            cmd.Parameters.AddWithValue("$num", receipt.ReceiptNumber);
            cmd.Parameters.AddWithValue("$type", (int)receipt.Type);
            cmd.Parameters.AddWithValue("$to", receipt.IssuedTo ?? "");
            cmd.Parameters.AddWithValue("$id", receipt.IdNumber ?? "");
            cmd.Parameters.AddWithValue("$org", receipt.OrganizationName ?? "");
            cmd.Parameters.AddWithValue("$club", receipt.ClubName ?? "");
            cmd.Parameters.AddWithValue("$date", receipt.DateIssued.ToString("o"));
            cmd.Parameters.AddWithValue("$items",
                JsonSerializer.Serialize(receipt.Items));
            cmd.Parameters.AddWithValue("$total", (double)receipt.TotalAmount);
            cmd.Parameters.AddWithValue("$notes", receipt.Notes ?? "");
            cmd.Parameters.AddWithValue("$cashier", receipt.CashierName ?? "");
            cmd.ExecuteNonQuery();
        }

        public void UpdateReceipt(Receipt receipt)
        {
            using var conn = new SqliteConnection(_connectionString);
            conn.Open();
            var cmd = conn.CreateCommand();
            cmd.CommandText = @"
        UPDATE Receipts SET
            Type             = $type,
            IssuedTo         = $to,
            IdNumber         = $id,
            OrganizationName = $org,
            ClubName         = $club,
            DateIssued       = $date,
            ItemsJson        = $items,
            TotalAmount      = $total,
            Notes            = $notes,
            CashierName      = $cashier
        WHERE Id = $id_pk";

            cmd.Parameters.AddWithValue("$type", (int)receipt.Type);
            cmd.Parameters.AddWithValue("$to", receipt.IssuedTo ?? "");
            cmd.Parameters.AddWithValue("$id", receipt.IdNumber ?? "");
            cmd.Parameters.AddWithValue("$org", receipt.OrganizationName ?? "");
            cmd.Parameters.AddWithValue("$club", receipt.ClubName ?? "");
            cmd.Parameters.AddWithValue("$date", receipt.DateIssued.ToString("o"));
            cmd.Parameters.AddWithValue("$items",
                JsonSerializer.Serialize(receipt.Items));
            cmd.Parameters.AddWithValue("$total", (double)receipt.TotalAmount);
            cmd.Parameters.AddWithValue("$notes", receipt.Notes ?? "");
            cmd.Parameters.AddWithValue("$cashier", receipt.CashierName ?? "");
            cmd.Parameters.AddWithValue("$id_pk", receipt.Id);
            cmd.ExecuteNonQuery();
        }

        public List<Receipt> GetAllReceipts()
        {
            var list = new List<Receipt>();
            using var conn = new SqliteConnection(_connectionString);
            conn.Open();
            var cmd = conn.CreateCommand();
            cmd.CommandText = @"
                SELECT * FROM Receipts
                WHERE IsDeleted = 0
                ORDER BY DateIssued DESC";

            using var reader = cmd.ExecuteReader();
            while (reader.Read())
            {
                try { list.Add(ReadReceipt(reader)); }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine(
                        $"Error reading receipt: {ex.Message}");
                }
            }
            return list;
        }

        public void DeleteReceipt(int id)
        {
            using var conn = new SqliteConnection(_connectionString);
            conn.Open();
            var cmd = conn.CreateCommand();
            cmd.CommandText = "DELETE FROM Receipts WHERE Id = $id";
            cmd.Parameters.AddWithValue("$id", id);
            cmd.ExecuteNonQuery();
        }

        public List<Receipt> SearchReceipts(string query)
        {
            var list = new List<Receipt>();
            using var conn = new SqliteConnection(_connectionString);
            conn.Open();
            var cmd = conn.CreateCommand();
            cmd.CommandText = @"
                SELECT * FROM Receipts
                WHERE IsDeleted = 0
                  AND (IssuedTo      LIKE $q
                   OR  ReceiptNumber LIKE $q
                   OR  IdNumber      LIKE $q
                   OR  ClubName      LIKE $q)
                ORDER BY DateIssued DESC";
            cmd.Parameters.AddWithValue("$q", $"%{query}%");

            using var reader = cmd.ExecuteReader();
            while (reader.Read())
            {
                try { list.Add(ReadReceipt(reader)); }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine(
                        $"Error reading receipt: {ex.Message}");
                }
            }
            return list;
        }

        // Safely read a string column — returns empty string if null
        private string SafeGetString(SqliteDataReader reader, int index)
        {
            return reader.IsDBNull(index)
                ? string.Empty
                : reader.GetString(index);
        }

        // Get receipts from current month only
        public List<Receipt> GetReceiptsThisMonth()
        {
            var list = new List<Receipt>();
            using var conn = new SqliteConnection(_connectionString);
            conn.Open();
            var cmd = conn.CreateCommand();
            cmd.CommandText = @"
        SELECT * FROM Receipts
        WHERE IsDeleted = 0
          AND strftime('%Y-%m', DateIssued) = $month
        ORDER BY DateIssued DESC";
            cmd.Parameters.AddWithValue("$month",
                DateTime.Now.ToString("yyyy-MM"));

            using var reader = cmd.ExecuteReader();
            while (reader.Read())
            {
                try { list.Add(ReadReceipt(reader)); }
                catch { }
            }
            return list;
        }

        // Get most recent N receipts
        public List<Receipt> GetRecentReceipts(int count = 5)
        {
            var list = new List<Receipt>();
            using var conn = new SqliteConnection(_connectionString);
            conn.Open();
            var cmd = conn.CreateCommand();
            cmd.CommandText = @"
                SELECT * FROM Receipts
                WHERE IsDeleted = 0
                ORDER BY DateIssued DESC
                LIMIT $count";
            cmd.Parameters.AddWithValue("$count", count);

            using var reader = cmd.ExecuteReader();
            while (reader.Read())
            {
                try { list.Add(ReadReceipt(reader)); }
                catch { }
            }
            return list;
        }

        // Get receipt by receipt number for verification
        public Receipt? GetReceiptByNumber(string receiptNumber)
        {
            using var conn = new SqliteConnection(_connectionString);
            conn.Open();
            var cmd = conn.CreateCommand();
            cmd.CommandText = @"
        SELECT * FROM Receipts
        WHERE ReceiptNumber = $num
        LIMIT 1";
            cmd.Parameters.AddWithValue("$num", receiptNumber);

            using var reader = cmd.ExecuteReader();
            if (reader.Read())
            {
                try { return ReadReceipt(reader); }
                catch { }
            }
            return null;
        }

        // Get daily totals for the past 30 days for the chart
        public List<(string Date, decimal Total)> GetDailyTotals(int days = 30)
        {
            var list = new List<(string, decimal)>();
            using var conn = new SqliteConnection(_connectionString);
            conn.Open();
            var cmd = conn.CreateCommand();
            cmd.CommandText = @"
        SELECT strftime('%Y-%m-%d', DateIssued) as Day,
               SUM(TotalAmount) as DayTotal
        FROM Receipts
        WHERE DateIssued >= date('now', $days)
        GROUP BY Day
        ORDER BY Day ASC";
            cmd.Parameters.AddWithValue("$days", $"-{days} days");

            using var reader = cmd.ExecuteReader();
            while (reader.Read())
            {
                list.Add((
                    reader.GetString(0),
                    (decimal)reader.GetDouble(1)));
            }
            return list;
        }

        // Get top 5 most frequent recipients
        public List<(string Name, int Count)> GetTopRecipients(int top = 5)
        {
            var list = new List<(string, int)>();
            using var conn = new SqliteConnection(_connectionString);
            conn.Open();
            var cmd = conn.CreateCommand();
            cmd.CommandText = @"
        SELECT IssuedTo, COUNT(*) as Total
        FROM Receipts
        GROUP BY IssuedTo
        ORDER BY Total DESC
        LIMIT $top";
            cmd.Parameters.AddWithValue("$top", top);

            using var reader = cmd.ExecuteReader();
            while (reader.Read())
                list.Add((reader.GetString(0), reader.GetInt32(1)));

            return list;
        }

        // Shared receipt reader — avoids code duplication
        private Receipt ReadReceipt(SqliteDataReader reader)
        {
            return new Receipt
            {
                Id = reader.GetInt32(0),
                ReceiptNumber = SafeGetString(reader, 1),
                Type = (ReceiptType)reader.GetInt32(2),
                IssuedTo = SafeGetString(reader, 3),
                IdNumber = SafeGetString(reader, 4),
                OrganizationName = SafeGetString(reader, 5),
                ClubName = SafeGetString(reader, 6),
                DateIssued = DateTime.Parse(SafeGetString(reader, 7)),
                Items = JsonSerializer
                                       .Deserialize<List<ReceiptItem>>(
                                       SafeGetString(reader, 8))
                                   ?? new List<ReceiptItem>(),
                TotalAmount = (decimal)reader.GetDouble(9),
                Notes = SafeGetString(reader, 10),
                CashierName = SafeGetString(reader, 11)
            };
        }

        // ── Soft delete ───────────────────────────────────────────────────
        public void SoftDeleteReceipt(int id)
        {
            using var conn = new SqliteConnection(_connectionString);
            conn.Open();
            var cmd = conn.CreateCommand();
            cmd.CommandText = @"
        UPDATE Receipts
        SET IsDeleted = 1,
            DeletedAt = $date
        WHERE Id = $id";
            cmd.Parameters.AddWithValue("$date",
                DateTime.Now.ToString("o"));
            cmd.Parameters.AddWithValue("$id", id);
            cmd.ExecuteNonQuery();
        }

        // ── Restore from trash ────────────────────────────────────────────
        public void RestoreReceipt(int id)
        {
            using var conn = new SqliteConnection(_connectionString);
            conn.Open();
            var cmd = conn.CreateCommand();
            cmd.CommandText = @"
        UPDATE Receipts
        SET IsDeleted = 0,
            DeletedAt = NULL
        WHERE Id = $id";
            cmd.Parameters.AddWithValue("$id", id);
            cmd.ExecuteNonQuery();
        }

        // ── Permanently delete ────────────────────────────────────────────
        public void PermanentlyDeleteReceipt(int id)
        {
            using var conn = new SqliteConnection(_connectionString);
            conn.Open();
            var cmd = conn.CreateCommand();
            cmd.CommandText = "DELETE FROM Receipts WHERE Id = $id";
            cmd.Parameters.AddWithValue("$id", id);
            cmd.ExecuteNonQuery();
        }

        // ── Get trash items ───────────────────────────────────────────────
        public List<Receipt> GetDeletedReceipts()
        {
            var list = new List<Receipt>();
            using var conn = new SqliteConnection(_connectionString);
            conn.Open();
            var cmd = conn.CreateCommand();
            cmd.CommandText = @"
        SELECT * FROM Receipts
        WHERE IsDeleted = 1
        ORDER BY DeletedAt DESC";

            using var reader = cmd.ExecuteReader();
            while (reader.Read())
            {
                try { list.Add(ReadReceipt(reader)); }
                catch { }
            }
            return list;
        }

        // ── Empty trash ───────────────────────────────────────────────────
        public void EmptyTrash()
        {
            using var conn = new SqliteConnection(_connectionString);
            conn.Open();
            var cmd = conn.CreateCommand();
            cmd.CommandText =
                "DELETE FROM Receipts WHERE IsDeleted = 1";
            cmd.ExecuteNonQuery();
        }

        // ── Check if receipt number already exists ────────────────────────
        public bool ReceiptNumberExists(
            string receiptNumber, int excludeId = 0)
        {
            using var conn = new SqliteConnection(_connectionString);
            conn.Open();
            var cmd = conn.CreateCommand();
            cmd.CommandText = @"
        SELECT COUNT(*) FROM Receipts
        WHERE ReceiptNumber = $num
          AND Id != $excludeId
          AND IsDeleted = 0";
            cmd.Parameters.AddWithValue("$num", receiptNumber);
            cmd.Parameters.AddWithValue("$excludeId", excludeId);
            return Convert.ToInt32(cmd.ExecuteScalar()) > 0;
        }
    }
}
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

            // Check whether the legacy/previous table exists
            var check = conn.CreateCommand();
            check.CommandText = "SELECT name FROM sqlite_master WHERE type='table' AND name='Receipts';";
            var receiptsExists = check.ExecuteScalar() != null;

            if (receiptsExists)
            {
                // Run migration in a transaction and execute statements one-by-one so
                // a missing table or other problem doesn't abort database init.
                using var tran = conn.BeginTransaction();
                try
                {
                    var createNew = conn.CreateCommand();
                    createNew.CommandText = @"
                        CREATE TABLE IF NOT EXISTS Receipts_New (
                            Id               INTEGER PRIMARY KEY AUTOINCREMENT,
                            ReceiptNumber    TEXT NOT NULL,
                            IssuedTo         TEXT,
                            IdNumber         TEXT,
                            OrganizationName TEXT,
                            DateIssued       TEXT,
                            ItemsJson        TEXT,
                            TotalAmount      REAL,
                            Notes            TEXT,
                            CashierName      TEXT,
                            IsDeleted        INTEGER DEFAULT 0,
                            DeletedAt        TEXT
                        );";
                    createNew.ExecuteNonQuery();

                    // Try copying data; if this fails for any reason just continue so
                    // app can create/repair the schema instead of crashing.
                    try
                    {
                        var copy = conn.CreateCommand();
                        copy.CommandText = @"
                            INSERT INTO Receipts_New 
                            SELECT Id, ReceiptNumber, IssuedTo, IdNumber, OrganizationName, 
                                   DateIssued, ItemsJson, TotalAmount, Notes, CashierName,
                                   IsDeleted, DeletedAt 
                            FROM Receipts;";
                        copy.ExecuteNonQuery();
                    }
                    catch (Microsoft.Data.Sqlite.SqliteException)
                    {
                        // If copy fails, continue — we'll drop/rename below or recreate later.
                    }

                    var drop = conn.CreateCommand();
                    drop.CommandText = "DROP TABLE IF EXISTS Receipts;";
                    drop.ExecuteNonQuery();

                    var rename = conn.CreateCommand();
                    rename.CommandText = "ALTER TABLE Receipts_New RENAME TO Receipts;";
                    rename.ExecuteNonQuery();

                    tran.Commit();
                }
                catch
                {
                    try { tran.Rollback(); } catch { }
                    // swallow — we'll attempt to ensure final schema below
                }
            }
            else
            {
                // No existing table — create the current schema directly
                var create = conn.CreateCommand();
                create.CommandText = @"
                    CREATE TABLE IF NOT EXISTS Receipts (
                        Id               INTEGER PRIMARY KEY AUTOINCREMENT,
                        ReceiptNumber    TEXT NOT NULL,
                        IssuedTo         TEXT,
                        IdNumber         TEXT,
                        OrganizationName TEXT,
                        DateIssued       TEXT,
                        ItemsJson        TEXT,
                        TotalAmount      REAL,
                        Notes            TEXT,
                        CashierName      TEXT,
                        IsDeleted        INTEGER DEFAULT 0,
                        DeletedAt        TEXT
                    );";
                create.ExecuteNonQuery();
            }

            // Safe column additions for upgrades — still run to ensure columns exist
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
                    var a = conn.CreateCommand();
                    a.CommandText = sql;
                    a.ExecuteNonQuery();
                }
                catch { }
            }

            // Seed preset items if table is empty
            SeedPresetItems(conn);
        }

        // ── Preset Items ──────────────────────────────────────────────
        private void SeedPresetItems(SqliteConnection conn)
        {
            var check = conn.CreateCommand();
            check.CommandText = @"
        CREATE TABLE IF NOT EXISTS PresetItems (
            Id           INTEGER PRIMARY KEY AUTOINCREMENT,
            Name         TEXT NOT NULL,
            DefaultPrice REAL DEFAULT 0,
            Category     TEXT DEFAULT '',
            Description  TEXT DEFAULT ''
        );";
            check.ExecuteNonQuery();

            // Safely add new columns if upgrading
            var alterCmds = new[]
            {
        "ALTER TABLE PresetItems ADD COLUMN Category TEXT DEFAULT '';",
        "ALTER TABLE PresetItems ADD COLUMN Description TEXT DEFAULT '';"
    };
            foreach (var sql in alterCmds)
            {
                try
                {
                    var a = conn.CreateCommand();
                    a.CommandText = sql;
                    a.ExecuteNonQuery();
                }
                catch { }
            }

            var count = conn.CreateCommand();
            count.CommandText = "SELECT COUNT(*) FROM PresetItems";
            long existing = (long)(count.ExecuteScalar() ?? 0L);
            if (existing > 0) return;

            // Demo preset items with categories
            var items = new[]
            {
        ("Membership Fee",    150.00, "Fees",
            "Annual membership fee"),
        ("Registration Fee",  200.00, "Fees",
            "Event or club registration fee"),
        ("T-Shirt",           250.00, "Merchandise",
            "Organization t-shirt"),
        ("ID Lace",            50.00, "Merchandise",
            "Branded ID lace"),
        ("Event Ticket",      100.00, "Fees",
            "General admission ticket"),
        ("Chocolate Chips",    85.00, "Food",
            "Pack of chocolate chip cookies"),
        ("Stick-O",            20.00, "Food",
            "Stick-O wafer sticks"),
        ("Bottled Water",      25.00, "Food",
            "500ml bottled water"),
        ("Rice Meal",          60.00, "Food",
            "Standard rice meal"),
        ("Yearbook",          350.00, "Merchandise",
            "Annual school yearbook")
    };

            foreach (var (name, price, cat, desc) in items)
            {
                var ins = conn.CreateCommand();
                ins.CommandText = @"
            INSERT INTO PresetItems
            (Name, DefaultPrice, Category, Description)
            VALUES ($n, $p, $c, $d)";
                ins.Parameters.AddWithValue("$n", name);
                ins.Parameters.AddWithValue("$p", price);
                ins.Parameters.AddWithValue("$c", cat);
                ins.Parameters.AddWithValue("$d", desc);
                ins.ExecuteNonQuery();
            }
        }

        // Full preset item model
        public class PresetItem
        {
            public int Id { get; set; }
            public string Name { get; set; } = "";
            public double DefaultPrice { get; set; }
            public string Category { get; set; } = "";
            public string Description { get; set; } = "";
        }

        public List<PresetItem> GetPresetItems()
        {
            var list = new List<PresetItem>();
            using var conn = new SqliteConnection(_connectionString);
            conn.Open();
            var cmd = conn.CreateCommand();
            cmd.CommandText = @"
        SELECT Id, Name, DefaultPrice, Category, Description
        FROM PresetItems
        ORDER BY Category ASC, Name ASC";
            using var reader = cmd.ExecuteReader();
            while (reader.Read())
            {
                list.Add(new PresetItem
                {
                    Id = reader.GetInt32(0),
                    Name = SafeGetString(reader, 1),
                    DefaultPrice = reader.GetDouble(2),
                    Category = SafeGetString(reader, 3),
                    Description = SafeGetString(reader, 4)
                });
            }
            return list;
        }

        public List<string> GetPresetCategories()
        {
            var list = new List<string>();
            using var conn = new SqliteConnection(_connectionString);
            conn.Open();
            var cmd = conn.CreateCommand();
            cmd.CommandText = @"
        SELECT DISTINCT Category FROM PresetItems
        WHERE Category != ''
        ORDER BY Category ASC";
            using var reader = cmd.ExecuteReader();
            while (reader.Read())
                list.Add(reader.GetString(0));
            return list;
        }

        public void AddPresetItem(PresetItem item)
        {
            using var conn = new SqliteConnection(_connectionString);
            conn.Open();
            var cmd = conn.CreateCommand();
            cmd.CommandText = @"
        INSERT INTO PresetItems
        (Name, DefaultPrice, Category, Description)
        VALUES ($n, $p, $c, $d)";
            cmd.Parameters.AddWithValue("$n", item.Name);
            cmd.Parameters.AddWithValue("$p", item.DefaultPrice);
            cmd.Parameters.AddWithValue("$c", item.Category ?? "");
            cmd.Parameters.AddWithValue("$d", item.Description ?? "");
            cmd.ExecuteNonQuery();
        }

        public void UpdatePresetItem(PresetItem item)
        {
            using var conn = new SqliteConnection(_connectionString);
            conn.Open();
            var cmd = conn.CreateCommand();
            cmd.CommandText = @"
        UPDATE PresetItems SET
            Name         = $n,
            DefaultPrice = $p,
            Category     = $c,
            Description  = $d
        WHERE Id = $id";
            cmd.Parameters.AddWithValue("$n", item.Name);
            cmd.Parameters.AddWithValue("$p", item.DefaultPrice);
            cmd.Parameters.AddWithValue("$c", item.Category ?? "");
            cmd.Parameters.AddWithValue("$d", item.Description ?? "");
            cmd.Parameters.AddWithValue("$id", item.Id);
            cmd.ExecuteNonQuery();
        }

        public void DeletePresetItem(int id)
        {
            using var conn = new SqliteConnection(_connectionString);
            conn.Open();
            var cmd = conn.CreateCommand();
            cmd.CommandText =
                "DELETE FROM PresetItems WHERE Id = $id";
            cmd.Parameters.AddWithValue("$id", id);
            cmd.ExecuteNonQuery();
        }

        public bool PresetItemNameExists(string name, int excludeId = 0)
        {
            using var conn = new SqliteConnection(_connectionString);
            conn.Open();
            var cmd = conn.CreateCommand();
            cmd.CommandText = @"
        SELECT COUNT(*) FROM PresetItems
        WHERE Name = $name AND Id != $id";
            cmd.Parameters.AddWithValue("$name", name);
            cmd.Parameters.AddWithValue("$id", excludeId);
            return Convert.ToInt32(cmd.ExecuteScalar()) > 0;
        }

        // ── Save / Update ─────────────────────────────────────────────
        public void SaveReceipt(Receipt receipt)
        {
            using var conn = new SqliteConnection(_connectionString);
            conn.Open();
            var cmd = conn.CreateCommand();
            cmd.CommandText = @"
                INSERT INTO Receipts
                (ReceiptNumber, IssuedTo, IdNumber, OrganizationName,
                 DateIssued, ItemsJson, TotalAmount, Notes, CashierName)
                VALUES
                ($num, $to, $id, $org,
                 $date, $items, $total, $notes, $cashier)";

            cmd.Parameters.AddWithValue("$num", receipt.ReceiptNumber);
            cmd.Parameters.AddWithValue("$to", receipt.IssuedTo ?? "");
            cmd.Parameters.AddWithValue("$id", receipt.IdNumber ?? "");
            cmd.Parameters.AddWithValue("$org", receipt.OrganizationName ?? "");
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
                    IssuedTo         = $to,
                    IdNumber         = $id,
                    OrganizationName = $org,
                    DateIssued       = $date,
                    ItemsJson        = $items,
                    TotalAmount      = $total,
                    Notes            = $notes,
                    CashierName      = $cashier
                WHERE Id = $id_pk";

            cmd.Parameters.AddWithValue("$to", receipt.IssuedTo ?? "");
            cmd.Parameters.AddWithValue("$id", receipt.IdNumber ?? "");
            cmd.Parameters.AddWithValue("$org", receipt.OrganizationName ?? "");
            cmd.Parameters.AddWithValue("$date", receipt.DateIssued.ToString("o"));
            cmd.Parameters.AddWithValue("$items",
                JsonSerializer.Serialize(receipt.Items));
            cmd.Parameters.AddWithValue("$total", (double)receipt.TotalAmount);
            cmd.Parameters.AddWithValue("$notes", receipt.Notes ?? "");
            cmd.Parameters.AddWithValue("$cashier", receipt.CashierName ?? "");
            cmd.Parameters.AddWithValue("$id_pk", receipt.Id);
            cmd.ExecuteNonQuery();
        }

        // ── Read ──────────────────────────────────────────────────────
        public List<Receipt> GetAllReceipts()
            => QueryReceipts(
                "SELECT * FROM Receipts WHERE IsDeleted = 0 " +
                "ORDER BY DateIssued DESC");

        public List<Receipt> GetReceiptsThisMonth()
            => QueryReceipts(
                "SELECT * FROM Receipts WHERE IsDeleted = 0 " +
                "AND strftime('%Y-%m', DateIssued) = '" +
                DateTime.Now.ToString("yyyy-MM") + "' " +
                "ORDER BY DateIssued DESC");

        public List<Receipt> GetRecentReceipts(int count = 5)
            => QueryReceipts(
                $"SELECT * FROM Receipts WHERE IsDeleted = 0 " +
                $"ORDER BY DateIssued DESC LIMIT {count}");

        public List<Receipt> GetDeletedReceipts()
            => QueryReceipts(
                "SELECT * FROM Receipts WHERE IsDeleted = 1 " +
                "ORDER BY DeletedAt DESC");

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
                   OR  OrganizationName LIKE $q)
                ORDER BY DateIssued DESC";
            cmd.Parameters.AddWithValue("$q", $"%{query}%");
            using var reader = cmd.ExecuteReader();
            while (reader.Read())
            {
                try { list.Add(ReadReceipt(reader)); }
                catch { }
            }
            return list;
        }

        public Receipt? GetReceiptByNumber(string receiptNumber)
        {
            using var conn = new SqliteConnection(_connectionString);
            conn.Open();
            var cmd = conn.CreateCommand();
            cmd.CommandText =
                "SELECT * FROM Receipts WHERE ReceiptNumber = $num LIMIT 1";
            cmd.Parameters.AddWithValue("$num", receiptNumber);
            using var reader = cmd.ExecuteReader();
            if (reader.Read())
            {
                try { return ReadReceipt(reader); }
                catch { }
            }
            return null;
        }

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
                WHERE IsDeleted = 0
                  AND DateIssued >= date('now', $days)
                GROUP BY Day ORDER BY Day ASC";
            cmd.Parameters.AddWithValue("$days", $"-{days} days");
            using var reader = cmd.ExecuteReader();
            while (reader.Read())
                list.Add((reader.GetString(0),
                          (decimal)reader.GetDouble(1)));
            return list;
        }

        public List<(string Name, int Count)> GetTopRecipients(int top = 5)
        {
            var list = new List<(string, int)>();
            using var conn = new SqliteConnection(_connectionString);
            conn.Open();
            var cmd = conn.CreateCommand();
            cmd.CommandText = @"
                SELECT IssuedTo, COUNT(*) as Total
                FROM Receipts WHERE IsDeleted = 0
                GROUP BY IssuedTo
                ORDER BY Total DESC LIMIT $top";
            cmd.Parameters.AddWithValue("$top", top);
            using var reader = cmd.ExecuteReader();
            while (reader.Read())
                list.Add((reader.GetString(0), reader.GetInt32(1)));
            return list;
        }

        // ── Soft delete / restore ─────────────────────────────────────
        public void SoftDeleteReceipt(int id)
        {
            using var conn = new SqliteConnection(_connectionString);
            conn.Open();
            var cmd = conn.CreateCommand();
            cmd.CommandText = @"
                UPDATE Receipts SET IsDeleted = 1,
                DeletedAt = $date WHERE Id = $id";
            cmd.Parameters.AddWithValue("$date",
                DateTime.Now.ToString("o"));
            cmd.Parameters.AddWithValue("$id", id);
            cmd.ExecuteNonQuery();
        }

        public void SoftDeleteMultiple(List<int> ids)
        {
            using var conn = new SqliteConnection(_connectionString);
            conn.Open();
            foreach (var id in ids)
            {
                var cmd = conn.CreateCommand();
                cmd.CommandText = @"
                    UPDATE Receipts SET IsDeleted = 1,
                    DeletedAt = $date WHERE Id = $id";
                cmd.Parameters.AddWithValue("$date",
                    DateTime.Now.ToString("o"));
                cmd.Parameters.AddWithValue("$id", id);
                cmd.ExecuteNonQuery();
            }
        }

        public void RestoreReceipt(int id)
        {
            using var conn = new SqliteConnection(_connectionString);
            conn.Open();
            var cmd = conn.CreateCommand();
            cmd.CommandText = @"
                UPDATE Receipts SET IsDeleted = 0,
                DeletedAt = NULL WHERE Id = $id";
            cmd.Parameters.AddWithValue("$id", id);
            cmd.ExecuteNonQuery();
        }

        public void RestoreMultiple(List<int> ids)
        {
            using var conn = new SqliteConnection(_connectionString);
            conn.Open();
            foreach (var id in ids)
            {
                var cmd = conn.CreateCommand();
                cmd.CommandText = @"
                    UPDATE Receipts SET IsDeleted = 0,
                    DeletedAt = NULL WHERE Id = $id";
                cmd.Parameters.AddWithValue("$id", id);
                cmd.ExecuteNonQuery();
            }
        }

        public void PermanentlyDeleteReceipt(int id)
        {
            using var conn = new SqliteConnection(_connectionString);
            conn.Open();
            var cmd = conn.CreateCommand();
            cmd.CommandText =
                "DELETE FROM Receipts WHERE Id = $id";
            cmd.Parameters.AddWithValue("$id", id);
            cmd.ExecuteNonQuery();
        }

        public void PermanentlyDeleteMultiple(List<int> ids)
        {
            using var conn = new SqliteConnection(_connectionString);
            conn.Open();
            foreach (var id in ids)
            {
                var cmd = conn.CreateCommand();
                cmd.CommandText =
                    "DELETE FROM Receipts WHERE Id = $id";
                cmd.Parameters.AddWithValue("$id", id);
                cmd.ExecuteNonQuery();
            }
        }

        public void EmptyTrash()
        {
            using var conn = new SqliteConnection(_connectionString);
            conn.Open();
            var cmd = conn.CreateCommand();
            cmd.CommandText =
                "DELETE FROM Receipts WHERE IsDeleted = 1";
            cmd.ExecuteNonQuery();
        }

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

        // ── Helpers ───────────────────────────────────────────────────

        private string SafeGetString(SqliteDataReader reader, int ordinal)
        {
            return !reader.IsDBNull(ordinal) ? reader.GetString(ordinal) : "";
        }
        private List<Receipt> QueryReceipts(string sql)
        {
            var list = new List<Receipt>();
            using var conn = new SqliteConnection(_connectionString);
            conn.Open();
            var cmd = conn.CreateCommand();
            cmd.CommandText = sql;
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

        private Receipt ReadReceipt(SqliteDataReader reader)
        {
            var receipt = new Receipt();

            // Helper to safely get values by column name
            string GetStr(string col) =>
                reader.GetOrdinal(col) >= 0 && !reader.IsDBNull(reader.GetOrdinal(col))
                    ? reader.GetString(reader.GetOrdinal(col))
                    : "";

            double GetDbl(string col) =>
                reader.GetOrdinal(col) >= 0 && !reader.IsDBNull(reader.GetOrdinal(col))
                    ? reader.GetDouble(reader.GetOrdinal(col))
                    : 0;

            int GetInt(string col) =>
                reader.GetOrdinal(col) >= 0 && !reader.IsDBNull(reader.GetOrdinal(col))
                    ? reader.GetInt32(reader.GetOrdinal(col))
                    : 0;

            receipt.Id = GetInt("Id");
            receipt.ReceiptNumber = GetStr("ReceiptNumber");
            receipt.IssuedTo = GetStr("IssuedTo");
            receipt.IdNumber = GetStr("IdNumber");
            receipt.OrganizationName = GetStr("OrganizationName");

            // Safe DateTime parsing
            var dateStr = GetStr("DateIssued");
            receipt.DateIssued = DateTime.TryParse(dateStr, out var d) ? d : DateTime.MinValue;

            // Safe JSON deserialization
            var itemsJson = GetStr("ItemsJson");
            receipt.Items = string.IsNullOrWhiteSpace(itemsJson)

                ? new List<ReceiptItem>()
                : JsonSerializer.Deserialize<List<ReceiptItem>>(itemsJson) ?? new List<ReceiptItem>();

            receipt.TotalAmount = (decimal)GetDbl("TotalAmount");
            receipt.Notes = GetStr("Notes");
            receipt.CashierName = GetStr("CashierName");


            return receipt;
        }
    }


}
using System;
using System.Text.RegularExpressions;

namespace EReceiptApp.Services
{
    public static class InputSanitizer
    {

        // ── Sanitize: strip dangerous content ────────────────────────

        // General text fields — removes HTML/script tags and
        // dangerous SQL characters
        public static string SanitizeText(string input)
        {
            if (string.IsNullOrWhiteSpace(input))
                return string.Empty;

            // Remove HTML and script tags
            input = Regex.Replace(input, @"<[^>]*>", string.Empty);

            // Remove common SQL injection patterns
            input = Regex.Replace(input,
                @"('|--|;|/\*|\*/|xp_|DROP|INSERT|DELETE|UPDATE|SELECT|EXEC|UNION)",
                string.Empty,
                RegexOptions.IgnoreCase);

            // Remove control characters
            input = Regex.Replace(input, @"[\x00-\x1F\x7F]", string.Empty);

            // Trim whitespace
            return input.Trim();
        }

        // Numeric fields — only allow digits, dots, and minus
        public static string SanitizeNumeric(string input)
        {
            if (string.IsNullOrWhiteSpace(input))
                return "0";

            // Only keep digits and decimal point
            input = Regex.Replace(input, @"[^\d.]", string.Empty);

            // Prevent multiple decimal points
            var parts = input.Split('.');
            if (parts.Length > 2)
                input = parts[0] + "." + parts[1];

            return string.IsNullOrWhiteSpace(input) ? "0" : input;
        }

        // Name fields — only allow letters, spaces, hyphens, dots
        public static string SanitizeName(string input)
        {
            if (string.IsNullOrWhiteSpace(input))
                return string.Empty;

            // Allow letters, numbers, spaces, hyphens, dots, apostrophes
            // This supports names like "4F BAKESHOP" and "113 STORE"
            input = Regex.Replace(input,
                @"[^a-zA-Z0-9À-ÖØ-öø-ÿ\s\-\.\']", string.Empty);

            return input.Trim();
        }

        // ID Number — only allow alphanumeric and hyphens
        public static string SanitizeIdNumber(string input)
        {
            if (string.IsNullOrWhiteSpace(input))
                return string.Empty;

            input = Regex.Replace(input, @"[^a-zA-Z0-9\-]", string.Empty);
            return input.Trim();
        }

        // ── Validate: check rules ─────────────────────────────────────

        public static (bool IsValid, string Error) ValidateName(
            string value, string fieldName, bool required = true)
        {
            if (required && string.IsNullOrWhiteSpace(value))
                return (false, $"{fieldName} is required.");

            if (value.Length > 100)
                return (false, $"{fieldName} must be under 100 characters.");

            return (true, string.Empty);
        }

        public static (bool IsValid, string Error) ValidateText(
            string value, string fieldName,
            bool required = false, int maxLength = 200)
        {
            if (required && string.IsNullOrWhiteSpace(value))
                return (false, $"{fieldName} is required.");

            if (value.Length > maxLength)
                return (false,
                    $"{fieldName} must be under {maxLength} characters.");

            return (true, string.Empty);
        }

        public static (bool IsValid, string Error) ValidateIdNumber(
            string value)
        {
            if (string.IsNullOrWhiteSpace(value))
                return (true, string.Empty); // optional field

            if (value.Length > 30)
                return (false, "ID Number must be under 30 characters.");

            if (!Regex.IsMatch(value, @"^[a-zA-Z0-9\-]+$"))
                return (false,
                    "ID Number can only contain letters, numbers, and hyphens.");

            return (true, string.Empty);
        }

        public static (bool IsValid, string Error) ValidateAmount(
            string value)
        {
            if (!decimal.TryParse(value, out decimal amount))
                return (false, "Price must be a valid number.");

            if (amount < 0)
                return (false, "Price cannot be negative.");

            if (amount > 999999)
                return (false, "Price cannot exceed 999,999.");

            return (true, string.Empty);
        }

        public static (bool IsValid, string Error) ValidateQuantity(
            string value)
        {
            if (!int.TryParse(value, out int qty))
                return (false, "Quantity must be a whole number.");

            if (qty <= 0)
                return (false, "Quantity must be at least 1.");

            if (qty > 9999)
                return (false, "Quantity cannot exceed 9,999.");

            return (true, string.Empty);
        }
    }
}
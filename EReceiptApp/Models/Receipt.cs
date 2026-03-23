using System;
using System.Collections.Generic;

namespace EReceiptApp.Models
{
    public enum ReceiptType
    {
        Standard,
        Membership
    }

    public class Receipt
    {
        public int Id { get; set; }
        public string ReceiptNumber { get; set; } = "";
        public ReceiptType Type { get; set; }
        public string IssuedTo { get; set; } = "";
        public string IdNumber { get; set; } = "";
        public string OrganizationName { get; set; } = "";
        public string ClubName { get; set; } = "";
        public DateTime DateIssued { get; set; } = DateTime.Now;
        public List<ReceiptItem> Items { get; set; } = new List<ReceiptItem>();
        public decimal TotalAmount { get; set; }
        public string Notes { get; set; } = "";
        public string CashierName { get; set; } = "";
    }
}
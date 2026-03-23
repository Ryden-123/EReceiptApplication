namespace EReceiptApp.Models
{
    public class ReceiptItem
    {
        public string Description { get; set; } = "";
        public int Quantity { get; set; } = 1;
        public decimal UnitPrice { get; set; }
        public decimal Total => Quantity * UnitPrice;
    }
}
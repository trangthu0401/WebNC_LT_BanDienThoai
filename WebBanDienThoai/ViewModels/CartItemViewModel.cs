namespace WebBanDienThoai.ViewModels
{
    public class CartItemViewModel
    {
        public int VariantId { get; set; } // <-- Không có dấu '?'
        public string ProductName { get; set; }
        public string Color { get; set; }
        public string Storage { get; set; }
        public int Quantity { get; set; }
        public decimal UnitPrice { get; set; }
        public string ImageUrl { get; set; }
    }
}
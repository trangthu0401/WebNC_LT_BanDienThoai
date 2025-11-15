// Trong file: Models/modelView/CartItemViewModel.cs
namespace WebBanDienThoai.Models.modelView
{
    public class CartItemViewModel
    {
        public int VariantId { get; set; }
        public int CartItemId { get; set; }
        public string ProductName { get; set; } = null!;
        public string? Color { get; set; }
        public string? ImageUrl { get; set; }
        public decimal Price { get; set; }
        public int Quantity { get; set; }
        public decimal TotalPrice { get; set; } // Price * Quantity
    }
}

// Trong file: Models/modelView/CartViewModel.cs
namespace WebBanDienThoai.Models.modelView
{
    public class CartViewModel
    {
        public List<CartItemViewModel> CartItems { get; set; }
        public decimal TotalAmount { get; set; }

        public CartViewModel()
        {
            CartItems = new List<CartItemViewModel>();
        }
    }
}
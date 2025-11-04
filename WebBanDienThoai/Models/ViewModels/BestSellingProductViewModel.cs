namespace WebBanDienThoai.Models.ViewModels
{
    public class BestSellingProductViewModel
    {
        // Sửa lỗi Non-nullable
        public string ProductName { get; set; } = string.Empty;

        public string? ImageUrl { get; set; }
        public int QuantitySold { get; set; }

        // Sửa: Bỏ '?' để khớp với CSDL (DemoWebBanDienThoaiContext)
        public decimal Price { get; set; }

        public int Stock { get; set; }

        // DÒNG BỊ LỖI COPY-PASTE ĐÃ ĐƯỢC XÓA TẠI ĐÂY
    }
}
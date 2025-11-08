using System.ComponentModel.DataAnnotations;

namespace WebBanDienThoai.Models.ViewModels
{
    /// <summary>
    /// ViewModel cho Modal "Sửa Biến thể".
    /// (Dùng cho ProductVariantController action Edit POST)
    /// </summary>
    public class ProductVariantEditViewModel
    {
        public int ProductId { get; set; } // Để biết đường quay lại
        public int VariantId { get; set; } // Để biết sửa cái nào

        [Required]
        public string Color { get; set; }
        [Required]
        public string Storage { get; set; }
        public string? Ram { get; set; }
        [Required]
        public decimal Price { get; set; }
        public decimal? DiscountPrice { get; set; }
        [Required]
        public int Stock { get; set; }
    }
}
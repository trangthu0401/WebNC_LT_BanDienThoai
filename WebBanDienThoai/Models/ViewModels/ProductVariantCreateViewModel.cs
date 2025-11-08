using Microsoft.AspNetCore.Http;
using System.ComponentModel.DataAnnotations;

namespace WebBanDienThoai.Models.ViewModels
{
    /// <summary>
    /// ViewModel cho Modal "Thêm mới Biến thể".
    /// (Dùng cho ProductVariantController action Create POST)
    /// </summary>
    public class ProductVariantCreateViewModel
    {
        public int ProductId { get; set; }

        [Required(ErrorMessage = "Màu sắc là bắt buộc")]
        public string? Color { get; set; }

        [Required(ErrorMessage = "Dung lượng là bắt buộc")]
        public string? Storage { get; set; }
        public string? Ram { get; set; }

        [Required(ErrorMessage = "Giá là bắt buộc")]
        public decimal Price { get; set; }
        public decimal? DiscountPrice { get; set; }

        [Required(ErrorMessage = "Tồn kho là bắt buộc")]
        public int Stock { get; set; }
        public IFormFile? ImageFile { get; set; }
    }
}

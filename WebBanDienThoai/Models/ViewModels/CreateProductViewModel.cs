using Microsoft.AspNetCore.Mvc.Rendering;
using WebBanDienThoai.Models;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

// === SỬA LỖI NAMESPACE TẠI ĐÂY ===
// Đảm bảo nó khớp với @model trong View
namespace WebBanDienThoai.Models.ViewModels
{
    public class CreateProductViewModel
    {
        // Sửa lỗi Non-nullable: Gán giá trị mặc định
        public Product Product { get; set; } = new Product();

        public IEnumerable<SelectListItem> BrandList { get; set; } = new List<SelectListItem>();

        [Display(Name = "Ảnh chính của sản phẩm")]
        // Sửa lỗi Non-nullable: Thêm '?' để cho phép null
        public IFormFile? MainImageFile { get; set; }

        // Biến thể đầu tiên
        [Required]
        // Sửa lỗi Non-nullable: Gán giá trị mặc định
        public string VariantColor { get; set; } = string.Empty;
        [Required]
        // Sửa lỗi Non-nullable: Gán giá trị mặc định
        public string VariantStorage { get; set; } = string.Empty;

        // Sửa lỗi Non-nullable: Thêm '?' để cho phép null
        public string? VariantRam { get; set; }

        [Required]
        public decimal VariantPrice { get; set; }
        [Required]
        public int VariantStock { get; set; }

        [Display(Name = "Ảnh riêng của biến thể (nếu có)")]
        // Sửa lỗi Non-nullable: Thêm '?' để cho phép null
        public IFormFile? VariantImageFile { get; set; }
    }
}
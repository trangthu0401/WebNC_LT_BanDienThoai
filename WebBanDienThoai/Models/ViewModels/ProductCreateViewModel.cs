using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Rendering;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using WebBanDienThoai.Models; // Cần using Models

namespace WebBanDienThoai.Models.ViewModels
{
    /// <summary>
    /// ViewModel cho trang "Tạo mới Sản phẩm".
    /// (Dùng cho Views/Product/Create.cshtml)
    /// </summary>
    public class ProductCreateViewModel
    {
        // 1. Dữ liệu cho Product (cha)
        public Product Product { get; set; } = new Product();
        public List<SelectListItem> BrandList { get; set; } = new List<SelectListItem>();

        [Required(ErrorMessage = "Vui lòng chọn ảnh cho sản phẩm")]
        public IFormFile? MainImageFile { get; set; } // Ảnh chính

        // 2. Dữ liệu cho Biến thể đầu tiên (con)
        [Required(ErrorMessage = "Màu sắc là bắt buộc")]
        public string VariantColor { get; set; }

        [Required(ErrorMessage = "Dung lượng là bắt buộc")]
        public string VariantStorage { get; set; }
        public string? VariantRam { get; set; }

        [Required(ErrorMessage = "Giá là bắt buộc")]
        public decimal VariantPrice { get; set; }

        [Required(ErrorMessage = "Tồn kho là bắt buộc")]
        public int VariantStock { get; set; }
        public IFormFile? VariantImageFile { get; set; } // Ảnh riêng của biến thể
    }
}
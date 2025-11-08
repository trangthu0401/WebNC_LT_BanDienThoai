using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Rendering;
using System.Collections.Generic;
using WebBanDienThoai.Models; // Cần using Models

namespace WebBanDienThoai.Models.ViewModels
{
    /// <summary>
    /// ViewModel cho trang "Sửa Sản phẩm".
    /// (Dùng cho Views/Product/Edit.cshtml)
    /// </summary>
    public class ProductEditViewModel
    {
        // Chỉ chứa thông tin Product (cha)
        public Product Product { get; set; } = new Product();
        public List<SelectListItem> BrandList { get; set; } = new List<SelectListItem>();
        public IFormFile? MainImageFile { get; set; } // Ảnh chính (nếu muốn đổi)
    }
}
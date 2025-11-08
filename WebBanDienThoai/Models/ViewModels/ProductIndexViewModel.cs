using System;
using System.Collections.Generic;

namespace WebBanDienThoai.Models.ViewModels
{
    // === DÙNG CHO TRANG INDEX (DANH SÁCH) SẢN PHẨM ===
    // (Model chính cho Views/Product/Index.cshtml)
    public class ProductIndexViewModel
    {
        public List<ProductListViewModel> Products { get; set; } = new List<ProductListViewModel>();
        public List<BrandCountViewModel> BrandCounts { get; set; } = new List<BrandCountViewModel>();
        public int TotalProductCount { get; set; }
    }

    // (ViewModel con cho 1 hàng (row) trong bảng)
    public class ProductListViewModel
    {
        public int ProductId { get; set; }
        public string Name { get; set; }
        public string MainImage { get; set; }
        public int BrandId { get; set; }
        public string BrandName { get; set; }
        public DateTime CreatedDate { get; set; }
        public bool IsActive { get; set; }
        public decimal FirstVariantPrice { get; set; }
        public decimal LowestPrice { get; set; }
        public int TotalStock { get; set; }
    }

    // (ViewModel con cho tab lọc Hãng)
    public class BrandCountViewModel
    {
        public int brandId { get; set; }
        public string BrandName { get; set; }
        public bool IsActive { get; set; }
        public int Count { get; set; }
    }
}
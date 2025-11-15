using System;
using System.Collections.Generic;

namespace WebBanDienThoai.Models.modelView
{
    public class BrandCount
    {
        public int brandId { get; set; }
        public string BrandName { get; set; } = string.Empty;
        public int Count { get; set; }
    }

    public class ProductListViewModel
    {
        public int ProductId { get; set; }
        public string Name { get; set; } = string.Empty;
        public string? MainImage { get; set; }
        public int? BrandId { get; set; }
        public string BrandName { get; set; } = string.Empty;
        public DateTime? CreatedDate { get; set; }
        public bool? IsActive { get; set; }
        public decimal Price { get; set; }
        public int Stock { get; set; }
        public bool IsFavorited { get; set; } = false;

        // === THÊM DÒNG NÀY (Để biết link đến variant nào) ===
        public int FirstVariantId { get; set; }
    }

    public class ManageProductsViewModel
    {
        public List<ProductListViewModel> Products { get; set; } = new List<ProductListViewModel>();
        public List<BrandCount> BrandCounts { get; set; } = new List<BrandCount>();
        public int? BrandId { get; set; }
        public int TotalProductCount { get; set; }
    }

    public class FavoritesViewModel
    {
        public List<ProductListViewModel> FavoriteProducts { get; set; } = new List<ProductListViewModel>();
        // === THÊM 2 DÒNG NÀY (Để fix lỗi dropdown ở trang Yêu thích) ===
        public List<BrandCount> BrandCounts { get; set; } = new List<BrandCount>();
        public int TotalProductCount { get; set; }
    }

    public class ProductSuggestionViewModel
    {
        public int ProductId { get; set; }
        public string Name { get; set; } = string.Empty;
        public string? MainImage { get; set; }
        public decimal Price { get; set; }
    }

    // === THÊM VIEWMODEL MỚI CHO TRANG CHI TIẾT SẢN PHẨM ===
    // (Chuyển đổi từ @model RabitStore.Pages.ProductDetailModel của bạn)
    public class ProductDetailViewModel
    {
        // Biến thể chính đang xem (VD: iPhone 15 Pro Max 256GB Titan Trắng)
        public ProductVariant ProductDetail { get; set; }

        // Tất cả biến thể CÙNG MỘT SẢN PHẨM (VD: 256GB Xanh, 512GB Trắng, ...)
        public List<ProductVariant> AllVariants { get; set; }

        // Các sản phẩm khác CÙNG MỘT HÃNG (VD: iPhone 15, iPhone 14...)
        public List<ProductVariant> RelatedProducts { get; set; }
    }
   
   
}
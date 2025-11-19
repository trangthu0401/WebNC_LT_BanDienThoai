// Pages/Products/Index.cshtml.cs

using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using WebBanDienThoai.Data; // S? d?ng DbContext c?a b?n
using WebBanDienThoai.Models;
using System.Collections.Generic;
using System.Linq;

namespace WebBanDienThoai.Pages.Products
{
    public class IndexModel : PageModel
    {
        private readonly ApplicationDbContext _context;

        // Constructor ?? inject DbContext
        public IndexModel(ApplicationDbContext context)
        {
            _context = context;
        }

        // --- Output: Danh sách S?n ph?m sau khi l?c & phân trang ---
        public List<ProductVariant> ProductVariants { get; set; } = new List<ProductVariant>();

        // --- Input Properties (Bindings) cho B? L?c và Phân Trang ---

        // L?c theo Hãng S?n Xu?t
        [BindProperty(SupportsGet = true)]
        public int? BrandIdFilter { get; set; }

        // L?c theo Kho?ng Giá
        [BindProperty(SupportsGet = true)]
        public decimal? MinPrice { get; set; }

        [BindProperty(SupportsGet = true)]
        public decimal? MaxPrice { get; set; }

        // L?c theo B? Nh? (Storage)
        [BindProperty(SupportsGet = true)]
        public string? StorageFilter { get; set; }

        // L?c theo RAM
        [BindProperty(SupportsGet = true)]
        public string? RamFilter { get; set; }

        // S?p x?p
        [BindProperty(SupportsGet = true)]
        public string SortBy { get; set; } = "Popular";

        // Phân Trang
        [BindProperty(SupportsGet = true)]
        public int PageNumber { get; set; } = 1;
        public int PageSize { get; set; } = 9; // 9 s?n ph?m/trang (3x3 grid)
        public int TotalPages { get; set; }
        public int TotalProducts { get; set; }

        // --- Danh sách tùy ch?n cho UI ---
        public SelectList BrandsList { get; set; }
        public List<string> StorageOptions { get; set; }
        public List<string> RamOptions { get; set; }


        public async Task OnGetAsync()
        {
            // 1. Chu?n b? d? li?u cho UI (L?y các tùy ch?n t? DB)

            // Danh sách Hãng S?n Xu?t
            var brands = await _context.Brands.OrderBy(b => b.BrandName).ToListAsync();
            BrandsList = new SelectList(brands, nameof(Brand.BrandId), nameof(Brand.BrandName), BrandIdFilter);

            // L?y các tùy ch?n RAM/Storage duy nh?t (Distinct) t? ProductVariants
            StorageOptions = await _context.ProductVariants
                                           .Where(pv => pv.Storage != null)
                                           .Select(pv => pv.Storage!)
                                           .Distinct()
                                           .OrderBy(s => s)
                                           .ToListAsync();

            RamOptions = await _context.ProductVariants
                                      .Where(pv => pv.Ram != null)
                                      .Select(pv => pv.Ram!)
                                      .Distinct()
                                      .OrderBy(r => r)
                                      .ToListAsync();


            // 2. Xây d?ng truy v?n EF Core
            var variantsQuery = _context.ProductVariants
                .Include(pv => pv.Product)        // T?i Product
                .Include(pv => pv.Product.Brand)  // T?i Brand
                .Where(pv => pv.IsActive == true) // Ch? l?y Variants ?ang ho?t ??ng
                .AsNoTracking() // T?i ?u hi?u su?t cho truy v?n ch? ??c
                .AsQueryable();

            // 3. Áp d?ng B? L?c

            // L?c theo Brand
            if (BrandIdFilter.HasValue)
            {
                variantsQuery = variantsQuery.Where(pv => pv.Product.BrandId == BrandIdFilter.Value);
            }

            // L?c theo Kho?ng Giá (s? d?ng DiscountPrice n?u có, ng??c l?i dùng Price)
            if (MinPrice.HasValue)
            {
                variantsQuery = variantsQuery.Where(pv => (pv.DiscountPrice ?? pv.Price) >= MinPrice.Value);
            }
            if (MaxPrice.HasValue)
            {
                variantsQuery = variantsQuery.Where(pv => (pv.DiscountPrice ?? pv.Price) <= MaxPrice.Value);
            }

            // L?c theo B? Nh? (Storage)
            if (!string.IsNullOrEmpty(StorageFilter))
            {
                variantsQuery = variantsQuery.Where(pv => pv.Storage == StorageFilter);
            }

            // L?c theo RAM
            if (!string.IsNullOrEmpty(RamFilter))
            {
                variantsQuery = variantsQuery.Where(pv => pv.Ram == RamFilter);
            }

            // 4. Áp d?ng S?p X?p
            variantsQuery = SortBy switch
            {
                "PriceAsc" => variantsQuery.OrderBy(pv => pv.DiscountPrice ?? pv.Price),
                "PriceDesc" => variantsQuery.OrderByDescending(pv => pv.DiscountPrice ?? pv.Price),
                "Newest" => variantsQuery.OrderByDescending(pv => pv.Product.ReleaseDate),
                _ => variantsQuery.OrderByDescending(pv => pv.Product.CreatedDate) // M?c ??nh: M?i t?o (gi? ??nh Popular)
            };

            // 5. Phân Trang (Pagination)
            TotalProducts = await variantsQuery.CountAsync();
            TotalPages = (int)Math.Ceiling((double)TotalProducts / PageSize);

            // ??m b?o PageNumber n?m trong ph?m vi h?p l?
            if (PageNumber < 1) PageNumber = 1;
            if (PageNumber > TotalPages && TotalPages > 0) PageNumber = TotalPages;

            ProductVariants = await variantsQuery
                .Skip((PageNumber - 1) * PageSize) // B? qua s? l??ng s?n ph?m ? các trang tr??c
                .Take(PageSize) // L?y s? l??ng s?n ph?m cho trang hi?n t?i
                .ToListAsync();
        }
    }
}
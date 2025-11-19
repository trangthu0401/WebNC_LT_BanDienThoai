// File: Controllers/ProductsController.cs

using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using System.Drawing.Printing;
using WebBanDienThoai.Data;
using WebBanDienThoai.Models;

namespace WebBanDienThoai.Controllers // Namespace đúng cho Controller
{
    public class ProductsController : Controller // Kế thừa từ Controller
    {
        private readonly ApplicationDbContext _context;
        private const int PageSize = 9;
        public ProductsController(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<IActionResult> Index(
            int? BrandIdFilter, decimal? MinPrice, decimal? MaxPrice,
            string? StorageFilter, string? RamFilter, string SortBy = "Popular",
            int PageNumber = 1)
        {
            // 1. Chuẩn bị dữ liệu cho UI (BrandsList, StorageOptions, RamOptions)
            var brands = await _context.Brands.OrderBy(b => b.BrandName).ToListAsync();
            ViewBag.BrandsList = new SelectList(brands, nameof(Brand.BrandId), nameof(Brand.BrandName), BrandIdFilter);

            ViewBag.StorageOptions = await _context.ProductVariants.Where(pv => pv.Storage != null).Select(pv => pv.Storage!).Distinct().OrderBy(s => s).ToListAsync();
            ViewBag.RamOptions = await _context.ProductVariants.Where(pv => pv.Ram != null).Select(pv => pv.Ram!).Distinct().OrderBy(r => r).ToListAsync();

            // Truyền trạng thái lọc/phân trang hiện tại qua ViewBag để View sử dụng
            ViewBag.BrandIdFilter = BrandIdFilter;
            ViewBag.MinPrice = MinPrice;
            ViewBag.MaxPrice = MaxPrice;
            ViewBag.StorageFilter = StorageFilter;
            ViewBag.RamFilter = RamFilter;
            ViewBag.SortBy = SortBy;
            ViewBag.PageNumber = PageNumber;


            // 2. Xây dựng truy vấn EF Core
            var variantsQuery = _context.ProductVariants
                .Include(pv => pv.Product)
                .ThenInclude(p => p.Brand)
                .Where(pv => pv.IsActive == true)
                .AsNoTracking()
                .AsQueryable();

            // 3. Áp dụng Bộ Lọc (Giữ nguyên logic từ IndexModel cũ)
            if (BrandIdFilter.HasValue) variantsQuery = variantsQuery.Where(pv => pv.Product.BrandId == BrandIdFilter.Value);
            if (MinPrice.HasValue) variantsQuery = variantsQuery.Where(pv => (pv.DiscountPrice ?? pv.Price) >= MinPrice.Value);
            if (MaxPrice.HasValue) variantsQuery = variantsQuery.Where(pv => (pv.DiscountPrice ?? pv.Price) <= MaxPrice.Value);
            if (!string.IsNullOrEmpty(StorageFilter)) variantsQuery = variantsQuery.Where(pv => pv.Storage == StorageFilter);
            if (!string.IsNullOrEmpty(RamFilter)) variantsQuery = variantsQuery.Where(pv => pv.Ram == RamFilter);


            // 4. Áp dụng Sắp Xếp
            variantsQuery = SortBy switch
            {
                "PriceAsc" => variantsQuery.OrderBy(pv => pv.DiscountPrice ?? pv.Price),
                "PriceDesc" => variantsQuery.OrderByDescending(pv => pv.DiscountPrice ?? pv.Price),
                "Newest" => variantsQuery.OrderByDescending(pv => pv.Product.ReleaseDate),
                _ => variantsQuery.OrderByDescending(pv => pv.Product.CreatedDate)
            };

            // 5. Phân Trang (Pagination)
            int TotalProducts = await variantsQuery.CountAsync();
            int TotalPages = (int)Math.Ceiling((double)TotalProducts / PageSize);

            if (PageNumber < 1) PageNumber = 1;
            if (PageNumber > TotalPages && TotalPages > 0) PageNumber = TotalPages;

            ViewBag.TotalPages = TotalPages; // Truyền TotalPages vào ViewBag

            var productVariants = await variantsQuery
                .Skip((PageNumber - 1) * PageSize)
                .Take(PageSize)
                .ToListAsync();

            // Trả về danh sách biến thể làm Model và trả về View Index.cshtml (Views/Products/Index.cshtml)
            return View("Index", productVariants);
        }

        // Action MVC thay thế cho OnGetAsync. Định tuyến URL: /products/detail/123
        [HttpGet("products/detail/{id:int}")]
        public async Task<IActionResult> Detail(int id) // Action tên Detail
        {
            if (id <= 0)
            {
                return NotFound();
            }

            // 1. Tải Biến thể hiện tại
            var productDetail = await _context.ProductVariants
                                              .Include(pv => pv.Product)
                                                  .ThenInclude(p => p.Brand)
                                              .FirstOrDefaultAsync(pv => pv.VariantId == id);

            if (productDetail == null)
            {
                return NotFound();
            }

            // 2 & 3. Tải các biến thể khác và Sản phẩm tương tự
            var currentProductId = productDetail.ProductId;
            var brandId = productDetail.Product.BrandId;

            var allVariants = await _context.ProductVariants
                                            .Include(pv => pv.Product)
                                            .Where(pv => pv.ProductId == currentProductId)
                                            .ToListAsync();

            var relatedProducts = await _context.ProductVariants
                                                .Include(pv => pv.Product)
                                                    .ThenInclude(p => p.Brand)
                                                .Where(pv => pv.Product.BrandId == brandId &&
                                                             pv.ProductId != currentProductId)
                                                .Take(6)
                                                .ToListAsync();

            // Truyền dữ liệu vào View (Views/Products/ProductDetail.cshtml)
            ViewBag.AllVariants = allVariants;
            ViewBag.RelatedProducts = relatedProducts;

            // TRẢ VỀ: Trả về View ProductDetail.cshtml cùng với Model chính (productDetail)
            return View("ProductDetail", productDetail);
        }
    }
}
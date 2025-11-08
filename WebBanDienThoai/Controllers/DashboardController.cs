// Thêm các using cần thiết
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using WebBanDienThoai.Models.ViewModels; // <-- Cần tạo các ViewModel này
using WebBanDienThoai.Models; // Cần Models

namespace WebBanDienThoai.Controllers
{
    // Ghi chú: Đây là Controller mới, tách từ AdminController
    public class DashboardController : Controller
    {
        // Ghi chú: Đổi tên từ DemoWebBanDienThoaiContext -> DemoWebBanDienThoaiDbContext
        private readonly DemoWebBanDienThoaiContext _context;

        public DashboardController(DemoWebBanDienThoaiContext context)
        {
            _context = context;
        }

        // --- 2. TRANG TỔNG QUAN (DASHBOARD) ---
        // GET: /Dashboard/Index (Hoặc /Admin/Index tùy vào Route của bạn)
        public async Task<IActionResult> Index()
        {
            try
            {
                var dashboardViewModel = new DashboardViewModel
                {
                    TotalRevenue = await GetTotalRevenue(),
                    ProductCount = await GetProductCount(),
                    UserCount = await GetUserCount(),
                    OrderCount = await GetOrderCount(),
                    TopSellingProducts = await GetTopSellingProducts(5)
                };
                return View(dashboardViewModel);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Lỗi tải dữ liệu Dashboard: {ex.Message}");
                return View(new DashboardViewModel { TopSellingProducts = new List<BestSellingProductViewModel>() });
            }
        }

        // --- HÀM HỖ TRỢ (PRIVATE) CHO DASHBOARD ---

        // Các hàm helper cho Dashboard
        private async Task<decimal> GetTotalRevenue()
        {
            // Ghi chú: Cần kiểm tra logic 'TotalAmount' là Nullable hay không
            return await _context.Orders.SumAsync(o => o.TotalAmount ?? 0m);
        }

        private async Task<int> GetProductCount()
        {
            return await _context.Products.CountAsync();
        }

        private async Task<int> GetUserCount()
        {
            return await _context.Customers.CountAsync();
        }

        private async Task<int> GetOrderCount()
        {
            return await _context.Orders.CountAsync();
        }

        // Hàm helper lấy Top 5 SP bán chạy
        private async Task<List<BestSellingProductViewModel>> GetTopSellingProducts(int topN = 5)
        {
            var topVariantsInfo = await _context.OrderDetails
                .Where(od => od.VariantId != null) // Ghi chú: Code gốc là 'HasValue'
                .GroupBy(od => od.VariantId)
                .Select(g => new {
                    VariantId = g.Key.Value, // Ghi chú: Code gốc là 'g.Key!.Value'
                    TotalQuantity = g.Sum(od => od.Quantity ?? 0)
                })
                .OrderByDescending(x => x.TotalQuantity)
                .Take(topN)
                .ToListAsync();

            var variantIds = topVariantsInfo.Select(v => v.VariantId).ToList();

            var variants = await _context.ProductVariants
                                     .Include(v => v.Product)
                                         .ThenInclude(p => p.Brand)
                                     .Where(v => variantIds.Contains(v.VariantId))
                                     .ToListAsync();

            var result = new List<BestSellingProductViewModel>();
            foreach (var topInfo in topVariantsInfo)
            {
                var variant = variants.FirstOrDefault(v => v.VariantId == topInfo.VariantId);
                if (variant != null && variant.Product != null)
                {
                    result.Add(new BestSellingProductViewModel
                    {
                        ProductName = (variant.Product.Name ?? "N/A") +
                                      (!string.IsNullOrEmpty(variant.Color) ? $" ({variant.Color}" : "") +
                                      (!string.IsNullOrEmpty(variant.Storage) ? $" - {variant.Storage})" : (!string.IsNullOrEmpty(variant.Color) ? ")" : "")),
                        ImageUrl = variant.ImageUrl ?? variant.Product.MainImage,
                        QuantitySold = topInfo.TotalQuantity,
                        Price = variant.DiscountPrice ?? variant.Price, // Ghi chú: Code gốc có '?? 0m'
                        Stock = variant.Stock ?? 0
                    });
                }
            }
            return result.OrderByDescending(r => r.QuantitySold).ToList();
        }
    }
}
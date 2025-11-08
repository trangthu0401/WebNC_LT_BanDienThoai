// Thêm các using cần thiết cho Controller, DbContext, Models, ViewModels
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using WebBanDienThoai.Models;        // Namespace Models của bạn
using WebBanDienThoai.Models.ViewModels; // Namespace ViewModels của bạn
using Microsoft.AspNetCore.Hosting; // Cần thiết cho việc lấy đường dẫn wwwroot
using System.IO;                    // Cần thiết cho việc xử lý file (Path, FileStream)
using Microsoft.AspNetCore.Mvc.Rendering; // Cần thiết cho SelectListItem (dropdown)
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Authorization; // Cần thiết cho IFormFile

namespace WebBanDienThoai.Controllers
{
    [Authorize(Roles = "Admin")]
    public class AdminController : Controller
    {
        private readonly DemoWebBanDienThoaiContext _context;
        private readonly IWebHostEnvironment _webHostEnvironment;

        // --- 1. CONSTRUCTOR (Hàm khởi tạo) ---
        public AdminController(DemoWebBanDienThoaiContext context, IWebHostEnvironment webHostEnvironment)
        {
            _context = context;
            _webHostEnvironment = webHostEnvironment;
        }

        // --- 2. TRANG TỔNG QUAN (DASHBOARD) ---
        // GET: /Admin/Index
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
                    TopSellingProducts = await GetTopSellingProducts(5) // Vẫn cần cho bảng
                };
                return View(dashboardViewModel);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Lỗi tải dữ liệu Dashboard: {ex.Message}");
                return View(new DashboardViewModel { TopSellingProducts = new List<BestSellingProductViewModel>() });
            }
        }

        // --- 3. QUẢN LÝ SẢN PHẨM (Trang chính) ---

        // GET: /Admin/ManageProducts
        public async Task<IActionResult> ManageProducts(int? brandId)
        {
            try
            {
                var productsQuery = _context.Products
                                            .AsNoTracking()
                                            .Include(p => p.Brand)
                                            .Include(p => p.ProductVariants)
                                            .AsQueryable();

                if (brandId.HasValue && brandId.Value > 0)
                {
                    productsQuery = productsQuery.Where(p => p.BrandId == brandId.Value);
                }

                var productList = await productsQuery
                    .OrderBy(p => p.Name)
                    .Select(p => new ProductListViewModel
                    {
                        ProductId = p.ProductId,
                        Name = p.Name ?? "N/A",
                        MainImage = p.MainImage,
                        BrandId = p.BrandId,
                        BrandName = p.Brand != null ? (p.Brand.BrandName ?? "N/A") : "N/A",
                        CreatedDate = p.CreatedDate ?? DateTime.MinValue,
                        IsActive = p.IsActive ?? false,
                        FirstVariantPrice = (p.ProductVariants != null && p.ProductVariants.Any())
                                            ? p.ProductVariants.OrderBy(v => v.VariantId).First().DiscountPrice.GetValueOrDefault(p.ProductVariants.OrderBy(v => v.VariantId).First().Price.GetValueOrDefault(0m))
                                            : 0m,
                        FirstVariantStock = (p.ProductVariants != null && p.ProductVariants.Any())
                                            ? p.ProductVariants.OrderBy(v => v.VariantId).First().Stock.GetValueOrDefault(0)
                                            : 0,
                        LowestPrice = (p.ProductVariants != null && p.ProductVariants.Any())
                            ? p.ProductVariants.Min(v => v.DiscountPrice.GetValueOrDefault(v.Price.GetValueOrDefault(0m)))
                            : 0m,
                        TotalStock = (p.ProductVariants != null && p.ProductVariants.Any())
                            ? p.ProductVariants.Sum(v => v.Stock.GetValueOrDefault(0))
                            : 0
                    })
                    .ToListAsync();

                var brandCounts = await _context.Brands
                    .AsNoTracking()
                    .Select(b => new BrandCount
                    {
                        brandId = b.BrandId,
                        BrandName = b.BrandName ?? "N/A",
                        IsActive = brandId.HasValue && b.BrandId == brandId.Value,
                        Count = _context.Products.Count(p => p.BrandId == b.BrandId)
                    })
                    .OrderBy(b => b.BrandName)
                    .ToListAsync();

                var totalProductCount = await _context.Products.CountAsync();

                var viewModel = new ManageProductsViewModel
                {
                    Products = productList,
                    BrandCounts = brandCounts,
                    TotalProductCount = totalProductCount
                };

                return View(viewModel);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Lỗi tải danh sách sản phẩm: {ex.Message}");
                return View("Error");
            }
        }

        // POST: /Admin/QuickEditProduct
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> QuickEditProduct(
            int productId,
            string name,
            int brandId,
            bool isActive,
            decimal lowestPrice,
            int totalStock
        )
        {
            try
            {
                var productToUpdate = await _context.Products
                                                .Include(p => p.ProductVariants)
                                                .FirstOrDefaultAsync(p => p.ProductId == productId);

                if (productToUpdate == null)
                {
                    TempData["StatusMessage"] = "Lỗi: Không tìm thấy sản phẩm.";
                    return RedirectToAction(nameof(ManageProducts));
                }

                productToUpdate.Name = name;
                productToUpdate.BrandId = brandId;
                productToUpdate.IsActive = isActive;

                var firstVariant = productToUpdate.ProductVariants?.OrderBy(v => v.VariantId).FirstOrDefault();

                if (firstVariant != null)
                {
                    firstVariant.Price = lowestPrice;
                    firstVariant.DiscountPrice = lowestPrice;
                    firstVariant.Stock = totalStock;
                }

                _context.Entry(productToUpdate).State = EntityState.Modified;
                if (firstVariant != null)
                {
                    _context.Entry(firstVariant).State = EntityState.Modified;
                }

                await _context.SaveChangesAsync();
                TempData["StatusMessage"] = "Cập nhật sản phẩm thành công.";
            }
            catch (Exception ex)
            {
                string fullErrorMessage = ex.ToString();
                Console.WriteLine($"--- DEBUG LỖI NGHIÊM TRỌNG KHI LƯU ID {productId}: {fullErrorMessage}");
                string displayError = ex.InnerException?.Message ?? ex.Message;
                TempData["StatusMessage"] = "Lỗi khi lưu sản phẩm: " + displayError;
            }

            return RedirectToAction(nameof(ManageProducts));
        }

        // POST: /Admin/DeleteProduct
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteProduct(int id)
        {
            try
            {
                var productToDelete = await _context.Products
                                                .Include(p => p.ProductVariants)
                                                .FirstOrDefaultAsync(p => p.ProductId == id);

                if (productToDelete == null) return NotFound();

                if (productToDelete.ProductVariants != null && productToDelete.ProductVariants.Any())
                {
                    _context.ProductVariants.RemoveRange(productToDelete.ProductVariants);
                }

                _context.Products.Remove(productToDelete);
                await _context.SaveChangesAsync();
                TempData["StatusMessage"] = "Đã xóa sản phẩm thành công.";
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Lỗi khi xóa sản phẩm ID {id}: {ex.Message}");
                TempData["StatusMessage"] = "Lỗi khi xóa sản phẩm.";
            }

            return RedirectToAction(nameof(ManageProducts));
        }


        // --- 4. THÊM SẢN PHẨM MỚI ---

        // GET: /Admin/CreateProduct
        public async Task<IActionResult> CreateProduct()
        {
            var viewModel = new CreateProductViewModel
            {
                BrandList = await _context.Brands
                                        .OrderBy(b => b.BrandName)
                                        .Select(b => new SelectListItem
                                        {
                                            Value = b.BrandId.ToString(),
                                            Text = b.BrandName
                                        })
                                        .ToListAsync(),
                Product = new Product()
            };
            return View(viewModel);
        }

        // POST: /Admin/CreateProduct
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateProduct(CreateProductViewModel viewModel)
        {
            if (ModelState.IsValid)
            {
                try
                {
                    string? mainImagePath = null;
                    if (viewModel.MainImageFile != null)
                    {
                        mainImagePath = await UploadFile(viewModel.MainImageFile);
                    }

                    string? variantImagePath = null;
                    if (viewModel.VariantImageFile != null)
                    {
                        variantImagePath = await UploadFile(viewModel.VariantImageFile);
                    }

                    Product newProduct = viewModel.Product!;
                    newProduct.CreatedDate = DateTime.Now;
                    newProduct.IsActive = true;
                    newProduct.MainImage = mainImagePath;

                    _context.Products.Add(newProduct);
                    await _context.SaveChangesAsync();

                    ProductVariant newVariant = new ProductVariant
                    {
                        ProductId = newProduct.ProductId,
                        Color = viewModel.VariantColor ?? string.Empty,
                        Storage = viewModel.VariantStorage ?? string.Empty,
                        Ram = viewModel.VariantRam ?? string.Empty,
                        Price = viewModel.VariantPrice,
                        Stock = viewModel.VariantStock,
                        ImageUrl = variantImagePath ?? mainImagePath,
                        IsActive = true,
                        CreatedDate = DateTime.Now
                    };

                    _context.ProductVariants.Add(newVariant);
                    await _context.SaveChangesAsync();

                    TempData["StatusMessage"] = "Thêm sản phẩm mới thành công.";
                    return RedirectToAction(nameof(ManageProducts));
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Lỗi khi tạo sản phẩm: {ex.Message}");
                    ModelState.AddModelError("", "Có lỗi xảy ra, không thể lưu sản phẩm.");
                }
            }

            viewModel.BrandList = await _context.Brands
                                           .OrderBy(b => b.BrandName)
                                           .Select(b => new SelectListItem
                                           {
                                               Value = b.BrandId.ToString(),
                                               Text = b.BrandName
                                           })
                                           .ToListAsync();
            return View(viewModel);
        }


        // --- 5. CHI TIẾT SẢN PHẨM & QUẢN LÝ BIẾN THỂ ---

        // GET: /Admin/ProductDetails/5
        public async Task<IActionResult> ProductDetails(int id)
        {
            try
            {
                var product = await _context.Products
                                            .Include(p => p.Brand)
                                            .Include(p => p.ProductVariants)
                                            .FirstOrDefaultAsync(p => p.ProductId == id);

                if (product == null) return NotFound();

                var viewModel = new ProductDetailViewModel
                {
                    Product = product,
                    Variants = product.ProductVariants.ToList()
                };
                return View(viewModel);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Lỗi khi lấy chi tiết sản phẩm: {ex.Message}");
                return View("Error");
            }
        }

        // POST: /Admin/AddVariant
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddVariant(AddVariantViewModel viewModel)
        {
            if (ModelState.IsValid)
            {
                try
                {
                    string? variantImagePath = null;
                    if (viewModel.ImageFile != null)
                    {
                        variantImagePath = await UploadFile(viewModel.ImageFile);
                    }

                    string storageValue = viewModel.Storage ?? string.Empty;
                    string ramValue = viewModel.Ram ?? string.Empty;

                    var newVariant = new ProductVariant
                    {
                        ProductId = viewModel.ProductId,
                        Color = viewModel.Color ?? string.Empty,
                        Storage = storageValue.Replace("GB", "").Trim(),
                        Ram = ramValue.Replace("GB", "").Trim(),
                        Price = viewModel.Price,
                        DiscountPrice = viewModel.DiscountPrice,
                        Stock = viewModel.Stock,
                        ImageUrl = variantImagePath,
                        IsActive = true,
                        CreatedDate = DateTime.Now
                    };

                    _context.ProductVariants.Add(newVariant);
                    await _context.SaveChangesAsync();
                    TempData["StatusMessage"] = "Thêm biến thể mới thành công!";
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Lỗi khi thêm biến thể: {ex.Message}");
                    TempData["StatusMessage"] = $"Lỗi khi thêm biến thể: {ex.Message}";
                }
            }
            return RedirectToAction(nameof(ProductDetails), new { id = viewModel.ProductId });
        }

        // POST: /Admin/EditVariant
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditVariant(
            int ProductId, int VariantId, string Color, string Storage, string Ram,
            decimal Price, decimal? DiscountPrice, int Stock)
        {
            if (Stock < 0 || Price < 0)
            {
                TempData["StatusMessage"] = "Lỗi: Giá hoặc Tồn kho không thể là số âm.";
                return RedirectToAction(nameof(ProductDetails), new { id = ProductId });
            }

            try
            {
                var variantToUpdate = await _context.ProductVariants.FindAsync(VariantId);
                if (variantToUpdate == null)
                {
                    TempData["StatusMessage"] = "Lỗi: Không tìm thấy biến thể.";
                    return RedirectToAction(nameof(ProductDetails), new { id = ProductId });
                }

                variantToUpdate.Color = Color;
                variantToUpdate.Storage = Storage.EndsWith("GB") ? Storage : Storage + "GB";
                variantToUpdate.Ram = string.IsNullOrEmpty(Ram) ? null : (Ram.EndsWith("GB") ? Ram : Ram + "GB");
                variantToUpdate.Price = Price;
                variantToUpdate.DiscountPrice = DiscountPrice;
                variantToUpdate.Stock = Stock;

                await _context.SaveChangesAsync();
                TempData["StatusMessage"] = "Cập nhật biến thể (ID: " + VariantId + ") thành công.";
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Lỗi khi sửa biến thể ID {VariantId}: {ex.Message}");
                TempData["StatusMessage"] = "Lỗi khi cập nhật biến thể: " + ex.Message;
            }
            return RedirectToAction(nameof(ProductDetails), new { id = ProductId });
        }

        // POST: /Admin/DeleteVariant
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteVariant(int id)
        {
            int productId = 0;
            try
            {
                var variantToDelete = await _context.ProductVariants.FindAsync(id);
                if (variantToDelete == null)
                {
                    TempData["StatusMessage"] = "Lỗi: Không tìm thấy biến thể để xóa.";
                    return RedirectToAction(nameof(ManageProducts));
                }
                productId = variantToDelete.ProductId;
                _context.ProductVariants.Remove(variantToDelete);
                await _context.SaveChangesAsync();
                TempData["StatusMessage"] = "Đã xóa biến thể thành công.";
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Lỗi khi xóa biến thể ID {id}: {ex.Message}");
                TempData["StatusMessage"] = "Lỗi khi xóa biến thể: " + ex.Message;
            }

            return RedirectToAction(nameof(ProductDetails), new { id = productId });
        }


        // --- 6. CÁC TRANG KHÁC (Chưa làm) ---
        public async Task<IActionResult> EditProduct(int id)
        {
            var product = await _context.Products.FindAsync(id);
            if (product == null) return NotFound();
            return View(/* viewModel */);
        }
        public IActionResult ManageOrders() { return View(); }
        public IActionResult ManageUsers() { return View(); }
        public IActionResult Statistics() { return View(); }


        // --- 7. HÀM HỖ TRỢ (PRIVATE) ---
        private async Task<string?> UploadFile(IFormFile file)
        {
            string uploadDir = Path.Combine(_webHostEnvironment.WebRootPath, "images", "products");
            if (!Directory.Exists(uploadDir)) Directory.CreateDirectory(uploadDir);

            string uniqueFileName = Guid.NewGuid().ToString() + "_" + Path.GetFileName(file.FileName);
            string filePath = Path.Combine(uploadDir, uniqueFileName);

            using (var fileStream = new FileStream(filePath, FileMode.Create))
            {
                await file.CopyToAsync(fileStream);
            }
            return "/images/products/" + uniqueFileName;
        }

        private async Task<decimal> GetTotalRevenue()
        {
            return await _context.Orders.SumAsync(o => o.TotalAmount) ;
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

        // HÀM NÀY VẪN CÒN ĐƯỢC SỬ DỤNG (CHO BẢNG BÊN DƯỚI)
        private async Task<List<BestSellingProductViewModel>> GetTopSellingProducts(int topN = 5)
        {
            var topVariantsInfo = await _context.OrderDetails
                .Where(od => od.VariantId.HasValue)
                .GroupBy(od => od.VariantId)
                .Select(g => new {
                    VariantId = g.Key!.Value,
                    TotalQuantity = g.Sum(od => od.Quantity ?? 0)
                })
                .OrderByDescending(x => x.TotalQuantity)
                .Take(topN)
                .ToListAsync();

            var variantIds = topVariantsInfo.Select(v => v.VariantId).ToList();

            var variants = await _context.ProductVariants
                                    .Include(v => v.Product)
                                        .ThenInclude(p => p!.Brand)
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
                        Price = variant.DiscountPrice ?? variant.Price ?? 0m,
                        Stock = variant.Stock ?? 0
                    });
                }
            }
            return result.OrderByDescending(r => r.QuantitySold).ToList();
        }

        // --- 8. API ENDPOINTS CHO DASHBOARD ---

        // === PHẦN API NÀY ĐÃ BỊ XÓA (VÌ ĐÃ XÓA BIỂU ĐỒ) ===
        // [HttpGet]
        // public async Task<IActionResult> GetSalesData() { ... }
        // === HẾT PHẦN BỊ XÓA ===

        // GET: /Admin/GetRevenueByDay?year=2025&month=10 (API cho biểu đồ doanh thu)
        [HttpGet]
        public async Task<IActionResult> GetRevenueByDay(int year, int month)
        {
            try
            {
                // 1. Lấy dữ liệu thô từ CSDL
                var revenueData = await _context.Orders
                    .Where(o => o.OrderDate.HasValue &&
                                o.OrderDate.Value.Year == year &&
                                o.OrderDate.Value.Month == month)
                    .GroupBy(o => o.OrderDate.Value.Day)
                    .Select(g => new
                    {
                        Day = g.Key,
                        Total = g.Sum(o => o.TotalAmount)
                    })
                    .ToDictionaryAsync(k => k.Day, v => v.Total);

                // 2. Tạo nhãn cho tất cả các ngày trong tháng
                int daysInMonth = DateTime.DaysInMonth(year, month);
                var labels = new List<string>();
                var data = new List<decimal>();

                for (int i = 1; i <= daysInMonth; i++)
                {
                    labels.Add("Ngày " + i);
                    if (revenueData.ContainsKey(i))
                    {
                        data.Add(revenueData[i]);
                    }
                    else
                    {
                        data.Add(0);
                    }
                }

                // 3. Trả về JSON
                return Json(new { labels, data });
            }
            catch (Exception ex)
            {
                return StatusCode(500, "Lỗi máy chủ: " + ex.Message);
            }
        }

    } // <-- Đóng class AdminController
} // <-- Đóng namespace
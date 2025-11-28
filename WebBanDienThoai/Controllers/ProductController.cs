using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using WebBanDienThoai.Data;
using WebBanDienThoai.Models;
using WebBanDienThoai.Models.ViewModels;

namespace WebBanDienThoai.Controllers
{
    [Authorize(Roles = "Admin")]
    public class ProductController : Controller
    {
        private readonly DemoWebBanDienThoaiDbContext _context;
        private readonly IWebHostEnvironment _webHostEnvironment;

        public ProductController(DemoWebBanDienThoaiDbContext context, IWebHostEnvironment webHostEnvironment)
        {
            _context = context;
            _webHostEnvironment = webHostEnvironment;
        }

        // --- INDEX: QUẢN LÝ SẢN PHẨM ---
        public async Task<IActionResult> Index(int? brandId, string searchId, int pageIndex = 1)
        {
            try
            {
                // ĐƠN GIẢN HÓA QUERY ĐỂ TEST TRƯỚC
                var productsQuery = _context.Products
                                            .Include(p => p.Brand)
                                            .Include(p => p.ProductVariants)
                                            .AsQueryable();

                // 1. Lọc
                if (!string.IsNullOrEmpty(searchId))
                {
                    productsQuery = productsQuery.Where(o =>
                        o.ProductId.ToString().Contains(searchId) ||
                        o.Name.Contains(searchId));
                }

                if (brandId.HasValue && brandId.Value > 0)
                {
                    productsQuery = productsQuery.Where(p => p.BrandId == brandId.Value);
                }

                // 2. Phân trang
                int pageSize = 10;
                int totalItems = await productsQuery.CountAsync();
                int totalPages = (int)Math.Ceiling(totalItems / (double)pageSize);

                pageIndex = Math.Max(1, pageIndex);
                if (pageIndex > totalPages && totalPages > 0) pageIndex = totalPages;

                // 3. Truy vấn đơn giản hóa
                var productList = await productsQuery
                    .OrderByDescending(p => p.CreatedDate)
                    .Skip((pageIndex - 1) * pageSize)
                    .Take(pageSize)
                    .Select(p => new ProducAdmintListViewModel
                    {
                        ProductId = p.ProductId,
                        Name = p.Name ?? "N/A",
                        MainImage = p.MainImage,
                        BrandId = p.BrandId,
                        BrandName = p.Brand != null ? p.Brand.BrandName ?? "N/A" : "N/A",
                        CreatedDate = p.CreatedDate,
                        IsActive = p.IsActive,

                        // Tính giá thấp nhất - đơn giản hóa
                        LowestPrice = p.ProductVariants.Any() ? p.ProductVariants.Min(v => v.Price) : 0,

                        // Tính tổng tồn kho - đơn giản hóa
                        TotalStock = p.ProductVariants.Sum(v => v.Stock)
                    })
                    .ToListAsync();

                // 4. Lấy danh sách hãng
                var brandCounts = await _context.Brands
                    .Select(b => new BrandCountViewModel
                    {
                        brandId = b.BrandId,
                        BrandName = b.BrandName ?? "N/A",
                        IsActive = brandId.HasValue && b.BrandId == brandId.Value,
                        Count = _context.Products.Count(p => p.BrandId == b.BrandId)
                    })
                    .OrderBy(b => b.BrandName)
                    .ToListAsync();

                var totalProductCount = await _context.Products.CountAsync();

                // 5. Tạo ViewModel
                var viewModel = new ProductIndexViewModel
                {
                    Products = productList,
                    BrandCounts = brandCounts,
                    TotalProductCount = totalProductCount
                };

                ViewBag.PageIndex = pageIndex;
                ViewBag.TotalPages = totalPages;
                ViewBag.SearchId = searchId;
                ViewBag.BrandId = brandId;

                return View(viewModel);
            }
            catch (Exception ex)
            {
                // HIỂN THỊ LỖI CHI TIẾT ĐỂ DEBUG
                return Content($"🔥 LỖI TRONG Product/Index: {ex.Message}<br><br>" +
                              $"Stack Trace: {ex.StackTrace}<br><br>" +
                              $"Inner Exception: {ex.InnerException?.Message}");
            }
        }

        // --- CREATE: THÊM SẢN PHẨM MỚI ---
        public async Task<IActionResult> Create()
        {
            var viewModel = new ProductCreateViewModel
            {
                BrandList = await _context.Brands.OrderBy(b => b.BrandName)
                                          .Select(b => new SelectListItem { Value = b.BrandId.ToString(), Text = b.BrandName })
                                          .ToListAsync(),
                Product = new Product()
            };
            return View(viewModel);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(ProductCreateViewModel viewModel)
        {
            // 🔥 RÀNG BUỘC SỐ DƯƠNG TRƯỚC KHI KIỂM TRA ModelState
            if (viewModel.VariantPrice <= 0)
            {
                ModelState.AddModelError("VariantPrice", "Giá sản phẩm phải lớn hơn 0.");
            }

            if (viewModel.VariantStock < 0)
            {
                ModelState.AddModelError("VariantStock", "Tồn kho không được âm.");
            }

            if (viewModel.Product.BatteryCapacity.HasValue && viewModel.Product.BatteryCapacity <= 0)
            {
                ModelState.AddModelError("Product.BatteryCapacity", "Dung lượng pin phải lớn hơn 0.");
            }

            if (viewModel.Product.ScreenSize.HasValue && viewModel.Product.ScreenSize <= 0)
            {
                ModelState.AddModelError("Product.ScreenSize", "Kích thước màn hình phải lớn hơn 0.");
            }

            if (viewModel.Product.Weight.HasValue && viewModel.Product.Weight <= 0)
            {
                ModelState.AddModelError("Product.Weight", "Trọng lượng phải lớn hơn 0.");
            }

            if (viewModel.Product.RefreshRate.HasValue && viewModel.Product.RefreshRate <= 0)
            {
                ModelState.AddModelError("Product.RefreshRate", "Tần số quét phải lớn hơn 0.");
            }

            if (ModelState.IsValid)
            {
                try
                {
                    string? mainImagePath = null;
                    if (viewModel.MainImageFile != null)
                    {
                        // 🔥 RÀNG BUỘC FILE ẢNH
                        if (viewModel.MainImageFile.Length == 0)
                        {
                            ModelState.AddModelError("MainImageFile", "File ảnh không được trống.");
                            viewModel.BrandList = await GetBrandListAsync();
                            return View(viewModel);
                        }

                        var allowedExtensions = new[] { ".jpg", ".jpeg", ".png", ".gif", ".webp" };
                        var fileExtension = Path.GetExtension(viewModel.MainImageFile.FileName).ToLower();
                        if (!allowedExtensions.Contains(fileExtension))
                        {
                            ModelState.AddModelError("MainImageFile", "Chỉ chấp nhận file ảnh (jpg, jpeg, png, gif, webp).");
                            viewModel.BrandList = await GetBrandListAsync();
                            return View(viewModel);
                        }

                        mainImagePath = await UploadFile(viewModel.MainImageFile);
                    }

                    string? variantImagePath = null;
                    if (viewModel.VariantImageFile != null)
                    {
                        // 🔥 RÀNG BUỘC FILE ẢNH BIẾN THỂ
                        if (viewModel.VariantImageFile.Length == 0)
                        {
                            ModelState.AddModelError("VariantImageFile", "File ảnh biến thể không được trống.");
                            viewModel.BrandList = await GetBrandListAsync();
                            return View(viewModel);
                        }

                        var allowedExtensions = new[] { ".jpg", ".jpeg", ".png", ".gif", ".webp" };
                        var fileExtension = Path.GetExtension(viewModel.VariantImageFile.FileName).ToLower();
                        if (!allowedExtensions.Contains(fileExtension))
                        {
                            ModelState.AddModelError("VariantImageFile", "Chỉ chấp nhận file ảnh (jpg, jpeg, png, gif, webp).");
                            viewModel.BrandList = await GetBrandListAsync();
                            return View(viewModel);
                        }

                        variantImagePath = await UploadFile(viewModel.VariantImageFile);
                    }

                    // 🔥 XỬ LÝ "GB" CHO STORAGE VÀ RAM
                    string processedStorage = ProcessStorageRamValue(viewModel.VariantStorage);
                    string processedRam = ProcessStorageRamValue(viewModel.VariantRam);

                    // 1. Lưu Product
                    Product newProduct = viewModel.Product!;
                    newProduct.CreatedDate = DateTime.Now;
                    newProduct.IsActive = true;
                    newProduct.MainImage = mainImagePath;

                    // 🔥 RÀNG BUỘC NGÀY PHÁT HÀNH KHÔNG Ở TƯƠNG LAI
                    if (newProduct.ReleaseDate > DateTime.Now)
                    {
                        ModelState.AddModelError("Product.ReleaseDate", "Ngày phát hành không được ở tương lai.");
                        viewModel.BrandList = await GetBrandListAsync();
                        return View(viewModel);
                    }

                    _context.Products.Add(newProduct);
                    await _context.SaveChangesAsync();

                    // 2. Lưu Variant
                    var newVariant = new ProductVariant
                    {
                        ProductId = newProduct.ProductId,
                        Color = viewModel.VariantColor?.Trim() ?? string.Empty,
                        Storage = processedStorage,
                        RAM = processedRam,
                        Price = viewModel.VariantPrice,
                        Stock = viewModel.VariantStock,
                        ImageUrl = variantImagePath ?? mainImagePath,
                        IsActive = true,
                        CreatedDate = DateTime.Now
                    };

                    _context.ProductVariants.Add(newVariant);
                    await _context.SaveChangesAsync();

                    TempData["StatusMessage"] = "Thêm sản phẩm thành công.";
                    return RedirectToAction(nameof(Index));
                }
                catch (Exception ex)
                {
                    ModelState.AddModelError("", "Lỗi lưu: " + ex.Message);
                }
            }

            viewModel.BrandList = await GetBrandListAsync();
            return View(viewModel);
        }

        // --- EDIT: SỬA SẢN PHẨM ---
        public async Task<IActionResult> Edit(int id)
        {
            var product = await _context.Products.FindAsync(id);
            if (product == null) return NotFound();

            var viewModel = new ProductEditViewModel
            {
                Product = product,
                BrandList = await GetBrandListAsync()
            };
            return View(viewModel);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, ProductEditViewModel viewModel)
        {
            if (id != viewModel.Product.ProductId) return NotFound();

            // 🔥 RÀNG BUỘC SỐ DƯƠNG CHO EDIT
            if (viewModel.Product.BatteryCapacity.HasValue && viewModel.Product.BatteryCapacity <= 0)
            {
                ModelState.AddModelError("Product.BatteryCapacity", "Dung lượng pin phải lớn hơn 0.");
            }

            if (viewModel.Product.ScreenSize.HasValue && viewModel.Product.ScreenSize <= 0)
            {
                ModelState.AddModelError("Product.ScreenSize", "Kích thước màn hình phải lớn hơn 0.");
            }

            if (viewModel.Product.Weight.HasValue && viewModel.Product.Weight <= 0)
            {
                ModelState.AddModelError("Product.Weight", "Trọng lượng phải lớn hơn 0.");
            }

            if (viewModel.Product.RefreshRate.HasValue && viewModel.Product.RefreshRate <= 0)
            {
                ModelState.AddModelError("Product.RefreshRate", "Tần số quét phải lớn hơn 0.");
            }

            // Cho phép không có file ảnh mới (giữ ảnh cũ)
            ModelState.Remove("MainImageFile");

            if (ModelState.IsValid)
            {
                try
                {
                    var productFromDb = await _context.Products.AsNoTracking().FirstOrDefaultAsync(p => p.ProductId == id);
                    if (productFromDb == null) return NotFound();

                    string? mainImagePath = productFromDb.MainImage;
                    if (viewModel.MainImageFile != null && viewModel.MainImageFile.Length > 0)
                    {
                        // 🔥 RÀNG BUỘC FILE ẢNH CHO EDIT
                        var allowedExtensions = new[] { ".jpg", ".jpeg", ".png", ".gif", ".webp" };
                        var fileExtension = Path.GetExtension(viewModel.MainImageFile.FileName).ToLower();
                        if (!allowedExtensions.Contains(fileExtension))
                        {
                            ModelState.AddModelError("MainImageFile", "Chỉ chấp nhận file ảnh (jpg, jpeg, png, gif, webp).");
                            viewModel.BrandList = await GetBrandListAsync();
                            return View(viewModel);
                        }

                        mainImagePath = await UploadFile(viewModel.MainImageFile);
                    }

                    // 🔥 RÀNG BUỘC NGÀY PHÁT HÀNH KHÔNG Ở TƯƠNG LAI
                    if (viewModel.Product.ReleaseDate > DateTime.Now)
                    {
                        ModelState.AddModelError("Product.ReleaseDate", "Ngày phát hành không được ở tương lai.");
                        viewModel.BrandList = await GetBrandListAsync();
                        return View(viewModel);
                    }

                    viewModel.Product.MainImage = mainImagePath;
                    viewModel.Product.CreatedDate = productFromDb.CreatedDate;
                    viewModel.Product.UpdatedDate = DateTime.Now;

                    _context.Update(viewModel.Product);
                    await _context.SaveChangesAsync();

                    TempData["StatusMessage"] = "Cập nhật thành công.";
                    return RedirectToAction(nameof(Index));
                }
                catch (Exception ex)
                {
                    ModelState.AddModelError("", "Lỗi cập nhật: " + ex.Message);
                }
            }

            viewModel.BrandList = await GetBrandListAsync();
            return View(viewModel);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
        {
            var product = await _context.Products
                .Include(p => p.ProductVariants)
                .FirstOrDefaultAsync(p => p.ProductId == id);

            if (product != null)
            {
                try
                {
                    // 🔥 FIX: Sửa lỗi LINQ translation - Kiểm tra ràng buộc đúng cách
                    // Lấy danh sách VariantIds của sản phẩm
                    var variantIds = product.ProductVariants.Select(pv => pv.VariantId).ToList();

                    // Kiểm tra xem có OrderDetails nào liên quan đến các biến thể này không
                    bool hasOrders = await _context.OrderDetails
                        .AnyAsync(od => variantIds.Contains(od.VariantId));

                    if (hasOrders)
                    {
                        TempData["errorMessage"] = "Không thể xóa sản phẩm vì đã có đơn hàng liên quan. Vui lòng ẩn sản phẩm thay vì xóa.";
                        return RedirectToAction(nameof(Index));
                    }

                    // 🔥 FIX: Kiểm tra thêm các ràng buộc khác
                    bool hasCartItems = await _context.CartItems
                        .AnyAsync(ci => variantIds.Contains(ci.VariantId));

                    bool hasReviewDetails = await _context.ReviewDetails
                        .AnyAsync(rd => variantIds.Contains(rd.VariantId));

                    bool hasFavoriteDetails = await _context.FavoriteDetails
                        .AnyAsync(fd => variantIds.Contains(fd.VariantId));

                    if (hasCartItems || hasReviewDetails || hasFavoriteDetails)
                    {
                        var constraints = new List<string>();
                        if (hasCartItems) constraints.Add("giỏ hàng");
                        if (hasReviewDetails) constraints.Add("đánh giá");
                        if (hasFavoriteDetails) constraints.Add("danh sách yêu thích");

                        TempData["errorMessage"] = $"Không thể xóa sản phẩm vì đang được sử dụng trong: {string.Join(", ", constraints)}. Vui lòng ẩn sản phẩm thay vì xóa.";
                        return RedirectToAction(nameof(Index));
                    }

                    // Nếu không có ràng buộc, thực hiện xóa
                    if (product.ProductVariants != null)
                        _context.ProductVariants.RemoveRange(product.ProductVariants);

                    _context.Products.Remove(product);
                    await _context.SaveChangesAsync();

                    TempData["StatusMessage"] = "Đã xóa sản phẩm thành công.";
                }
                catch (DbUpdateException dbEx)
                {
                    // Xử lý lỗi chi tiết hơn
                    var baseException = dbEx.GetBaseException() as SqlException;

                    if (baseException != null && baseException.Number == 547)
                    {
                        TempData["errorMessage"] = "Không thể xóa sản phẩm do có dữ liệu liên quan (đơn hàng, giỏ hàng, đánh giá, yêu thích). Vui lòng ẩn sản phẩm thay vì xóa.";
                    }
                    else
                    {
                        TempData["errorMessage"] = "Lỗi cơ sở dữ liệu khi xóa sản phẩm.";
                    }
                }
                catch (Exception ex)
                {
                    TempData["errorMessage"] = $"Lỗi không xác định khi xóa sản phẩm: {ex.Message}";
                }
            }
            else
            {
                TempData["errorMessage"] = "Không tìm thấy sản phẩm để xóa.";
            }

            return RedirectToAction(nameof(Index));
        }

        // --- ACTION TEST ĐỂ DEBUG ---
        [AllowAnonymous]
        public IActionResult Test()
        {
            return Content("✅ ProductController đang hoạt động!");
        }

        [AllowAnonymous]
        public async Task<IActionResult> TestDb()
        {
            try
            {
                var productCount = await _context.Products.CountAsync();
                var brandCount = await _context.Brands.CountAsync();
                return Content($" Database OK! Products: {productCount}, Brands: {brandCount}");
            }
            catch (Exception ex)
            {
                return Content($"❌ Lỗi database: {ex.Message}");
            }
        }

        // --- HÀM HỖ TRỢ PRIVATE ---
        private async Task<string?> UploadFile(IFormFile file)
        {
            string uploadDir = Path.Combine(_webHostEnvironment.WebRootPath, "images", "products");
            if (!Directory.Exists(uploadDir)) Directory.CreateDirectory(uploadDir);

            string fileName = Guid.NewGuid().ToString() + "_" + Path.GetFileName(file.FileName);
            string filePath = Path.Combine(uploadDir, fileName);

            using (var stream = new FileStream(filePath, FileMode.Create))
            {
                await file.CopyToAsync(stream);
            }
            return "/images/products/" + fileName;
        }

        private async Task<List<SelectListItem>> GetBrandListAsync()
        {
            return await _context.Brands
                .OrderBy(b => b.BrandName)
                .Select(b => new SelectListItem
                {
                    Value = b.BrandId.ToString(),
                    Text = b.BrandName
                })
                .ToListAsync();
        }

        // 🔥 HÀM XỬ LÝ "GB" CHO STORAGE VÀ RAM
        private string ProcessStorageRamValue(string? value)
        {
            if (string.IsNullOrWhiteSpace(value))
                return string.Empty;

            // Loại bỏ "GB" nếu có và trim
            string cleanedValue = value.Replace("GB", "").Trim();

            // Nếu là số thì thêm "GB"
            if (int.TryParse(cleanedValue, out int numericValue))
            {
                return $"{numericValue}GB";
            }

            return cleanedValue;
        }
    }
}
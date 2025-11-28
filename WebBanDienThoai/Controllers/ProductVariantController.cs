using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using WebBanDienThoai.Data;
using WebBanDienThoai.Models;
using WebBanDienThoai.Models.ViewModels;

namespace WebBanDienThoai.Controllers
{
    [Authorize(Roles = "Admin")]
    public class ProductVariantController : Controller
    {
        private readonly DemoWebBanDienThoaiDbContext _context;
        private readonly IWebHostEnvironment _webHostEnvironment;

        public ProductVariantController(DemoWebBanDienThoaiDbContext context, IWebHostEnvironment webHostEnvironment)
        {
            _context = context;
            _webHostEnvironment = webHostEnvironment;
        }

        // --- 1. DANH SÁCH BIẾN THỂ ---
        [HttpGet]
        [Route("ProductVariant")]
        [Route("ProductVariant/Index")]
        public async Task<IActionResult> Index(int productId)
        {
            try
            {
                if (productId <= 0)
                {
                    TempData["errorMessage"] = "ID sản phẩm không hợp lệ.";
                    return RedirectToAction("Index", "Product");
                }

                var product = await _context.Products
                                            .Include(p => p.Brand)
                                            .Include(p => p.ProductVariants)
                                            .FirstOrDefaultAsync(p => p.ProductId == productId);

                if (product == null)
                {
                    TempData["errorMessage"] = $"Không tìm thấy sản phẩm ID: {productId}";
                    return RedirectToAction("Index", "Product");
                }

                var viewModel = new ProductVariantIndexViewModel
                {
                    Product = product,
                    Variants = product.ProductVariants.ToList(),
                    CreateForm = new ProductVariantCreateViewModel { ProductId = productId }
                };

                return View(viewModel);
            }
            catch (Exception ex)
            {
                TempData["errorMessage"] = $"Lỗi: {ex.Message}";
                return RedirectToAction("Index", "Product");
            }
        }

        // --- 2. THÊM BIẾN THỂ MỚI ---
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind(Prefix = "CreateForm")]ProductVariantCreateViewModel viewModel)
        {
            if (!ModelState.IsValid)
            {
                TempData["errorMessage"] = "Dữ liệu không hợp lệ. Vui lòng kiểm tra lại.";
                return RedirectToAction(nameof(Index), new { productId = viewModel.ProductId });
            }

            // 🛡️ 1. VALIDATION: Số tiền và Tồn kho không được âm
            if (viewModel.Price < 0)
            {
                TempData["errorMessage"] = "Lỗi: Giá bán gốc không được nhỏ hơn 0.";
                return RedirectToAction(nameof(Index), new { productId = viewModel.ProductId });
            }

            if (viewModel.Stock < 0)
            {
                TempData["errorMessage"] = "Lỗi: Số lượng tồn kho không được nhỏ hơn 0.";
                return RedirectToAction(nameof(Index), new { productId = viewModel.ProductId });
            }

            // 🛡️ 2. BUSINESS LOGIC: Kiểm tra Giá khuyến mãi
            if (viewModel.DiscountPrice.HasValue)
            {
                if (viewModel.DiscountPrice.Value < 0)
                {
                    TempData["errorMessage"] = "Lỗi: Giá khuyến mãi không được nhỏ hơn 0.";
                    return RedirectToAction(nameof(Index), new { productId = viewModel.ProductId });
                }

                if (viewModel.DiscountPrice.Value >= viewModel.Price)
                {
                    TempData["errorMessage"] = "Lỗi Logic: Giá khuyến mãi phải nhỏ hơn Giá gốc.";
                    return RedirectToAction(nameof(Index), new { productId = viewModel.ProductId });
                }
            }

            try
            {
                // Xử lý chuỗi (Trim và thêm GB)
                string storageValue = ProcessStorageRamValue(viewModel.Storage);
                string ramValue = ProcessStorageRamValue(viewModel.Ram);
                string colorValue = viewModel.Color?.Trim() ?? "Mặc định";

                // 🛡️ 3. BUSINESS LOGIC: Chống trùng lặp biến thể
                // Kiểm tra xem trong sản phẩm này đã có biến thể nào có cùng Màu + Storage + RAM chưa
                bool isDuplicate = await _context.ProductVariants.AnyAsync(v =>
                    v.ProductId == viewModel.ProductId &&
                    v.Color == colorValue &&
                    v.Storage == storageValue &&
                    v.RAM == ramValue);

                if (isDuplicate)
                {
                    TempData["errorMessage"] = $"Lỗi: Biến thể '{colorValue} - {storageValue} - {ramValue}' đã tồn tại trong sản phẩm này.";
                    return RedirectToAction(nameof(Index), new { productId = viewModel.ProductId });
                }

                // Xử lý ảnh
                string? variantImagePath = null;
                if (viewModel.ImageFile != null && viewModel.ImageFile.Length > 0)
                {
                    variantImagePath = await UploadFile(viewModel.ImageFile);
                }

                var newVariant = new ProductVariant
                {
                    ProductId = viewModel.ProductId,
                    Color = colorValue,
                    Storage = storageValue,
                    RAM = ramValue,
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
                TempData["errorMessage"] = $"Lỗi hệ thống: {ex.Message}";
            }

            return RedirectToAction(nameof(Index), new { productId = viewModel.ProductId });
        }

        // --- 3. SỬA BIẾN THỂ ---
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(ProductVariantEditViewModel viewModel)
        {
            if (!ModelState.IsValid)
            {
                TempData["errorMessage"] = "Dữ liệu sửa không hợp lệ.";
                return RedirectToAction(nameof(Index), new { productId = viewModel.ProductId });
            }

            // 🛡️ 1. VALIDATION: Số tiền và Tồn kho
            if (viewModel.Price < 0)
            {
                TempData["errorMessage"] = "Lỗi: Giá bán gốc không được âm.";
                return RedirectToAction(nameof(Index), new { productId = viewModel.ProductId });
            }
            if (viewModel.Stock < 0)
            {
                TempData["errorMessage"] = "Lỗi: Tồn kho không được âm.";
                return RedirectToAction(nameof(Index), new { productId = viewModel.ProductId });
            }

            // 🛡️ 2. BUSINESS LOGIC: Giá khuyến mãi
            if (viewModel.DiscountPrice.HasValue)
            {
                if (viewModel.DiscountPrice.Value < 0)
                {
                    TempData["errorMessage"] = "Lỗi: Giá khuyến mãi không được âm.";
                    return RedirectToAction(nameof(Index), new { productId = viewModel.ProductId });
                }
                if (viewModel.DiscountPrice.Value >= viewModel.Price)
                {
                    TempData["errorMessage"] = "Lỗi: Giá khuyến mãi phải thấp hơn giá gốc.";
                    return RedirectToAction(nameof(Index), new { productId = viewModel.ProductId });
                }
            }

            try
            {
                var variantToUpdate = await _context.ProductVariants.FindAsync(viewModel.VariantId);

                if (variantToUpdate == null)
                {
                    TempData["errorMessage"] = "Không tìm thấy biến thể.";
                    return RedirectToAction(nameof(Index), new { productId = viewModel.ProductId });
                }

                string storageValue = ProcessStorageRamValue(viewModel.Storage);
                string ramValue = ProcessStorageRamValue(viewModel.Ram);
                string colorValue = viewModel.Color?.Trim() ?? "Mặc định";

                // 🛡️ 3. BUSINESS LOGIC: Check trùng lặp khi sửa (Trừ chính nó ra)
                bool isDuplicate = await _context.ProductVariants.AnyAsync(v =>
                    v.VariantId != viewModel.VariantId && // Không check chính nó
                    v.ProductId == viewModel.ProductId &&
                    v.Color == colorValue &&
                    v.Storage == storageValue &&
                    v.RAM == ramValue);

                if (isDuplicate)
                {
                    TempData["errorMessage"] = $"Lỗi: Cập nhật thất bại vì biến thể '{colorValue} - {storageValue} - {ramValue}' đã tồn tại.";
                    return RedirectToAction(nameof(Index), new { productId = viewModel.ProductId });
                }

                // Cập nhật dữ liệu
                variantToUpdate.Color = colorValue;
                variantToUpdate.Storage = storageValue;
                variantToUpdate.RAM = ramValue;
                variantToUpdate.Price = viewModel.Price;
                variantToUpdate.DiscountPrice = viewModel.DiscountPrice;
                variantToUpdate.Stock = viewModel.Stock;
                variantToUpdate.UpdatedDate = DateTime.Now;

                _context.ProductVariants.Update(variantToUpdate);
                await _context.SaveChangesAsync();

                TempData["StatusMessage"] = $"Cập nhật biến thể (ID: {viewModel.VariantId}) thành công.";
            }
            catch (Exception ex)
            {
                TempData["errorMessage"] = $"Lỗi cập nhật: {ex.Message}";
            }

            return RedirectToAction(nameof(Index), new { productId = viewModel.ProductId });
        }

        // --- 4. XÓA BIẾN THỂ (Giữ nguyên logic kiểm tra khóa ngoại tốt của bạn) ---
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int variantId, int productId)
        {
            if (productId <= 0) return RedirectToAction("Index", "Product");

            var variantToDelete = await _context.ProductVariants.FindAsync(variantId);
            if (variantToDelete == null)
            {
                TempData["errorMessage"] = "Không tìm thấy biến thể.";
                return RedirectToAction(nameof(Index), new { productId = productId });
            }

            try
            {
                // Kiểm tra ràng buộc
                var hasOrderDetails = await _context.OrderDetails.AnyAsync(od => od.VariantId == variantId);
                var hasCartItems = await _context.CartItems.AnyAsync(ci => ci.VariantId == variantId);
                var hasReviewDetails = await _context.ReviewDetails.AnyAsync(rd => rd.VariantId == variantId);

                if (hasOrderDetails || hasCartItems || hasReviewDetails)
                {
                    string msg = "Không thể xóa biến thể này vì dữ liệu liên quan: ";
                    if (hasOrderDetails) msg += "[Đơn hàng] ";
                    if (hasCartItems) msg += "[Giỏ hàng] ";
                    if (hasReviewDetails) msg += "[Đánh giá] ";
                    msg += ". Vui lòng chọn Ẩn biến thể.";

                    TempData["errorMessage"] = msg;
                    return RedirectToAction(nameof(Index), new { productId = productId });
                }

                _context.ProductVariants.Remove(variantToDelete);
                await _context.SaveChangesAsync();
                TempData["StatusMessage"] = "Đã xóa biến thể thành công.";
            }
            catch (Exception ex)
            {
                TempData["errorMessage"] = $"Lỗi xóa: {ex.Message}";
            }

            return RedirectToAction(nameof(Index), new { productId = productId });
        }

        // --- 5. ẨN/HIỆN (Giữ nguyên) ---
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ToggleStatus(int variantId, int productId)
        {
            try
            {
                var variant = await _context.ProductVariants.FindAsync(variantId);
                if (variant != null)
                {
                    variant.IsActive = !variant.IsActive;
                    variant.UpdatedDate = DateTime.Now;
                    await _context.SaveChangesAsync();
                    TempData["StatusMessage"] = $"Đã {(variant.IsActive ? "hiện" : "ẩn")} biến thể.";
                }
            }
            catch { TempData["errorMessage"] = "Lỗi khi đổi trạng thái."; }
            return RedirectToAction(nameof(Index), new { productId = productId });
        }

        // --- HELPER METHODS ---
        private async Task<string?> UploadFile(IFormFile file)
        {
            if (file == null || file.Length == 0) return null;
            string uploadDir = Path.Combine(_webHostEnvironment.WebRootPath, "images", "products");
            if (!Directory.Exists(uploadDir)) Directory.CreateDirectory(uploadDir);
            string uniqueFileName = Guid.NewGuid().ToString() + "_" + Path.GetFileName(file.FileName);
            string filePath = Path.Combine(uploadDir, uniqueFileName);
            using (var stream = new FileStream(filePath, FileMode.Create)) await file.CopyToAsync(stream);
            return "/images/products/" + uniqueFileName;
        }

        private string ProcessStorageRamValue(string? value)
        {
            if (string.IsNullOrWhiteSpace(value)) return string.Empty;
            string cleaned = value.Replace("GB", "", StringComparison.OrdinalIgnoreCase).Trim();
            if (int.TryParse(cleaned, out int num)) return $"{num}GB";
            return cleaned;
        }
    }
}
// Thêm các using cần thiết
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using WebBanDienThoai.Models;
using WebBanDienThoai.Models.ViewModels;

namespace WebBanDienThoai.Controllers
{
    // GHI CHÚ: Tên Controller đã được đổi thành "ProductVariant"
    public class ProductVariantController : Controller
    {
        // GHI CHÚ: Sửa lại tên DbContext cho đúng
        private readonly DemoWebBanDienThoaiContext _context;
        private readonly IWebHostEnvironment _webHostEnvironment;

        // GHI CHÚ: Sửa lại tên DbContext cho đúng
        public ProductVariantController(DemoWebBanDienThoaiContext context, IWebHostEnvironment webHostEnvironment)
        {
            _context = context;
            _webHostEnvironment = webHostEnvironment;
        }

        // --- 1. DANH SÁCH BIẾN THỂ (Trang chi tiết) ---
        // GET: /ProductVariant/Index/5
        public async Task<IActionResult> Index(int productId)
        {
            try
            {
                var product = await _context.Products
                                        .Include(p => p.Brand)
                                        .Include(p => p.ProductVariants)
                                        .FirstOrDefaultAsync(p => p.ProductId == productId);

                if (product == null)
                {
                    return NotFound();
                }

                // GHI CHÚ: Sửa sang ViewModel mới (ProductVariantIndexViewModel)
                var viewModel = new ProductVariantIndexViewModel
                {
                    Product = product,
                    Variants = product.ProductVariants.ToList(),
                    // GHI CHÚ: Đổi tên 'AddVariantForm' -> 'CreateForm'
                    CreateForm = new ProductVariantCreateViewModel { ProductId = productId }
                };

                return View(viewModel);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Lỗi khi lấy chi tiết sản phẩm: {ex.Message}");
                return View("Error", new { message = $"Lỗi khi lấy chi tiết sản phẩm: {ex.Message}" });
            }
        }

        // --- 2. THÊM BIẾN THỂ MỚI ---
        // POST: /ProductVariant/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        // GHI CHÚ: Sửa sang ViewModel mới (ProductVariantCreateViewModel)
        public async Task<IActionResult> Create(ProductVariantCreateViewModel viewModel)
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

                    // GHI CHÚ: Giữ nguyên logic xử lý "GB" của bạn
                    string storageValue = viewModel.Storage ?? string.Empty;
                    string ramValue = viewModel.Ram ?? string.Empty;

                    var newVariant = new ProductVariant
                    {
                        ProductId = viewModel.ProductId,
                        Color = viewModel.Color ?? string.Empty,
                        Storage = storageValue.Replace("GB", "").Trim(),
                        RAM = ramValue.Replace("GB", "").Trim(),
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
            else
            {
                TempData["StatusMessage"] = "Lỗi: Dữ liệu không hợp lệ. Vui lòng kiểm tra lại các trường.";
            }

            // GHI CHÚ: Sửa lại tên tham số (id -> productId)
            return RedirectToAction(nameof(Index), new { productId = viewModel.ProductId });
        }

        // --- 3. SỬA BIẾN THỂ ---
        // POST: /ProductVariant/Edit
        [HttpPost]
        [ValidateAntiForgeryToken]
        // GHI CHÚ: Sửa sang ViewModel mới (ProductVariantEditViewModel)
        public async Task<IActionResult> Edit(ProductVariantEditViewModel viewModel)
        {
            if (viewModel.Stock < 0 || viewModel.Price < 0)
            {
                TempData["StatusMessage"] = "Lỗi: Giá hoặc Tồn kho không thể là số âm.";
                return RedirectToAction(nameof(Index), new { productId = viewModel.ProductId });
            }

            // GHI CHÚ: Thêm check ModelState (cho [Required])
            if (ModelState.IsValid)
            {
                try
                {
                    var variantToUpdate = await _context.ProductVariants.FindAsync(viewModel.VariantId);

                    if (variantToUpdate == null)
                    {
                        TempData["StatusMessage"] = "Lỗi: Không tìm thấy biến thể.";
                        return RedirectToAction(nameof(Index), new { productId = viewModel.ProductId });
                    }

                    // GHI CHÚ: Cập nhật từ ViewModel
                    variantToUpdate.Color = viewModel.Color;
                    variantToUpdate.Storage = viewModel.Storage;
                    variantToUpdate.RAM = viewModel.Ram;
                    variantToUpdate.Price = viewModel.Price;
                    variantToUpdate.DiscountPrice = viewModel.DiscountPrice;
                    variantToUpdate.Stock = viewModel.Stock;
                    variantToUpdate.UpdatedDate = DateTime.Now; // (Nên thêm)

                    _context.ProductVariants.Update(variantToUpdate);
                    await _context.SaveChangesAsync();

                    TempData["StatusMessage"] = "Cập nhật biến thể (ID: " + viewModel.VariantId + ") thành công.";
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Lỗi khi sửa biến thể ID {viewModel.VariantId}: {ex.Message}");
                    TempData["StatusMessage"] = "Lỗi khi cập nhật biến thể: " + ex.Message;
                }
            }
            else
            {
                TempData["StatusMessage"] = "Lỗi: Dữ liệu sửa không hợp lệ.";
            }

            return RedirectToAction(nameof(Index), new { productId = viewModel.ProductId });
        }

        // --- 4. XÓA BIẾN THỂ ---
        // POST: /ProductVariant/Delete
        [HttpPost]
        [ValidateAntiForgeryToken]
        // GHI CHÚ: Sửa tham số (id -> variantId, thêm productId)
        public async Task<IActionResult> Delete(int variantId, int productId)
        {
            // Tìm biến thể trong CSDL
            var variantToDelete = await _context.ProductVariants.FindAsync(variantId);

            // Xử lý nếu không tìm thấy (trường hợp này hiếm khi xảy ra)
            if (variantToDelete == null)
            {
                TempData["errorMessage"] = "Lỗi: Không tìm thấy biến thể để xóa.";
                if (productId > 0)
                    return RedirectToAction(nameof(Index), new { productId = productId });
                return RedirectToAction("Index", "Product");
            }

            // Gán lại productId để đảm bảo chuyển hướng về đúng trang
            if (productId == 0)
            {
                productId = variantToDelete.ProductId;
            }

            try
            {
                // Thực hiện xóa
                _context.ProductVariants.Remove(variantToDelete);
                await _context.SaveChangesAsync();

                TempData["errorMessage"] = "Đã xóa biến thể thành công.";
            }
            catch (DbUpdateException dbEx) // Bắt lỗi từ CSDL (quan trọng nhất)
            {
                // Lấy lỗi gốc bên trong (thường là SqlException)
                var baseException = dbEx.GetBaseException() as SqlException;

                // Mã 547 là mã lỗi "Vi phạm ràng buộc khóa ngoại" của SQL Server
                if (baseException != null && baseException.Number == 547)
                {
                    // Lấy nội dung thông báo lỗi
                    string errorMessage = baseException.Message;

                    // KIỂM TRA CHÍNH XÁC LỖI TỪ BẢNG NÀO
                    if (errorMessage.Contains("OrderDetails"))
                    {
                        TempData["errorMessage"] = "Lỗi: Không thể xóa. Biến thể này đã tồn tại trong 'Chi tiết Đơn hàng' của khách.";
                    }
                    else if (errorMessage.Contains("CartItems"))
                    {
                        TempData["errorMessage"] = "Lỗi: Không thể xóa. Biến thể này đang nằm trong 'Giỏ hàng' của một khách hàng.";
                    }
                    else if (errorMessage.Contains("ReviewDetails"))
                    {
                        TempData["errorMessage"] = "Lỗi: Không thể xóa. Biến thể này đã được 'Đánh giá' bởi khách hàng.";
                    }
                    else if (errorMessage.Contains("FavoriteDetails"))
                    {
                        TempData["errorMessage"] = "Lỗi: Không thể xóa. Biến thể này nằm trong 'Danh sách Yêu thích' của khách hàng.";
                    }
                    else
                    {
                        // Lỗi 547 mà không xác định được (trường hợp dự phòng)
                        TempData["errorMessage"] = "Lỗi: Không thể xóa do vi phạm ràng buộc dữ liệu không xác định.";
                    } 
                }
                else
                {
                    // Các lỗi CSDL khác
                    Console.WriteLine($"Lỗi DbUpdateException khi xóa: {dbEx.Message}");
                    TempData["errorMessage"] = "Lỗi cơ sở dữ liệu khi xóa.";
                }
            }
            catch (Exception ex) // Bắt các lỗi chung khác
            {
                Console.WriteLine($"Lỗi chung khi xóa biến thể ID {variantId}: {ex.Message}");
                TempData["StatusMessage"] = "Lỗi không xác định khi xóa biến thể.";
            }

            // Luôn chuyển hướng về trang chi tiết sản phẩm
            return RedirectToAction(nameof(Index), new { productId = productId });
        }

        // --- 5. HÀM HỖ TRỢ (PRIVATE) ---
        private async Task<string?> UploadFile(IFormFile file)
        {
            string uploadDir = Path.Combine(_webHostEnvironment.WebRootPath, "images", "products");

            if (!Directory.Exists(uploadDir))
            {
                Directory.CreateDirectory(uploadDir);
            }

            string uniqueFileName = Guid.NewGuid().ToString() + "_" + Path.GetFileName(file.FileName);
            string filePath = Path.Combine(uploadDir, uniqueFileName);

            using (var fileStream = new FileStream(filePath, FileMode.Create))
            {
                await file.CopyToAsync(fileStream);
            }

            return "/images/products/" + uniqueFileName;
        }
    }
}
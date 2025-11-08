// Thêm các using cần thiết
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using WebBanDienThoai.Models; // Cần Models
using WebBanDienThoai.Models.ViewModels; // Cần ViewModels

namespace WebBanDienThoai.Controllers
{
    public class ProductController : Controller
    {
        private readonly DemoWebBanDienThoaiContext _context;
        private readonly IWebHostEnvironment _webHostEnvironment;

        public ProductController(DemoWebBanDienThoaiContext context, IWebHostEnvironment webHostEnvironment)
        {
            _context = context;
            _webHostEnvironment = webHostEnvironment;
        }

        // --- 2. QUẢN LÝ SẢN PHẨM (Trang chính) ---
        public async Task<IActionResult> Index(int? brandId, string searchId)
        {
            try
            {
                var productsQuery = _context.Products
                                        .Include(p => p.Brand)
                                        .Include(p => p.ProductVariants)
                                        .AsQueryable();

                if (!string.IsNullOrEmpty(searchId))
                {
                    productsQuery = productsQuery.Where(o => o.ProductId.ToString().Contains(searchId) || o.Name.Contains(searchId));
                }

                if (brandId.HasValue && brandId.Value > 0)
                {
                    productsQuery = productsQuery.Where(p => p.BrandId == brandId.Value);
                }

                // ====================================================================
                // SỬA LẠI KHỐI .Select() DƯỚI ĐÂY
                // ====================================================================
                var productList = await productsQuery
                    .OrderBy(p => p.Name)
                    .Select(p => new ProductListViewModel
                    {
                        ProductId = p.ProductId,
                        Name = p.Name ?? "N/A",
                        MainImage = p.MainImage,
                        BrandId = p.BrandId,
                        BrandName = p.Brand != null ? (p.Brand.BrandName ?? "N/A") : "N/A",

                        CreatedDate = p.CreatedDate.GetValueOrDefault(),

                        IsActive = p.IsActive,

                        FirstVariantPrice = (p.ProductVariants != null && p.ProductVariants.Any())
                                            ? p.ProductVariants.OrderBy(v => v.VariantId).First().DiscountPrice.GetValueOrDefault(p.ProductVariants.OrderBy(v => v.VariantId).First().Price.GetValueOrDefault())
                                            : 0m,

                        LowestPrice = (p.ProductVariants != null && p.ProductVariants.Any())
                                            ? p.ProductVariants.Min(v => v.DiscountPrice.GetValueOrDefault(v.Price.GetValueOrDefault()))
                                            : 0m,

                        TotalStock = p.ProductVariants.Sum(v => v.Stock).GetValueOrDefault()
                    })
                    .ToListAsync();
                // ====================================================================
                // KẾT THÚC SỬA
                // ====================================================================

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

                var viewModel = new ProductIndexViewModel
                {
                    Products = productList,
                    BrandCounts = brandCounts,
                    TotalProductCount = totalProductCount
                };

                ViewBag.SearchId = searchId;

                return View(viewModel);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Lỗi tải danh sách sản phẩm: {ex.Message}");
                return View("Error", new { message = $"Lỗi tải danh sách sản phẩm: {ex.Message}" });
            }
        }

        // POST: /Product/Delete
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
        {
            try
            {
                var productToDelete = await _context.Products
                                                .Include(p => p.ProductVariants)
                                                .FirstOrDefaultAsync(p => p.ProductId == id);

                if (productToDelete == null)
                {
                    return NotFound();
                }

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

            return RedirectToAction(nameof(Index));
        }


        // --- 3. THÊM SẢN PHẨM MỚI ---

        // GET: /Product/Create
        public async Task<IActionResult> Create()
        {
            var viewModel = new ProductCreateViewModel
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

        // POST: /Product/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(ProductCreateViewModel viewModel)
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

                    // 1. Lưu Product
                    Product newProduct = viewModel.Product!;
                    newProduct.CreatedDate = DateTime.Now;
                    newProduct.IsActive = true;
                    newProduct.MainImage = mainImagePath;

                    _context.Products.Add(newProduct);
                    await _context.SaveChangesAsync();

                    // 2. Lưu Biến thể đầu tiên
                    var newVariant = new ProductVariant
                    {
                        ProductId = newProduct.ProductId,
                        Color = viewModel.VariantColor ?? "Mặc định",
                        Storage = viewModel.VariantStorage ?? "N/A",
                        RAM = viewModel.VariantRam,
                        Price = viewModel.VariantPrice,
                        DiscountPrice = null,
                        Stock = viewModel.VariantStock,
                        ImageUrl = variantImagePath ?? mainImagePath,
                        IsActive = true,
                        CreatedDate = DateTime.Now
                    };

                    _context.ProductVariants.Add(newVariant);
                    await _context.SaveChangesAsync();

                    TempData["StatusMessage"] = "Thêm sản phẩm mới (và biến thể đầu tiên) thành công.";
                    return RedirectToAction(nameof(Index));
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Lỗi khi tạo sản phẩm: {ex.Message}");
                    ModelState.AddModelError("", "Có lỗi xảy ra, không thể lưu sản phẩm.");
                }
            }

            // Nếu lỗi, tải lại BrandList
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

        // --- 4. SỬA SẢN PHẨM ---

        // GET: /Product/Edit/5
        public async Task<IActionResult> Edit(int id)
        {
            var product = await _context.Products.FindAsync(id);
            if (product == null)
            {
                return NotFound();
            }

            var brandList = await _context.Brands
                                    .OrderBy(b => b.BrandName)
                                    .Select(b => new SelectListItem
                                    {
                                        Value = b.BrandId.ToString(),
                                        Text = b.BrandName,
                                        Selected = (b.BrandId == product.BrandId)
                                    })
                                    .ToListAsync();

            var viewModel = new ProductEditViewModel
            {
                Product = product,
                BrandList = brandList
            };

            return View(viewModel);
        }

        // POST: /Product/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, ProductEditViewModel viewModel)
        {
            if (id != viewModel.Product.ProductId)
            {
                return NotFound();
            }

            ModelState.Remove("MainImageFile");

            if (ModelState.IsValid)
            {
                try
                {
                    var productFromDb = await _context.Products.AsNoTracking()
                                                  .FirstOrDefaultAsync(p => p.ProductId == id);

                    if (productFromDb == null) return NotFound();

                    string? mainImagePath = productFromDb.MainImage;
                    if (viewModel.MainImageFile != null)
                    {
                        mainImagePath = await UploadFile(viewModel.MainImageFile);
                    }

                    Product productToUpdate = viewModel.Product;
                    productToUpdate.MainImage = mainImagePath;
                    productToUpdate.CreatedDate = productFromDb.CreatedDate;
                    productToUpdate.UpdatedDate = DateTime.Now;

                    _context.Update(productToUpdate);
                    await _context.SaveChangesAsync();

                    TempData["StatusMessage"] = "Cập nhật sản phẩm thành công.";
                    return RedirectToAction(nameof(Index));
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!_context.Products.Any(e => e.ProductId == viewModel.Product.ProductId))
                    {
                        return NotFound();
                    }
                    else
                    {
                        throw;
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Lỗi khi cập nhật sản phẩm: {ex.Message}");
                    ModelState.AddModelError("", "Có lỗi xảy ra, không thể lưu sản phẩm.");
                }
            }

            // Nếu Model không hợp lệ, tải lại BrandList
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
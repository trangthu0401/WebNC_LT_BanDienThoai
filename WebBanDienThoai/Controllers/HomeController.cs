using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using WebBanDienThoai.Models;
using WebBanDienThoai.Models.modelView; // BẮT BUỘC THÊM DÒNG NÀY
using Microsoft.EntityFrameworkCore;
using System.Linq;
using System.Threading.Tasks;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Collections.Generic;

namespace WebBanDienThoai.Controllers
{
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;
        private readonly DemoWebBanDienThoaiContext _context;

        public HomeController(ILogger<HomeController> logger, DemoWebBanDienThoaiContext context)
        {
            _logger = logger;
            _context = context;
        }

        // Action cho Trang chủ (Views/Home/Index.cshtml)
        public async Task<IActionResult> Index(int? id, string sortOrder, string searchString)
        {
            ViewData["CurrentSort"] = sortOrder;
            ViewData["CurrentSearch"] = searchString;
            try
            {
                int customerId = 1; // Giả định CustomerId = 1
                var favoritedVariantIds = new HashSet<int?>();
                var customerFavorite = await _context.Favorites
                                      .Include(f => f.FavoriteDetails)
                                      .FirstOrDefaultAsync(f => f.CustomerId == customerId);

                if (customerFavorite != null)
                {
                    favoritedVariantIds = customerFavorite.FavoriteDetails.Select(fd => fd.VariantId).ToHashSet();
                }

                var productsQuery = _context.Products
                    .AsNoTracking()
                    .Where(p => p.IsActive == true && p.ProductVariants.Any(v => v.IsActive == true));

                if (id != null && id > 0)
                {
                    productsQuery = productsQuery.Where(p => p.BrandId == id);
                }

                if (!String.IsNullOrEmpty(searchString))
                {
                    productsQuery = productsQuery.Where(p => p.Name.ToLower().Contains(searchString.ToLower()));
                }

                var viewModelQuery = productsQuery
                    .Include(p => p.Brand)
                    .Include(p => p.ProductVariants)
                    .Select(p => new ProductListViewModel
                    {
                        ProductId = p.ProductId,
                        Name = p.Name,
                        MainImage = p.MainImage,
                        BrandName = p.Brand.BrandName,
                        IsActive = p.IsActive ?? false,
                        CreatedDate = p.CreatedDate ?? DateTime.MinValue,
                        Price = p.ProductVariants
                                 .Where(v => v.IsActive == true)
                                 .Select(v => v.DiscountPrice ?? v.Price)
                                 .Min() ?? 0M,
                        Stock = p.ProductVariants
                                 .Where(v => v.IsActive == true)
                                 .Sum(v => v.Stock ?? 0),
                        IsFavorited = p.ProductVariants.Any(v => favoritedVariantIds.Contains(v.VariantId)),
                        FirstVariantId = p.ProductVariants
                                          .Where(v => v.IsActive == true)
                                          .Select(v => v.VariantId)
                                          .FirstOrDefault()
                    });

                switch (sortOrder)
                {
                    case "price_desc": viewModelQuery = viewModelQuery.OrderByDescending(p => p.Price); break;
                    case "price_asc": viewModelQuery = viewModelQuery.OrderBy(p => p.Price); break;
                    default: viewModelQuery = viewModelQuery.OrderByDescending(p => p.CreatedDate); break;
                }

                var dsSanPham = await viewModelQuery.ToListAsync();
                var dsHang = await _context.Brands.AsNoTracking().Where(b => b.Products.Any(p => p.IsActive == true)).Select(b => new BrandCount { brandId = b.BrandId, BrandName = b.BrandName, Count = b.Products.Count(p => p.IsActive == true) }).ToListAsync();
                var totalProductCount = await _context.Products.CountAsync(p => p.IsActive == true && p.ProductVariants.Any(v => v.IsActive == true));

                var viewModel = new ManageProductsViewModel
                {
                    Products = dsSanPham,
                    BrandCounts = dsHang,
                    TotalProductCount = totalProductCount,
                    BrandId = id
                };

                return View(viewModel);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi khi lấy dữ liệu cho trang Home Index.");
                return RedirectToAction("Error");
            }
        }

        // Action cho Trang chi tiết (Views/Home/ProductDetail.cshtml)
        // 'id' ở đây là VariantId
        public async Task<IActionResult> ProductDetail(int id)
        {
            try
            {
                var productDetail = await _context.ProductVariants
                    .AsNoTracking()
                    .Include(v => v.Product)
                    .Include(v => v.Product.Brand)
                    .FirstOrDefaultAsync(v => v.VariantId == id);

                if (productDetail == null) return NotFound();

                var allVariants = await _context.ProductVariants
                    .AsNoTracking()
                    .Where(v => v.ProductId == productDetail.ProductId && v.IsActive == true)
                    .ToListAsync();

                var relatedProducts = await _context.ProductVariants
                    .AsNoTracking()
                    .Include(v => v.Product.Brand)
                    .Where(v => v.Product.BrandId == productDetail.Product.BrandId &&
                                v.ProductId != productDetail.ProductId &&
                                v.IsActive == true)
                    .Take(4)
                    .ToListAsync();

                var viewModel = new ProductDetailViewModel
                {
                    ProductDetail = productDetail,
                    AllVariants = allVariants,
                    RelatedProducts = relatedProducts
                };

                return View(viewModel);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi khi lấy ProductDetail.");
                return RedirectToAction("Error");
            }
        }

        // Action cho Trang yêu thích (Views/Home/Favorites.cshtml)
        public async Task<IActionResult> Favorites(string sortOrder)
        {
            ViewData["CurrentSort"] = sortOrder;
            try
            {
                int customerId = 1; // Giả định CustomerId = 1
                var favoritesQuery = _context.FavoriteDetails.AsNoTracking().Where(fd => fd.Favorite.CustomerId == customerId && fd.Variant.IsActive == true).Include(fd => fd.Variant.Product.Brand).Select(fd => fd.Variant).Select(v => new ProductListViewModel { ProductId = v.ProductId, Name = v.Product.Name + " (" + v.Color + ", " + v.Storage + ")", MainImage = v.ImageUrl ?? v.Product.MainImage, BrandName = v.Product.Brand.BrandName, IsActive = v.IsActive ?? false, CreatedDate = v.CreatedDate ?? DateTime.MinValue, Price = (v.DiscountPrice ?? v.Price) ?? 0M, Stock = v.Stock ?? 0 });
                switch (sortOrder)
                {
                    case "price_desc": favoritesQuery = favoritesQuery.OrderByDescending(p => p.Price); break;
                    case "price_asc": favoritesQuery = favoritesQuery.OrderBy(p => p.Price); break;
                    default: favoritesQuery = favoritesQuery.OrderByDescending(p => p.CreatedDate); break;
                }
                var favoriteProducts = await favoritesQuery.ToListAsync();
                var dsHang = await _context.Brands.AsNoTracking().Where(b => b.Products.Any(p => p.IsActive == true)).Select(b => new BrandCount { brandId = b.BrandId, BrandName = b.BrandName, Count = b.Products.Count(p => p.IsActive == true) }).ToListAsync();
                var totalProductCount = await _context.Products.CountAsync(p => p.IsActive == true && p.ProductVariants.Any(v => v.IsActive == true));
                var viewModel = new FavoritesViewModel { FavoriteProducts = favoriteProducts, BrandCounts = dsHang, TotalProductCount = totalProductCount };
                return View(viewModel);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi khi lấy dữ liệu cho trang Favorites.");
                return RedirectToAction("Error");
            }
        }

        [HttpPost]
        public async Task<IActionResult> AddToFavorites(int id)
        {
            try
            {
                int customerId = 1; var customerFavorite = await _context.Favorites.FirstOrDefaultAsync(f => f.CustomerId == customerId);
                if (customerFavorite == null) { customerFavorite = new Favorite { CustomerId = customerId }; _context.Favorites.Add(customerFavorite); await _context.SaveChangesAsync(); }
                var variantToAdd = await _context.ProductVariants.Where(v => v.ProductId == id && v.IsActive == true).Select(v => v.VariantId).FirstOrDefaultAsync();
                if (variantToAdd == 0) { return Json(new { success = false, message = "Sản phẩm không có biến thể hợp lệ." }); }
                bool alreadyExists = await _context.FavoriteDetails.AnyAsync(fd => fd.FavoriteId == customerFavorite.FavoriteId && fd.VariantId == variantToAdd);
                if (!alreadyExists) { _context.FavoriteDetails.Add(new FavoriteDetail { FavoriteId = customerFavorite.FavoriteId, VariantId = variantToAdd }); await _context.SaveChangesAsync(); }
                return Json(new { success = true, message = "Đã thêm vào yêu thích!" });
            }
            catch (Exception ex) { _logger.LogError(ex, "Lỗi khi thêm vào Favorites."); return Json(new { success = false, message = "Lỗi máy chủ." }); }
        }

        [HttpPost]
        public async Task<IActionResult> RemoveFromFavorites(int id)
        {
            try
            {
                int customerId = 1; var customerFavorite = await _context.Favorites.Include(f => f.FavoriteDetails).FirstOrDefaultAsync(f => f.CustomerId == customerId);
                if (customerFavorite == null) { return Json(new { success = true }); }
                var variantIdsToRemove = await _context.ProductVariants.Where(v => v.ProductId == id).Select(v => v.VariantId).ToListAsync();
                if (!variantIdsToRemove.Any()) { return Json(new { success = false, message = "Không tìm thấy sản phẩm." }); }
                var detailToRemove = customerFavorite.FavoriteDetails.Where(fd => fd.VariantId != null && variantIdsToRemove.Contains(fd.VariantId.Value)).ToList();
                if (detailToRemove.Any()) { _context.FavoriteDetails.RemoveRange(detailToRemove); await _context.SaveChangesAsync(); }
                return Json(new { success = true, message = "Đã xóa khỏi yêu thích." });
            }
            catch (Exception ex) { _logger.LogError(ex, "Lỗi khi xóa khỏi Favorites."); return Json(new { success = false, message = "Lỗi máy chủ." }); }
        }

        [HttpGet]
        public async Task<IActionResult> SearchSuggestions(string term)
        {
            if (string.IsNullOrEmpty(term) || term.Length < 2) return Json(new List<ProductSuggestionViewModel>());
            var suggestions = await _context.Products.Where(p => p.Name.ToLower().Contains(term.ToLower()) && p.IsActive == true).Select(p => new ProductSuggestionViewModel { ProductId = p.ProductId, Name = p.Name, MainImage = p.MainImage, Price = p.ProductVariants.Where(v => v.IsActive == true).Select(v => v.DiscountPrice ?? v.Price).Min() ?? 0M }).Take(5).ToListAsync();
            return Json(suggestions);
        }

        [HttpPost]
        public async Task<IActionResult> GetAiResponse([FromBody] ChatRequest request)
        {
            try
            {
                var apiKey = "AIzaSyBnUmG9b_K-Q1fTQ0i6loBKulGRNuXU0fg"; var apiUrl = $"https://generativelanguage.googleapis.com/v1beta/models/gemini-1.5-flash:generateContent?key={apiKey}";
                using (var httpClient = new HttpClient())
                {
                    httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json")); var systemInstruction = "Bạn là Rabit AI, trợ lý tư vấn điện thoại của Rabit Store. Hãy trả lời ngắn gọn, thân thiện."; var payload = new { contents = new[] { new { role = "user", parts = new[] { new { text = systemInstruction } } }, new { role = "model", parts = new[] { new { text = "Chào bạn! Tôi có thể giúp gì?" } } }, new { role = "user", parts = new[] { new { text = request.Message } } } } };
                    var jsonPayload = JsonSerializer.Serialize(payload); var content = new StringContent(jsonPayload, Encoding.UTF8, "application/json"); var response = await httpClient.PostAsync(apiUrl, content);
                    if (response.IsSuccessStatusCode)
                    {
                        var responseBody = await response.Content.ReadAsStringAsync(); using (var jsonDoc = JsonDocument.Parse(responseBody)) { var botText = jsonDoc.RootElement.GetProperty("candidates")[0].GetProperty("content").GetProperty("parts")[0].GetProperty("text").GetString(); return Ok(new { success = true, message = botText }); }
                    }
                    else
                    {
                        var errorBody = await response.Content.ReadAsStringAsync(); _logger.LogError($"Lỗi API Gemini: {errorBody}"); return Ok(new { success = false, message = "Xin lỗi, AI đang bận. Bạn thử lại sau nhé." });
                    }
                }
            }
            catch (Exception ex) { _logger.LogError(ex, "Lỗi nghiêm trọng khi gọi GetAiResponse."); return Ok(new { success = false, message = "Lỗi kết nối đến máy chủ AI." }); }
        }
        public class ChatRequest { public string Message { get; set; } }


        // === CÁC ACTION CỦA GIỎ HÀNG ĐÃ ĐƯỢC XÓA KHỎI ĐÂY ===


        public IActionResult Privacy() { return View(); }
        public IActionResult Home() { return View(); }
        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error() { return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier }); }
    }
}
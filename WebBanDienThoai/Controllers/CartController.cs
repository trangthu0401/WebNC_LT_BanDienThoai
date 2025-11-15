using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WebBanDienThoai.Models;
using WebBanDienThoai.Models.modelView; // Thêm dòng này
using System.Collections.Generic;        // Thêm dòng này
using System.Linq;
using System.Threading.Tasks;

namespace WebBanDienThoai.Controllers
{
    public class CartController : Controller
    {
        private readonly DemoWebBanDienThoaiContext _context;
        private readonly ILogger<CartController> _logger;

        public CartController(DemoWebBanDienThoaiContext context, ILogger<CartController> logger)
        {
            _context = context;
            _logger = logger;
        }

        // Sẽ chạy khi người dùng truy cập /Cart/Index (link "Giỏ hàng" trên Header)
        [HttpGet]
        public async Task<IActionResult> Index()
        {
            int customerId = 1; // Giả định CustomerId = 1
            try
            {
                var cartItems = await _context.CartItems
                    .AsNoTracking()
                    .Where(c => c.CustomerId == customerId)
                    .Include(c => c.Variant)
                    .ThenInclude(v => v.Product)
                    .ToListAsync();

                var itemViewModels = new List<CartItemViewModel>();
                decimal totalAmount = 0;

                foreach (var item in cartItems)
                {
                    if (item.Variant == null || item.Variant.IsActive == false || item.Variant.Product == null)
                    {
                        continue;
                    }

                    var price = item.Variant.DiscountPrice ?? item.Variant.Price ?? 0;
                    var totalPrice = price * item.Quantity;

                    itemViewModels.Add(new CartItemViewModel
                    {
                        CartItemId = item.CartItemId,
                        VariantId = item.Variant.VariantId,
                        ProductName = item.Variant.Product.Name,
                        Color = item.Variant.Color,
                        ImageUrl = item.Variant.ImageUrl ?? item.Variant.Product.MainImage,
                        Price = price,
                        Quantity = item.Quantity,
                        TotalPrice = totalPrice
                    });

                    totalAmount += totalPrice;
                }

                var viewModel = new CartViewModel
                {
                    CartItems = itemViewModels,
                    TotalAmount = totalAmount
                };

                // Trả về View("Index.cshtml") trong thư mục "Views/Cart/"
                // (Vì bạn đã đổi tên file _Layout.cshtml thành Index.cshtml)
                return View(viewModel);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi khi lấy dữ liệu cho trang Cart Index.");
                return RedirectToAction("Error", "Home"); // Chuyển đến trang Error
            }
        }

        // Sẽ chạy khi form submit từ /Cart/Index (nút Cập nhật/Xóa)
        [HttpPost]
        public async Task<IActionResult> UpdateCart(Dictionary<int, int> quantities, int[] selectedItemIds, string action)
        {
            int customerId = 1; // Giả định CustomerId = 1
            try
            {
                if (action == "update")
                {
                    foreach (var item in quantities)
                    {
                        int cartItemId = item.Key;
                        int newQuantity = item.Value;
                        if (newQuantity < 1) newQuantity = 1;

                        var cartItem = await _context.CartItems
                            .FirstOrDefaultAsync(c => c.CartItemId == cartItemId && c.CustomerId == customerId);

                        if (cartItem != null)
                        {
                            cartItem.Quantity = newQuantity;
                        }
                    }
                }
                else if (action == "delete")
                {
                    if (selectedItemIds != null && selectedItemIds.Any())
                    {
                        var itemsToRemove = await _context.CartItems
                            .Where(c => c.CustomerId == customerId && selectedItemIds.Contains(c.CartItemId))
                            .ToListAsync();

                        if (itemsToRemove.Any())
                        {
                            _context.CartItems.RemoveRange(itemsToRemove);
                        }
                    }
                }

                await _context.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi khi cập nhật giỏ hàng.");
            }

            // Quay trở lại trang Giỏ hàng (/Cart/Index)
            return RedirectToAction("Index");
        }

        // Sẽ được gọi bằng AJAX từ nút "Add" trên trang ProductDetail
        [HttpPost]
        public async Task<IActionResult> AddToCart(int variantId, int quantity = 1)
        {
            if (quantity < 1) quantity = 1;

            int customerId = 1; // Giả định CustomerId = 1
            try
            {
                var variant = await _context.ProductVariants
                    .FirstOrDefaultAsync(v => v.VariantId == variantId && v.IsActive == true);

                if (variant == null)
                {
                    return Json(new { success = false, message = "Sản phẩm không tồn tại." });
                }
                if (variant.Stock < quantity)
                {
                    return Json(new { success = false, message = "Sản phẩm không đủ hàng." });
                }

                // KIỂM TRA SẢN PHẨM ĐÃ CÓ TRONG GIỎ CHƯA
                var existingCartItem = await _context.CartItems
                    .FirstOrDefaultAsync(c => c.CustomerId == customerId && c.VariantId == variantId);

                if (existingCartItem != null)
                {
                    // ĐÃ CÓ -> CẬP NHẬT SỐ LƯỢNG
                    existingCartItem.Quantity += quantity;
                }
                else
                {
                    // CHƯA CÓ -> THÊM MỚI
                    var newCartItem = new CartItem
                    {
                        CustomerId = customerId,
                        VariantId = variantId,
                        Quantity = quantity
                    };
                    _context.CartItems.Add(newCartItem);
                }

                await _context.SaveChangesAsync();
                return Json(new { success = true, message = "Đã thêm vào giỏ hàng!" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi khi thêm vào giỏ hàng.");
                return Json(new { success = false, message = "Lỗi máy chủ." });
            }
        }
    }
}
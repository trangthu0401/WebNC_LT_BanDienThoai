using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Linq;
using System.Threading.Tasks;
using WebBanDienThoai.Models;
using WebBanDienThoai.ViewModels;
using WebBanDienThoai.Utils; // Thư viện VnPay Helper
using System.Collections.Generic;
using System.Security.Claims; // Để lấy UserID

namespace WebBanDienThoai.Controllers
{
    public class CheckoutController : Controller
    {
        private readonly DemoWebBanDienThoai1Context _context;
        private readonly IHttpContextAccessor _httpContextAccessor;

        public CheckoutController(DemoWebBanDienThoai1Context context, IHttpContextAccessor httpContextAccessor)
        {
            _context = context;
            _httpContextAccessor = httpContextAccessor;
        }

        // === HÀM HELPER: LẤY CUSTOMER TỪNG HỆ THỐNG ĐĂNG NHẬP ===
        private async Task<Customer?> GetCurrentUserCustomer()
        {
            // Lấy AccountID từ Claim (đã được lưu khi đăng nhập)
            var accountIdString = User.FindFirstValue("AccountID"); 
            
            if (string.IsNullOrEmpty(accountIdString))
            {
                return null; // Trả về null nếu chưa đăng nhập
            }
            
            int accountId = int.Parse(accountIdString);

            // Dùng AccountID để tìm Customer
            var customer = await _context.Customers
                .Where(c => c.AccountID == accountId)
                .FirstOrDefaultAsync();

            return customer;
        }

        // =================================================================
        // === BƯỚC 1: HIỂN THỊ TRANG THÔNG TIN (Trang 1) ===
        // =================================================================
        [HttpGet]
        public async Task<IActionResult> Index()
        {
            var customer = await GetCurrentUserCustomer();
            if (customer == null)
            {
                // Nếu chưa đăng nhập, chuyển hướng đến trang đăng nhập
                return RedirectToAction("Login", "Account");
            }
            int currentCustomerId = customer.CustomerID;
            
            var defaultAddress = await _context.Addresses
                .Where(a => a.CustomerID == currentCustomerId && a.IsDefault == true)
                .FirstOrDefaultAsync();
                
            var account = await _context.Accounts.FindAsync(customer.AccountID);

            var cartItems = await _context.CartItems
                .Where(ci => ci.CustomerID == currentCustomerId) 
                .Include(ci => ci.Variant)
                    .ThenInclude(v => v.Product)
                .ToListAsync();

            var viewModel = new CheckoutViewModel
            {
                FullName = customer.FullName,
                Phone = customer.Phone,
                Email = account?.Email,
                Street = defaultAddress?.Street,
                District = defaultAddress?.District,
                City = defaultAddress?.City,
                CartItems = cartItems.Select(ci => new CartItemViewModel
                {
                    VariantId = ci.VariantId.GetValueOrDefault(), 
                    ProductName = ci.Variant.Product.Name,
                    Color = ci.Variant.Color,
                    Storage = ci.Variant.Storage,
                    Quantity = ci.Quantity,
                    UnitPrice = ci.Variant.DiscountPrice ?? ci.Variant.Price,
                    ImageUrl = ci.Variant.ImageUrl ?? ci.Variant.Product.MainImage
                }).ToList(),
                Subtotal = cartItems.Sum(ci => (ci.Variant.DiscountPrice ?? ci.Variant.Price) * ci.Quantity),
                ShippingFee = 0 // Trang 1 luôn hiển thị là 0
            };
            return View(viewModel);
        }

        // =================================================================
        // === BƯỚC 2: NHẬN THÔNG TIN VÀ CHUYỂN SANG THANH TOÁN ===
        // =================================================================
        [HttpPost]
        public IActionResult SubmitInfo(CheckoutViewModel model)
        {
            // Lưu địa chỉ vào TempData (Session)
            TempData["Street"] = model.Street;
            TempData["District"] = model.District;
            TempData["City"] = model.City;
            TempData["Note"] = model.Note;

            // XỬ LÝ PHÍ VẬN CHUYỂN
            decimal shippingFee = 0;
            if (model.ShippingMethod == "delivery") // Nếu chọn "Giao hàng tận nơi"
            {
                shippingFee = 100000; // Phí là 100.000 VND
            }
            TempData["ShippingFee"] = shippingFee;

            // Chuyển hướng người dùng đến Action "Payment" (GET)
            return RedirectToAction("Payment");
        }

        // =================================================================
        // === BƯỚC 3: HIỂN THỊ TRANG THANH TOÁN (Trang 2) ===
        // =================================================================
        [HttpGet]
        public async Task<IActionResult> Payment()
        {
            var customer = await GetCurrentUserCustomer();
            if (customer == null) { return RedirectToAction("Login", "Account"); }
            int currentCustomerId = customer.CustomerID;
            
            var account = await _context.Accounts.FindAsync(customer.AccountID);
            var cartItems = await _context.CartItems
                .Where(ci => ci.CustomerID == currentCustomerId)
                .Include(ci => ci.Variant).ThenInclude(v => v.Product)
                .ToListAsync();
            if (!cartItems.Any()) { return RedirectToAction("Index", "Home"); }

            // LẤY PHÍ VẬN CHUYỂN TỪ TEMPDATA
            decimal shippingFee = Convert.ToDecimal(TempData["ShippingFee"] ?? 0);
            
            var viewModel = new CheckoutViewModel
            {
                FullName = customer.FullName,
                Phone = customer.Phone,
                Email = account?.Email,
                Street = TempData["Street"]?.ToString(),
                District = TempData["District"]?.ToString(),
                City = TempData["City"]?.ToString(),
                Note = TempData["Note"]?.ToString(),
                CartItems = cartItems.Select(ci => new CartItemViewModel
                {
                    VariantId = ci.VariantId.GetValueOrDefault(),
                    ProductName = ci.Variant.Product.Name,
                    Color = ci.Variant.Color,
                    Storage = ci.Variant.Storage,
                    Quantity = ci.Quantity,
                    UnitPrice = ci.Variant.DiscountPrice ?? ci.Variant.Price,
                    ImageUrl = ci.Variant.ImageUrl ?? ci.Variant.Product.MainImage
                }).ToList(),
                
                // GÁN PHÍ VẬN CHUYỂN
                Subtotal = cartItems.Sum(ci => (ci.Variant.DiscountPrice ?? ci.Variant.Price) * ci.Quantity),
                ShippingFee = shippingFee 
            };
            
            TempData.Keep(); // Giữ lại TempData
            return View("Payment", viewModel); 
        }

        // =================================================================
        // === BƯỚC 4: XỬ LÝ ĐẶT HÀNG (API) [HttpPost] ===
        // (Đã đơn giản hóa, KHÔNG còn AJAX)
        // =================================================================
        [HttpPost]
        public async Task<IActionResult> PlaceOrder(CheckoutViewModel model)
        {
            var customer = await GetCurrentUserCustomer();
            if (customer == null) { return Unauthorized("Vui lòng đăng nhập."); }
            int currentCustomerId = customer.CustomerID;
            
            var cartItems = await _context.CartItems
                .Where(ci => ci.CustomerID == currentCustomerId)
                .Include(ci => ci.Variant)
                .ToListAsync();
            if (!cartItems.Any()) { return BadRequest("Giỏ hàng rỗng."); }

            // LẤY LẠI PHÍ VẬN CHUYỂN TỪ TEMPDATA
            decimal shippingFee = Convert.ToDecimal(TempData["ShippingFee"] ?? 0);
            
            // TÍNH TỔNG TIỀN (BAO GỒM PHÍ SHIP)
            decimal subtotal = cartItems.Sum(ci => (ci.Variant.DiscountPrice ?? ci.Variant.Price) * ci.Quantity);
            decimal totalAmount = subtotal + shippingFee; 

            var order = new Order
            {
                CustomerID = currentCustomerId,
                OrderDate = DateTime.Now,
                Status = "Pending", 
                TotalAmount = totalAmount, // Lưu tổng tiền ĐÚNG
                OrderDetails = cartItems.Select(item => new OrderDetail
                {
                    VariantId = item.VariantId,
                    Quantity = item.Quantity,
                    UnitPrice = item.Variant.DiscountPrice ?? item.Variant.Price
                }).ToList()
            };

            _context.Orders.Add(order);
            await _context.SaveChangesAsync(); 

            // --- PHÂN LOẠI (NGÃ RẼ) ---
            switch (model.PaymentMethod)
            {
                case "COD":
                    return await ProcessCOD(order, cartItems);

                case "VnPay": // Cho "Liên kết ngân hàng"
                case "QR":    // Cho "Thanh toán QR"
                    return ProcessVnPay(order); // Cả hai đều dùng VnPay Sandbox

                default:
                    return BadRequest("Phương thức thanh toán không hợp lệ.");
            }
        }

        // =================================================================
        // === CÁC HÀM XỬ LÝ RIÊNG LẺ (BACKEND) ===
        // =================================================================

        private async Task<IActionResult> ProcessCOD(Order order, List<CartItem> cartItems)
        {
            order.Status = "Processing"; 
            _context.CartItems.RemoveRange(cartItems); 
            await _context.SaveChangesAsync();
            return RedirectToAction("OrderConfirmation", new { id = order.OrderId });
        }

        // Sửa lại: Hàm này trả về IActionResult (Redirect)
        private IActionResult ProcessVnPay(Order order)
        {
            var request = _httpContextAccessor.HttpContext.Request;
            string vnp_Returnurl = $"{request.Scheme}://{request.Host}/Checkout/PaymentCallbackVnPay";
            string vnp_IpAddr = _httpContextAccessor.HttpContext.Connection.RemoteIpAddress.ToString();
            
            // Hàm CreateVnPayPaymentUrl (mục 7) sẽ tạo URL Sandbox
            string paymentUrl = CreateVnPayPaymentUrl(order, vnp_Returnurl, vnp_IpAddr); 
            return Redirect(paymentUrl);
        }
        
        // =================================================================
        // === CÁC ACTION NHẬN CALLBACK TỪ API ===
        // =================================================================
        
        [HttpGet]
        public async Task<IActionResult> PaymentCallbackVnPay()
        {
            bool isSuccess = Request.Query["vnp_ResponseCode"] == "00"; 
            int orderId = Convert.ToInt32(Request.Query["vnp_TxnRef"]); 
            
            if (isSuccess) // (Nên kiểm tra chữ ký (Hash) ở đây)
            {
                var order = await _context.Orders.FindAsync(orderId);
                if (order != null && order.Status == "Pending")
                {
                    order.Status = "Processing"; 
                    var cartItems = await _context.CartItems.Where(c => c.CustomerID == order.CustomerID).ToListAsync();
                    _context.CartItems.RemoveRange(cartItems);
                    await _context.SaveChangesAsync();
                }
                return RedirectToAction("OrderConfirmation", new { id = orderId });
            }
            else
            {
                // (Nên xóa đơn hàng "Pending" hoặc đánh dấu là "Failed")
                return RedirectToAction("PaymentFailed");
            }
        }

        // =================================================================
        // === CÁC TRANG KẾT QUẢ (Cần tạo View cho chúng) ===
        // =================================================================
        public IActionResult OrderConfirmation(int id)
        {
            ViewBag.OrderId = id;
            return View(); // Tạo View: /Views/Checkout/OrderConfirmation.cshtml
        }
        public IActionResult PaymentFailed()
        {
            return View(); // Tạo View: /Views/Checkout/PaymentFailed.cshtml
        }

        // =================================================================
        // === HÀM HELPER TẠO URL VNPAY (SANDBOX) ===
        // =================================================================
        private string CreateVnPayPaymentUrl(Order order, string vnp_Returnurl, string vnp_IpAddr)
        {
            // === THAY BẰNG KEY SANDBOX CỦA BẠN ===
            // (Bạn phải đăng ký tài khoản Merchant Sandbox với VnPay)
            string vnp_TmnCode = "YOUR_TMN_CODE"; 
            string vnp_HashSecret = "YOUR_HASH_SECRET";
            string vnp_Url = "https://sandbox.vnpayment.vn/paymentv2/vpcpay.html";
            // ======================================

            SortedList<string, string> requestData = new SortedList<string, string>(StringComparer.Ordinal);
            requestData.Add("vnp_Version", "2.1.0");
            requestData.Add("vnp_Command", "pay");
            requestData.Add("vnp_TmnCode", vnp_TmnCode);
            requestData.Add("vnp_Amount", (order.TotalAmount * 100).ToString()); // VnPay dùng xu (x100)
            requestData.Add("vnp_CreateDate", order.OrderDate.Value.ToString("yyyyMMddHHmmss"));
            var expireDate = order.OrderDate.Value.AddMinutes(2).AddSeconds(30); 
            requestData.Add("vnp_ExpireDate", expireDate.ToString("yyyyMMddHHmmss")); 
            requestData.Add("vnp_CurrCode", "VND");
            requestData.Add("vnp_IpAddr", vnp_IpAddr);
            requestData.Add("vnp_Locale", "vn");
            requestData.Add("vnp_OrderInfo", $"Thanh toan don hang #{order.OrderId}");
            requestData.Add("vnp_OrderType", "other"); 
            requestData.Add("vnp_ReturnUrl", vnp_Returnurl);
            requestData.Add("vnp_TxnRef", order.OrderId.ToString()); 

            string queryString = VnPayLibrary.GetRequestDataQueryString(requestData);
            string vnp_SecureHash = VnPayLibrary.HmacSHA512(vnp_HashSecret, queryString);
            string paymentUrl = vnp_Url + "?" + queryString + "&vnp_SecureHash=" + vnp_SecureHash;

            return paymentUrl;
        }
    }
}
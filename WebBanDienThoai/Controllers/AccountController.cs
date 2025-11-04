using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Security.Claims;
using System.Threading.Tasks;
using WebBanDienThoai.Models; // Đảm bảo using Models
using WebBanDienThoai.ViewModels; // Đảm bảo using ViewModels

namespace WebBanDienThoai.Controllers
{
    public class AccountController : Controller
    {
        // Sửa: Dùng DbContext chính của bạn (khớp với file SQL)
        private readonly DemoWebBanDienThoaiContext _context;

        public AccountController(DemoWebBanDienThoaiContext context)
        {
            _context = context;
        }

        [HttpGet]
        public IActionResult Login()
        {
            if (User.Identity != null && User.Identity.IsAuthenticated)
                return RedirectToAction("Index", "Home");
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Login(LoginViewModel model)
        {
            if (!ModelState.IsValid)
                return View(model);

            // Sửa lỗi 'bool?': Thêm '== true'
            var account = await _context.Accounts
                .FirstOrDefaultAsync(a => a.Email == model.EmailOrPhone && a.IsActive == true);

            // Sửa: Dùng BCrypt (khớp với lỗi SaltParseException của bạn)
            if (account != null && BCrypt.Net.BCrypt.Verify(model.Password, account.Password))
            {
                var claims = new List<Claim>
                {
                    new Claim(ClaimTypes.Name, account.Email),
                    // Sửa: Khớp CSDL (chữ hoa)
                    new Claim("AccountID", account.AccountID.ToString()),
                    new Claim(ClaimTypes.Role, account.Role)
                };

                await SignInUserAsync(claims, model.RememberMe);

                // ==================================================
                // === SỬA LOGIC CHUYỂN HƯỚNG THEO YÊU CẦU CỦA BẠN ===
                // ==================================================

                // 1. Kiểm tra Role của tài khoản
                if (account.Role == "Admin")
                {
                    // 2. Nếu là Admin, chuyển hướng đến trang Admin
                    return RedirectToAction("Index", "Admin");
                }
                else
                {
                    // 3. Nếu là Customer, chuyển hướng đến trang Home
                    return RedirectToAction("Index", "Home");
                }
                // ==================================================
            }

            ModelState.AddModelError(string.Empty, "Email hoặc mật khẩu không chính xác.");
            return View(model);
        }

        [HttpGet]
        public IActionResult Register()
        {
            if (User.Identity != null && User.Identity.IsAuthenticated)
                return RedirectToAction("Index", "Home");
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Register(RegisterViewModel model)
        {
            if (!ModelState.IsValid)
                return View(model);

            bool emailExists = await _context.Accounts.AnyAsync(a => a.Email == model.Email);
            if (emailExists)
            {
                ModelState.AddModelError("Email", "Email đã được sử dụng.");
                return View(model);
            }

            // Sửa: Dùng BCrypt để mã hóa (khớp với logic Login)
            var hashedPassword = BCrypt.Net.BCrypt.HashPassword(model.Password);

            var newAccount = new Account
            {
                Email = model.Email,
                Password = hashedPassword, // Lưu mật khẩu đã mã hóa
                Role = "Customer",
                IsActive = true,
                CreatedAt = DateTime.Now
            };

            try
            {
                _context.Accounts.Add(newAccount);
                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = "Đăng ký thành công. Vui lòng đăng nhập.";
                return RedirectToAction("Login");
            }
            catch (Exception ex)
            {
                // Thêm log lỗi để dễ gỡ rối
                Console.WriteLine(ex.ToString());
                ModelState.AddModelError(string.Empty, "Đã xảy ra lỗi. Vui lòng thử lại.");
                return View(model);
            }
        }

        [Authorize]
        [HttpGet]
        public async Task<IActionResult> Profile()
        {
            var accountIdString = User.FindFirstValue("AccountID");
            if (string.IsNullOrEmpty(accountIdString))
            {
                return Unauthorized("Không tìm thấy thông tin tài khoản.");
            }
            var accountId = int.Parse(accountIdString);

            var account = await _context.Accounts.FindAsync(accountId);
            if (account == null) return Unauthorized();

            // Sửa: Khớp CSDL (chữ hoa)
            var customer = await _context.Customers.FirstOrDefaultAsync(c => c.AccountID == accountId);

            if (customer == null)
            {
                customer = new Customer
                {
                    // Sửa: Khớp CSDL (chữ hoa)
                    AccountID = accountId,
                    FullName = account.Email.Split('@')[0],
                    Gender = "Khác",
                    CustomerType = "Thường"
                };
                _context.Customers.Add(customer);
                await _context.SaveChangesAsync();
            }

            var viewModel = new ProfileViewModel
            {
                Email = account.Email,
                FullName = customer.FullName,
                Phone = customer.Phone,
                Gender = customer.Gender,
                BirthDate = customer.BirthDate // Giả định đây là DateTime?
            };

            return View(viewModel);
        }

        [Authorize]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Profile(ProfileViewModel model)
        {
            if (!ModelState.IsValid)
            {
                model.Email = User.Identity?.Name ?? "";
                return View(model);
            }

            var accountIdString = User.FindFirstValue("AccountID");
            var accountId = int.Parse(accountIdString!);

            // Sửa: Khớp CSDL (chữ hoa)
            var customerToUpdate = await _context.Customers.FirstOrDefaultAsync(c => c.AccountID == accountId);
            if (customerToUpdate == null)
            {
                return NotFound("Không tìm thấy hồ sơ khách hàng.");
            }

            customerToUpdate.FullName = model.FullName;
            customerToUpdate.Phone = model.Phone;
            customerToUpdate.Gender = model.Gender;
            customerToUpdate.BirthDate = model.BirthDate; // Giả định đây là DateTime?

            try
            {
                _context.Customers.Update(customerToUpdate);
                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = "Cập nhật thông tin thành công!";
            }
            catch (DbUpdateException ex)
            {
                // Thêm log lỗi
                Console.WriteLine(ex.ToString());
                ModelState.AddModelError("", "Không thể lưu thay đổi. Vui lòng thử lại.");
            }

            model.Email = User.Identity?.Name ?? "";
            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Logout()
        {
            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            // Sửa: Chuyển hướng về trang Login
            return RedirectToAction("Login", "Account");
        }

        private async Task SignInUserAsync(List<Claim> claims, bool isPersistent)
        {
            var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
            var principal = new ClaimsPrincipal(identity);
            var authProps = new AuthenticationProperties
            {
                IsPersistent = isPersistent,
                ExpiresUtc = isPersistent ? DateTimeOffset.UtcNow.AddDays(30) : DateTimeOffset.UtcNow.AddHours(8)
            };
            await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, principal, authProps);
        }
    }
}
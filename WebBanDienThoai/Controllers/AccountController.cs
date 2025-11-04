using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Security.Claims;
using System.Threading.Tasks;
using WebBanDienThoai.Data;
using WebBanDienThoai.Models;
using WebBanDienThoai.ViewModels;

namespace WebBanDienThoai.Controllers
{
    public class AccountController : Controller
    {
        private readonly AppDbContext _context;

        public AccountController(AppDbContext context)
        {
            _context = context;
        }

        [HttpGet]
        public IActionResult Login()
        {
            if (User.Identity.IsAuthenticated)
                return RedirectToAction("Index", "Home");

            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Login(LoginViewModel model)
        {
            if (!ModelState.IsValid)
                return View(model);

            var account = await _context.Accounts
                .FirstOrDefaultAsync(a => a.Email == model.EmailOrPhone && a.IsActive);

            if (account != null && BCrypt.Net.BCrypt.Verify(model.Password, account.Password))
            {
                var claims = new List<Claim>
                {
                    new Claim(ClaimTypes.Name, account.Email),
                    new Claim("AccountID", account.AccountID.ToString()),
                    new Claim(ClaimTypes.Role, account.Role)
                };

                await SignInUserAsync(claims, model.RememberMe);
                return RedirectToAction("Index", "Home");
            }

            ModelState.AddModelError(string.Empty, "Email hoặc mật khẩu không chính xác.");
            return View(model);
        }

       
        [HttpGet]
        public IActionResult Register()
        {
            if (User.Identity.IsAuthenticated)
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

            var hashedPassword = BCrypt.Net.BCrypt.HashPassword(model.Password);

            var newAccount = new Account
            {
                Email = model.Email,
                Password = hashedPassword,
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
            catch (Exception)
            {
                ModelState.AddModelError(string.Empty, "Đã xảy ra lỗi. Vui lòng thử lại.");
                return View(model);
            }
        }

        // GET: /Account/Profile
        [Authorize] // <-- Bắt buộc người dùng phải đăng nhập
        [HttpGet]
        public async Task<IActionResult> Profile()
        {
            // 1. Lấy AccountID từ Claim (đã lưu khi đăng nhập)
            var accountIdString = User.FindFirstValue("AccountID");
            if (string.IsNullOrEmpty(accountIdString))
            {
                return Unauthorized("Không tìm thấy thông tin tài khoản.");
            }
            var accountId = int.Parse(accountIdString);

            // 2. Tìm tài khoản (để lấy Email)
            var account = await _context.Accounts.FindAsync(accountId);
            if (account == null) return Unauthorized();

            // 3. Tìm thông tin Customer
            var customer = await _context.Customers.FirstOrDefaultAsync(c => c.AccountID == accountId);

            // 4. [LOGIC QUAN TRỌNG] Nếu Customer CHƯA tồn tại, tạo mới
            if (customer == null)
            {
                customer = new Customer
                {
                    AccountID = accountId,
                    FullName = account.Email.Split('@')[0], // Tạm lấy tên email làm tên
                    Gender = "Khác",
                    CustomerType = "Thường"
                    // Phone và BirthDate sẽ là NULL
                };
                _context.Customers.Add(customer);
                await _context.SaveChangesAsync();
            }

            // 5. Tạo ViewModel để gửi ra View
            var viewModel = new ProfileViewModel
            {
                Email = account.Email,
                FullName = customer.FullName,
                Phone = customer.Phone,
                Gender = customer.Gender,
                BirthDate = customer.BirthDate
            };

            return View(viewModel);
        }

        // POST: /Account/Profile
        [Authorize]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Profile(ProfileViewModel model)
        {
            if (!ModelState.IsValid)
            {
                // Gán lại Email vì nó không được post về
                model.Email = User.Identity.Name;
                return View(model);
            }

            // 1. Lấy AccountID từ Claim
            var accountIdString = User.FindFirstValue("AccountID");
            var accountId = int.Parse(accountIdString);

            // 2. Tìm Customer để cập nhật
            var customerToUpdate = await _context.Customers.FirstOrDefaultAsync(c => c.AccountID == accountId);
            if (customerToUpdate == null)
            {
                return NotFound("Không tìm thấy hồ sơ khách hàng.");
            }

            // 3. Cập nhật thông tin từ ViewModel vào Model
            customerToUpdate.FullName = model.FullName;
            customerToUpdate.Phone = model.Phone;
            customerToUpdate.Gender = model.Gender;
            customerToUpdate.BirthDate = model.BirthDate;

            // 4. Lưu thay đổi
            try
            {
                _context.Customers.Update(customerToUpdate);
                await _context.SaveChangesAsync();

                TempData["SuccessMessage"] = "Cập nhật thông tin thành công!";
            }
            catch (DbUpdateException ex)
            {
                // (Log lỗi ex)
                ModelState.AddModelError("", "Không thể lưu thay đổi. Vui lòng thử lại.");
            }

            // Gán lại Email để hiển thị
            model.Email = User.Identity.Name;
            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Logout()
        {
            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            return RedirectToAction("Index", "Home");
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

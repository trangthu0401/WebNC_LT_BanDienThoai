using BCrypt.Net;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using WebBanDienThoai.Data;
using WebBanDienThoai.Models;
using WebBanDienThoai.Models.ViewModels;
using WebBanDienThoai.ViewModels;

namespace WebBanDienThoai.Controllers
{
    public class AccountController : Controller
    {
        private readonly DemoWebBanDienThoaiDbContext _context;
        private readonly EmailSender _emailSender;

        public AccountController(DemoWebBanDienThoaiDbContext context, EmailSender emailSender)
        {
            _context = context;
            _emailSender = emailSender;
        }

        [HttpGet]
        public IActionResult Login()
        {
            if (User.Identity?.IsAuthenticated == true)
            {
                return RedirectToAction("Index", "Home");
            }
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Login(LoginViewModel model)
        {
            if (!ModelState.IsValid) return View(model);
            var account = await _context.Accounts
                .Include(a => a.Customer)
                .FirstOrDefaultAsync(a =>
                    (a.Email == model.EmailOrPhone || (a.Customer != null && a.Customer.Phone == model.EmailOrPhone))
                    && a.IsActive == true);

            if (account != null && BCrypt.Net.BCrypt.Verify(model.Password, account.Password))
            {
                var claims = new List<Claim>
                {
                    new Claim(ClaimTypes.Name, account.Email),
                    new Claim(ClaimTypes.Role, account.Role),
                };

                if (account.Customer != null)
                {
                    claims.Add(new Claim(ClaimTypes.NameIdentifier, account.Customer.CustomerID.ToString()));
                    claims.Add(new Claim("FullName", account.Customer.FullName));
                    claims.Add(new Claim("AccountID", account.AccountID.ToString()));
                }
                else
                {
                    claims.Add(new Claim(ClaimTypes.NameIdentifier, account.AccountID.ToString()));
                    claims.Add(new Claim("FullName", "Quản trị viên"));
                    claims.Add(new Claim("AccountID", account.AccountID.ToString()));
                }

                await SignInUserAsync(claims, model.RememberMe);

                if (account.Role == "Admin" && model.LoginAsAdmin)
                    return RedirectToAction("Index", "Dashboard");

                return RedirectToAction("Index", "Home");
            }

            ModelState.AddModelError(string.Empty, "Sai thông tin đăng nhập hoặc tài khoản bị khóa.");
            return View(model);
        }


        [HttpGet]
        public IActionResult Register()
        {
            if (User.Identity?.IsAuthenticated == true)
                return RedirectToAction("Index", "Home");
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Register(RegisterViewModel model)
        {
            if (!ModelState.IsValid) return View(model);

            if (await _context.Accounts.AnyAsync(a => a.Email == model.Email))
            {
                ModelState.AddModelError("Email", "Email đã tồn tại.");
                return View(model);
            }
            if (await _context.Customers.AnyAsync(c => c.Phone == model.Phone))
            {
                ModelState.AddModelError("Phone", "SĐT đã được sử dụng.");
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

                var newCustomer = new Customer
                {
                    AccountID = newAccount.AccountID,
                    FullName = model.FullName,
                    Phone = model.Phone,
                    Gender = model.Gender,
                    BirthDate = model.BirthDate,
                    CustomerType = "Thường"
                };
                _context.Customers.Add(newCustomer);
                await _context.SaveChangesAsync();

                TempData["SuccessMessage"] = "Đăng ký thành công. Vui lòng đăng nhập.";
                return RedirectToAction("Login");
            }
            catch (Exception ex)
            {
                ModelState.AddModelError("", "Lỗi hệ thống: " + ex.Message);
                return View(model);
            }
        }

        [HttpGet]
        public IActionResult ForgotPassword()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ForgotPassword(ForgotPasswordViewModel model)
        {
            if (!ModelState.IsValid) return View(model);

            var account = await _context.Accounts.FirstOrDefaultAsync(a => a.Email == model.Email);

            if (account == null)
            {

                ViewBag.Message = "Nếu email tồn tại, chúng tôi đã gửi hướng dẫn đặt lại mật khẩu.";
                return View();
            }


            string token = Guid.NewGuid().ToString();


            var callbackUrl = Url.Action("ResetPassword", "Account",
                new { email = model.Email, token = token }, protocol: HttpContext.Request.Scheme);

            string emailBody = $@"
                <div style='font-family: Arial, sans-serif; padding: 20px;'>
                    <h2>Rabit Store - Yêu cầu đặt lại mật khẩu</h2>
                    <p>Xin chào,</p>
                    <p>Bạn nhận được email này vì hệ thống nhận được yêu cầu lấy lại mật khẩu cho tài khoản: <strong>{model.Email}</strong></p>
                    <p>Vui lòng nhấn vào nút bên dưới để tạo mật khẩu mới:</p>
                    <a href='{callbackUrl}' style='display: inline-block; padding: 10px 20px; background-color: #0d6efd; color: #fff; text-decoration: none; border-radius: 5px; font-weight: bold;'>Đặt lại mật khẩu ngay</a>
                    <p style='margin-top: 20px; color: #666;'>Nếu bạn không yêu cầu điều này, vui lòng bỏ qua email.</p>
                </div>";

            try
            {

                await _emailSender.SendEmailAsync(model.Email, "Rabit Store - Quên mật khẩu", emailBody);

                ViewBag.Message = "Đã gửi email hướng dẫn. Vui lòng kiểm tra hộp thư (bao gồm cả mục Spam).";
            }
            catch (Exception ex)
            {

                ModelState.AddModelError("", "Không thể gửi email do lỗi máy chủ. Vui lòng thử lại sau.");
                return View(model);
            }

            return View();
        }


        [HttpGet]
        public IActionResult ResetPassword(string email, string token)
        {
            if (string.IsNullOrEmpty(email) || string.IsNullOrEmpty(token))
            {
                return RedirectToAction("Login");
            }
            var model = new ResetPasswordViewModel { Email = email, Token = token };
            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ResetPassword(ResetPasswordViewModel model)
        {
            if (!ModelState.IsValid) return View(model);

            var account = await _context.Accounts.FirstOrDefaultAsync(a => a.Email == model.Email);

            if (account != null)
            {

                account.Password = BCrypt.Net.BCrypt.HashPassword(model.NewPassword);

                _context.Accounts.Update(account);
                await _context.SaveChangesAsync();

                TempData["SuccessMessage"] = "Đổi mật khẩu thành công. Vui lòng đăng nhập lại.";
                return RedirectToAction("Login");
            }

            ModelState.AddModelError("", "Đã xảy ra lỗi. Không tìm thấy tài khoản.");
            return View(model);
        }


        [Authorize]
        [HttpGet]
        [Route("/Account/Profile")]
        public async Task<IActionResult> Profile()
        {
            try
            {
                var accountId = int.Parse(User.FindFirstValue("AccountID"));
                var account = await _context.Accounts
                    .Include(a => a.Customer)
                    .FirstOrDefaultAsync(a => a.AccountID == accountId);

                if (account == null || account.Customer == null) return NotFound();

                var model = new AccountViewModels
                {
                    Email = account.Email,
                    FullName = account.Customer.FullName,
                    Phone = account.Customer.Phone,
                    Gender = account.Customer.Gender,
                    BirthDate = account.Customer.BirthDate
                };
                return View(model);
            }
            catch { return RedirectToAction("Error", "Home"); }
        }

        [Authorize]
        [HttpPost]
        [Route("/Account/Profile")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Profile(AccountViewModels model)
        {
            try
            {
                if (!ModelState.IsValid) return View(model);

                var accountId = int.Parse(User.FindFirstValue("AccountID"));
                var account = await _context.Accounts
                    .Include(a => a.Customer)
                    .FirstOrDefaultAsync(a => a.AccountID == accountId);

                if (account == null || account.Customer == null) return NotFound();


                if (!string.IsNullOrEmpty(model.Phone) &&
                    await _context.Customers.AnyAsync(c => c.Phone == model.Phone && c.CustomerID != account.Customer.CustomerID))
                {
                    ModelState.AddModelError("Phone", "Số điện thoại đã được sử dụng bởi tài khoản khác.");
                    return View(model);
                }


                account.Customer.FullName = model.FullName;
                account.Customer.Phone = model.Phone;
                account.Customer.Gender = model.Gender;
                account.Customer.BirthDate = model.BirthDate;

                _context.Customers.Update(account.Customer);
                await _context.SaveChangesAsync();

                // Cập nhật lại Cookie (để hiện tên mới ngay lập tức)
                if (User.FindFirstValue("FullName") != model.FullName)
                {
                    await UpdateFullNameClaim(model.FullName);
                }

                TempData["SuccessMessage"] = "Cập nhật thông tin thành công!";
                return RedirectToAction("Profile");
            }
            catch (Exception ex)
            {
                ModelState.AddModelError("", "Lỗi hệ thống: " + ex.Message);
                return View(model);
            }
        }


        public async Task<IActionResult> Logout()
        {
            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            return RedirectToAction("Index", "Home");
        }


        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ToggleActive(int accountId, int customerId)
        {
            var account = await _context.Accounts.FindAsync(accountId);
            if (account == null) return NotFound();

            account.IsActive = !account.IsActive;

            try
            {
                _context.Accounts.Update(account);
                await _context.SaveChangesAsync();
                string statusMessage = account.IsActive ? "mở khóa" : "khóa";
                TempData["SuccessMessage"] = $"Đã {statusMessage} tài khoản '{account.Email}' thành công.";
            }
            catch
            {
                TempData["ErrorMessage"] = "Không thể cập nhật trạng thái tài khoản.";
            }

            return RedirectToAction("Details", "Customer", new { id = customerId });
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


        private async Task UpdateFullNameClaim(string fullName)
        {
            var identity = (ClaimsIdentity)User.Identity;
            var existingClaim = identity.FindFirst("FullName");
            if (existingClaim != null) identity.RemoveClaim(existingClaim);
            identity.AddClaim(new Claim("FullName", fullName));

            var principal = new ClaimsPrincipal(identity);
            await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, principal);
        }
    }
}
// Import các thư viện cần thiết
using Microsoft.AspNetCore.Identity; // (Vẫn cần cho IdentityUser/Role nếu AppDbContext dùng)
using Microsoft.EntityFrameworkCore;
using WebBanDienThoai.Models;
using WebBanDienThoai.Data;
using Microsoft.AspNetCore.Authentication.Cookies; // Quan trọng
using Microsoft.Extensions.Logging;
using System.Linq;

var builder = WebApplication.CreateBuilder(args);

// --- 1. Lấy chuỗi kết nối ---
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");

// --- 2. Đăng ký Dịch vụ (Services) ---

// DbContext chính cho Sản phẩm, Account, Customer...
builder.Services.AddDbContext<DemoWebBanDienThoaiContext>(options =>
    options.UseSqlServer(connectionString));

// DbContext cho Identity (có thể không cần thiết nếu AccountController không dùng)
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlServer(connectionString));


// ==========================================================
// === SỬA LỖI CHUYỂN HƯỚNG TẠI ĐÂY ===
// ==========================================================

// 1. Đăng ký Authentication VÀ ĐẶT "Cookies" LÀM MẶC ĐỊNH
//    Bằng cách chỉ gọi AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(CookieAuthenticationDefaults.AuthenticationScheme, options =>
    {
        options.LoginPath = "/Account/Login";
        options.AccessDeniedPath = "/Home/AccessDenied";
        options.ExpireTimeSpan = TimeSpan.FromDays(30);
        options.SlidingExpiration = true;
    });

// 2. Đăng ký Authorization (Nó sẽ tự động dùng Scheme mặc định "Cookies" ở trên)
builder.Services.AddAuthorization();

// 3. XÓA BỎ HỆ THỐNG IDENTITY (Vì AccountController của bạn không dùng)
/*
builder.Services.AddIdentity<IdentityUser, IdentityRole>()
    .AddEntityFrameworkStores<AppDbContext>()
    .AddDefaultTokenProviders();
*/
// ==========================================================


builder.Services.AddControllersWithViews();


// --- 3. Xây dựng ứng dụng ---
var app = builder.Build();

// --- 4. Cấu hình HTTP request pipeline (Middleware) ---

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

// Phải có 2 dòng này và ĐÚNG THỨ TỰ (Auth trước Authorize)
app.UseAuthentication();
app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Dashboard}/{action=Index}/{id?}");


// (Code tự tạo Admin vẫn giữ nguyên)
using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    var logger = services.GetRequiredService<ILogger<Program>>();

    try
    {
        var context = services.GetRequiredService<DemoWebBanDienThoaiContext>();

        string adminEmail = "admin@gmail.com";
        string adminPassword = "admin123";
        string adminRole = "Admin";

        var existingAdmin = context.Accounts.FirstOrDefault(a => a.Email == adminEmail);

        if (existingAdmin == null)
        {
            logger.LogInformation("Không tìm thấy tài khoản Admin. Đang tạo...");

            string hashedPassword = BCrypt.Net.BCrypt.HashPassword(adminPassword);

            var adminAccount = new Account
            {
                Email = adminEmail,
                Password = hashedPassword,
                Role = adminRole,
                IsActive = true,
                CreatedAt = DateTime.Now
            };

            context.Accounts.Add(adminAccount);
            context.SaveChanges();

            logger.LogInformation("Tài khoản Admin (admin@gmail.com) đã được tạo thành công.");
        }
        else
        {
            if (!existingAdmin.Password.StartsWith("$2a$"))
            {
                logger.LogWarning("Phát hiện mật khẩu Admin dạng thuần. Đang cập nhật...");
                existingAdmin.Password = BCrypt.Net.BCrypt.HashPassword(adminPassword);
                context.SaveChanges();
                logger.LogInformation("Đã cập nhật mật khẩu Admin sang dạng mã hóa.");
            }
            else
            {
                logger.LogInformation("Tài khoản Admin (admin@gmail.com) đã tồn tại. Bỏ qua.");
            }
        }
    }
    catch (Exception ex)
    {
        logger.LogError(ex, "Lỗi xảy ra khi tạo tài khoản Admin.");
    }
}

// --- 5. Chạy ứng dụng ---
app.Run();
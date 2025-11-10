using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.EntityFrameworkCore;
using WebBanDienThoai.Models; // (Models của bạn)

var builder = WebApplication.CreateBuilder(args);

// --- 1. LẤY CHUỖI KẾT NỐI ---
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");

// --- 2. ĐĂNG KÝ DỊCH VỤ (SERVICES) ---

// Đăng ký AppDbContext với SQL Server
// (Sửa lỗi "Unable to resolve service for type 'AppDbContext'")
builder.Services.AddDbContext<DemoWebBanDienThoai1Context>(options =>
    options.UseSqlServer(connectionString));

// Đăng ký dịch vụ cho Controllers và Views
builder.Services.AddControllersWithViews();

// Đăng ký dịch vụ Authentication (xác thực) bằng Cookie
builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.LoginPath = "/Account/Login";
        options.AccessDeniedPath = "/Home/AccessDenied";
        options.ExpireTimeSpan = TimeSpan.FromDays(30);
        options.SlidingExpiration = true;
    });

// ĐĂNG KÝ CÁC DỊCH VỤ CHO CHECKOUT
builder.Services.AddHttpContextAccessor(); // (Sửa lỗi "IHttpContextAccessor")
builder.Services.AddSession(options => // (Cần cho TempData)
{
    options.IdleTimeout = TimeSpan.FromMinutes(30);
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
});


// --- 3. XÂY DỰNG ỨNG DỤNG (APP) ---
var app = builder.Build();

// --- 4. CẤU HÌNH HTTP REQUEST PIPELINE (MIDDLEWARE) ---

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles(); // Cho phép dùng file CSS, JS, Images...

app.UseRouting(); // Bật tính năng Định tuyến (Routing)

// KÍCH HOẠT SESSION (PHẢI NẰM TRƯỚC Authentication)
app.UseSession();

// KÍCH HOẠT XÁC THỰC VÀ PHÂN QUYỀN
app.UseAuthentication(); // <-- Quan trọng: Xác thực
app.UseAuthorization(); // <-- Quan trọng: Phân quyền

// Cấu hình định tuyến mặc định
app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.EntityFrameworkCore; // <-- Thêm EF Core
using WebBanDienThoai.Data; // <-- Thêm namespace ch?a AppDbContext

var builder = WebApplication.CreateBuilder(args);

// --- 1. L?y chu?i k?t n?i ---
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");

// --- 2. ??ng kư d?ch v? (Services) ---

// ??ng kư AppDbContext v?i SQL Server
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlServer(connectionString));

// ??ng kư d?ch v? cho Controllers và Views
builder.Services.AddControllersWithViews();

// ??ng k� d?ch v? Authentication (x�c th?c) b?ng Cookie
builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.LoginPath = "/Account/Login"; // ???ng d?n ??n trang ??ng nh?p
        options.AccessDeniedPath = "/Home/AccessDenied"; // (T�y ch?n) Trang t? ch?i
        options.ExpireTimeSpan = TimeSpan.FromDays(30);
        options.SlidingExpiration = true;
    });


// --- 3. X�y d?ng ?ng d?ng (app) ---
var app = builder.Build();

// --- 4. C?u h́nh HTTP request pipeline (Middleware) ---

// C?u h́nh cho môi tr??ng development
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles(); // Cho ph�p d�ng file CSS, JS, Images...

app.UseRouting(); // B?t t�nh n?ng ??nh tuy?n (Routing)

// KÍCH HO?T XÁC TH?C VÀ PHÂN QUY?N
// (Ph?i n?m gi?a UseRouting và MapControllerRoute)
app.UseAuthentication(); // <-- Quan tr?ng: Xác th?c
app.UseAuthorization(); // <-- Quan tr?ng: Phân quy?n

// C?u h́nh ??nh tuy?n m?c ??nh
app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();